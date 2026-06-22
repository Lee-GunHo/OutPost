using UnityEngine;

public class MonsterPresenter : MonoBehaviour, IDamageable
{
    private MonsterModel monsterModel;
    private MonsterStateManager stateManager;
    private Rigidbody rigid;
    private Transform playerTransform;

    public float MoveSpeed => monsterModel.MoveSpeed;
    public float ChaseRange => monsterModel.ChaseRange;
    public float AttackRange => monsterModel.AttackRange;
    public float HitDuration => monsterModel.HitDuration;
    public bool IsDead => monsterModel.IsDead;

    private void Awake()
    {
        monsterModel = GetComponent<MonsterModel>();
        stateManager = GetComponent<MonsterStateManager>();
        rigid = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log("플레이어 찾음: " + player.name);
        }
        else
        {
            Debug.LogWarning("Player 태그를 가진 오브젝트를 찾지 못했습니다.");
        }
    }

    public bool IsPlayerInChaseRange()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("playerTransform이 null입니다.");
            return false;
        }

        float distance = GetDistanceToPlayer();

        return distance <= ChaseRange;
    }

    public bool IsPlayerInAttackRange()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("playerTransform이 null입니다.");
            return false;
        }

        float distance = GetDistanceToPlayer();

        return distance <= AttackRange;
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    public void ChasePlayer()
    {
        if (playerTransform == null)
        {
            StopMove();
            return;
        }

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        direction = direction.normalized;

        rigid.linearVelocity = direction * MoveSpeed;
    }

    public void StopMove()
    {
        rigid.linearVelocity = Vector3.zero;
    }

    public void Attack()
    {
        Debug.Log("몬스터 공격");
    }

    public void TakeDamage(int damage)
    {
        if (monsterModel.IsDead)
        {
            return;
        }

        monsterModel.TakeDamage(damage);

        Debug.Log("몬스터 피격, 현재 체력: " + monsterModel.CurrentHp);

        if (monsterModel.IsDead)
        {
            stateManager.ChangeState(stateManager.DeadState);
        }
        else
        {
            stateManager.ChangeState(stateManager.HitState);
        }
    }

    public void Dead()
    {
        StopMove();
        Debug.Log("몬스터 사망");

        // 일단 테스트용으로 오브젝트 제거
        Destroy(gameObject, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        MonsterModel model = GetComponent<MonsterModel>();

        if (model == null)
        {
            return;
        }

        // 인식 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, model.ChaseRange);

        // 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, model.AttackRange);
    }
}