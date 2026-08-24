using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 넥서스의 체력을 UI에 표시
/// </summary>
public class NexusView : MonoBehaviour
{
    [Header("체력 UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    public void Initialize(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
        }

        RefreshHealth(currentHealth, maxHealth);
    }

    public void RefreshHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}