using System.Collections.Generic;

/// <summary>
/// 대사 템플릿의 {toolGrade}, {deathCount}, {questName} 같은 변수를 치환.
/// </summary>
public static class NPCTipTextFormatter
{
    public static string Format(string template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template) || variables == null)
        {
            return template;
        }

        string result = template;

        foreach (KeyValuePair<string, string> variable in variables)
        {
            result = result.Replace("{" + variable.Key + "}", variable.Value ?? string.Empty);
        }

        return result;
    }
}
