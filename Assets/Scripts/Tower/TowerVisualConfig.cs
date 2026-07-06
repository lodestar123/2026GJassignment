using UnityEngine;

/// <summary>
/// 타워·사거리 링 스프라이트 전용 설정.
/// Project: Assets/Resources/BeatDefender/TowerVisualConfig.asset
/// </summary>
[CreateAssetMenu(fileName = "TowerVisualConfig", menuName = "Beat Defender/Tower Visual Config")]
public class TowerVisualConfig : ScriptableObject
{
    public const string ResourcesPath = "BeatDefender/TowerVisualConfig";
    public const string AssetPath = "Assets/Resources/BeatDefender/TowerVisualConfig.asset";

    static TowerVisualConfig _cached;

    [Header("타워 이미지 (Lv1~3)")]
    [SerializeField] Sprite towerLevel1;
    [SerializeField] Sprite towerLevel2;
    [SerializeField] Sprite towerLevel3;

    [Header("사거리 링")]
    [SerializeField] Sprite rangeRing;

    public static TowerVisualConfig Instance
    {
        get
        {
            if (_cached != null)
                return _cached;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isUpdating && !UnityEditor.EditorApplication.isCompiling)
            {
                _cached = UnityEditor.AssetDatabase.LoadAssetAtPath<TowerVisualConfig>(AssetPath);
                if (_cached != null)
                    return _cached;
            }
#endif
            _cached = Resources.Load<TowerVisualConfig>(ResourcesPath);
            return _cached;
        }
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => _cached = null;
#endif

    public static Sprite GetTowerSprite(int level)
    {
        var config = Instance;
        if (config == null)
            return null;

        return config.ResolveTower(level);
    }

    public static Sprite GetRangeRingSprite()
    {
        var config = Instance;
        return config != null ? config.rangeRing : null;
    }

    Sprite ResolveTower(int level)
    {
        return level switch
        {
            1 => towerLevel1,
            2 => towerLevel2 != null ? towerLevel2 : towerLevel1,
            >= 3 => towerLevel3 != null ? towerLevel3 : (towerLevel2 != null ? towerLevel2 : towerLevel1),
            _ => towerLevel1
        };
    }

    public void SetSprites(Sprite level1, Sprite level2, Sprite level3, Sprite ring)
    {
        towerLevel1 = level1;
        towerLevel2 = level2;
        towerLevel3 = level3;
        rangeRing = ring;
    }
}
