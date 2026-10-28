using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPresenter playerPresenter;
    [SerializeField] private PlayerInputManager playerInputManager;

    [Header("Status Bars")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider mpSlider;

    [Header("Status Labels")]
    [SerializeField] private TMP_Text hpLabel;
    [SerializeField] private TMP_Text mpLabel;

    [Header("Stat Panel")]
    [SerializeField] private GameObject statPanel;
    [SerializeField] private TMP_FontAsset koreanFont;

    [Header("Stat Texts")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text mpText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text statPointText;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button hpUpgradeButton;
    [SerializeField] private Button mpUpgradeButton;
    [SerializeField] private Button attackUpgradeButton;
    [SerializeField] private Button defenseUpgradeButton;
    [SerializeField] private Button moveSpeedUpgradeButton;

    private int statPanelClosedFrame = -1;
    private int displayedAttackPower = int.MinValue;

    private void Awake()
    {
        ResolvePlayerReferences();

        if (statPanel != null && koreanFont != null)
        {
            foreach (TMP_Text label in statPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = koreanFont;
                if (label.text.Trim() == "Status") label.text = "능력치";
            }
        }

        if (statPanel != null)
        {
            statPanel.SetActive(false);
        }

        UIState.SetStatWindowOpen(false);
    }

    private void OnEnable()
    {
        ResolvePlayerReferences();

        if (playerPresenter != null)
        {
            playerPresenter.OnPlayerStatusChanged += RefreshStatusUI;
        }

        AddUpgradeButtonListeners();
    }

    private void Start()
    {
        RefreshStatusUI();
    }

    private void OnDisable()
    {
        if (playerPresenter != null)
        {
            playerPresenter.OnPlayerStatusChanged -= RefreshStatusUI;
        }

        RemoveUpgradeButtonListeners();

        if (statPanel != null)
        {
            statPanel.SetActive(false);
        }

        UIState.SetStatWindowOpen(false);
    }

    private void Update()
    {
        if (statPanel != null && statPanel.activeSelf && playerPresenter != null &&
            displayedAttackPower != playerPresenter.CombatAttackPower)
        {
            RefreshStatTexts();
        }

        // ESC takes priority if L was also pressed during this frame.
        if (statPanelClosedFrame == Time.frameCount || UIState.WasEscapeHandledThisFrame)
        {
            return;
        }

        if (playerInputManager == null)
        {
            ResolvePlayerReferences();

            if (playerInputManager == null)
            {
                return;
            }
        }

        if (playerInputManager.IsStatWindowPressed)
        {
            ToggleStatPanel();
        }
    }

    private void ResolvePlayerReferences()
    {
        if (playerPresenter != null && playerInputManager != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning("Player object with Player tag was not found.");
            return;
        }

        if (playerPresenter == null)
        {
            playerPresenter = playerObject.GetComponent<PlayerPresenter>();

            if (playerPresenter == null)
            {
                Debug.LogWarning("PlayerPresenter was not found on Player object.");
            }
        }

        if (playerInputManager == null)
        {
            playerInputManager = playerObject.GetComponent<PlayerInputManager>();

            if (playerInputManager == null)
            {
                Debug.LogWarning("PlayerInputManager was not found on Player object.");
            }
        }
    }

    private void ToggleStatPanel()
    {
        if (statPanel == null)
        {
            Debug.LogWarning("StatPanel is not assigned.");
            return;
        }

        bool nextState = !statPanel.activeSelf;
        if (nextState && !UIState.CanOpenWindow)
            return;

        statPanel.SetActive(nextState);
        UIState.SetStatWindowOpen(nextState);

        RefreshStatusUI();
    }

    public void CloseStatPanel()
    {
        statPanelClosedFrame = Time.frameCount;

        if (statPanel != null)
        {
            statPanel.SetActive(false);
        }

        UIState.SetStatWindowOpen(false);
    }

    public void RefreshStatusUI()
    {
        if (playerPresenter == null)
        {
            ResolvePlayerReferences();

            if (playerPresenter == null)
            {
                return;
            }
        }

        RefreshStatusBars();
        RefreshStatusLabels();
        RefreshStatTexts();
        RefreshUpgradeButtons();
    }

    private void RefreshStatusBars()
    {
        if (hpSlider != null)
        {
            float hpRatio = 0f;

            if (playerPresenter.MaxHp > 0)
            {
                hpRatio = (float)playerPresenter.CurrentHp / playerPresenter.MaxHp;
            }

            hpSlider.SetValueWithoutNotify(hpRatio);
        }

        if (mpSlider != null)
        {
            float mpRatio = 0f;

            if (playerPresenter.MaxMp > 0)
            {
                mpRatio = (float)playerPresenter.CurrentMp / playerPresenter.MaxMp;
            }

            mpSlider.SetValueWithoutNotify(mpRatio);
        }
    }

    private void RefreshStatusLabels()
    {
        if (hpLabel != null)
        {
            hpLabel.text = $"{playerPresenter.CurrentHp} / {playerPresenter.MaxHp}";
        }

        if (mpLabel != null)
        {
            mpLabel.text = $"{playerPresenter.CurrentMp} / {playerPresenter.MaxMp}";
        }
    }

    private void RefreshStatTexts()
    {
        if (hpText != null)
        {
            hpText.text = $"체력: {playerPresenter.CurrentHp} / {playerPresenter.MaxHp}\n<size=80%>강화 {playerPresenter.HpUpgradeLevel}</size>";
        }

        if (mpText != null)
        {
            mpText.text = $"마나: {playerPresenter.CurrentMp} / {playerPresenter.MaxMp}\n<size=80%>강화 {playerPresenter.MpUpgradeLevel}</size>";
        }

        if (attackText != null)
        {
            displayedAttackPower = playerPresenter.CombatAttackPower;
            attackText.text = $"공격력: {displayedAttackPower}\n<size=80%>강화 {playerPresenter.AttackUpgradeLevel}</size>";
        }

        if (defenseText != null)
        {
            defenseText.text = $"방어력: {playerPresenter.DefensePower}\n<size=80%>강화 {playerPresenter.DefenseUpgradeLevel}</size>";
        }

        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"이동속도: {playerPresenter.MoveSpeed:F1}\n<size=80%>강화 {playerPresenter.MoveSpeedUpgradeLevel}</size>";
        }

        if (levelText != null)
        {
            levelText.text = $"레벨: {playerPresenter.Level}";
        }

        if (expText != null)
        {
            expText.text = $"경험치: {playerPresenter.CurrentExp} / {playerPresenter.RequiredExp}";
        }

        if (statPointText != null)
        {
            statPointText.text = $"능력치 포인트: {playerPresenter.StatPoint}";
        }
    }

    private void RefreshUpgradeButtons()
    {
        bool canUpgrade = playerPresenter != null && playerPresenter.StatPoint > 0;

        if (hpUpgradeButton != null)
        {
            hpUpgradeButton.interactable = canUpgrade;
        }

        if (mpUpgradeButton != null)
        {
            mpUpgradeButton.interactable = canUpgrade;
        }

        if (attackUpgradeButton != null)
        {
            attackUpgradeButton.interactable = canUpgrade;
        }

        if (defenseUpgradeButton != null)
        {
            defenseUpgradeButton.interactable = canUpgrade;
        }

        if (moveSpeedUpgradeButton != null)
        {
            moveSpeedUpgradeButton.interactable = canUpgrade;
        }
    }

    private void AddUpgradeButtonListeners()
    {
        if (hpUpgradeButton != null)
        {
            hpUpgradeButton.onClick.AddListener(OnHpUpgradeButtonClicked);
        }

        if (mpUpgradeButton != null)
        {
            mpUpgradeButton.onClick.AddListener(OnMpUpgradeButtonClicked);
        }

        if (attackUpgradeButton != null)
        {
            attackUpgradeButton.onClick.AddListener(OnAttackUpgradeButtonClicked);
        }

        if (defenseUpgradeButton != null)
        {
            defenseUpgradeButton.onClick.AddListener(OnDefenseUpgradeButtonClicked);
        }

        if (moveSpeedUpgradeButton != null)
        {
            moveSpeedUpgradeButton.onClick.AddListener(OnMoveSpeedUpgradeButtonClicked);
        }
    }

    private void RemoveUpgradeButtonListeners()
    {
        if (hpUpgradeButton != null)
        {
            hpUpgradeButton.onClick.RemoveListener(OnHpUpgradeButtonClicked);
        }

        if (mpUpgradeButton != null)
        {
            mpUpgradeButton.onClick.RemoveListener(OnMpUpgradeButtonClicked);
        }

        if (attackUpgradeButton != null)
        {
            attackUpgradeButton.onClick.RemoveListener(OnAttackUpgradeButtonClicked);
        }

        if (defenseUpgradeButton != null)
        {
            defenseUpgradeButton.onClick.RemoveListener(OnDefenseUpgradeButtonClicked);
        }

        if (moveSpeedUpgradeButton != null)
        {
            moveSpeedUpgradeButton.onClick.RemoveListener(OnMoveSpeedUpgradeButtonClicked);
        }
    }

    private void OnHpUpgradeButtonClicked()
    {
        if (playerPresenter != null)
        {
            playerPresenter.UpgradeHp();
        }
    }

    private void OnMpUpgradeButtonClicked()
    {
        if (playerPresenter != null)
        {
            playerPresenter.UpgradeMp();
        }
    }

    private void OnAttackUpgradeButtonClicked()
    {
        if (playerPresenter != null)
        {
            playerPresenter.UpgradeAttack();
        }
    }

    private void OnDefenseUpgradeButtonClicked()
    {
        if (playerPresenter != null)
        {
            playerPresenter.UpgradeDefense();
        }
    }

    private void OnMoveSpeedUpgradeButtonClicked()
    {
        if (playerPresenter != null)
        {
            playerPresenter.UpgradeMoveSpeed();
        }
    }
}
