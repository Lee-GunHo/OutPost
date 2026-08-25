using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterWaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MonsterSpawnData
    {
        public GameObject monsterPrefab;

        [Tooltip("몬스터 등장 가중치. 테스트는 1로 두면 됨. 나중에 0.1, 0.05처럼 낮게 설정 가능")]
        [Range(0f, 1f)]
        public float spawnWeight = 1f;
    }

    [System.Serializable]
    public class WaveData
    {
        public string waveName = "Wave";

        [Tooltip("이 웨이브에서 총 몇 마리 소환할지")]
        public int monsterCount = 5;

        [Tooltip("몬스터를 몇 초 간격으로 한 마리씩 소환할지")]
        public float spawnInterval = 0.5f;

        [Tooltip("이 웨이브에서 나올 몬스터 목록")]
        public MonsterSpawnData[] monsterSpawnDatas;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform monsterParent;

    [Header("Wave Settings")]
    [SerializeField] private WaveData[] waves;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float nextWaveDelay = 5f;
    [SerializeField] private bool loopWaves = false;

    [Header("Spawn Position")]
    [SerializeField] private float safeRadius = 5f;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float navMeshSearchRadius = 10f;

    [Header("Spawn Check")]
    [SerializeField] private float spawnCheckRadius = 0.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Limit")]
    [SerializeField] private int maxAliveMonsterCount = 20;

    [Header("Wave Target")]
    [SerializeField] private Transform nexusTransform;

    private readonly List<GameObject> spawnedMonsters = new List<GameObject>();

    private int currentWaveIndex = 0;
    private bool isRunning;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (nexusTransform == null)
        {
            GameObject nexusObject = GameObject.FindGameObjectWithTag("Nexus");

            if (nexusObject != null)
            {
                nexusTransform = nexusObject.transform;
            }
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        StartWaves();
    }

    private void Update()
    {
        RemoveDestroyedMonsters();
    }

    public void StartWaves()
    {
        if (isRunning)
        {
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("Player가 없어서 웨이브를 시작할 수 없습니다.");
            return;
        }

        if (!TryResolveNexusTransform())
        {
            Debug.LogWarning("Nexus가 없어서 웨이브를 시작할 수 없습니다.");
            return;
        }

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("Wave Data가 없습니다.");
            return;
        }

        isRunning = true;

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.StartMonsterWaveProgress();
        }

        SetNexusHealthVisible(true);

        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (true)
        {
            if (currentWaveIndex >= waves.Length)
            {
                if (loopWaves)
                {
                    currentWaveIndex = 0;
                }
                else
                {
                    Debug.Log("모든 웨이브 종료");
                    CompleteMonsterWave(true);
                    yield break;
                }
            }

            WaveData currentWave = waves[currentWaveIndex];

            Debug.Log($"웨이브 시작: {currentWaveIndex + 1} / {waves.Length}, 이름: {currentWave.waveName}");

            yield return StartCoroutine(SpawnWave(currentWave));

            Debug.Log($"웨이브 스폰 완료: {currentWave.waveName}");

            yield return StartCoroutine(WaitUntilWaveClear());

            Debug.Log($"웨이브 클리어: {currentWave.waveName}");

            currentWaveIndex++;

            yield return new WaitForSeconds(nextWaveDelay);
        }
    }

    private bool TryResolveNexusTransform()
    {
        if (nexusTransform != null)
        {
            return true;
        }

        GameObject nexusObject = GameObject.FindGameObjectWithTag("Nexus");

        if (nexusObject == null)
        {
            Debug.LogWarning("Nexus 태그가 붙은 오브젝트를 찾지 못했습니다.");
            return false;
        }

        nexusTransform = nexusObject.transform;

        Debug.Log("Nexus 자동 연결 완료: " + nexusTransform.name);

        return true;
    }

    public void RestartWaveFromSavedProgress()
    {
        Debug.Log("저장된 웨이브 진행 상태 감지. 웨이브를 처음부터 재시작합니다.");

        DespawnCurrentWaveMonsters();

        currentWaveIndex = 0;
        isRunning = false;

        StartWaves();
    }

    private void DespawnCurrentWaveMonsters()
    {
        foreach (GameObject monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster);
            }
        }

        spawnedMonsters.Clear();

        MonsterPresenter[] monsters =
            FindObjectsByType<MonsterPresenter>(FindObjectsSortMode.None);

        foreach (MonsterPresenter monster in monsters)
        {
            if (monster == null)
            {
                continue;
            }

            if (monster.GetComponent<BossPresenter>() != null)
            {
                continue;
            }

            Destroy(monster.gameObject);
        }

        Debug.Log("기존 웨이브 몬스터 제거 완료");
    }

    public void FailWaveByNexusDestroyed()
    {
        if (!isRunning)
        {
            return;
        }

        Debug.Log("넥서스 파괴로 웨이브 실패");

        DespawnCurrentWaveMonsters();
        CompleteMonsterWave(false);
    }

    private void CompleteMonsterWave(bool isCleared)
    {
        isRunning = false;

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.EndMonsterWaveProgress();

            if (isCleared)
            {
                GameProgressManager.Instance.AddRaidClear();
            }
        }

        SetNexusHealthVisible(false);
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        int spawnedCount = 0;

        while (spawnedCount < wave.monsterCount)
        {
            RemoveDestroyedMonsters();

            if (spawnedMonsters.Count >= maxAliveMonsterCount)
            {
                yield return null;
                continue;
            }

            if (TrySpawnMonster(wave))
            {
                spawnedCount++;
            }

            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private bool TrySpawnMonster(WaveData wave)
    {
        if (!TryGetRandomMonsterPrefab(wave, out GameObject monsterPrefab))
        {
            Debug.LogWarning("선택 가능한 몬스터 프리팹이 없습니다.");
            return false;
        }

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            Debug.LogWarning("스폰 위치를 찾지 못했습니다.");
            return false;
        }

        GameObject monster = Instantiate(
            monsterPrefab,
            spawnPosition,
            Quaternion.identity,
            monsterParent
        );

        MonsterPresenter monsterPresenter = monster.GetComponent<MonsterPresenter>();

        if (monsterPresenter != null)
        {
            monsterPresenter.InitializeAsWaveMonster(nexusTransform);
        }

        spawnedMonsters.Add(monster);

        Debug.Log("웨이브 몬스터 스폰: " + monster.name);

        return true;
    }

    private bool TryGetRandomMonsterPrefab(WaveData wave, out GameObject selectedPrefab)
    {
        selectedPrefab = null;

        if (wave.monsterSpawnDatas == null || wave.monsterSpawnDatas.Length == 0)
        {
            return false;
        }

        float totalWeight = 0f;

        foreach (MonsterSpawnData spawnData in wave.monsterSpawnDatas)
        {
            if (spawnData == null)
            {
                continue;
            }

            if (spawnData.monsterPrefab == null)
            {
                continue;
            }

            if (spawnData.spawnWeight <= 0f)
            {
                continue;
            }

            totalWeight += spawnData.spawnWeight;
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (MonsterSpawnData spawnData in wave.monsterSpawnDatas)
        {
            if (spawnData == null || spawnData.monsterPrefab == null || spawnData.spawnWeight <= 0f)
            {
                continue;
            }

            currentWeight += spawnData.spawnWeight;

            if (randomValue <= currentWeight)
            {
                selectedPrefab = spawnData.monsterPrefab;
                return true;
            }
        }

        return false;
    }

    private IEnumerator WaitUntilWaveClear()
    {
        while (spawnedMonsters.Count > 0)
        {
            RemoveDestroyedMonsters();
            yield return null;
        }
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        int maxTryCount = 100;

        for (int i = 0; i < maxTryCount; i++)
        {
            Vector3 candidatePosition = GetRandomPositionAroundSpawnCenter();

            if (!TryGetNearestNavMeshPosition(candidatePosition, out Vector3 navMeshPosition))
            {
                continue;
            }

            if (IsBlocked(navMeshPosition))
            {
                continue;
            }

            spawnPosition = navMeshPosition;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private Vector3 GetRandomPositionAroundSpawnCenter()
    {
        Transform center = nexusTransform;

        if (center == null)
        {
            center = player;
        }

        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(safeRadius, spawnRadius);

        Vector3 direction = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );

        return center.position + direction * randomDistance;
    }

    private bool TryGetNearestNavMeshPosition(Vector3 position, out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = position;
        return false;
    }

    private bool IsBlocked(Vector3 position)
    {
        if (obstacleLayer.value == 0)
        {
            return false;
        }

        return Physics.CheckSphere(position, spawnCheckRadius, obstacleLayer);
    }

    private void RemoveDestroyedMonsters()
    {
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null)
            {
                spawnedMonsters.RemoveAt(i);
            }
        }
    }

    private void OnValidate()
    {
        if (spawnRadius <= safeRadius)
        {
            spawnRadius = safeRadius + 1f;
        }

        if (maxAliveMonsterCount < 1)
        {
            maxAliveMonsterCount = 1;
        }

        if (nextWaveDelay < 0f)
        {
            nextWaveDelay = 0f;
        }
    }

    private void SetNexusHealthVisible(bool isVisible)
    {
        NexusPresenter nexus = FindFirstObjectByType<NexusPresenter>();

        if (nexus != null)
        {
            nexus.SetHealthVisible(isVisible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, safeRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, spawnRadius);
    }
}