using System;
using UnityEngine;

/// <summary>
/// 넥서스의 체력 상태 관리
/// </summary>
public class NexusModel : MonoBehaviour
{
    public event Action<int, int> OnHealthChanged;
    public event Action OnHealthDepleted;

    [Header("넥서스 체력")]
    [SerializeField] private int maxHealth = 1000;

    private int currentHealth;
    private bool isHealthDepleted;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsHealthDepleted => isHealthDepleted;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isHealthDepleted || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            isHealthDepleted = true;
            OnHealthDepleted?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (isHealthDepleted || amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}