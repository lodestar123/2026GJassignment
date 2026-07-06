#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SettingsPanelUIEditor
{
    const string GameScenePath = "Assets/Scenes/GameScene.unity";
    const string StartScenePath = "Assets/Scenes/StartScene.unity";

    [MenuItem("Beat Defender/Rebuild Settings Panel")]
    public static void RebuildSettingsPanelMenu()
    {
        var previous = SceneManager.GetActiveScene().path;
        int count = 0;

        if (RebuildGameScene())
            count++;
        if (RebuildStartScene())
            count++;

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        if (count == 0)
            Debug.LogWarning("Beat Defender: SettingsPanelUI를 찾지 못했습니다.");
        else
            Debug.Log($"Beat Defender: 설정 패널 리빌드 완료 ({count}개 씬).");
    }

    static bool RebuildGameScene()
    {
        if (!File.Exists(GameScenePath))
            return false;

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var pauseRoot = GameObject.Find("PauseMenuController")?.transform as RectTransform;
        if (pauseRoot == null)
        {
            var existing = Object.FindFirstObjectByType<SettingsPanelUI>(FindObjectsInactive.Include);
            pauseRoot = existing != null ? existing.transform as RectTransform : null;
        }

        if (pauseRoot == null)
            return false;

        var settingsUi = pauseRoot.GetComponent<SettingsPanelUI>();
        if (settingsUi != null && settingsUi.PanelRoot != null)
        {
            Debug.Log("Beat Defender: GameScene 설정 패널은 씬 오브젝트로 유지됩니다. Hierarchy에서 직접 수정하세요.");
            return true;
        }

        Undo.RegisterFullObjectHierarchyUndo(pauseRoot.gameObject, "Rebuild Settings Panel");
        SettingsPanelVisualBuilder.Build(pauseRoot, new SettingsPanelVisualBuilder.BuildOptions
        {
            ShowInputOffset = true,
            CloseReturnsToPauseMenu = true
        });

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    static bool RebuildStartScene()
    {
        if (!File.Exists(StartScenePath))
            return false;

        var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        var menu = Object.FindFirstObjectByType<StartMenuUI>();
        if (menu == null)
            return false;

        var root = menu.transform as RectTransform;
        Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Rebuild Settings Panel");
        StartSceneVisualBuilder.EnsureSettingsButton(root);
        SettingsPanelVisualBuilder.Build(root, new SettingsPanelVisualBuilder.BuildOptions
        {
            ShowInputOffset = false,
            CloseReturnsToPauseMenu = false
        });

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }
}
#endif
