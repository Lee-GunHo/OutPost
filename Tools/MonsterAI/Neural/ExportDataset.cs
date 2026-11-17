using System;
using System.Globalization;
using System.IO;
using System.Text;

// Offline adapter: calls the SAME pure C# teacher/encoder as the game's fallback.
internal static class ExportDataset
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    public static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass output CSV path.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));
        using (var writer = new StreamWriter(args[0], false, new UTF8Encoding(false)))
        {
            writer.WriteLine("split,distance,meleeRange,lowHealth,recentlyHit,x0,x1,x2,x3,melee,projectile,circle,charge");
            WriteSplit(writer, "train", 12000, 5101);
            WriteSplit(writer, "validation", 3000, 5102);
            WriteSplit(writer, "test", 4000, 5103);
        }
        Console.WriteLine("Exported 19000 teacher-labelled observations: " + args[0]);
    }

    private static void WriteSplit(StreamWriter writer, string split, int count, int seed)
    {
        var random = new Random(seed);
        var input = new float[4];
        for (int i = 0; i < count; i++)
        {
            float melee = (float)(random.NextDouble() * MonsterNeuralPolicyModel.MaxMeleeRange);
            float distance = (float)(random.NextDouble() * (random.NextDouble() < 0.5 ? 6 : 12));
            // Extra observations around the three rule discontinuities; held-out splits use other seeds.
            if (i % 5 == 0)
            {
                float boundary = i % 3 == 0 ? 2f : i % 3 == 1 ? 2.3f : melee + 0.5f;
                distance = Math.Max(0, Math.Min(12, boundary + (float)(random.NextDouble() - 0.5) * 0.3f));
            }
            var observation = new MonsterNeuralPolicyModel.Observation
            {
                distance = distance, meleeRange = melee,
                lowHealth = i % 4 >= 2, recentlyHit = i % 2 == 1
            };
            if (!MonsterNeuralPolicyModel.Encode(observation, input)) throw new Exception("Invalid generated input");
            var cells = new string[13];
            cells[0] = split;
            cells[1] = distance.ToString("R", Invariant);
            cells[2] = melee.ToString("R", Invariant);
            cells[3] = observation.lowHealth ? "1" : "0";
            cells[4] = observation.recentlyHit ? "1" : "0";
            for (int j = 0; j < 4; j++)
            {
                cells[5 + j] = input[j].ToString("R", Invariant);
                cells[9 + j] = MonsterNeuralPolicyModel.TeacherPreference(j, observation).ToString("R", Invariant);
            }
            writer.WriteLine(string.Join(",", cells));
        }
    }
}
