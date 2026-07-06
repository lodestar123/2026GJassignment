using UnityEngine;

/// <summary>
/// 타워·맵 마커 프리팹 — <b>MapPrefabRegistry.asset</b> Inspector에서 등록.
/// Beat Defender/Build Map &amp; Tower Prefabs 로 생성·갱신.
/// </summary>
[CreateAssetMenu(fileName = "MapPrefabRegistry", menuName = "Beat Defender/Map Prefab Registry")]
public class MapPrefabRegistry : ScriptableObject
{
    public const string ResourcesPath = "BeatDefender/MapPrefabRegistry";
    public const string AssetPath = "Assets/Resources/BeatDefender/MapPrefabRegistry.asset";

    static MapPrefabRegistry _cached;

    [Header("Tower Prefabs")]
    [SerializeField] GameObject beatTower;

    [Header("Map Prefabs")]
    [SerializeField] GameObject towerPlacementCell;
    [SerializeField] GameObject core;
    [SerializeField] GameObject pathWaypointMarker;
    [SerializeField] GameObject spawnPointMarker;

    [Header("Sprites (스프라이트 교체 시 여기 또는 프리팹 Inspector)")]
    [SerializeField] Sprite placementTileEmpty;
    [SerializeField] Sprite placementTileAvailable;
    [SerializeField] Sprite towerLevel1;
    [SerializeField] Sprite towerLevel2;
    [SerializeField] Sprite towerLevel3;
    [SerializeField] Sprite coreSprite;
    [SerializeField] Sprite rangeRingSprite;

    public static MapPrefabRegistry Instance => Get();

    public static MapPrefabRegistry Get()
    {
        if (_cached != null)
            return _cached;

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isUpdating && !UnityEditor.EditorApplication.isCompiling)
        {
            _cached = UnityEditor.AssetDatabase.LoadAssetAtPath<MapPrefabRegistry>(AssetPath);
            if (_cached != null)
                return _cached;
        }
#endif

        _cached = Resources.Load<MapPrefabRegistry>(ResourcesPath);
        if (_cached != null)
            return _cached;

        var untyped = Resources.Load(ResourcesPath);
        if (untyped is MapPrefabRegistry registry)
            _cached = registry;

        return _cached;
    }

    public static void InvalidateCache() => _cached = null;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => InvalidateCache();
#endif

    public GameObject BeatTower => beatTower;
    public GameObject TowerPlacementCell => towerPlacementCell;
    public GameObject Core => core;
    public GameObject PathWaypointMarker => pathWaypointMarker;
    public GameObject SpawnPointMarker => spawnPointMarker;

    public Sprite PlacementTileEmpty => placementTileEmpty;
    public Sprite PlacementTileAvailable => placementTileAvailable;
    public Sprite CoreSprite => coreSprite;
    public Sprite RangeRingSprite => rangeRingSprite;

    public GameObject GetTowerPrefab(TowerType type)
    {
        var prefab = type switch
        {
            TowerType.Beat => beatTower,
            _ => null
        };

        if (prefab != null)
            return prefab;

        return type == TowerType.Beat
            ? Resources.Load<GameObject>("BeatDefender/BeatTower")
            : null;
    }

    public static bool IsUsableSprite(Sprite sprite) =>
        sprite != null && sprite.texture != null;

    public void SetPrefabs(
        GameObject beatTowerPrefab,
        GameObject placementCellPrefab,
        GameObject corePrefab,
        GameObject pathMarkerPrefab,
        GameObject spawnMarkerPrefab)
    {
        beatTower = beatTowerPrefab;
        towerPlacementCell = placementCellPrefab;
        core = corePrefab;
        pathWaypointMarker = pathMarkerPrefab;
        spawnPointMarker = spawnMarkerPrefab;
    }

    public void SetSprites(
        Sprite placementEmpty,
        Sprite placementAvailable,
        Sprite towerLv1,
        Sprite towerLv2,
        Sprite towerLv3,
        Sprite coreArt,
        Sprite ringArt)
    {
        placementTileEmpty = placementEmpty;
        placementTileAvailable = placementAvailable;
        towerLevel1 = towerLv1;
        towerLevel2 = towerLv2;
        towerLevel3 = towerLv3;
        coreSprite = coreArt;
        rangeRingSprite = ringArt;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (beatTower == null || towerPlacementCell == null)
            Debug.LogWarning(
                "[MapPrefabRegistry] 프리팹 슬롯이 비어 있습니다. " +
                "메뉴 Beat Defender → Build Map & Tower Prefabs 실행.",
                this);
    }
#endif
}
