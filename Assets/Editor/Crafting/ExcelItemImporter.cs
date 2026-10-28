using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 프로젝트 루트의 아이템.xlsx를 기준으로 아이템/레시피/제작대를 생성·갱신.
/// 엑셀을 고친 뒤 메뉴를 다시 실행하면 같은 에셋을 덮어쓴다(여러 번 실행해도 안전).
///
/// - Item ID/스탯/설명/최대 스택 → ItemData (아이콘, 프리팹 연결은 유지)
/// - Recipe 열 ("Potato1,Wheat1") → CraftingRecipe
/// - Craftingbox Type 열: Normal = 손 제작, Wood/Copper/Silver/Gold = 해당 등급 제작대, Pot = 항아리
///   (상위 제작대는 lowerTierTable 링크로 하위 등급 레시피 화면을 화살표로 오감)
/// - 제작대(나무/구리/은/금) + 항아리 설치 프리팹 생성 후 ItemData.placeablePrefab에 연결
/// </summary>
public static class ExcelItemImporter
{
    private const string ExcelFileName = "아이템.xlsx";
    private const string NewItemFolder = "Assets/Item";
    private const string RecipeFolder = "Assets/Script/crafting/Data/Recipes";
    private const string StationPrefabFolder = "Assets/Prefab/Crafting/Stations";
    private const string StationMaterialFolder = "Assets/Prefab/Crafting/Stations/Materials";
    private const string DroppedItemPrefabPath = "Assets/Prefab/DropItem/Prefab_DroppedItem_Generic.prefab";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    // 제작대 순서. 앞쪽 제작대의 레시피는 뒤쪽(상위) 제작대에서도 제작 가능
    private static readonly string[] TableTiers = { "Wood", "Copper", "Silver", "Gold" };
    private const string HandStation = "normal";
    private const string PotStation = "pot";

    private static readonly Dictionary<string, Color> StationFallbackColors = new Dictionary<string, Color>
    {
        { "wood", new Color(0.55f, 0.36f, 0.2f) },
        { "copper", new Color(0.78f, 0.45f, 0.25f) },
        { "silver", new Color(0.75f, 0.77f, 0.8f) },
        { "gold", new Color(0.95f, 0.78f, 0.25f) },
        { "pot", new Color(0.5f, 0.5f, 0.5f) },
    };

    private class ItemRow
    {
        public int Id;
        public string Name;
        public Dictionary<string, string> Cells;
        public ItemData Item;
        public bool IsNew;

        public string Get(string header)
        {
            return Cells.TryGetValue(header, out string value) ? value : null;
        }
    }

    /// <summary>
    /// 커맨드라인 배치 모드용: Unity.exe -batchmode -quit -projectPath . -executeMethod ExcelItemImporter.ImportBatch
    /// </summary>
    public static void ImportBatch()
    {
        try
        {
            Import();
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Crafting/Import Items From Excel")]
    public static void Import()
    {
        string excelPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ExcelFileName);

        if (!File.Exists(excelPath))
        {
            EditorUtility.DisplayDialog("엑셀 임포트", $"{excelPath} 파일이 없습니다.", "확인");
            return;
        }

        List<ItemRow> rows = ReadRows(excelPath);
        List<string> warnings = new List<string>();

        MatchOrCreateItems(rows);
        foreach (ItemRow row in rows)
            ApplyItemFields(row, rows, warnings);

        AssetDatabase.SaveAssets();

        Dictionary<ItemRow, CraftingRecipe> recipes = CreateRecipes(rows, warnings);

        List<CraftingRecipe> handRecipes = new List<CraftingRecipe>();
        List<CraftingRecipe> potRecipes = new List<CraftingRecipe>();
        List<CraftingRecipe>[] tableRecipes = TableTiers.Select(_ => new List<CraftingRecipe>()).ToArray();
        AssignRecipesToStations(recipes, handRecipes, potRecipes, tableRecipes, warnings);

        List<ItemData> stationItems = new List<ItemData>();
        CraftingTableModel lowerTier = null;
        for (int i = 0; i < TableTiers.Length; i++)
        {
            // 나무부터 만들어서, 각 제작대가 바로 아래 등급 프리팹을 lowerTierTable로 가리키게 함
            ItemData item = BuildStation(rows, TableTiers[i] + "craftingbox", TableTiers[i].ToLowerInvariant(),
                TableTiers[i] + " 제작대", tableRecipes[i], lowerTier, warnings);
            if (item == null)
                continue;

            stationItems.Add(item);
            lowerTier = item.placeablePrefab.GetComponent<CraftingTableModel>();
        }

        ItemData pot = BuildStation(rows, "pot", "pot", "항아리", potRecipes, null, warnings);
        if (pot != null)
            stationItems.Add(pot);

        RegisterInItemDatabases(rows.Select(r => r.Item));
        CheckDuplicateIds(warnings);
        // 제작대뿐 아니라 상자 등 프리팹이 있는 모든 설치 아이템을 복원 목록에 등록
        List<ItemData> placeableItems = rows
            .Select(r => r.Item)
            .Where(item => item.toolType == ToolType.Placeable && item.placeablePrefab != null)
            .ToList();
        UpdateGameScene(handRecipes, tableRecipes[0], placeableItems, warnings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary =
            $"아이템 {rows.Count}개, 레시피 {recipes.Count}개 처리\n" +
            $"손 제작 {handRecipes.Count} / 항아리 {potRecipes.Count} / " +
            string.Join(" / ", TableTiers.Select((t, i) => $"{t} {tableRecipes[i].Count}"));

        foreach (string warning in warnings)
            Debug.LogWarning("[ExcelItemImporter] " + warning);

        Debug.Log("[ExcelItemImporter] " + summary);
        EditorUtility.DisplayDialog("엑셀 임포트 완료",
            summary + (warnings.Count > 0 ? $"\n\n경고 {warnings.Count}개 — Console 확인" : ""), "확인");
    }

    // ---------------------------------------------------------------- 엑셀

    private static List<ItemRow> ReadRows(string excelPath)
    {
        List<ItemRow> rows = new List<ItemRow>();

        foreach (Dictionary<string, string> cells in XlsxSheetReader.ReadFirstSheet(excelPath))
        {
            // 이름이 없는 행은 빈 ID 슬롯(예약 번호)
            if (!cells.TryGetValue("itemname", out string name) || string.IsNullOrWhiteSpace(name))
                continue;

            if (!cells.TryGetValue("itemid", out string idText) || !int.TryParse(idText, out int id))
                continue;

            rows.Add(new ItemRow { Id = id, Name = name.Trim(), Cells = cells });
        }

        return rows;
    }

    // ---------------------------------------------------------------- ItemData

    /// <summary>
    /// 1차: 이름으로 기존 에셋 매칭("Copper Armor" ↔ CopperArmor).
    /// 2차: 이름이 바뀐 행은 아직 매칭 안 된 에셋 중 같은 ID로 매칭(Wood Crafting Box ↔ CraftingBox).
    /// 둘 다 없으면 새로 생성.
    /// </summary>
    private static void MatchOrCreateItems(List<ItemRow> rows)
    {
        List<ItemData> allItems = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList();

        HashSet<ItemData> claimed = new HashSet<ItemData>();

        foreach (ItemRow row in rows)
        {
            string key = XlsxSheetReader.Normalize(row.Name);
            row.Item = allItems.FirstOrDefault(item => !claimed.Contains(item) &&
                (XlsxSheetReader.Normalize(item.itemName) == key || XlsxSheetReader.Normalize(item.name) == key));

            if (row.Item != null)
                claimed.Add(row.Item);
        }

        foreach (ItemRow row in rows.Where(r => r.Item == null))
        {
            row.Item = allItems.FirstOrDefault(item => !claimed.Contains(item) && item.itemID == row.Id);

            if (row.Item != null)
                claimed.Add(row.Item);
        }

        foreach (ItemRow row in rows.Where(r => r.Item == null))
        {
            ItemData created = ScriptableObject.CreateInstance<ItemData>();
            string folder = GetFolderForType(row.Get("itemtype"));
            EnsureFolder(folder);
            AssetDatabase.CreateAsset(created, $"{folder}/{ToAssetName(row.Name)}.asset");
            row.Item = created;
            row.IsNew = true;
        }
    }

    private static void ApplyItemFields(ItemRow row, List<ItemRow> rows, List<string> warnings)
    {
        ItemData item = row.Item;
        bool isNew = row.IsNew;
        string type = XlsxSheetReader.Normalize(row.Get("itemtype"));

        item.itemID = row.Id;
        item.itemName = row.Name;

        if (TryMapItemType(type, out ItemType itemType))
            item.itemType = itemType;
        else
            warnings.Add($"{row.Name}: 알 수 없는 Itemtype '{row.Get("itemtype")}'");

        if (type == "sword")
            item.toolType = ToolType.Sword;
        else if (type == "pick")
            item.toolType = ToolType.Pickaxe;
        else if (type == "hoe")
            item.toolType = ToolType.Hoe; // 밭 갈기 (FarmManager)

        if (System.Enum.TryParse(row.Get("itemgrade"), true, out ItemGrade grade))
            item.itemGrade = grade;

        item.hpBonus = ParseInt(row.Get("hpbonus"), item.hpBonus);
        item.attackBonus = ParseInt(row.Get("attackbonus"), item.attackBonus);
        item.defenseBonus = ParseInt(row.Get("defensebonus"), item.defenseBonus);
        item.moveSpeedBonus = ParseFloat(row.Get("movespeedbonus"), item.moveSpeedBonus);
        item.pickPower = ParseInt(row.Get("pickstats"), item.pickPower);
        item.attackRecoveryDuration = ParseFloat(row.Get("weaponrecoveryduration"), item.attackRecoveryDuration);
        // 설치 높이는 프리팹마다 맞춰둔 값(흙 0.5 등)이 있어서, 엑셀 값은 새로 만든 아이템에만 적용
        if (isNew)
            item.placeableYOffset = ParseFloat(row.Get("placeableyoffset"), item.placeableYOffset);

        item.maxStack = ParseInt(row.Get("maxstack"), item.maxStack);
        item.description = row.Get("description") ?? item.description;

        // 씨앗 아이콘이 아직 없으면 작물 아이콘을 임시로 사용 ("Carrot Seed" → Carrot)
        if (item.icon == null && type == "food" && row.Name.EndsWith(" Seed"))
        {
            string cropKey = XlsxSheetReader.Normalize(row.Name.Substring(0, row.Name.Length - " Seed".Length));
            ItemRow crop = rows.FirstOrDefault(r => XlsxSheetReader.Normalize(r.Name) == cropKey);
            if (crop != null)
                item.icon = crop.Item.icon;
        }

        EditorUtility.SetDirty(item);
    }

    private static bool TryMapItemType(string type, out ItemType itemType)
    {
        switch (type)
        {
            case "material": itemType = ItemType.Material; return true;
            case "armor": itemType = ItemType.Armor; return true;
            case "hat": itemType = ItemType.Head; return true;
            case "shoes": itemType = ItemType.Shoes; return true;
            case "ring": itemType = ItemType.Ring; return true;
            case "necklace": itemType = ItemType.Necklace; return true;
            case "sword":
            case "bow":
            case "pick":
            case "hoe": itemType = ItemType.Weapon; return true;
            case "food": itemType = ItemType.Food; return true;
            case "consumable": itemType = ItemType.Consumable; return true;
            default: itemType = default; return false;
        }
    }

    private static string GetFolderForType(string excelType)
    {
        switch (XlsxSheetReader.Normalize(excelType))
        {
            case "armor":
            case "hat":
            case "shoes":
            case "ring":
            case "necklace": return NewItemFolder + "/Clothes";
            case "sword":
            case "bow":
            case "pick":
            case "hoe": return NewItemFolder + "/Weapons";
            default: return NewItemFolder;
        }
    }

    // ---------------------------------------------------------------- 레시피

    private static readonly Regex IngredientPattern = new Regex(@"^(.*?)\s*(\d+)$");

    private static Dictionary<ItemRow, CraftingRecipe> CreateRecipes(List<ItemRow> rows, List<string> warnings)
    {
        EnsureFolder(RecipeFolder);
        Dictionary<ItemRow, CraftingRecipe> recipes = new Dictionary<ItemRow, CraftingRecipe>();

        foreach (ItemRow row in rows)
        {
            string recipeText = row.Get("recipe");
            if (string.IsNullOrWhiteSpace(recipeText))
                continue;

            List<(ItemData item, int amount)> ingredients = ParseIngredients(recipeText, rows, row.Name, warnings);
            if (ingredients.Count == 0)
                continue;

            string path = $"{RecipeFolder}/{ToAssetName(row.Name)}Recipe.asset";
            CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);

            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            SerializedObject serialized = new SerializedObject(recipe);
            serialized.FindProperty("recipeName").stringValue = row.Get("description") ?? row.Name;
            serialized.FindProperty("description").stringValue = row.Get("description") ?? string.Empty;
            serialized.FindProperty("resultItem").objectReferenceValue = row.Item;
            serialized.FindProperty("resultAmount").intValue = 1;

            SerializedProperty list = serialized.FindProperty("ingredients");
            list.arraySize = ingredients.Count;
            for (int i = 0; i < ingredients.Count; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ingredients[i].item;
                element.FindPropertyRelative("amount").intValue = ingredients[i].amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            recipes[row] = recipe;
        }

        AssetDatabase.SaveAssets();
        return recipes;
    }

    // "Potato1,Wheat1" / "Gold 1" → (아이템, 개수) 목록
    private static List<(ItemData, int)> ParseIngredients(
        string recipeText, List<ItemRow> rows, string resultName, List<string> warnings)
    {
        List<(ItemData, int)> result = new List<(ItemData, int)>();

        foreach (string token in recipeText.Split(','))
        {
            string trimmed = token.Trim();
            if (trimmed.Length == 0)
                continue;

            Match match = IngredientPattern.Match(trimmed);
            string name = match.Success ? match.Groups[1].Value : trimmed;
            int amount = match.Success ? int.Parse(match.Groups[2].Value) : 1;

            string key = XlsxSheetReader.Normalize(name);
            ItemRow ingredient = rows.FirstOrDefault(r => XlsxSheetReader.Normalize(r.Name) == key);

            if (ingredient == null)
            {
                warnings.Add($"{resultName}: 레시피 재료 '{name}'를 아이템 목록에서 찾지 못함");
                continue;
            }

            result.Add((ingredient.Item, amount));
        }

        return result;
    }

    private static void AssignRecipesToStations(
        Dictionary<ItemRow, CraftingRecipe> recipes,
        List<CraftingRecipe> handRecipes,
        List<CraftingRecipe> potRecipes,
        List<CraftingRecipe>[] tableRecipes,
        List<string> warnings)
    {
        foreach (KeyValuePair<ItemRow, CraftingRecipe> pair in recipes)
        {
            string station = XlsxSheetReader.Normalize(pair.Key.Get("craftingboxtypy") ?? pair.Key.Get("craftingboxtype"));

            if (station == HandStation)
            {
                handRecipes.Add(pair.Value);
                continue;
            }

            if (station == PotStation)
            {
                potRecipes.Add(pair.Value);
                continue;
            }

            int tier = System.Array.FindIndex(TableTiers, t => t.ToLowerInvariant() == station);
            if (tier < 0)
            {
                warnings.Add($"{pair.Key.Name}: 알 수 없는 Craftingbox Type '{station}' — 어느 제작대에도 넣지 않음");
                continue;
            }

            // 각 제작대에는 자기 등급 레시피만. 하위 등급은 제작 UI의 화살표로 lowerTierTable을 따라가서 제작(코어키퍼 방식)
            tableRecipes[tier].Add(pair.Value);
        }
    }

    // ---------------------------------------------------------------- 제작대 프리팹

    private static ItemData BuildStation(
        List<ItemRow> rows, string itemKey, string colorKey, string tableName,
        List<CraftingRecipe> recipes, CraftingTableModel lowerTier, List<string> warnings)
    {
        ItemRow row = rows.FirstOrDefault(r => XlsxSheetReader.Normalize(r.Name) == XlsxSheetReader.Normalize(itemKey));
        if (row == null)
        {
            warnings.Add($"제작대 아이템 '{itemKey}'가 엑셀에 없어 프리팹을 만들지 않음");
            return null;
        }

        EnsureFolder(StationMaterialFolder);
        string assetName = ToAssetName(row.Name);

        // 큐브 + 아이콘 텍스처 (씬에 있던 기존 Crafting Box와 같은 형태)
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = assetName;
        root.GetComponent<MeshRenderer>().sharedMaterial =
            GetOrCreateStationMaterial(assetName, row.Item.icon, StationFallbackColors[colorKey]);

        BoxCollider interactionTrigger = root.AddComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = new Vector3(2f, 1.5f, 2f);

        CraftingTableModel model = root.AddComponent<CraftingTableModel>();
        CraftingTableInteractable interactable = root.AddComponent<CraftingTableInteractable>();

        SerializedObject modelSerialized = new SerializedObject(model);
        modelSerialized.FindProperty("tableName").stringValue =
            string.IsNullOrWhiteSpace(row.Item.description) ? tableName : row.Item.description;
        SetObjectList(modelSerialized.FindProperty("tableRecipes"), recipes);
        modelSerialized.FindProperty("lowerTierTable").objectReferenceValue = lowerTier;
        modelSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject interactableSerialized = new SerializedObject(interactable);
        interactableSerialized.FindProperty("craftingTableModel").objectReferenceValue = model;
        interactableSerialized.ApplyModifiedPropertiesWithoutUndo();

        AddBreakable(root, row.Item);

        EnsureFolder(StationPrefabFolder);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{StationPrefabFolder}/{assetName}.prefab");
        Object.DestroyImmediate(root);

        row.Item.toolType = ToolType.Placeable;
        // 큐브(높이 1, 가운데 기준점)가 맵 블록처럼 바닥(y=0)에 붙도록. 프리팹 모양은 임포터가 만들기 때문에 엑셀 값보다 우선
        row.Item.placeableYOffset = 0.5f;
        row.Item.placeablePrefab = prefab;
        EditorUtility.SetDirty(row.Item);
        return row.Item;
    }

    /// <summary>
    /// 벽처럼 부술 수 있게 하고, 부서지면 제작대 아이템 1개를 돌려줌.
    /// 씬에 미리 놓인 제작대는 파괴 상태를 저장할 수 없어 직접 설치한 것만 부서지게 함.
    /// </summary>
    private static void AddBreakable(GameObject root, ItemData stationItem)
    {
        BreakableWallModel wallModel = root.AddComponent<BreakableWallModel>();
        root.AddComponent<BreakableWallView>();
        BreakableWall wall = root.AddComponent<BreakableWall>();
        PlacedBlockItemDrop itemDrop = root.AddComponent<PlacedBlockItemDrop>();

        // 플레이어 공격 판정은 몸 중앙(y≈1) 위 0.8 높이로 나가서 높이 1짜리 큐브 위를 지나감.
        // 맵 벽과 같은 1x2x1 크기(바닥~y=2)의 보이지 않는 타격 판정을 붙여 벽처럼 맞게 함.
        // 트리거라 이동/설치 판정에는 영향 없고, 자식 오브젝트라 상호작용 트리거 이벤트와도 섞이지 않음
        GameObject hitBox = new GameObject("HitBox");
        hitBox.transform.SetParent(root.transform, false);
        BoxCollider hitCollider = hitBox.AddComponent<BoxCollider>();
        hitCollider.isTrigger = true;
        hitCollider.center = new Vector3(0f, 0.5f, 0f);
        hitCollider.size = new Vector3(1f, 2f, 1f);

        SerializedObject modelSerialized = new SerializedObject(wallModel);
        modelSerialized.FindProperty("minDropCount").intValue = 0;
        modelSerialized.FindProperty("maxDropCount").intValue = 0;
        modelSerialized.FindProperty("showSaveDebugLog").boolValue = false;
        modelSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject wallSerialized = new SerializedObject(wall);
        wallSerialized.FindProperty("onlyBreakIfPlayerPlaced").boolValue = true;
        wallSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject droppedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DroppedItemPrefabPath);
        SerializedObject dropSerialized = new SerializedObject(itemDrop);
        dropSerialized.FindProperty("item").objectReferenceValue = stationItem;
        dropSerialized.FindProperty("droppedItemPrefab").objectReferenceValue =
            droppedPrefab != null ? droppedPrefab.GetComponent<DroppedItemPresenter>() : null;
        dropSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material GetOrCreateStationMaterial(string assetName, Sprite icon, Color fallbackColor)
    {
        string path = $"{StationMaterialFolder}/{assetName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (icon != null)
        {
            material.mainTexture = icon.texture;
            material.color = Color.white;
        }
        else
        {
            material.color = fallbackColor;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------------------------------------------------------------- 등록 / 씬

    private static void RegisterInItemDatabases(IEnumerable<ItemData> items)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDatabase", new[] { "Assets" }))
        {
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(guid));
            SerializedObject serialized = new SerializedObject(database);
            AddMissing(serialized.FindProperty("items"), items);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CheckDuplicateIds(List<string> warnings)
    {
        IEnumerable<IGrouping<int, ItemData>> duplicates = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .GroupBy(item => item.itemID)
            .Where(group => group.Count() > 1);

        foreach (IGrouping<int, ItemData> group in duplicates)
            warnings.Add($"중복 itemID {group.Key}: {string.Join(", ", group.Select(i => i.name))}");
    }

    /// <summary>
    /// GameScene의 손 제작 목록, 설치 블록 복구 목록, 미리 배치된 제작대를 갱신.
    /// 씬이 에디터에 열려 있고 저장 안 된 변경이 있으면 저장은 사용자에게 맡김.
    /// </summary>
    private static void UpdateGameScene(
        List<CraftingRecipe> handRecipes, List<CraftingRecipe> woodTableRecipes,
        List<ItemData> stationItems, List<string> warnings)
    {
        Scene scene = SceneManager.GetSceneByPath(GameScenePath);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        bool wasDirty = wasOpen && scene.isDirty;

        if (!wasOpen)
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);

        foreach (CraftingPresenter presenter in FindInScene<CraftingPresenter>(scene))
        {
            SerializedObject serialized = new SerializedObject(presenter);
            SetObjectList(serialized.FindProperty("handRecipes"), handRecipes);
            serialized.ApplyModifiedProperties();
        }

        foreach (SeedMapModel seedMap in FindInScene<SeedMapModel>(scene))
        {
            SerializedObject serialized = new SerializedObject(seedMap);
            AddMissing(serialized.FindProperty("placeableBlockItems"), stationItems);
            serialized.ApplyModifiedProperties();
        }

        // 프리팹이 아니라 씬에 직접 놓인 제작대 = 시작용 나무 제작대
        foreach (CraftingTableModel table in FindInScene<CraftingTableModel>(scene))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(table))
                continue;

            SerializedObject serialized = new SerializedObject(table);
            SetObjectList(serialized.FindProperty("tableRecipes"), woodTableRecipes);
            serialized.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);

        if (wasDirty)
        {
            warnings.Add("GameScene에 저장 안 된 변경이 있어 자동 저장하지 않았습니다. 씬을 직접 저장(Ctrl+S)해 주세요.");
        }
        else
        {
            EditorSceneManager.SaveScene(scene);
        }

        if (!wasOpen)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    // ---------------------------------------------------------------- 유틸

    private static void SetObjectList<T>(SerializedProperty list, List<T> values) where T : Object
    {
        list.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void AddMissing<T>(SerializedProperty list, IEnumerable<T> values) where T : Object
    {
        HashSet<Object> existing = new HashSet<Object>();
        for (int i = 0; i < list.arraySize; i++)
            existing.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);

        foreach (T value in values)
        {
            if (value == null || existing.Contains(value))
                continue;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
            existing.Add(value);
        }
    }

    private static int ParseInt(string text, int fallback)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? Mathf.RoundToInt(value)
            : fallback;
    }

    private static float ParseFloat(string text, float fallback)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : fallback;
    }

    // "Copper Crafting Box" → "CopperCraftingBox", "C.P Soup" → "CPSoup"
    private static string ToAssetName(string name)
    {
        return new string(name.Where(char.IsLetterOrDigit).ToArray());
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
