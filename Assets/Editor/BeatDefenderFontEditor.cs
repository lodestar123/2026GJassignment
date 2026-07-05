#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 폰트 일괄 적용 — UI 레이아웃/씬 계층은 건드리지 않음.
/// </summary>
public static class BeatDefenderFontEditor
{
    const string PretendardFontPath = "Assets/Resources/Font/PretendardVariable SDF.asset";

    [MenuItem("Beat Defender/Apply Pretendard Font To All Scenes")]
    public static void ApplyPretendardFontToAllScenes()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PretendardFontPath);
        if (font == null)
        {
            Debug.LogError("Beat Defender: Pretendard font asset not found at " + PretendardFontPath);
            return;
        }

        var scenePaths = new[]
        {
            "Assets/Scenes/StartScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        var previousScene = SceneManager.GetActiveScene().path;
        var count = 0;

        foreach (var path in scenePaths)
        {
            if (!System.IO.File.Exists(path))
                continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                tmp.font = font;
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);

        Debug.Log($"Beat Defender: Applied Pretendard font to {count} TextMeshProUGUI components.");
    }
}
#endif
