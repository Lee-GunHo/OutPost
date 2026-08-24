using System;
using UnityEngine;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Monster Progress")]
    [SerializeField] private int normalMonsterKillCount = 0;
    [SerializeField] private int bossKillCount = 0;
    [SerializeField] private bool bossKilled = false;

    [Header("Raid Progress")]
    [SerializeField] private int raidClearCount = 0;

    [Header("Map Progress")]
    [SerializeField] private int progressLevel = 0;
    [SerializeField] private int normalMonsterKillRequiredForProgress = 10;

    public int NormalMonsterKillCount => normalMonsterKillCount;
    public int BossKillCount => bossKillCount;
    public bool BossKilled => bossKilled;
    public int RaidClearCount => raidClearCount;
    public int ProgressLevel => progressLevel;

    public event Action<int> OnNormalMonsterKillCountChanged;
    public event Action<int> OnProgressLevelChanged;
    public event Action OnBossKilled;
    public event Action<int> OnRaidClearCountChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddNormalMonsterKill()
    {
        normalMonsterKillCount++;

        Debug.Log("일반 몬스터 처치 수: " + normalMonsterKillCount);

        CheckNormalMonsterProgress();

        OnNormalMonsterKillCountChanged?.Invoke(normalMonsterKillCount);
    }

    public void AddBossKill()
    {
        bossKillCount++;
        bossKilled = true;

        Debug.Log("보스 처치 수: " + bossKillCount);

        OnBossKilled?.Invoke();
    }

    public void AddRaidClear()
    {
        raidClearCount++;

        Debug.Log("습격 클리어 수: " + raidClearCount);

        OnRaidClearCountChanged?.Invoke(raidClearCount);
    }

    private void CheckNormalMonsterProgress()
    {
        if (normalMonsterKillCount >= normalMonsterKillRequiredForProgress && progressLevel < 1)
        {
            progressLevel = 1;

            Debug.Log("진행도 상승. Progress Level: " + progressLevel);

            OnProgressLevelChanged?.Invoke(progressLevel);
        }
    }

    public void LoadProgressData(
    int savedNormalMonsterKillCount,
    int savedBossKillCount,
    bool savedBossKilled,
    int savedRaidClearCount,
    int savedProgressLevel
)
    {
        normalMonsterKillCount = Mathf.Max(0, savedNormalMonsterKillCount);
        bossKillCount = Mathf.Max(0, savedBossKillCount);
        bossKilled = savedBossKilled;
        raidClearCount = Mathf.Max(0, savedRaidClearCount);
        progressLevel = Mathf.Max(0, savedProgressLevel);

        OnNormalMonsterKillCountChanged?.Invoke(normalMonsterKillCount);
        OnRaidClearCountChanged?.Invoke(raidClearCount);
        OnProgressLevelChanged?.Invoke(progressLevel);

        Debug.Log(
            "진행도 데이터 로드 완료" +
            " / 일반 몬스터 처치 수: " + normalMonsterKillCount +
            " / 보스 처치 수: " + bossKillCount +
            " / 보스 처치 여부: " + bossKilled +
            " / 습격 클리어 수: " + raidClearCount +
            " / 진행도 레벨: " + progressLevel
        );
    }
}