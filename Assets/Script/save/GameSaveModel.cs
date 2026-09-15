using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Defines the files belonging to the current playthrough.
/// Scene transitions and UI are coordinated by MainSceneManager.
/// </summary>
public sealed class GameSaveModel
{
    public const string PlayerFileName = "player_save.json";
    private readonly string directory;
    private readonly string inventoryFileName;

    public GameSaveModel(string directory, string inventoryFileName)
    {
        if (string.IsNullOrWhiteSpace(inventoryFileName) ||
            Path.GetFileName(inventoryFileName) != inventoryFileName ||
            inventoryFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Save filename must be a filename without directories.");

        this.directory = Path.GetFullPath(directory);
        this.inventoryFileName = inventoryFileName;
    }

    public bool HasPlayerSave()
    {
        return HasContent(PlayerFileName) || HasContent(inventoryFileName);
    }

    private bool HasContent(string fileName)
    {
        string path = Path.Combine(directory, fileName);
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }

    public void DeleteAllProgress()
    {
        if (!Directory.Exists(directory))
            return;

        // Only known progress files in this directory; never settings or subdirectories.
        List<string> paths = new List<string>();
        foreach (string path in Directory.GetFiles(directory))
        {
            if (IsProgressFile(Path.GetFileName(path)))
                paths.Add(path);
        }

        foreach (string path in paths)
            File.Delete(path);
    }

    private bool IsProgressFile(string fileName)
    {
        if (fileName.EndsWith(".tmp", StringComparison.Ordinal))
            fileName = fileName.Substring(0, fileName.Length - 4);

        return fileName == inventoryFileName || fileName == PlayerFileName ||
            fileName == "quest_save.json" || fileName == "chest_save.json" ||
            IsWorldFile(fileName, "chunk_modifications_") ||
            IsWorldFile(fileName, "placed_blocks_");
    }

    private static bool IsWorldFile(string fileName, string prefix)
    {
        const string extension = ".json";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(extension, StringComparison.Ordinal))
            return false;

        string seed = fileName.Substring(prefix.Length,
            fileName.Length - prefix.Length - extension.Length);
        return int.TryParse(seed, out _);
    }
}
