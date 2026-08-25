using UnityEngine;

public class MonsterCleanupManager : MonoBehaviour
{
    public static MonsterCleanupManager Instance { get; private set; }

    [Header("Cleanup Option")]
    [SerializeField] private bool cleanupOnStart = true;
    [SerializeField] private int waitFrameCount = 2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private System.Collections.IEnumerator Start()
    {
        if (!cleanupOnStart)
        {
            yield break;
        }

        for (int i = 0; i < waitFrameCount; i++)
        {
            yield return null;
        }

        DespawnAllNormalMonsters();
    }

    public void DespawnAllNormalMonsters()
    {
        MonsterPresenter[] monsters =
            FindObjectsByType<MonsterPresenter>(FindObjectsSortMode.None);

        foreach (MonsterPresenter monster in monsters)
        {
            if (monster == null)
            {
                continue;
            }

            // 혹시 보스에 MonsterPresenter가 붙는 구조가 생겨도 보스는 제외
            if (monster.GetComponent<BossPresenter>() != null)
            {
                continue;
            }

            Destroy(monster.gameObject);
        }

        Debug.Log("게임 시작 시 모든 일반 몬스터 제거 완료");
    }
}