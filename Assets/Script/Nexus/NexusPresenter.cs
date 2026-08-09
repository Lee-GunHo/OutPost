using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NexusModel과 NexusView를 연결합니다.
/// 넥서스는 IDamageable을 구현해서 몬스터 공격 대상이 될 수 있습니다.
/// </summary>
[RequireComponent(typeof(NexusModel))]
public class NexusPresenter : MonoBehaviour, IDamageable
{
    [Header("넥서스 구성 요소")]
    [SerializeField] private NexusModel nexusModel;
    [SerializeField] private NexusView nexusView;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private bool pauseGameOnNexusDestroyed = true;

    private const string GameOverSceneName = "GameOverScene";
    private bool isGameOverProcessed;

    public int CurrentHealth => nexusModel != null ? nexusModel.CurrentHealth : 0;
    public int MaxHealth => nexusModel != null ? nexusModel.MaxHealth : 0;
    public bool IsHealthDepleted => nexusModel != null && nexusModel.IsHealthDepleted;

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
        {
            return;
        }

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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (nexusModel == null)
        {
            return;
        }

        nexusModel.OnHealthChanged -= HandleHealthChanged;
        nexusModel.OnHealthDepleted -= HandleHealthDepleted;
    }

    /// <summary>
    /// 적의 공격 스크립트에서 호출
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isGameOverProcessed)
        {
            return;
        }

        nexusModel?.TakeDamage(damage);
    }

    public void Heal(int amount)
    {
        if (isGameOverProcessed)
        {
            return;
        }

        nexusModel?.Heal(amount);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        nexusView?.RefreshHealth(currentHealth, maxHealth);
    }

    private void HandleHealthDepleted()
    {
        if (isGameOverProcessed)
        {
            return;
        }

        isGameOverProcessed = true;

        Debug.Log("넥서스의 체력이 모두 소진되었습니다. 게임 오버 처리.");
        SceneManager.LoadScene(GameOverSceneName);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (pauseGameOnNexusDestroyed)
        {
            Time.timeScale = 0f;
        }
    }

    [ContextMenu("테스트 피해 100")]
    private void TestDamage()
    {
        TakeDamage(100);
    }

    [ContextMenu("테스트 넥서스 파괴")]
    private void TestDestroyNexus()
    {
        TakeDamage(MaxHealth);
    }
}