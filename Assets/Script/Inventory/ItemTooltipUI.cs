using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ItemTooltipUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemDescText;
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        Hide();
    }

    private void Update()
    {
        if (gameObject.activeSelf)
            transform.position = (Vector2)Input.mousePosition + offset;
    }

    public void Show(ItemStack stack)
    {
        if (stack == null) return;

        itemNameText.text = stack.item.itemName;
        itemTypeText.text = stack.item.itemType.ToString();
        itemDescText.text = stack.item.description;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}