using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 창고의 아이템 데이터만 관리
/// ChestSaveManager와 연결되어 게임을 종료해도 슬롯 상태를 유지
/// </summary>
public class ChestModel : MonoBehaviour
{
    public event Action OnChestChanged;

    [Header("영구 저장 ID")]
    [Tooltip("모든 창고가 서로 다른 값을 가져야 합니다. 한번 사용한 ID는 바꾸지 마세요.")]
    [SerializeField] private string persistentChestId;

    [Header("창고 설정")]
    [SerializeField, Min(1)] private int maxSlots = 40;

    [Header("최초 1회 시작 아이템")]
    [Tooltip("저장 데이터가 아직 없는 새 창고에만 적용됩니다.")]
    [SerializeField]
    private List<ItemStack> startingItems =
        new List<ItemStack>();

    public List<ItemStack> Items { get; private set; } =
        new List<ItemStack>();

    public string PersistentChestId => persistentChestId;
    public int SlotCount => Items.Count;

    private ChestSaveManager saveManager;
    private bool isInitialized;

    private void Awake()
    {
        CreateEmptySlots();
    }

    private void Start()
    {
        InitializePersistence();
    }

    private void OnDestroy()
    {
        if (!isInitialized)
            return;

        // 모든 변경은 발생 즉시 저장되므로 파괴 시에는 구독만 해제
        // 애플리케이션 종료 중 Manager가 먼저 파괴된 경우 재생성되는 문제도 방지
        OnChestChanged -= SaveCurrentState;
    }

    private void OnValidate()
    {
        maxSlots = Mathf.Max(1, maxSlots);
    }

    /// <summary>
    /// 절차 생성 창고는 생성 직후 Start 이전에 이 함수를 호출하여
    /// 월드 좌표 기반 ID 등을 직접 지정할 수 있음.
    /// </summary>
    public void SetPersistentChestId(string chestId)
    {
        if (string.IsNullOrWhiteSpace(chestId))
        {
            Debug.LogError("Chest ID를 빈 값으로 설정할 수 없습니다.");
            return;
        }

        if (isInitialized)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 이미 저장 시스템이 초기화된 뒤에는 " +
                "Chest ID를 변경하지 않는 것이 안전합니다."
            );
        }

        persistentChestId = chestId.Trim();
    }

    [ContextMenu("Generate New Chest ID")]
    private void GenerateNewChestId()
    {
        persistentChestId = Guid.NewGuid().ToString("N");
        Debug.Log($"새 Chest ID 생성 완료: {persistentChestId}", this);
    }

    [ContextMenu("Delete This Chest Save")]
    public void DeleteThisChestSave()
    {
        EnsureChestId();

        ChestSaveManager manager = ChestSaveManager.GetOrCreate();
        bool deleted = manager.DeleteChestSave(persistentChestId);

        if (!deleted)
        {
            Debug.Log("삭제할 기존 창고 저장 데이터가 없습니다.", this);
        }
    }

    public ItemStack GetItem(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
            return null;

        return Items[slotIndex];
    }

    public void SetItemAt(int slotIndex, ItemStack itemStack)
    {
        if (!IsValidIndex(slotIndex))
            return;

        Items[slotIndex] = CloneStack(itemStack);
        OnChestChanged?.Invoke();
    }

    public int AddItemAndGetRemaining(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return amount;

        int remainingAmount = amount;
        int maxStack = Mathf.Max(1, item.maxStack);
        bool changed = false;

        for (int i = 0; i < Items.Count && remainingAmount > 0; i++)
        {
            ItemStack stack = Items[i];

            if (IsEmpty(stack) ||
                stack.item != item ||
                stack.amount >= maxStack)
            {
                continue;
            }

            int addAmount = Mathf.Min(
                maxStack - stack.amount,
                remainingAmount
            );

            stack.amount += addAmount;
            remainingAmount -= addAmount;
            changed = true;
        }

        for (int i = 0; i < Items.Count && remainingAmount > 0; i++)
        {
            if (!IsEmpty(Items[i]))
                continue;

            int addAmount = Mathf.Min(maxStack, remainingAmount);

            Items[i] = new ItemStack(item, addAmount);
            remainingAmount -= addAmount;
            changed = true;
        }

        if (changed)
        {
            OnChestChanged?.Invoke();
        }

        return remainingAmount;
    }

    public int RemoveItemAt(int slotIndex, int amount)
    {
        if (!IsValidIndex(slotIndex) || amount <= 0)
            return 0;

        ItemStack stack = Items[slotIndex];

        if (IsEmpty(stack))
            return 0;

        int removedAmount = Mathf.Min(stack.amount, amount);

        stack.amount -= removedAmount;

        if (stack.amount <= 0)
        {
            Items[slotIndex] = null;
        }

        OnChestChanged?.Invoke();
        return removedAmount;
    }

    public void ForceSave()
    {
        SaveCurrentState();
    }

    private void InitializePersistence()
    {
        if (isInitialized)
            return;

        EnsureChestId();

        saveManager = ChestSaveManager.GetOrCreate();

        if (saveManager == null || !saveManager.IsReady)
        {
            Debug.LogError(
                $"{gameObject.name}: ChestSaveManager에 ItemDatabase가 연결되지 않아 " +
                "창고 영구 저장을 시작하지 않습니다. 기존 저장 파일은 덮어쓰지 않습니다.",
                this
            );

            CreateEmptySlots();
            ApplyStartingItems();
            OnChestChanged?.Invoke();
            return;
        }

        bool hasSavedData = saveManager.TryLoadChest(
            persistentChestId,
            maxSlots,
            out List<ItemStack> loadedItems
        );

        if (hasSavedData)
        {
            Items = loadedItems;
        }
        else
        {
            CreateEmptySlots();
            ApplyStartingItems();
        }

        OnChestChanged += SaveCurrentState;
        isInitialized = true;

        // 새 창고도 즉시 파일에 등록
        SaveCurrentState();

        // Presenter/View가 이미 구독한 경우 현재 상태를 갱신
        // SaveCurrentState는 직접 호출했으므로 저장 이벤트를 다시 호출하지 않음.
    }

    private void SaveCurrentState()
    {
        if (string.IsNullOrWhiteSpace(persistentChestId))
            return;

        if (saveManager == null)
        {
            saveManager = ChestSaveManager.GetOrCreate();
        }

        if (saveManager == null || !saveManager.IsReady)
            return;

        saveManager.SaveChest(
            persistentChestId,
            Items
        );
    }

    private void EnsureChestId()
    {
        if (!string.IsNullOrWhiteSpace(persistentChestId))
            return;

        // 수동 ID가 없을 때 사용하는 안정적인 임시 규칙
        // 정적인 창고는 같은 씬과 같은 위치에 있으면 같은 ID를 사용
        string sceneName = SceneManager.GetActiveScene().name;
        Vector3 position = transform.position;

        int positionX = Mathf.RoundToInt(position.x * 1000f);
        int positionY = Mathf.RoundToInt(position.y * 1000f);
        int positionZ = Mathf.RoundToInt(position.z * 1000f);

        persistentChestId =
            $"{sceneName}_Chest_{positionX}_{positionY}_{positionZ}";

        Debug.LogWarning(
            $"{gameObject.name}: Chest ID가 비어 있어 위치 기반 ID를 자동 생성했습니다. " +
            $"ID={persistentChestId}. 창고가 이동하거나 같은 위치에 여러 개 있다면 " +
            "Inspector 메뉴의 Generate New Chest ID를 사용하세요.",
            this
        );
    }

    private void CreateEmptySlots()
    {
        Items.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            Items.Add(null);
        }
    }

    private void ApplyStartingItems()
    {
        if (startingItems == null)
            return;

        int copyCount = Mathf.Min(startingItems.Count, Items.Count);

        for (int i = 0; i < copyCount; i++)
        {
            ItemStack startingItem = startingItems[i];

            if (IsEmpty(startingItem))
                continue;

            int amount = Mathf.Clamp(
                startingItem.amount,
                1,
                Mathf.Max(1, startingItem.item.maxStack)
            );

            Items[i] = new ItemStack(
                startingItem.item,
                amount
            );
        }
    }

    private bool IsValidIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < Items.Count;
    }

    private static bool IsEmpty(ItemStack itemStack)
    {
        return itemStack == null ||
               itemStack.item == null ||
               itemStack.amount <= 0;
    }

    private static ItemStack CloneStack(ItemStack itemStack)
    {
        if (IsEmpty(itemStack))
            return null;

        return new ItemStack(
            itemStack.item,
            itemStack.amount
        );
    }
}