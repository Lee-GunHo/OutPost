using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class CraftingRecipeSlotView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;

    private CraftingRecipe recipe;

    public event Action<CraftingRecipe> OnClicked;
    public event Action<CraftingRecipe> OnPointerEntered;
    public event Action OnPointerExited;


    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Initialize(CraftingRecipe newRecipe)
    {
        recipe = newRecipe;

        if (recipe == null || recipe.ResultItem == null)
        {
            Clear();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = recipe.ResultItem.icon;
            itemIcon.enabled = recipe.ResultItem.icon != null;
        }

        if (countText != null)
        {
            countText.text = recipe.ResultAmount > 1
                ? recipe.ResultAmount.ToString()
                : string.Empty;
        }
    }

    public void SetCraftable(bool canCraft)
    {
        if (itemIcon != null)
        {
            itemIcon.color = canCraft
                ? Color.white
                : new Color(0.35f, 0.35f, 0.35f, 1f);
        }
    }

    public void Clear()
    {
        recipe = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            itemIcon.color = Color.white;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
        }
    }

    private void HandleClicked()
    {
        if (recipe != null)
        {
            OnClicked?.Invoke(recipe);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (recipe != null)
        {
            OnPointerEntered?.Invoke(recipe);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExited?.Invoke();
    }
}