using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [SerializeField] private Transform player;
    [SerializeField] private string saveFileName = "SaveFile.json";

    //0721 건호 추가
    [Header("Inventory Save")]
    [SerializeField] private InventoryModel inventoryModel;
    [SerializeField] private HotbarModel hotbarModel;
    [SerializeField] private EquipmentModel equipmentModel;
    
    [Header("Inventory Load")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private InventoryPresenter inventoryPresenter;
    [SerializeField] private PlayerModel playerModel;

    private void Awake()
    {
        Instance = this;
    }

    [ContextMenu("Save Game")]

    public void SaveGame()
    {
        if (player == null)
        {
            Debug.LogError("[SAVE] Player가 연결되지 않았습니다.");
            return;
        }

        SaveData data = new SaveData();

        data.playerX = player.position.x;
        data.playerY = player.position.y;
        data.playerZ = player.position.z;

        data.playTime = Time.time;

        //0721 건호 추가
        SaveInventoryItems(data);
        SaveHotbarItems(data);
        SaveEquipmentItems(data);

        string json = JsonUtility.ToJson(data, true);

        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        File.WriteAllText(path, json);

        Debug.Log("[SAVE] 저장 완료: " + path);
    }

    //0721 건호 추가
    [ContextMenu("Load Game")]
    public void LoadGame()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            saveFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                "[LOAD] 저장 파일이 없습니다: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogError(
                "[LOAD] 저장 데이터를 읽지 못했습니다.");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[LOAD] ItemDatabase가 연결되지 않았습니다.");
            return;
        }

        RemoveCurrentEquipmentStats();

        inventoryModel.ClearAllItems();
        hotbarModel.ClearAllItems();
        equipmentModel.ClearAllEquipment();

        LoadPlayerData(data);
        LoadInventoryItems(data);
        LoadHotbarItems(data);
        LoadEquipmentItems(data);

        hotbarModel.SelectSlot(data.selectedHotbarIndex);

        if (inventoryPresenter != null)
            inventoryPresenter.RefreshAfterLoad();

        Debug.Log("[LOAD] 불러오기 완료: " + path);
    }
    private void RemoveCurrentEquipmentStats()
    {
        if (equipmentModel == null ||
            playerModel == null)
        {
            return;
        }

        ItemType[] equipmentTypes =
        {
        ItemType.Weapon,
        ItemType.Head,
        ItemType.Armor,
        ItemType.Shoes,
        ItemType.Ring,
        ItemType.Necklace
    };

        foreach (ItemType itemType in equipmentTypes)
        {
            ItemStack stack =
                equipmentModel.GetEquippedItem(itemType);

            if (stack != null && stack.item != null)
            {
                playerModel.RemoveEquipmentStats(
                    stack.item);
            }
        }
    }

    private void LoadPlayerData(SaveData data)
    {
        if (player == null)
            return;

        player.position = new Vector3(
            data.playerX,
            data.playerY,
            data.playerZ);
    }

    private void LoadInventoryItems(SaveData data)
    {
        if (data.inventoryItems == null)
            return;

        foreach (ItemSlotSaveData slotData
                 in data.inventoryItems)
        {
            ItemData item =
                itemDatabase.GetItemByID(slotData.itemID);

            if (item == null)
                continue;

            ItemStack stack =
                new ItemStack(item, slotData.amount);

            inventoryModel.SetItemAt(
                slotData.slotIndex,
                stack);
        }
    }

    private void LoadHotbarItems(SaveData data)
    {
        if (data.hotbarItems == null)
            return;

        foreach (ItemSlotSaveData slotData
                 in data.hotbarItems)
        {
            ItemData item =
                itemDatabase.GetItemByID(slotData.itemID);

            if (item == null)
                continue;

            ItemStack stack =
                new ItemStack(item, slotData.amount);

            hotbarModel.SetItem(
                slotData.slotIndex,
                stack);
        }
    }
    private void LoadEquipmentItems(SaveData data)
    {
        if (data.equipmentItems == null)
            return;

        foreach (EquipmentSlotSaveData equipmentData
                 in data.equipmentItems)
        {
            ItemData item =
                itemDatabase.GetItemByID(
                    equipmentData.itemID);

            if (item == null)
                continue;

            ItemStack stack =
                new ItemStack(
                    item,
                    equipmentData.amount);

            equipmentModel.Equip(
                equipmentData.itemType,
                stack);

            if (playerModel != null)
                playerModel.AddEquipmentStats(item);
        }
    }
    private void SaveInventoryItems(SaveData data)
    {
        if (inventoryModel == null)
        {
            Debug.LogWarning(
                "[SAVE] InventoryModel이 연결되지 않았습니다.");
            return;
        }

        for (int i = 0; i < inventoryModel.Items.Count; i++)
        {
            ItemStack stack = inventoryModel.Items[i];

            if (stack == null || stack.item == null)
                continue;

            data.inventoryItems.Add(
                new ItemSlotSaveData(
                    i,
                    stack.item.itemID,
                    stack.amount));
        }
    }

    private void SaveHotbarItems(SaveData data)
    {
        if (hotbarModel == null)
        {
            Debug.LogWarning(
                "[SAVE] HotbarModel이 연결되지 않았습니다.");
            return;
        }

        for (int i = 0; i < hotbarModel.Items.Count; i++)
        {
            ItemStack stack = hotbarModel.Items[i];

            if (stack == null || stack.item == null)
                continue;

            data.hotbarItems.Add(
                new ItemSlotSaveData(
                    i,
                    stack.item.itemID,
                    stack.amount));
        }

        data.selectedHotbarIndex =
            hotbarModel.SelectedIndex;
    }

    private void SaveEquipmentItems(SaveData data)
    {
        if (equipmentModel == null)
        {
            Debug.LogWarning(
                "[SAVE] EquipmentModel이 연결되지 않았습니다.");
            return;
        }

        ItemType[] equipmentTypes =
        {
        ItemType.Weapon,
        ItemType.Head,
        ItemType.Armor,
        ItemType.Shoes,
        ItemType.Ring,
        ItemType.Necklace
    };

        foreach (ItemType itemType in equipmentTypes)
        {
            ItemStack stack =
                equipmentModel.GetEquippedItem(itemType);

            if (stack == null || stack.item == null)
                continue;

            data.equipmentItems.Add(
                new EquipmentSlotSaveData(
                    itemType,
                    stack.item.itemID,
                    stack.amount));
        }
    }

}



[System.Serializable]
public class SaveData
{
    public float playerX;
    public float playerY;
    public float playerZ;

    public float playTime;
    //0721 건호 추가
    // 인벤토리 슬롯 저장
    public List<ItemSlotSaveData> inventoryItems = new();

    // 핫바 슬롯 저장
    public List<ItemSlotSaveData> hotbarItems = new();

    // 장착 장비 저장
    public List<EquipmentSlotSaveData> equipmentItems = new();

    // 현재 선택된 핫바 번호
    public int selectedHotbarIndex;
}


//0721 건호 추가
[System.Serializable]
public class ItemSlotSaveData
{
    public int slotIndex;
    public int itemID;
    public int amount;

    public ItemSlotSaveData(
        int slotIndex,
        int itemID,
        int amount)
    {
        this.slotIndex = slotIndex;
        this.itemID = itemID;
        this.amount = amount;
    }
}

[System.Serializable]
public class EquipmentSlotSaveData
{
    public ItemType itemType;
    public int itemID;
    public int amount;

    public EquipmentSlotSaveData(
        ItemType itemType,
        int itemID,
        int amount)
    {
        this.itemType = itemType;
        this.itemID = itemID;
        this.amount = amount;
    }
}