using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 역할별 NPC 프리팹(Dialogue/Battle/Shop)에 플레이어와 같은 3D 모델을 붙임.
/// 대화/전투 NPC는 모자·옷 색만 바꾼 텍스처(Assets/FBX/NPC), 상점 NPC는 플레이어 텍스처 그대로 + 표지판.
/// 여러 번 실행해도 이전에 만든 NPCVisual/ShopSign을 지우고 다시 만듦.
/// </summary>
public static class NPCVisualBuilder
{
    private const string ModelPath = "Assets/FBX/Monster/Animation/CHA A.fbx";
    private const string ControllerPath = "Assets/Animation/Player/PlayerLocomotion.controller";
    private const string MaterialFolder = "Assets/FBX/NPC";
    private const string VisualName = "NPCVisual";
    private const string SignName = "ShopSign";

    // 게임 씬에 실제로 놓인 플레이어(Variant)의 외형 배치를 그대로 따라감 (플레이어 크기를 바꾸면 메뉴만 다시 실행)
    private const string PlayerPrefabPath = "Assets/Prefab/Character/Player_Main Variant.prefab";
    private const string PlayerVisualName = "PlayerWalkingVisual";

    private static readonly (string prefab, string material, bool sign)[] Targets =
    {
        ("Assets/Prefab/NPC/DialogueNPC_Main.prefab", MaterialFolder + "/NPC_Dialogue.mat", false),
        ("Assets/Prefab/NPC/BattleNPC_Main.prefab", MaterialFolder + "/NPC_Battle.mat", false),
        ("Assets/Prefab/NPC/ShopNPC_Main.prefab", "Assets/FBX/Player/Materials/Player_texture.mat", true),
    };

    [MenuItem("Tools/NPC/Build NPC Visuals")]
    public static void Build()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        if (model == null)
        {
            Debug.LogError("NPC 모델을 찾지 못했습니다: " + ModelPath);
            return;
        }

        if (!TryGetPlayerVisualPlacement(out Vector3 visualPosition, out Vector3 visualScale))
        {
            Debug.LogError("플레이어 외형 배치를 읽지 못했습니다: " + PlayerPrefabPath + " / " + PlayerVisualName);
            return;
        }

        foreach ((string prefabPath, string materialPath, bool withSign) in Targets)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                Debug.LogError("NPC 머티리얼을 찾지 못했습니다: " + materialPath);
                continue;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                BuildVisual(contents, model, controller, material, withSign, visualPosition, visualScale);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log("NPC 외형 적용: " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }

    // 플레이어 루트 스케일까지 곱해서, 스케일 1인 NPC 루트 아래에서 같은 크기로 보이게 함
    private static bool TryGetPlayerVisualPlacement(out Vector3 position, out Vector3 scale)
    {
        position = Vector3.zero;
        scale = Vector3.one;

        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Transform visual = player != null ? player.transform.Find(PlayerVisualName) : null;

        if (visual == null)
            return false;

        Vector3 rootScale = player.transform.localScale;
        position = Vector3.Scale(rootScale, visual.localPosition);
        scale = Vector3.Scale(rootScale, visual.localScale);
        return true;
    }

    private static void BuildVisual(GameObject root, GameObject model, RuntimeAnimatorController controller,
        Material material, bool withSign, Vector3 visualPosition, Vector3 visualScale)
    {
        RemoveChild(root.transform, VisualName);
        RemoveChild(root.transform, SignName);

        // 예전 캡슐 외형은 끄기만 함 (Player_Main과 같은 방식, 콜라이더/스크립트는 그대로)
        MeshRenderer capsuleRenderer = root.GetComponent<MeshRenderer>();
        if (capsuleRenderer != null)
            capsuleRenderer.enabled = false;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        visual.name = VisualName;
        visual.transform.localPosition = visualPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = visualScale;

        SkinnedMeshRenderer body = visual.GetComponentInChildren<SkinnedMeshRenderer>();
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials = RepeatMaterial(material, renderer.sharedMaterials.Length);

        // 플레이어와 같은 컨트롤러: 기본 Idle, 전투 동료는 CompanionPresenter가 IsWalking을 켬
        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        if (withSign && body != null)
            BuildSign(root.transform, body);
    }

    /// <summary>
    /// 몸 앞에 세운 나무 표지판(판자 3장 + 받침대 + 못). 크기/위치는 모델 키 대비 비율(블렌더 시안 기준)이라
    /// 모델 스케일이 바뀌어도 같은 자리에 붙음. 모델 정면은 +Z.
    /// </summary>
    private static void BuildSign(Transform root, SkinnedMeshRenderer body)
    {
        Bounds bounds = GetBodyBounds(root, body);
        float h = bounds.size.y;

        Transform sign = AttachNew(new GameObject(SignName), root).transform;
        sign.localPosition = new Vector3(bounds.center.x, bounds.min.y + h * 0.326f, bounds.center.z + h * 0.194f);

        float width = h * 0.486f;
        float height = h * 0.257f;
        float thickness = h * 0.031f;
        const int planks = 3;
        float plankHeight = height / planks;

        Material[] woods =
        {
            GetOrCreateMaterial("ShopSign_Wood1", new Color(0.62f, 0.40f, 0.22f), 0f),
            GetOrCreateMaterial("ShopSign_Wood2", new Color(0.55f, 0.35f, 0.19f), 0f),
            GetOrCreateMaterial("ShopSign_Wood3", new Color(0.66f, 0.44f, 0.25f), 0f),
        };
        Material postWood = GetOrCreateMaterial("ShopSign_Post", new Color(0.45f, 0.29f, 0.16f), 0f);
        Material nail = GetOrCreateMaterial("ShopSign_Nail", new Color(0.5f, 0.5f, 0.52f), 0.9f);

        for (int i = 0; i < planks; i++)
        {
            float y = -height / 2f + plankHeight * (i + 0.5f);
            CreatePart(PrimitiveType.Cube, "Plank" + i, sign, new Vector3(0f, y, 0f),
                new Vector3(width * (i == 1 ? 0.97f : 1f), plankHeight * 0.94f, thickness), woods[i]);

            foreach (float x in new[] { -1f, 1f })
            {
                CreatePart(PrimitiveType.Sphere, "Nail", sign,
                    new Vector3(x * (width / 2f - h * 0.036f), y, thickness / 2f),
                    Vector3.one * h * 0.02f, nail);
            }
        }

        // 받침대: 판 아래로 내려오고 판 뒤(몸 쪽)에 붙음
        CreatePart(PrimitiveType.Cube, "Post", sign, new Vector3(0f, -h * 0.186f, -h * 0.034f),
            new Vector3(h * 0.043f, h * 0.243f, h * 0.031f), postWood);
    }

    // 렌더러 월드 바운드를 root 기준으로 바꿈. (이 리그는 BakeMesh 결과가 본 스케일 때문에 렌더러 변환과 안 맞아서 쓰지 않음)
    private static Bounds GetBodyBounds(Transform root, SkinnedMeshRenderer body)
    {
        Bounds world = body.bounds;
        Bounds bounds = new Bounds(root.InverseTransformPoint(world.center), Vector3.zero);
        bounds.Encapsulate(root.InverseTransformPoint(world.min));
        bounds.Encapsulate(root.InverseTransformPoint(world.max));
        return bounds;
    }

    private static void CreatePart(PrimitiveType type, string name, Transform parent, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject part = AttachNew(GameObject.CreatePrimitive(type), parent);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // 새 오브젝트는 열린 씬에 생기므로, 프리팹 편집용 씬으로 옮긴 뒤 붙임
    private static GameObject AttachNew(GameObject go, Transform parent)
    {
        SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Material GetOrCreateMaterial(string name, Color color, float metallic)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Directory.CreateDirectory(MaterialFolder);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", metallic > 0f ? 0.6f : 0.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material[] RepeatMaterial(Material material, int count)
    {
        Material[] materials = new Material[Mathf.Max(1, count)];

        for (int i = 0; i < materials.Length; i++)
            materials[i] = material;

        return materials;
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }
}
