using UnityEngine;

/// <summary>
/// 기존 프리팹과 외부 참조를 유지하기 위한 호환 클래스
/// 실제 획득 처리는 DroppedItemPresenter가 담당
/// </summary>
public class DroppedItem : DroppedItemPresenter
{
    // 기존 DroppedItem 컴포넌트에 저장된 값을 유지하기 위한 필드
    // 새 오브젝트에서는 DroppedItemModel Inspector를 사용
    [SerializeField, HideInInspector] private ItemData itemData;
    [SerializeField, HideInInspector] private int amount = 1;

    protected override void ApplyLegacySerializedData()
    {
        if (Model == null || itemData == null)
            return;

        Model.Configure(itemData, amount);
    }
}