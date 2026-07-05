using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// MusicNoteWalkCycle_cute 스프라이트 시트 — 8분음표(EighthNote) 워크 사이클.
/// </summary>
public static class EnemyWalkSpriteLibrary
{
    public const string WalkSheetPath = "Assets/Sprites/MusicNoteWalkCycle_cute.png";

    static Sprite[] _eighthNoteWalk;

    public static bool HasEighthNoteWalk
    {
        get
        {
            EnsureLoaded();
            return _eighthNoteWalk != null && _eighthNoteWalk.Length > 0;
        }
    }

    public static Sprite[] GetEighthNoteWalkFrames()
    {
        EnsureLoaded();
        return _eighthNoteWalk ?? System.Array.Empty<Sprite>();
    }

    public static Sprite FirstFrame
    {
        get
        {
            EnsureLoaded();
            return _eighthNoteWalk != null && _eighthNoteWalk.Length > 0 ? _eighthNoteWalk[0] : null;
        }
    }

    public static void EnsureLoaded()
    {
        if (_eighthNoteWalk != null && _eighthNoteWalk.Length > 0)
            return;

        var config = Resources.Load<EnemyVisualConfig>(EnemyVisualConfig.ResourcesPath);
        if (config != null && config.EighthNoteWalkCycle != null && config.EighthNoteWalkCycle.Length > 0)
        {
            _eighthNoteWalk = config.EighthNoteWalkCycle;
            return;
        }

#if UNITY_EDITOR
        _eighthNoteWalk = LoadFromEditorAssetDatabase();
#endif
    }

#if UNITY_EDITOR
    static Sprite[] LoadFromEditorAssetDatabase()
    {
        return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(WalkSheetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name, StringComparer.Ordinal)
            .ToArray();
    }
#endif
}
