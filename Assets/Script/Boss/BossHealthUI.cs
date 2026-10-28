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
    private float nextTargetRefresh;

    private void Awake()
    {
        // Resolve dynamically, including bosses created after this canvas starts.
        bossPresenter = null;
        HideUI();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTargetRefresh) return;
        nextTargetRefresh = Time.unscaledTime + 0.25f;
        BossPresenter closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (BossPresenter boss in BossPresenter.ActiveBosses)
        {
            if (boss == null || boss.IsDead || !boss.IsTargetInDetectionRange()) continue;
            float distance = boss.GetDistanceToTarget();
            if (distance >= closestDistance) continue;
            closest = boss;
            closestDistance = distance;
        }
        if (closest != bossPresenter) SetBoss(closest);
        else if (closest == null) HideUI();
    }

    private void OnDisable()
    {
        UnsubscribeBoss();
        bossPresenter = null;
        HideUI();
    }

    public void SetBoss(BossPresenter newBossPresenter)
    {
        UnsubscribeBoss();

        bossPresenter = newBossPresenter;
        if (bossPresenter == null) { HideUI(); return; }

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
