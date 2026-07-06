#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환·Play 진입 시 파괴된 Global Volume이 Inspector에 남아 VolumeEditor가 터지는 현상 방지.
/// </summary>
[InitializeOnLoad]
static class BeatDefenderEditorSelectionGuard
{
    static BeatDefenderEditorSelectionGuard()
    {
        EditorSceneManager.sceneOpened += (_, _) => ClearStaleSelection();
        EditorSceneManager.sceneClosing += (_, _) => ClearStaleSelection();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            ClearVolumeSelection();
        else if (state == PlayModeStateChange.EnteredEditMode)
            ClearStaleSelection();
    }

    static void ClearVolumeSelection()
    {
        if (Selection.activeObject is Volume)
            Selection.activeObject = null;
    }

    static void ClearStaleSelection()
    {
        var active = Selection.activeObject;
        if (active == null)
            return;

        if (active is Object unityObject && !unityObject)
            Selection.activeObject = null;
    }
}
#endif
