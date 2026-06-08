using UnityEngine;

public class BreakableWall : MonoBehaviour
{
    [Header("Drop Setting")]
    [Tooltip("벽이 부서질 때 떨어질 아이템 프리팹")]
    public GameObject dropItemPrefab;

    [Tooltip("아이템이 몇 개 떨어지는지")]
    public int minDropCount = 1;
    public int maxDropCount = 3;

    [Tooltip("아이템과 벽 사이 간격")]
    public float dropSpread = 0.3f;

    /// <summary>
    /// 벽을 부수는 함수
    /// PlayerInteractor.cs에서 호출됨.
    /// </summary>
    public void Break()
    {
        // 1개 ~ 3개 랜덤 개수 결정
        int randomDropCount = Random.Range(minDropCount, maxDropCount + 1);

        for(int i = 0; i < randomDropCount; i++)
        {
            // 떨어질 아이템 프리팹이 연결되어 있을 때만 생성
            if(dropItemPrefab != null)
            {
                // 기본 드랍 위치는 현재 벽의 위치
                Vector3 dropPosition = transform.position;

                // 아이템이 벽 중심에 겹치지 않도록
                // XZ 평면에서 살짝 랜덤하게 위치를 바꿈.
                dropPosition.x += Random.Range(-dropSpread, dropSpread);
                dropPosition.z += Random.Range(-dropSpread, dropSpread);

                // 아이템이 바닥에 뭍히지 않도록 Y 위치를 살짝 올림
                dropPosition.y = 0.5f;

                // 아이템 프리팹을 실제 씬에 생성
                Instantiate(dropItemPrefab, dropPosition, Quaternion.identity);
            }
        }

        // 아이템을 생성한 뒤, 벽 오브젝트 제거
        Destroy(gameObject);
    }
}
