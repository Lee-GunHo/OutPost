using UnityEngine;

/// <summary>
/// 드롭 프리팹 생성과 파괴 대상 제거를 담당
/// 저장 방식이나 파괴 가능 여부는 판단하지 않음.
/// </summary>
public class BreakableWallView : MonoBehaviour
{
    public void SpawnDrops(
        GameObject dropItemPrefab,
        int minDropCount,
        int maxDropCount,
        float dropSpread)
    {
        if (dropItemPrefab == null)
            return;

        int minimum = Mathf.Max(0, minDropCount);
        int maximum = Mathf.Max(minimum, maxDropCount);
        int randomDropCount = Random.Range(minimum, maximum + 1);

        for (int i = 0; i < randomDropCount; i++)
        {
            Vector3 dropPosition = transform.position;
            dropPosition.x += Random.Range(-dropSpread, dropSpread);
            dropPosition.z += Random.Range(-dropSpread, dropSpread);
            dropPosition.y = 0.5f;

            Instantiate(
                dropItemPrefab,
                dropPosition,
                Quaternion.identity
            );
        }
    }

    public void DestroyTarget(GameObject target)
    {
        Destroy(target != null ? target : gameObject);
    }
}