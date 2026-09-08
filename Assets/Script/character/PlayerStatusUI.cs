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

    private void Awake()
    {
        ResolvePlayerReferences();

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
        // ESC takes priority if L was also pressed during this frame.
        if (statPanelClosedFrame == Time.frameCount)
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
            hpText.text = $"HP: {playerPresenter.CurrentHp} / {playerPresenter.MaxHp}  Lv.{playerPresenter.HpUpgradeLevel}";
        }

        if (mpText != null)
        {
            mpText.text = $"MP: {playerPresenter.CurrentMp} / {playerPresenter.MaxMp}  Lv.{playerPresenter.MpUpgradeLevel}";
        }

        if (attackText != null)
        {
            attackText.text = $"Attack: {playerPresenter.AttackPower}  Lv.{playerPresenter.AttackUpgradeLevel}";
        }

        if (defenseText != null)
        {
            defenseText.text = $"Defense: {playerPresenter.DefensePower}  Lv.{playerPresenter.DefenseUpgradeLevel}";
        }

        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"Move Speed: {playerPresenter.MoveSpeed:F1}  Lv.{playerPresenter.MoveSpeedUpgradeLevel}";
        }

        if (levelText != null)
        {
            levelText.text = $"Level: {playerPresenter.Level}";
        }

        if (expText != null)
        {
            expText.text = $"EXP: {playerPresenter.CurrentExp} / {playerPresenter.RequiredExp}";
        }

        if (statPointText != null)
        {
            statPointText.text = $"Stat Points: {playerPresenter.StatPoint}";
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
