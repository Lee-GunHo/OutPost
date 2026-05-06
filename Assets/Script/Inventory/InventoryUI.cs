using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public Transform slotParent;
    public InventorySlotUI slotPrefab;

    public GameObject inventoryPanel;
    public KeyCode toggleKey = KeyCode.I;

    private readonly List<InventorySlotUI> slotUIs = new();

    private void Start()
    {
        CreateSlots();
        Refresh();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        bool isOpen = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            Refresh();
        }
    }

    private void CreateSlots()
    {
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        slotUIs.Clear();

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            InventorySlotUI slotUI = Instantiate(slotPrefab, slotParent);
            slotUIs.Add(slotUI);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].Set(inventory.slots[i]);
        }
    }
}
