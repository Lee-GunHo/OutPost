using UnityEngine;

/// <summary>
/// ChunkManager(ChunkModel)의 GlobalSeed를 씬 전환/재실행 사이에도 유지시켜 줌.
/// 새로하기 시에는 이전과 다른 랜덤 시드를 생성하고,
/// 이어하기 시에는 기존에 저장된 시드를 그대로 사용해 저장된 맵과 동일하게 불러올 수 있게 함.
/// </summary>
public static class GlobalSeedManager
{
    private const string SeedPrefKey = "OutPost_GlobalSeed";

    public static bool HasStoredSeed => PlayerPrefs.HasKey(SeedPrefKey);

    public static int StoredSeed => PlayerPrefs.GetInt(SeedPrefKey, 1234);

    public static void SetStoredSeed(int seed)
    {
        PlayerPrefs.SetInt(SeedPrefKey, seed);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 이전에 저장된 시드와 다른 랜덤 시드를 새로 생성하여 저장하고 반환함.
    /// </summary>
    public static int GenerateNewSeed()
    {
        bool hadPreviousSeed = HasStoredSeed;
        int previousSeed = StoredSeed;

        int newSeed;
        do
        {
            newSeed = Random.Range(int.MinValue, int.MaxValue);
        }
        while (hadPreviousSeed && newSeed == previousSeed);

        SetStoredSeed(newSeed);
        return newSeed;
    }
}
