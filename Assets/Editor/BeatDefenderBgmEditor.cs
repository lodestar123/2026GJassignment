#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 BGM 기본 클립 연결 — Beat Defender/Wire Scene BGM Defaults.
/// </summary>
public static class BeatDefenderBgmEditor
{
    const string StartClipPath = "Assets/Resources/bgm/start.mp3";
    const string GameClipPath = "Assets/Resources/bgm/game1.mp3";

    [MenuItem("Beat Defender/Wire Scene BGM Defaults")]
    public static void WireDefaults()
    {
        var previous = SceneManager.GetActiveScene().path;
        var startClip = AssetDatabase.LoadAssetAtPath<AudioClip>(StartClipPath);
        var gameClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GameClipPath);

        WireScene("Assets/Scenes/StartScene.unity", startClip, "StartScene BGM");
        WireScene("Assets/Scenes/GameScene.unity", gameClip, "GameScene BGM");

        if (!string.IsNullOrEmpty(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        Debug.Log("Beat Defender: StartScene → start.mp3, GameScene → game1.mp3 연결 완료. Inspector에서 clip 교체 가능.");
    }

    static void WireScene(string scenePath, AudioClip clip, string objectName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Beat Defender: 클립을 찾을 수 없습니다 — {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var player = Object.FindFirstObjectByType<SceneBgmPlayer>(FindObjectsInactive.Include);

        if (player == null)
        {
            var go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, "Wire Scene BGM");
            player = Undo.AddComponent<SceneBgmPlayer>(go);
        }
        else if (player.gameObject.name != objectName)
        {
            player.gameObject.name = objectName;
        }

        var so = new SerializedObject(player);
        so.FindProperty("bgmClip").objectReferenceValue = clip;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
