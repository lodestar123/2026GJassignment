#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 스프라이트 시트 → .anim + Animator Controller + 몬스터 프리팹 생성.
/// </summary>
public static class EnemyAnimationBuilder
{
    const string AnimFolder = "Assets/Animations/Enemy";
    const string PrefabFolder = "Assets/Prefabs/Enemies";
    const string RegistryPath = "Assets/Resources/BeatDefender/EnemyPrefabRegistry.asset";
    const string CircleSpritePath = "Assets/Sprites/Circle.png";
    const string DownbeatWalkSheetPath = "Assets/Sprites/DoubleNoteWalkCycle_v3.png";

    const float WalkFps = 10f;
    const float EighthNoteScale = 0.28f;
    const float DownbeatScale = 0.55f;
    const float EliteScale = 0.72f;

    [MenuItem("Beat Defender/Build Enemy Prefabs (Animator)")]
    public static void BuildFromMenu()
    {
        if (BuildAll())
            Debug.Log("Beat Defender: Enemy prefabs + Animator clips built. Registry updated.");
        else
            Debug.LogError("Beat Defender: Build failed — check MusicNoteWalkCycle_cute.png exists.");
    }

    [MenuItem("Beat Defender/Setup Enemy Walk Sprites")]
    public static void LegacyMenuRedirect() => BuildFromMenu();

    [MenuItem("Beat Defender/Rebuild Downbeat Walk Animation")]
    public static void RebuildDownbeatAnimFromMenu()
    {
        if (RebuildDownbeatAnimation())
            Debug.Log("Beat Defender: Downbeat_Walk.anim + Downbeat.controller rebuilt.");
        else
            Debug.LogError("Beat Defender: DoubleNoteWalkCycle_v3.png sprites not found.");
    }

    public static bool BuildAll()
    {
        var walkFrames = LoadSprites(EnemyWalkSpriteLibrary.WalkSheetPath);
        if (walkFrames.Length == 0)
            return false;

        EnsureFolder(AnimFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(Path.GetDirectoryName(RegistryPath));

        var walkClip = CreateOrUpdateSpriteWalkClip(
            $"{AnimFolder}/EighthNote_Walk.anim", "EighthNote_Walk", walkFrames, WalkFps);
        var controller = CreateOrUpdateSpriteController(
            $"{AnimFolder}/EighthNote.controller", "Walk", walkClip);
        var circle = LoadCircleSprite();

        var eighth = CreateOrUpdateEighthNotePrefab(walkFrames[0], controller);
        RebuildDownbeatAnimation();

        var downbeatFrames = LoadSprites(DownbeatWalkSheetPath);
        var down = downbeatFrames.Length > 0
            ? LoadPrefabOrFallback($"{PrefabFolder}/Downbeat.prefab")
            : CreateOrUpdateStaticPrefab(
                "Downbeat", circle, new Color(0.67f, 0.28f, 0.74f), DownbeatScale, 0.5f, 5);
        var elite = CreateOrUpdateStaticPrefab(
            "Elite", circle, new Color(1f, 0.78f, 0.22f), EliteScale, 0.58f, 6);

        SaveRegistry(eighth, down, elite);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    static bool RebuildDownbeatAnimation()
    {
        var frames = LoadSprites(DownbeatWalkSheetPath);
        if (frames.Length == 0)
            return false;

        var clip = CreateOrUpdateSpriteWalkClip(
            $"{AnimFolder}/Downbeat_Walk.anim", "Downbeat_Walk", frames, WalkFps);
        CreateOrUpdateSpriteController($"{AnimFolder}/Downbeat.controller", "Downbeat_Walk", clip);
        return true;
    }

    static GameObject LoadPrefabOrFallback(string path) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static Sprite LoadCircleSprite() =>
        AssetDatabase.LoadAllAssetsAtPath(CircleSpritePath)
            .OfType<Sprite>()
            .FirstOrDefault();

    static Sprite[] LoadSprites(string assetPath) =>
        AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

    static AnimationClip CreateOrUpdateSpriteWalkClip(
        string path, string clipName, Sprite[] frames, float fps)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = clipName };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = fps;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimatorController CreateOrUpdateSpriteController(
        string path, string stateName, AnimationClip walkClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        if (controller.layers == null || controller.layers.Length == 0)
        {
            Debug.LogError($"[EnemyAnimationBuilder] Invalid controller (no layers): {path}");
            return controller;
        }

        var sm = controller.layers[0].stateMachine;

        AnimatorState walkState = null;
        foreach (var child in sm.states)
        {
            if (child.state.name == stateName)
            {
                walkState = child.state;
                break;
            }
        }

        if (walkState == null)
            walkState = sm.AddState(stateName, new Vector3(300f, 0f, 0f));

        walkState.motion = walkClip;
        sm.defaultState = walkState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static GameObject CreateOrUpdateEighthNotePrefab(Sprite firstFrame, RuntimeAnimatorController controller)
    {
        var path = $"{PrefabFolder}/EighthNote.prefab";
        return SaveFreshPrefab(path, "EighthNote", go =>
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = firstFrame;
            sr.color = Color.white;
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.38f;

            go.AddComponent<EnemyHealth>();
            go.AddComponent<EnemyMovement>();
            go.AddComponent<EnemyPathProgress>();
            go.AddComponent<EnemyBeatBounce>();

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            go.transform.localScale = Vector3.one * EighthNoteScale;
        });
    }

    static GameObject CreateOrUpdateStaticPrefab(
        string name,
        Sprite sprite,
        Color color,
        float scale,
        float colliderRadius,
        int sortingOrder)
    {
        var path = $"{PrefabFolder}/{name}.prefab";
        return SaveFreshPrefab(path, name, go =>
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = colliderRadius;

            go.AddComponent<EnemyHealth>();
            go.AddComponent<EnemyMovement>();
            go.AddComponent<EnemyPathProgress>();
            go.AddComponent<EnemyBeatBounce>();

            go.transform.localScale = Vector3.one * scale;
        });
    }

    /// <summary>기존 프리팹(깨진 컴포넌트 포함)을 삭제하고 항상 새로 생성.</summary>
    static GameObject SaveFreshPrefab(string path, string name, Action<GameObject> configure)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        var go = new GameObject(name);
        try
        {
            configure(go);
            return PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void SaveRegistry(GameObject eighth, GameObject down, GameObject elite)
    {
        var registry = AssetDatabase.LoadAssetAtPath<EnemyPrefabRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<EnemyPrefabRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        registry.SetPrefabs(eighth, down, elite);
        EditorUtility.SetDirty(registry);
    }

    static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        var folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
