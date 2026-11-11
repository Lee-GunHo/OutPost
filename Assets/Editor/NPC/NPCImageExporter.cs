using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// NPCPresenter가 붙은 모든 NPC 프리팹을 PNG(투명 배경)로 찍어서 프로젝트 루트의 NPCRenders/ 폴더에 저장.
/// 열린 씬은 건드리지 않음 — 별도 미리보기 씬에 프리팹을 꺼내서 전용 카메라/조명으로 렌더링 후 바로 지움.
/// 애니메이터가 있으면 기본 상태(Idle) 첫 프레임 포즈로 찍음(없으면 바인드 포즈).
/// </summary>
public static class NPCImageExporter
{
    private const string SearchFolder = "Assets/Prefab";
    private const string OutputFolderName = "NPCRenders";
    private const int ImageSize = 1024;
    private const float FieldOfView = 20f;
    private const float FramePadding = 1.08f;

    // 모델 정면은 +Z (NPCVisualBuilder 기준). 각도는 정면에서 Y축으로 돌린 카메라 위치.
    private static readonly (string suffix, float yaw)[] Angles =
    {
        ("front", 0f),
        ("quarter", 35f),
    };

    [MenuItem("Tools/NPC/Export NPC Images (PNG)")]
    public static void ExportAll()
    {
        string outputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutputFolderName);
        Directory.CreateDirectory(outputFolder);

        List<string> written = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SearchFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null || prefab.GetComponent<NPCPresenter>() == null)
                continue;

            foreach ((string suffix, float yaw) in Angles)
            {
                string file = Path.Combine(outputFolder, prefab.name + "_" + suffix + ".png");

                if (RenderPrefab(prefab, yaw, file))
                    written.Add(file);
            }
        }

        if (written.Count == 0)
        {
            Debug.LogWarning("NPC 이미지: 렌더링할 NPC 프리팹을 찾지 못했습니다 (" + SearchFolder + ", NPCPresenter).");
            return;
        }

        Debug.Log("NPC 이미지 " + written.Count + "장 저장: " + outputFolder + "\n" + string.Join("\n", written));
        EditorUtility.RevealInFinder(written[0]);
    }

    private static bool RenderPrefab(GameObject prefab, float yaw, string file)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture rt = null;
        Texture2D tex = null;
        bool sampling = false;

        try
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // 상호작용 UI(Canvas)는 찍지 않음
            foreach (Canvas canvas in instance.GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(false);

            sampling = SampleIdlePose(instance);

            if (!TryGetVisibleBounds(instance, out Bounds bounds))
            {
                Debug.LogWarning("NPC 이미지: 보이는 렌더러가 없어 건너뜀 - " + prefab.name);
                return false;
            }

            Camera camera = CreateCamera(scene, bounds, yaw);
            CreateLights(scene, yaw);

            rt = new RenderTexture(ImageSize, ImageSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 8,
            };
            camera.targetTexture = rt;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(file, tex.EncodeToPNG());
            camera.targetTexture = null;
            return true;
        }
        finally
        {
            if (sampling)
                AnimationMode.StopAnimationMode();
            if (rt != null)
                Object.DestroyImmediate(rt);
            if (tex != null)
                Object.DestroyImmediate(tex);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    // 애니메이션 창과 같은 방식(AnimationMode)으로 기본 상태 클립 첫 프레임을 적용 — 휴머노이드 클립도 동작.
    private static bool SampleIdlePose(GameObject instance)
    {
        Animator animator = instance.GetComponentInChildren<Animator>();
        RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;

        if (controller == null || controller.animationClips.Length == 0)
            return false;

        AnimationClip clip = controller.animationClips[0];
        foreach (AnimationClip candidate in controller.animationClips)
        {
            if (candidate != null && candidate.name.ToLowerInvariant().Contains("idle"))
            {
                clip = candidate;
                break;
            }
        }

        if (clip == null)
            return false;

        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(animator.gameObject, clip, 0f);
        AnimationMode.EndSampling();
        return true;
    }

    private static bool TryGetVisibleBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is ParticleSystemRenderer)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static Camera CreateCamera(Scene scene, Bounds bounds, float yaw)
    {
        GameObject go = new GameObject("NPCImageCamera");
        SceneManager.MoveGameObjectToScene(go, scene);

        Camera camera = go.AddComponent<Camera>();
        camera.scene = scene;
        camera.cameraType = CameraType.Game;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.fieldOfView = FieldOfView;
        camera.allowHDR = false; // HDR 중간 버퍼(B10G11R11)는 알파가 없어서 투명 배경이 사라짐
        camera.allowMSAA = true;

        UniversalAdditionalCameraData urp = go.AddComponent<UniversalAdditionalCameraData>();
        urp.renderPostProcessing = false;
        urp.renderShadows = false;

        // 바운드 구가 세로/가로 화각에 다 들어가는 거리 (정사각형 이미지라 화각 동일)
        float radius = bounds.extents.magnitude;
        float distance = radius * FramePadding / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

        go.transform.position = bounds.center + direction * distance;
        go.transform.LookAt(bounds.center);
        camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
        camera.farClipPlane = distance + radius * 2f;
        return camera;
    }

    // 미리보기 씬은 환경광이 없어서 키/필/림 조명 3개로 밝힘 (카메라 각도를 따라 돌아감)
    private static void CreateLights(Scene scene, float yaw)
    {
        CreateLight(scene, "Key", new Vector3(30f, 180f + yaw - 35f, 0f), 1.15f, new Color(1f, 0.97f, 0.92f));
        CreateLight(scene, "Fill", new Vector3(10f, 180f + yaw + 50f, 0f), 0.55f, new Color(0.85f, 0.9f, 1f));
        CreateLight(scene, "Rim", new Vector3(20f, yaw, 0f), 0.6f, Color.white);
    }

    private static void CreateLight(Scene scene, string name, Vector3 euler, float intensity, Color color)
    {
        GameObject go = new GameObject("NPCImageLight_" + name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.rotation = Quaternion.Euler(euler);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.None;
    }
}
