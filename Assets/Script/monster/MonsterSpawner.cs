using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MonsterSpawnData
    {
        public GameObject monsterPrefab;

        [Range(0f, 1f)]
        public float spawnChance = 1f;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private MonsterSpawnData[] monsterSpawnDatas;
    [SerializeField] private Transform monsterParent;

    [Header("Spawn Settings")]
    [Tooltip("몇 초마다 스폰할지")]
    [SerializeField] private float spawnInterval = 1f;

    [Tooltip("한 번에 몇 마리 스폰할지")]
    [SerializeField] private int spawnCount = 1;

    [Tooltip("플레이어 주변 스폰 금지 반경")]
    [SerializeField] private float safeRadius = 5f;

    [Tooltip("플레이어 기준 최대 스폰 반경")]
    [SerializeField] private float spawnRadius = 12f;

    [Tooltip("동시에 존재 가능한 최대 몬스터 수")]
    [SerializeField] private int maxAliveMonsterCount = 20;

    [Header("Spawn Check")]
    [Tooltip("스폰 위치 주변 장애물 검사 반경")]
    [SerializeField] private float spawnCheckRadius = 0.5f;

    [Tooltip("벽/장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    private float spawnTimer;
    private List<GameObject> spawnedMonsters = new List<GameObject>();

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
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (monsterSpawnDatas == null || monsterSpawnDatas.Length == 0)
        {
            return;
        }

        RemoveDestroyedMonsters();

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnMonsters();
        }
    }

    private void SpawnMonsters()
    {
        if (spawnedMonsters.Count >= maxAliveMonsterCount)
        {
            return;
        }

        int availableSpawnCount = maxAliveMonsterCount - spawnedMonsters.Count;
        int finalSpawnCount = Mathf.Min(spawnCount, availableSpawnCount);

        for (int i = 0; i < finalSpawnCount; i++)
        {
            if (!TryGetRandomMonsterPrefab(out GameObject monsterPrefab))
            {
                Debug.Log("이번 스폰 확률에 성공한 몬스터가 없습니다.");
                continue;
            }

            if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                Debug.LogWarning("스폰 위치를 찾지 못했습니다.");
                continue;
            }

            GameObject monster = Instantiate(
                monsterPrefab,
                spawnPosition,
                Quaternion.identity,
                monsterParent
            );

            spawnedMonsters.Add(monster);

            Debug.Log("몬스터 스폰: " + monster.name);
        }
    }

    private bool TryGetRandomMonsterPrefab(out GameObject selectedPrefab)
    {
        List<GameObject> spawnablePrefabs = new List<GameObject>();

        foreach (MonsterSpawnData spawnData in monsterSpawnDatas)
        {
            if (spawnData == null)
            {
                continue;
            }

            if (spawnData.monsterPrefab == null)
            {
                continue;
            }

            if (Random.value <= spawnData.spawnChance)
            {
                spawnablePrefabs.Add(spawnData.monsterPrefab);
            }
        }

        if (spawnablePrefabs.Count == 0)
        {
            selectedPrefab = null;
            return false;
        }

        int randomIndex = Random.Range(0, spawnablePrefabs.Count);
        selectedPrefab = spawnablePrefabs[randomIndex];
        return true;
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        int maxTryCount = 30;

        for (int i = 0; i < maxTryCount; i++)
        {
            Vector3 candidatePosition = GetRandomPositionAroundPlayer();

            if (IsBlocked(candidatePosition))
            {
                continue;
            }

            spawnPosition = candidatePosition;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private Vector3 GetRandomPositionAroundPlayer()
    {
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(safeRadius, spawnRadius);

        Vector3 direction = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );

        return player.position + direction * randomDistance;
    }

    private bool IsBlocked(Vector3 position)
    {
        return Physics.CheckSphere(
            position,
            spawnCheckRadius,
            obstacleLayer
        );
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

        if (spawnInterval < 0.1f)
        {
            spawnInterval = 0.1f;
        }

        if (spawnCount < 1)
        {
            spawnCount = 1;
        }

        if (maxAliveMonsterCount < 1)
        {
            maxAliveMonsterCount = 1;
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