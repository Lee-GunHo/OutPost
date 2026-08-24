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

    /// <summary>
    /// root의 자식 중 "{root 이름}Up{stageIndex}", "{root 이름}Down{stageIndex}"
    /// 이름을 가진 오브젝트를 제거. 해당 이름의 파츠가 없는 프리팹(단일 단계)에서는 아무 동작도 하지 않음.
    /// </summary>
    public void DestroyStageParts(GameObject root, int stageIndex)
    {
        if (root == null)
            return;

        DestroyChildIfExists(root.transform, root.name + "Up" + stageIndex);
        DestroyChildIfExists(root.transform, root.name + "Down" + stageIndex);
    }

    private void DestroyChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
            Destroy(child.gameObject);
    }
}