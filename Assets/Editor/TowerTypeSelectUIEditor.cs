#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TowerTypeSelect — Btn_Tower 1개만 리빌드 (구 Btn_Beat/Strike/Boost 제거).
/// </summary>
public static class TowerTypeSelectUIEditor
{
    [MenuItem("Beat Defender/Rebuild Tower Select Button")]
    public static void RebuildTowerSelectButtonMenu()
    {
        var scene = SceneManager.GetActiveScene();
        if (!RebuildInScene(scene, save: true))
        {
            Debug.LogWarning(
                "Beat Defender: 현재 씬에 TowerTypeSelect가 없습니다. GameScene 또는 TutorialScene을 연 뒤 다시 실행하세요.");
            return;
        }

        Debug.Log(
            $"Beat Defender: '{scene.name}' — Tower Select 버튼 리빌드 완료. Hierarchy의 Btn_Tower를 직접 편집하세요.");
    }

    static bool RebuildInScene(Scene scene, bool save)
    {
        if (!scene.isLoaded)
            return false;

        TowerTypeSelectUI target = null;
        foreach (var ui in Object.FindObjectsByType<TowerTypeSelectUI>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ui.gameObject.scene != scene)
                continue;

            target = ui;
            break;
        }

        if (target == null)
            return false;

        Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Rebuild Tower Select Button");
        TowerTypeSelectVisualBuilder.RebuildButton(target.transform);
        EditorUtility.SetDirty(target);

        if (save)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return true;
    }
}
#endif
