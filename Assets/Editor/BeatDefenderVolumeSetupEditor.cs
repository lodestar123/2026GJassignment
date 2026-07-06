#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// GameScene과 동일한 Global Volume + 카메라 포스트프로세싱을 모든 게임 씬에 적용.
/// 프로필: Assets/Settings/DefaultVolumeProfile.asset
/// </summary>
public static class BeatDefenderVolumeSetupEditor
{
    public const string VolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";

    static readonly string[] ScenePaths =
    {
        "Assets/Scenes/GameScene.unity",
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/TutorialScene.unity",
        "Assets/Scenes/PracticeScene.unity",
    };

    [MenuItem("Beat Defender/Apply Volume To All Scenes")]
    public static void ApplyVolumeToAllScenesMenu()
    {
        var previous = SceneManager.GetActiveScene().path;
        int count = ApplyVolumeToAllScenes();

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        if (count == 0)
            Debug.LogWarning("Beat Defender: 적용할 씬이 없습니다.");
        else
            Debug.Log($"Beat Defender: Global Volume 적용 완료 ({count}개 씬) — {VolumeProfilePath}");
    }

    /// <summary>배치 모드·메뉴 공용.</summary>
    public static int ApplyVolumeToAllScenes()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            Debug.LogError($"Beat Defender: Volume profile not found at {VolumeProfilePath}");
            return 0;
        }

        int count = 0;
        foreach (var path in ScenePaths)
        {
            if (!File.Exists(path))
                continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!ApplyToScene(scene, profile))
                continue;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            count++;
        }

        return count;
    }

    static bool ApplyToScene(Scene scene, VolumeProfile profile)
    {
        int volumeLayer = LayerMask.NameToLayer("volume");
        LayerMask volumeMask = BuildVolumeLayerMask(volumeLayer);

        EnsureGlobalVolume(scene, profile, volumeLayer);

        var camera = FindMainCamera(scene);
        if (camera == null)
        {
            Debug.LogWarning($"Beat Defender: {scene.name} — Main Camera 없음, 스킵");
            return false;
        }

        EnsureCameraPostProcessing(camera, volumeMask);
        return true;
    }

    static LayerMask BuildVolumeLayerMask(int volumeLayer)
    {
        // GameScene Main Camera: UI(5) + volume(6) = 96
        int mask = 0;
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            mask |= 1 << uiLayer;
        if (volumeLayer >= 0)
            mask |= 1 << volumeLayer;
        return mask != 0 ? mask : ~0;
    }

    static Camera FindMainCamera(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam.CompareTag("MainCamera"))
                    return cam;
            }
        }

        return null;
    }

    static void EnsureGlobalVolume(Scene scene, VolumeProfile profile, int volumeLayer)
    {
        var globals = new System.Collections.Generic.List<Volume>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var volume in root.GetComponentsInChildren<Volume>(true))
            {
                if (volume.isGlobal)
                    globals.Add(volume);
            }
        }

        for (int i = 1; i < globals.Count; i++)
            Undo.DestroyObjectImmediate(globals[i].gameObject);

        Volume global = globals.Count > 0 ? globals[0] : null;

        GameObject go;
        if (global != null)
        {
            go = global.gameObject;
        }
        else
        {
            go = new GameObject("Global Volume");
            Undo.RegisterCreatedObjectUndo(go, "Apply Global Volume");
            global = Undo.AddComponent<Volume>(go);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        go.name = "Global Volume";
        if (volumeLayer >= 0)
            go.layer = volumeLayer;

        global.isGlobal = true;
        global.priority = 0f;
        global.weight = 1f;
        global.sharedProfile = profile;
        EditorUtility.SetDirty(go);

        if (Selection.activeObject is Volume selected
            && selected != null
            && selected.gameObject.scene == scene
            && selected.isGlobal
            && selected != global)
            Selection.activeObject = global;
    }

    static void EnsureCameraPostProcessing(Camera camera, LayerMask volumeMask)
    {
        var data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (data == null)
            data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);

        data.renderPostProcessing = true;
        data.volumeLayerMask = volumeMask;
        EditorUtility.SetDirty(camera.gameObject);
    }
}
#endif
