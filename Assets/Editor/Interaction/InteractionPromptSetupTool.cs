using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 공용 F키 안내 UI 프리팹 생성 + 오브젝트마다 붙어 있던 예전 F키 Canvas 정리.
/// 예전 방식(상자/제작대/NPC마다 Screen Space Overlay Canvas)은 에디터 화면을 가리고 지저분해서,
/// InteractionFocusController + InteractionPromptUI(씬에 하나) + InteractionHighlight(흰 외곽선)로 대체함.
/// </summary>
public static class InteractionPromptSetupTool
{
    private const string PrefabFolder = "Assets/Resources/Interaction";
    private const string PrefabPath = PrefabFolder + "/InteractionPrompt.prefab";
    private const string FontPath = "Assets/Font/DungGeunMo SDF.asset";
    private const string PrefabSearchFolder = "Assets/Prefab";

    // 예전 F키 Canvas 이름 (상자/제작대: Canvas_InteractionMark, NPC: Canvas_NPCTrigger)
    private static readonly string[] OldMarkNames = { "Canvas_InteractionMark", "Canvas_NPCTrigger" };

    [MenuItem("Tools/Interaction/Create Interaction Prompt Prefab")]
    public static void CreatePromptPrefab()
    {
        if (File.Exists(PrefabPath) &&
            !EditorUtility.DisplayDialog("F키 안내 프리팹",
                PrefabPath + " 가 이미 있습니다. 기본 모양으로 덮어쓸까요?\n(프리팹에서 수정한 색/크기는 초기화됨)",
                "덮어쓰기", "취소"))
        {
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            Debug.LogWarning(FontPath + " 폰트를 찾지 못해 TMP 기본 폰트로 생성합니다. 한글이 깨지면 프리팹에서 폰트를 바꿔주세요.");
        }

        EnsureFolder(PrefabFolder);

        GameObject root = InteractionPromptUI.BuildDefaultHierarchy(font);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("F키 안내 프리팹 생성: " + PrefabPath);
    }

    [MenuItem("Tools/Interaction/Remove Old F-Key Canvases From Prefabs")]
    public static void RemoveOldMarks()
    {
        if (!EditorUtility.DisplayDialog("예전 F키 Canvas 제거",
                PrefabSearchFolder + " 아래 프리팹에서 Canvas_InteractionMark / Canvas_NPCTrigger 를 삭제합니다.\n" +
                "(새 F키 안내 + 흰색 외곽선이 대신 표시됨)",
                "삭제", "취소"))
        {
            return;
        }

        List<string> changed = new List<string>();
        List<string> skipped = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (asset == null || !HasOldMark(asset.transform))
                continue;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            int removed = 0;

            try
            {
                foreach (Transform mark in FindOldMarks(contents.transform))
                {
                    // 다른 프리팹을 중첩해서 가져온 경우엔 그 원본 프리팹에서 지워야 함
                    if (PrefabUtility.IsPartOfPrefabInstance(mark.gameObject))
                    {
                        skipped.Add(path + " / " + mark.name + " (중첩 프리팹 — 원본에서 삭제 필요)");
                        continue;
                    }

                    Object.DestroyImmediate(mark.gameObject);
                    removed++;
                }

                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    changed.Add(path + " (" + removed + "개)");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        AssetDatabase.SaveAssets();

        foreach (string path in changed)
            Debug.Log("예전 F키 Canvas 제거: " + path);

        foreach (string message in skipped)
            Debug.LogWarning("건너뜀: " + message);

        EditorUtility.DisplayDialog("예전 F키 Canvas 제거",
            "수정한 프리팹 " + changed.Count + "개" +
            (skipped.Count > 0 ? "\n건너뜀 " + skipped.Count + "개 — Console 확인" : ""), "확인");
    }

    private static bool HasOldMark(Transform root)
    {
        return FindOldMarks(root).Count > 0;
    }

    private static List<Transform> FindOldMarks(Transform root)
    {
        List<Transform> marks = new List<Transform>();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root || System.Array.IndexOf(OldMarkNames, child.name) < 0)
                continue;

            // 이름이 같은 일반 오브젝트를 지우지 않도록 Canvas가 붙은 것만
            if (child.GetComponent<Canvas>() != null)
            {
                marks.Add(child);
            }
        }

        return marks;
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
