using UnityEngine;

/// <summary>
/// 기존 SeedMapGenerator와 공격 코드의 BreakableWall 참조를 유지하기 위한 호환 클래스
/// 실제 파괴 제어는 BreakableWallPresenter가 담당
/// </summary>
public class BreakableWall : BreakableWallPresenter
{
    // 기존 프리팹에 직렬화된 값을 유지하기 위한 호환 필드
    // 새 프리팹에서는 BreakableWallModel Inspector를 사용
    [HideInInspector] public GameObject dropItemPrefab;
    [HideInInspector] public int minDropCount = 1;
    [HideInInspector] public int maxDropCount = 3;
    [HideInInspector] public float dropSpread = 0.3f;
    [SerializeField, HideInInspector] private bool showSaveDebugLog = true;

    protected override void ApplyLegacySerializedData()
    {
        if (Model == null || dropItemPrefab == null)
            return;

        Model.ConfigureDropSettings(
            dropItemPrefab,
            minDropCount,
            maxDropCount,
            dropSpread,
            showSaveDebugLog
        );
    }
}