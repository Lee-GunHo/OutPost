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
        if (item == null || item.item == null)
        {
            Hide();
            return;
        }

        itemNameText.text = item.item.itemName;
        itemTypeText.text = item.item.itemType.ToString();

        string statText = GetStatText(item.item);

        if (string.IsNullOrEmpty(statText))
        {
            itemDescText.text = item.item.description;
        }
        else
        {
            itemDescText.text = $"{item.item.description}\n\n{statText}";
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private string GetStatText(ItemData item)
    {
        string text = "";

        if (item.hpBonus != 0)
            text += $"체력 +{item.hpBonus}\n";

        if (item.attackBonus != 0)
            text += $"공격력 +{item.attackBonus}\n";

        if (item.defenseBonus != 0)
            text += $"방어력 +{item.defenseBonus}\n";

        if (item.moveSpeedBonus != 0)
            text += $"이동속도 +{item.moveSpeedBonus}\n";

        return text;
    }

}