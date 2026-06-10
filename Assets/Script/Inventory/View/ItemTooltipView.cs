using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemDescText;
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);

    private void Awake()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

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
        {
            transform.position = (Vector2)Input.mousePosition + offset;
        }
    }

    public void Show(ItemStack item)
    {
        if (item == null) return;

        itemNameText.text = item.item.itemName;
        itemTypeText.text = item.item.itemType.ToString();
        itemDescText.text = item.item.description;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}