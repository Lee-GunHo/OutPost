using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossPresenter bossPresenter;

    [Header("UI")]
    [SerializeField] private GameObject bossHealthPanel;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    private void Awake()
    {
        HideUI();
    }

    private void Start()
    {
        // 보스가 처음부터 씬에 있는 경우 대비
        if (bossPresenter == null)
        {
            BossPresenter foundBoss = FindFirstObjectByType<BossPresenter>();

            if (foundBoss != null)
            {
                SetBoss(foundBoss);
            }
        }
        else
        {
            SetBoss(bossPresenter);
        }
    }

    private void OnDisable()
    {
        UnsubscribeBoss();
    }

    public void SetBoss(BossPresenter newBossPresenter)
    {
        if (newBossPresenter == null)
        {
            return;
        }

        UnsubscribeBoss();

        bossPresenter = newBossPresenter;

        bossPresenter.OnBossHpChanged += RefreshUI;
        bossPresenter.OnBossDead += HandleBossDead;

        RefreshUI(bossPresenter.CurrentHp, bossPresenter.MaxHp);
    }

    private void UnsubscribeBoss()
    {
        if (bossPresenter == null)
        {
            return;
        }

        bossPresenter.OnBossHpChanged -= RefreshUI;
        bossPresenter.OnBossDead -= HandleBossDead;
    }

    private void RefreshUI(int currentHp, int maxHp)
    {
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(true);
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (hpText != null)
        {
            hpText.text = $"Boss HP: {currentHp} / {maxHp}";
        }
    }

    private void HandleBossDead()
    {
        HideUI();
        UnsubscribeBoss();
        bossPresenter = null;
    }

    private void HideUI()
    {
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(false);
        }
    }
}