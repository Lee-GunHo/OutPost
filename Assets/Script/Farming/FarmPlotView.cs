using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 밭 한 칸의 모양. 갈색 흙판 + 단계별 작물.
/// 작물 모양은 CropData.stagePrefabs가 있으면 그걸 쓰고, 없으면 줄기(+다 자라면 열매)를 큐브로 만든다.
/// 콜라이더가 없어 이동/설치 판정에 영향이 없음(수확은 마우스 칸 기준으로 FarmManager가 처리).
/// </summary>
public class FarmPlotView : MonoBehaviour
{
    private const float SoilHeight = 0.08f;
    private const float SoilSize = 0.94f;

    private static readonly Dictionary<Color, Material> MaterialCache = new Dictionary<Color, Material>();

    private GameObject cropRoot;
    private CropData shownCrop;
    private int shownStage = -1;

    public void Initialize(Color soilColor)
    {
        GameObject soil = CreateBox("Soil", transform,
            new Vector3(0f, SoilHeight * 0.5f, 0f),
            new Vector3(SoilSize, SoilHeight, SoilSize),
            soilColor);

        soil.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    public void Refresh(FarmPlot plot)
    {
        CropData crop = plot.HasCrop ? plot.Crop : null;
        int stage = plot.Stage;

        if (crop == shownCrop && stage == shownStage)
            return;

        shownCrop = crop;
        shownStage = stage;

        if (cropRoot != null)
            Destroy(cropRoot);

        cropRoot = null;

        if (crop == null)
            return;

        cropRoot = new GameObject("Crop");
        cropRoot.transform.SetParent(transform, false);
        cropRoot.transform.localPosition = new Vector3(0f, SoilHeight, 0f);

        if (crop.stagePrefabs != null && stage < crop.stagePrefabs.Length && crop.stagePrefabs[stage] != null)
        {
            Instantiate(crop.stagePrefabs[stage], cropRoot.transform, false);
            return;
        }

        BuildDefaultCrop(crop, stage);
    }

    // 단계가 오를수록 줄기가 굵고 높아지고, 다 자라면 열매 색 덩어리가 붙음
    private void BuildDefaultCrop(CropData crop, int stage)
    {
        int lastStage = Mathf.Max(1, crop.stageCount - 1);
        float t = stage / (float)lastStage;
        float height = Mathf.Lerp(0.12f, 0.55f, t);
        float width = Mathf.Lerp(0.08f, 0.22f, t);

        CreateBox("Stem", cropRoot.transform,
            new Vector3(0f, height * 0.5f, 0f),
            new Vector3(width, height, width),
            crop.stemColor);

        // 잎: 2단계부터 양옆으로
        if (stage >= 1)
        {
            float leaf = width * 1.6f;
            CreateBox("LeafA", cropRoot.transform, new Vector3(width, height * 0.6f, 0f),
                new Vector3(leaf, width * 0.5f, width * 0.6f), crop.stemColor);
            CreateBox("LeafB", cropRoot.transform, new Vector3(-width, height * 0.45f, 0f),
                new Vector3(leaf, width * 0.5f, width * 0.6f), crop.stemColor);
        }

        if (stage >= lastStage)
        {
            CreateBox("Produce", cropRoot.transform,
                new Vector3(0f, height + 0.1f, 0f),
                new Vector3(0.28f, 0.22f, 0.28f),
                crop.produceColor);
        }
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        Object.Destroy(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial(color);
        return box;
    }

    private static Material GetMaterial(Color color)
    {
        if (MaterialCache.TryGetValue(color, out Material cached) && cached != null)
            return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = "FarmMaterial", color = color };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        MaterialCache[color] = material;
        return material;
    }
}
