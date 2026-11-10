using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// A redo journal keeps inventory and progress files from different save moments
// from being loaded together after an interrupted write. Existing JSON schemas stay intact.
public sealed class SaveCheckpointModel
{
    public const string JournalFileName = "save_checkpoint.json";
    private readonly Dictionary<string, string> pending = new Dictionary<string, string>();
    public bool HasPending => pending.Count > 0;

    [Serializable] private sealed class Entry { public string fileName; public string json; }
    [Serializable] private sealed class Journal { public List<Entry> entries = new List<Entry>(); }

    public void Stage(string path, string json) { pending[Path.GetFullPath(path)] = json; }
    public void Clear() { pending.Clear(); }

    public void Commit(string directory)
    {
        if (!HasPending) return;
        string root = Path.GetFullPath(directory);
        var journal = new Journal();
        foreach (var item in pending)
        {
            if (!string.Equals(Path.GetDirectoryName(item.Key), root, StringComparison.OrdinalIgnoreCase))
                throw new IOException("A checkpoint cannot write outside the save directory.");
            journal.entries.Add(new Entry { fileName = Path.GetFileName(item.Key), json = item.Value });
        }
        string journalPath = Path.Combine(root, JournalFileName);
        // Once this is durable, recovery can finish every file even if the process stops.
        WriteAtomic(journalPath, JsonUtility.ToJson(journal));
        Apply(root, journal);
        File.Delete(journalPath);
        pending.Clear();
    }

    public static void Recover(string directory)
    {
        string path = Path.Combine(directory, JournalFileName);
        if (!File.Exists(path)) return;
        Journal journal = JsonUtility.FromJson<Journal>(File.ReadAllText(path));
        if (journal == null || journal.entries == null) throw new InvalidDataException("Invalid save checkpoint.");
        Apply(directory, journal);
        File.Delete(path);
    }

    private static void Apply(string directory, Journal journal)
    {
        // Validate every entry before touching any destination.
        foreach (Entry entry in journal.entries)
            if (entry == null || string.IsNullOrWhiteSpace(entry.fileName) ||
                Path.GetFileName(entry.fileName) != entry.fileName ||
                entry.fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                entry.fileName == JournalFileName || entry.json == null)
                throw new InvalidDataException("Invalid checkpoint entry.");
        foreach (Entry entry in journal.entries)
            WriteAtomic(Path.Combine(directory, entry.fileName), entry.json);
    }

    public static void WriteAtomic(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        string temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }
}
