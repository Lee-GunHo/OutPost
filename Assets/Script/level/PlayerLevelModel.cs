using UnityEngine;

public class PlayerLevelModel : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int requiredExp = 100;
    [SerializeField] private int statPoint = 0;

    [Header("Growth Data")]
    [SerializeField] private int expIncreasePerLevel = 50;
    [SerializeField] private int statPointPerLevel = 3;

    private int baseRequiredExp;

    public int Level => level;
    public int CurrentExp => currentExp;
    public int RequiredExp => requiredExp;
    public int StatPoint => statPoint;

    private void Awake()
    {
        baseRequiredExp = requiredExp;
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentExp += amount;

        Debug.Log($"경험치 획득: {amount}, 현재 경험치: {currentExp}/{requiredExp}");

        while (currentExp >= requiredExp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= requiredExp;
        level++;

        statPoint += statPointPerLevel;
        requiredExp += expIncreasePerLevel;

        Debug.Log($"레벨업! 현재 레벨: {level}, 스탯 포인트: {statPoint}");
    }

    public bool UseStatPoint(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (statPoint < amount)
        {
            Debug.Log("스탯 포인트가 부족합니다.");
            return false;
        }

        statPoint -= amount;
        return true;
    }

    public void LoadSavedLevelData(int savedLevel, int savedCurrentExp, int savedStatPoint)
    {
        level = Mathf.Max(1, savedLevel);
        currentExp = Mathf.Max(0, savedCurrentExp);
        statPoint = Mathf.Max(0, savedStatPoint);

        RecalculateRequiredExp();
    }

    private void RecalculateRequiredExp()
    {
        requiredExp = baseRequiredExp + (level - 1) * expIncreasePerLevel;
    }
}