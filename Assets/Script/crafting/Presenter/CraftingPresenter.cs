using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CraftingPresenter : MonoBehaviour
{
    public static CraftingPresenter Instance { get; private set; }

    [Header("Model")]
    [SerializeField] private InventoryModel inventoryModel;

    [Header("View")]
    [SerializeField] private CraftingView craftingView;

    [SerializeField] private CraftingTooltipView craftingTooltipView;

    [Header("Presenter")]
    [Tooltip("손 제작일 때 craftingView가 들어가는 부모 — 인벤토리 창 안쪽")]
    [SerializeField] private Transform tabParent;

    [Header("Hand Crafting Recipes")]
    [Tooltip("제작대 없이도 항상 제작 가능한 레시피")]
    [SerializeField]
    private List<CraftingRecipe> handRecipes
        = new List<CraftingRecipe>();

    private CraftingModel craftingModel;

    private readonly List<CraftingRecipe> displayedRecipes
        = new List<CraftingRecipe>();

    private CraftingTableModel currentTable;
    private PlayerPresenter currentPlayer;
    private CraftingTableInteractable currentInteractable;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        bool escapePressed =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        if (escapePressed)
        {
            Close();
        }
    }

    private void Start()
    {
        if (inventoryModel == null || craftingView == null)
        {
            Debug.LogError(
                "CraftingPresenter의 참조가 연결되지 않았습니다.");
            return;
        }

        craftingModel = new CraftingModel(inventoryModel);

        craftingView.OnRecipeClicked += HandleRecipeClicked;
        craftingView.OnRecipePointerEntered += HandleRecipePointerEntered;
        craftingView.OnRecipePointerExited += HandleRecipePointerExited;
        inventoryModel.OnInventoryChanged += RefreshCraftableState;

        craftingView.SetVisible(false);
    }

    private void OnDestroy()
    {
        if (craftingView != null)
        {
            craftingView.OnRecipeClicked -= HandleRecipeClicked;
            craftingView.OnRecipePointerEntered -= HandleRecipePointerEntered;
            craftingView.OnRecipePointerExited -= HandleRecipePointerExited;
        }

        if (inventoryModel != null)
        {
            inventoryModel.OnInventoryChanged -= RefreshCraftableState;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// table이 있으면 그 제작대 전용 레시피만, 없으면 손 제작 레시피만 보여줌
    /// </summary>
    public void Open(
        CraftingTableModel table,
        PlayerPresenter player,
        CraftingTableInteractable interactable)
    {
        if (player == null || craftingModel == null)
        {
            Debug.LogWarning(
                "제작 UI를 열 수 없습니다: 필요한 참조가 없습니다.");
            return;
        }

        if (isOpen)
        {
            Close();
        }

        currentTable = table;
        currentPlayer = player;
        currentInteractable = interactable;
        isOpen = true;

        UIState.SetCraftingOpen(true);
        currentPlayer.StopMove();

        // 제작대에서 열면 인벤토리 창과 분리된 독립 창으로, 손 제작이면 인벤토리 안으로
        Transform parent = table != null ? craftingView.transform.root : tabParent;
        if (parent != null)
            craftingView.transform.SetParent(parent, false);

        RebuildDisplayedRecipes();
        craftingView.ShowRecipes(displayedRecipes);
        craftingView.SetCloseButtonVisible(currentTable != null);
        craftingView.SetVisible(true);
        RefreshCraftableState();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        CraftingTableInteractable closedInteractable = currentInteractable;

        isOpen = false;
        craftingView.SetVisible(false);

        UIState.SetCraftingOpen(false);

        currentTable = null;
        currentPlayer = null;
        currentInteractable = null;

        if (closedInteractable != null)
        {
            closedInteractable.ShowInteractionMarkIfPossible();
        }
    }

    public bool IsOpenedTable(CraftingTableModel table)
    {
        return isOpen && currentTable == table;
    }

    private void RebuildDisplayedRecipes()
    {
        displayedRecipes.Clear();

        if (currentTable != null)
        {
            displayedRecipes.AddRange(currentTable.TableRecipes);
        }
        else
        {
            displayedRecipes.AddRange(handRecipes);
        }
    }

    private void HandleRecipeClicked(CraftingRecipe recipe)
    {
        if (craftingModel == null)
        {
            return;
        }

        CraftingResult result = craftingModel.Craft(recipe);

        switch (result)
        {
            case CraftingResult.Success:
                Debug.Log($"{recipe.RecipeName} 제작 성공");
                break;

            case CraftingResult.NotEnoughIngredients:
                Debug.Log("제작 재료가 부족합니다.");
                break;

            case CraftingResult.InventoryFull:
                Debug.Log("인벤토리 공간이 부족합니다.");
                break;

            default:
                Debug.LogWarning("올바르지 않은 제작법입니다.");
                break;
        }

        RefreshCraftableState();
        if (result == CraftingResult.Success)
        {
            HandleRecipePointerEntered(recipe);
        }
    }
    private void HandleRecipePointerEntered(CraftingRecipe recipe)
    {
        if (recipe == null ||
            inventoryModel == null ||
            craftingTooltipView == null)
        {
            return;
        }

        List<int> ownedAmounts = new List<int>();

        foreach (CraftingIngredient ingredient in recipe.Ingredients)
        {
            int ownedAmount =
                inventoryModel.GetItemCount(ingredient.Item);

            ownedAmounts.Add(ownedAmount);
        }

        craftingTooltipView.Show(recipe, ownedAmounts);
    }

    private void HandleRecipePointerExited()
    {
        if (craftingTooltipView != null)
        {
            craftingTooltipView.Hide();
        }
    }

    private void RefreshCraftableState()
    {
        if (craftingModel == null || craftingView == null)
        {
            return;
        }

        for (int i = 0; i < displayedRecipes.Count; i++)
        {
            bool canCraft = craftingModel.CanCraft(displayedRecipes[i]);
            craftingView.SetRecipeCraftable(i, canCraft);
        }
    }
}