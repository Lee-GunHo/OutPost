using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public Vector3 playerPosition;

    public int currentHp;
    public int currentMp;

    public int attackPower;
    public int defensePower;

    public int level;
    public int currentExp;
    public int statPoint;

    [Header("Stat Upgrade")]
    public int hpUpgradeLevel;
    public int mpUpgradeLevel;
    public int attackUpgradeLevel;
    public int defenseUpgradeLevel;
    public int moveSpeedUpgradeLevel;

    // 진행도 저장 정보
    public int normalMonsterKillCount;
    public int bossKillCount;
    public bool bossKilled;
    public int raidClearCount;
    public int progressLevel;
    public bool isMonsterWaveInProgress;
}