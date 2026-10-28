using System;
using System.Collections.Generic;
using UnityEngine;

public class BossPresenter : MonoBehaviour, IDamageable
{
    public static readonly HashSet<BossPresenter> ActiveBosses = new HashSet<BossPresenter>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => ActiveBosses.Clear();
    private void OnEnable() => ActiveBosses.Add(this);
    private void OnDisable() => ActiveBosses.Remove(this);

    public void InitializeChunkSpawn(int seed, Vector2Int chunk, Vector2Int local, Vector2Int global)
    {
        bossModel.SetSpawnIdentity(seed, chunk, local, global);
    }
    private BossModel bossModel;
    private BossView bossView;
    private BossStateManager stateManager;

    private Transform playerTransform;
    private bool isDeathProcessed;

    public Transform Target => playerTransform;

    public int CurrentHp => bossModel.CurrentHp;
    public int MaxHp => bossModel.MaxHp;
    public int ExpReward => bossModel.ExpReward;

    public float DetectionRange => bossModel.DetectionRange;

    public bool IsDead => bossModel.IsDead;
    public bool HasTarget => playerTransform != null;

    public event Action<int, int> OnBossHpChanged;
    public event Action OnBossDead;

    private void Awake()
    {
        bossModel = GetComponent<BossModel>();
        bossView = GetComponent<BossView>();
        stateManager = GetComponent<BossStateManager>();
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player object was not found.");
        }

        NotifyBossHpChanged();
    }

    public float GetDistanceToTarget()
    {
        if (playerTransform == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(transform.position, playerTransform.position);
    }

    public bool IsTargetInDetectionRange()
    {
        return GetDistanceToTarget() <= DetectionRange;
    }

    public void StopMove()
    {
        // This boss is rooted in the ground, so it does not move.
    }

    public void LookAtTarget()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    public void TakeDamage(int damage)
    {
        if (bossModel.IsDead)
        {
            return;
        }

        bossModel.TakeDamage(damage);

        Debug.Log($"Boss damaged. HP: {CurrentHp}/{MaxHp}");

        NotifyBossHpChanged();

        // No groggy / no hit state.
        if (bossModel.IsDead)
        {
            stateManager.ChangeState(stateManager.DeadState);
        }
    }

    public void Dead()
    {
        if (isDeathProcessed)
        {
            return;
        }

        isDeathProcessed = true;

        if (bossModel.DropItemPrefab != null)
        {
            if (bossView == null)
                bossView = gameObject.AddComponent<BossView>();
            bossView.SpawnDrop(bossModel.DropItemPrefab, transform.position);
        }

        if (bossModel.HasSpawnIdentity)
            ChunkModificationSaveManager.GetOrCreate().RegisterDestroyedObject(
                bossModel.WorldSeed, bossModel.SpawnChunk, bossModel.SpawnLocalCell,
                bossModel.SpawnGlobalCell, ChunkModificationSaveManager.ObjectTypeBoss);

        GiveExpToPlayer();

        AddBossKillProgress();

        NotifyBossDead();

        Debug.Log("Boss dead.");

        Destroy(gameObject, 1f);
    }

    private void AddBossKillProgress()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("GameProgressManager가 없습니다.");
            return;
        }

        GameProgressManager.Instance.AddBossKill();
    }

    private void NotifyBossHpChanged()
    {
        OnBossHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    private void NotifyBossDead()
    {
        OnBossDead?.Invoke();
    }

    private void GiveExpToPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        PlayerPresenter player = playerObject.GetComponent<PlayerPresenter>();

        if (player == null)
        {
            return;
        }

        player.AddExp(ExpReward);
    }
}
