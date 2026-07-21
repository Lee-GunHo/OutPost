using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemDescText;
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);
    [SerializeField] private EquipmentModel equipmentModel;
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
        ItemData equippedItem = GetEquippedItem(item);

        string text = "";

        text += GetIntStatLine(
            "체력",
            item.hpBonus,
            equippedItem != null
                ? equippedItem.hpBonus
                : null);

        text += GetIntStatLine(
            "공격력",
            item.attackBonus,
            equippedItem != null
                ? equippedItem.attackBonus
                : null);

        text += GetIntStatLine(
            "방어력",
            item.defenseBonus,
            equippedItem != null
                ? equippedItem.defenseBonus
                : null);

        text += GetFloatStatLine(
            "이동속도",
            item.moveSpeedBonus,
            equippedItem != null
                ? equippedItem.moveSpeedBonus
                : null);

        return text;
    }
    private ItemData GetEquippedItem(ItemData item)
    {
        if (equipmentModel == null || item == null)
            return null;

        ItemStack equippedStack =
            equipmentModel.GetEquippedItem(item.itemType);

        if (equippedStack == null ||
            equippedStack.item == null)
        {
            return null;
        }

        // 현재 착용한 장비 자체에 마우스를 올린 경우
        // 자기 자신과 비교하지 않는다.
        if (equippedStack.item == item)
            return null;

        return equippedStack.item;
    }
    private string GetIntStatLine(
    string statName,
    int value,
    int? equippedValue)
    {
        if (value == 0 &&
            (!equippedValue.HasValue ||
             equippedValue.Value == 0))
        {
            return "";
        }

        string valueText =
            value >= 0 ? $"+{value}" : value.ToString();

        string line = $"{statName} {valueText}";

        if (equippedValue.HasValue)
        {
            int difference =
                value - equippedValue.Value;

            line += GetComparisonText(difference);
        }

        return line + "\n";
    }
    private string GetFloatStatLine(
    string statName,
    float value,
    float? equippedValue)
    {
        if (Mathf.Approximately(value, 0f) &&
            (!equippedValue.HasValue ||
             Mathf.Approximately(
                 equippedValue.Value,
                 0f)))
        {
            return "";
        }

        string valueText =
            value >= 0f
                ? $"+{value:0.##}"
                : $"{value:0.##}";

        string line = $"{statName} {valueText}";

        if (equippedValue.HasValue)
        {
            float difference =
                value - equippedValue.Value;

            line += GetComparisonText(difference);
        }

        return line + "\n";
    }
    private string GetComparisonText(float difference)
    {
        if (difference > 0f)
        {
            return
                $" <color=#55FF55>(+{difference:0.##})</color>";
        }

        if (difference < 0f)
        {
            return
                $" <color=#FF5555>({difference:0.##})</color>";
        }

        return " <color=#AAAAAA>(동일)</color>";
    }
}