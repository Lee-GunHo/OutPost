using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 상호작용 대상의 흰색 외곽선 표시.
/// InteractionFocusController가 대상을 처음 잡을 때 자동으로 붙이므로 프리팹에 미리 붙일 필요는 없음
/// (색/두께를 바꾸고 싶은 오브젝트만 직접 붙여서 인스펙터 값 조절).
/// 원본 렌더러마다 같은 메시를 쓰는 자식 렌더러를 만들어 InteractionOutline 셰이더로 그리고, 켜고 끄기만 한다.
/// </summary>
[DisallowMultipleComponent]
public class InteractionHighlight : MonoBehaviour
{
    private const string ShaderResourcePath = "Interaction/InteractionOutline";
    private const string OutlineObjectName = "__InteractionOutline";
    private const int UILayer = 5;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    // 원본 메시 → 법선을 위치별로 평균낸 외곽선용 메시 (큐브처럼 모서리 법선이 갈라진 메시도 테두리가 끊기지 않게)
    private static readonly Dictionary<Mesh, Mesh> outlineMeshCache = new Dictionary<Mesh, Mesh>();
    private static Material outlineMaterial;
    private static bool shaderMissingWarned;

    [SerializeField] private Color outlineColor = Color.white;
    [Tooltip("외곽선 두께 (화면 픽셀)")]
    [SerializeField, Min(0f)] private float outlineWidth = 3f;

    private readonly List<Renderer> sourceRenderers = new List<Renderer>();
    private readonly List<GameObject> outlineObjects = new List<GameObject>();
    private bool isBuilt;
    private bool isHighlighted;

    public bool IsHighlighted => isHighlighted;

    public void SetHighlighted(bool highlighted)
    {
        if (highlighted && !isBuilt)
        {
            Build();
        }

        isHighlighted = highlighted;

        foreach (GameObject outlineObject in outlineObjects)
        {
            if (outlineObject != null)
            {
                outlineObject.SetActive(highlighted);
            }
        }
    }

    /// <summary>
    /// 화면에 보이는 원본 렌더러들을 합친 범위 (F키 안내 위치 계산용). 렌더러가 없으면 false.
    /// </summary>
    public bool TryGetBounds(out Bounds bounds)
    {
        if (!isBuilt)
        {
            CollectSourceRenderers();
        }

        bounds = default;
        bool hasBounds = false;

        foreach (Renderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer == null || !sourceRenderer.enabled || !sourceRenderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                bounds = sourceRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sourceRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private void OnDisable()
    {
        if (isHighlighted)
        {
            SetHighlighted(false);
        }
    }

    private void Build()
    {
        isBuilt = true;
        CollectSourceRenderers();

        Material material = GetOutlineMaterial();

        if (material == null)
            return;

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor(OutlineColorId, outlineColor);
        propertyBlock.SetFloat(OutlineWidthId, outlineWidth);

        foreach (Renderer sourceRenderer in sourceRenderers)
        {
            Renderer outlineRenderer = CreateOutlineRenderer(sourceRenderer, material);

            if (outlineRenderer == null)
                continue;

            outlineRenderer.SetPropertyBlock(propertyBlock);
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outlineRenderer.gameObject.SetActive(false);
            outlineObjects.Add(outlineRenderer.gameObject);
        }
    }

    private void CollectSourceRenderers()
    {
        sourceRenderers.Clear();

        foreach (Renderer candidate in GetComponentsInChildren<Renderer>())
        {
            if (candidate.gameObject.name == OutlineObjectName ||
                candidate.gameObject.layer == UILayer ||
                candidate.GetComponent<TMP_Text>() != null ||
                candidate.isPartOfStaticBatch)
            {
                continue;
            }

            if (candidate is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                sourceRenderers.Add(candidate);
            }
            else if (candidate is MeshRenderer)
            {
                MeshFilter filter = candidate.GetComponent<MeshFilter>();

                if (filter != null && filter.sharedMesh != null)
                {
                    sourceRenderers.Add(candidate);
                }
            }
        }
    }

    private static Renderer CreateOutlineRenderer(Renderer source, Material material)
    {
        GameObject outlineObject = new GameObject(OutlineObjectName);
        outlineObject.layer = source.gameObject.layer;
        outlineObject.transform.SetParent(source.transform, false);

        if (source is SkinnedMeshRenderer skinnedSource)
        {
            Mesh mesh = GetOutlineMesh(skinnedSource.sharedMesh);
            SkinnedMeshRenderer skinned = outlineObject.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = mesh;
            skinned.bones = skinnedSource.bones;
            skinned.rootBone = skinnedSource.rootBone;
            skinned.localBounds = skinnedSource.localBounds;
            skinned.quality = skinnedSource.quality;
            skinned.updateWhenOffscreen = skinnedSource.updateWhenOffscreen;
            skinned.sharedMaterials = RepeatMaterial(material, mesh.subMeshCount);
            return skinned;
        }

        Mesh sourceMesh = source.GetComponent<MeshFilter>().sharedMesh;
        Mesh outlineMesh = GetOutlineMesh(sourceMesh);
        outlineObject.AddComponent<MeshFilter>().sharedMesh = outlineMesh;

        MeshRenderer meshRenderer = outlineObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = RepeatMaterial(material, outlineMesh.subMeshCount);
        return meshRenderer;
    }

    private static Material[] RepeatMaterial(Material material, int count)
    {
        Material[] materials = new Material[Mathf.Max(1, count)];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }

        return materials;
    }

    private static Material GetOutlineMaterial()
    {
        if (outlineMaterial != null)
            return outlineMaterial;

        Shader shader = Resources.Load<Shader>(ShaderResourcePath);

        if (shader == null)
        {
            shader = Shader.Find("OutPost/InteractionOutline");
        }

        if (shader == null)
        {
            if (!shaderMissingWarned)
            {
                shaderMissingWarned = true;
                Debug.LogWarning("Resources/" + ShaderResourcePath + " 셰이더를 찾지 못해 상호작용 외곽선을 표시하지 않습니다.");
            }

            return null;
        }

        outlineMaterial = new Material(shader)
        {
            name = "InteractionOutline (Runtime)",
            hideFlags = HideFlags.DontSave
        };

        return outlineMaterial;
    }

    private static Mesh GetOutlineMesh(Mesh source)
    {
        if (outlineMeshCache.TryGetValue(source, out Mesh cached) && cached != null)
            return cached;

        // Read/Write가 꺼진 임포트 메시는 정점을 읽을 수 없으므로 원본 그대로 사용 (모서리에서 테두리가 살짝 끊길 수 있음)
        if (!source.isReadable)
        {
            outlineMeshCache[source] = source;
            return source;
        }

        Vector3[] vertices = source.vertices;
        Vector3[] normals = source.normals;

        if (normals == null || normals.Length != vertices.Length)
        {
            outlineMeshCache[source] = source;
            return source;
        }

        Dictionary<Vector3, Vector3> normalSumByPosition = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            normalSumByPosition.TryGetValue(vertices[i], out Vector3 sum);
            normalSumByPosition[vertices[i]] = sum + normals[i];
        }

        Vector3[] smoothedNormals = new Vector3[normals.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 smoothed = normalSumByPosition[vertices[i]];
            smoothedNormals[i] = smoothed.sqrMagnitude > 1e-8f ? smoothed.normalized : normals[i];
        }

        Mesh outlineMesh = Instantiate(source);
        outlineMesh.name = source.name + "_Outline";
        outlineMesh.normals = smoothedNormals;
        outlineMesh.hideFlags = HideFlags.DontSave;

        outlineMeshCache[source] = outlineMesh;
        return outlineMesh;
    }
}
