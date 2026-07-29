using UnityEngine;

/// <summary>
/// NexusModel과 NexusView를 연결합니다.
/// </summary>
[RequireComponent(typeof(NexusModel))]
public class NexusPresenter : MonoBehaviour
{
    [Header("넥서스 구성 요소")]
    [SerializeField] private NexusModel nexusModel;
    [SerializeField] private NexusView nexusView;

    private void Awake()
    {
        if (nexusModel == null)
        {
            nexusModel = GetComponent<NexusModel>();
        }
    }

    private void OnEnable()
    {
        if (nexusModel == null)
        {
            nexusModel = GetComponent<NexusModel>();
        }

        if (nexusModel == null)
            return;

        nexusModel.OnHealthChanged += HandleHealthChanged;
        nexusModel.OnHealthDepleted += HandleHealthDepleted;
    }

    private void Start()
    {
        if (nexusModel == null)
        {
            Debug.LogError($"{gameObject.name}에 NexusModel이 없습니다.");
            enabled = false;
            return;
        }

        nexusView?.Initialize(
            nexusModel.CurrentHealth,
            nexusModel.MaxHealth
        );
    }

    private void OnDisable()
    {
        if (nexusModel == null)
            return;

        nexusModel.OnHealthChanged -= HandleHealthChanged;
        nexusModel.OnHealthDepleted -= HandleHealthDepleted;
    }

    /// <summary>
    /// 적의 공격 스크립트에서 호출
    /// </summary>
    public void TakeDamage(int damage)
    {
        nexusModel?.TakeDamage(damage);
    }

    public void Heal(int amount)
    {
        nexusModel?.Heal(amount);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        nexusView?.RefreshHealth(currentHealth, maxHealth);
    }

    private void HandleHealthDepleted()
    {
        Debug.Log("넥서스의 체력이 모두 소진되었습니다.");

        // 여기서 게임 오버 처리를 호출하면 됩니당!
    }

    [ContextMenu("테스트 피해 100")]
    private void TestDamage()
    {
        TakeDamage(100);
    }
}