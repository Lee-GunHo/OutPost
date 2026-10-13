using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

/// <summary>
/// .xlsx의 첫 번째 시트를 "헤더 → 값" 딕셔너리 목록으로 읽음.
/// xlsx는 zip 안의 XML이라 외부 라이브러리 없이 읽을 수 있음.
/// 엑셀에서 파일을 열어둔 상태여도 읽을 수 있도록 FileShare.ReadWrite로 연다.
/// </summary>
public static class XlsxSheetReader
{
    public static List<Dictionary<string, string>> ReadFirstSheet(string path)
    {
        using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read);

        List<string> sharedStrings = ReadSharedStrings(zip);
        XmlDocument sheet = LoadXml(zip, "xl/worksheets/sheet1.xml");

        if (sheet == null)
            throw new IOException("시트를 찾을 수 없습니다: xl/worksheets/sheet1.xml");

        Dictionary<string, string> headerByColumn = null;
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        foreach (XmlElement row in sheet.GetElementsByTagName("row"))
        {
            Dictionary<string, string> cellsByColumn = new Dictionary<string, string>();

            foreach (XmlNode node in row.ChildNodes)
            {
                if (node is not XmlElement cell || cell.LocalName != "c")
                    continue;

                string value = ReadCellValue(cell, sharedStrings);

                if (!string.IsNullOrWhiteSpace(value))
                    cellsByColumn[ColumnLetters(cell.GetAttribute("r"))] = value.Trim();
            }

            // 첫 번째 행은 헤더
            if (headerByColumn == null)
            {
                headerByColumn = cellsByColumn;
                continue;
            }

            Dictionary<string, string> rowByHeader = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> pair in cellsByColumn)
            {
                if (headerByColumn.TryGetValue(pair.Key, out string header))
                    rowByHeader[Normalize(header)] = pair.Value;
            }

            rows.Add(rowByHeader);
        }

        return rows;
    }

    /// <summary>
    /// 대소문자/공백/기호를 무시하고 비교하기 위한 키. "C.P Soup" → "cpsoup"
    /// </summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        StringBuilder builder = new StringBuilder(text.Length);

        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        List<string> result = new List<string>();
        XmlDocument document = LoadXml(zip, "xl/sharedStrings.xml");

        if (document == null)
            return result;

        foreach (XmlElement item in document.GetElementsByTagName("si"))
            result.Add(item.InnerText);

        return result;
    }

    private static string ReadCellValue(XmlElement cell, List<string> sharedStrings)
    {
        string type = cell.GetAttribute("t");

        if (type == "inlineStr")
            return cell.InnerText;

        XmlNode valueNode = null;

        foreach (XmlNode child in cell.ChildNodes)
        {
            if (child.LocalName == "v")
                valueNode = child;
        }

        if (valueNode == null)
            return null;

        if (type == "s" && int.TryParse(valueNode.InnerText, out int index) &&
            index >= 0 && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return valueNode.InnerText;
    }

    private static XmlDocument LoadXml(ZipArchive zip, string entryName)
    {
        ZipArchiveEntry entry = zip.GetEntry(entryName);

        if (entry == null)
            return null;

        using Stream entryStream = entry.Open();
        XmlDocument document = new XmlDocument();
        document.Load(entryStream);
        return document;
    }

    // "AB12" → "AB"
    private static string ColumnLetters(string cellReference)
    {
        int length = 0;

        while (length < cellReference.Length && char.IsLetter(cellReference[length]))
            length++;

        return cellReference.Substring(0, length);
    }
}
