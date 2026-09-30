using UnityEngine;

/// <summary>
/// RuleBasedDiagnoser가 사용하는 규칙 기준값. 코드 수정 없이 인스펙터에서 튜닝 가능.
/// </summary>
[CreateAssetMenu(fileName = "DifficultyRuleConfig", menuName = "NPC/Difficulty Rule Config")]
public class DifficultyRuleConfig : ScriptableObject
{
    [Header("전투 미숙 (CombatStruggle)")]
    [Tooltip("최근 5분 내 사망 횟수가 이 값 이상이면 전투 미숙으로 판단")]
    public int combatStruggleRecentDeathThreshold = 2;

    [Header("장비 부족 (EquipmentLack)")]
    [Tooltip("현재 장착 도구 등급이 이 값 이하일 때 장비 부족 후보")]
    public ItemGrade equipmentLackMaxGrade = ItemGrade.Normal;
    [Tooltip("장비 부족으로 판단하기 위한 최소 누적 사망 횟수")]
    public int equipmentLackDeathThreshold = 1;

    [Header("길 잃음 (Lost)")]
    [Tooltip("한 구역에 이 시간(초) 이상 머무르면 길 잃음 후보")]
    public float lostZoneDwellThresholdSec = 180f;
    [Tooltip("체크 시, 수락했지만 완료하지 못한 퀘스트가 있을 때만 길 잃음으로 판단")]
    public bool lostRequiresActiveQuest = true;

    [Header("자원 부족 (ResourceLack)")]
    [Tooltip("이 시간(초) 이상 플레이했는데 몬스터 처치 수가 적으면 자원 부족 후보")]
    public float resourceLackElapsedThresholdSec = 300f;
    [Tooltip("이 값 이하의 몬스터 처치 수일 때 자원 부족 후보")]
    public int resourceLackMaxMonsterKill = 3;

    [Header("확신도(Confidence) 계산")]
    [Tooltip("규칙이 막 조건을 만족했을 때의 최소 확신도")]
    [Range(0f, 1f)] public float minConfidence = 0.5f;
    [Tooltip("규칙 조건을 크게 초과했을 때의 최대 확신도")]
    [Range(0f, 1f)] public float maxConfidence = 0.95f;
    [Tooltip("아무 규칙도 해당하지 않아 Smooth로 판단될 때의 확신도")]
    [Range(0f, 1f)] public float smoothConfidence = 0.8f;
}
