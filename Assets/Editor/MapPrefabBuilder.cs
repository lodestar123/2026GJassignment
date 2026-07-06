#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 타워·맵 마커 프리팹 생성 — EnemyAnimationBuilder와 동일한 Registry 패턴.
/// </summary>
public static class MapPrefabBuilder
{
    const string TowerFolder = "Assets/Prefabs/Towers";
    const string MapFolder = "Assets/Prefabs/Map";
    const string RegistryPath = "Assets/Resources/BeatDefender/MapPrefabRegistry.asset";
    const string GameScenePath = "Assets/Scenes/GameScene.unity";

    const string TowerLv1Path = "Assets/Sprites/Tower_Level1_1.png";
    const string TowerLv2Path = "Assets/Sprites/Tower_Level2.png";
    const string TowerLv3Path = "Assets/Sprites/Tower_Level3.png";
    const string TowerLv3SpriteName = "Tower_Level3_1";
    const string PlacementEmptyPath = "Assets/Sprites/PlacementTile_Empty_1.png";
    const string PlacementAvailablePath = "Assets/Sprites/PlacementTile_Available.png";
    const string CoreSpritePath = "Assets/Sprites/DefensePoint_Core_1.png";
    const string RangeRingSpritePath = "Assets/Sprites/RangeIndicatorRing.png";
    const string CircleSpritePath = "Assets/Sprites/Circle.png";
    const string SpawnPortalPath = "Assets/Sprites/EnemySpawnPoint_Portal.png";

    [MenuItem("Beat Defender/Build Map & Tower Prefabs")]
    public static void BuildFromMenu()
    {
        if (BuildAll())
            Debug.Log("Beat Defender: Map & Tower prefabs built. MapPrefabRegistry updated.");
        else
            Debug.LogError("Beat Defender: Map prefab build failed.");
    }

    [MenuItem("Beat Defender/Apply Map Visuals From Registry")]
    public static void ApplyMapVisualsFromRegistryMenu()
    {
        MapPrefabRegistry.InvalidateCache();
        var registry = MapPrefabRegistry.Get();
        if (registry == null)
        {
            Debug.LogError("Beat Defender: MapPrefabRegistry not found.");
            return;
        }

        MapSceneVisuals.ApplyAll();
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("Beat Defender: Map visuals applied from MapPrefabRegistry.");
    }

    [MenuItem("Beat Defender/Rebuild GameScene Spawn Points")]
    public static void RebuildGameSceneSpawnPoints()
    {
        if (!File.Exists(GameScenePath))
        {
            Debug.LogError($"GameScene not found: {GameScenePath}");
            return;
        }

        MapPrefabRegistry.InvalidateCache();
        var registry = MapPrefabRegistry.Get();
        if (registry == null)
        {
            Debug.LogError("Beat Defender: MapPrefabRegistry not found.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != SceneNames.Game)
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        MapSpawnPointBuilder.EnsureAll();
        MapSceneVisuals.ApplyAll();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Beat Defender: GameScene spawn points rebuilt (Spawn_S1 / Spawn_S2).");
    }

    [MenuItem("Beat Defender/Rebuild GameScene Placement Grid")]
    public static void RebuildGameScenePlacementGrid()
    {
        if (!File.Exists(GameScenePath))
        {
            Debug.LogError($"GameScene not found: {GameScenePath}");
            return;
        }

        MapPrefabRegistry.InvalidateCache();
        var registry = MapPrefabRegistry.Get();
        if (registry == null)
        {
            Debug.LogError("Beat Defender: MapPrefabRegistry not found.");
            return;
        }

        if (registry.TowerPlacementCell == null)
        {
            Debug.LogError(
                "Beat Defender: TowerPlacementCell prefab is missing in MapPrefabRegistry. " +
                "Run Build Map & Tower Prefabs first.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != SceneNames.Game)
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        var grid = UnityEngine.Object.FindAnyObjectByType<PlacementGrid>();
        if (grid == null)
        {
            Debug.LogError("PlacementGrid not found in GameScene.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(grid.gameObject, "Rebuild Placement Grid");
        grid.BuildForEditor();
        MapSpawnPointBuilder.EnsureAll();
        MapSceneVisuals.ApplyAll();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Beat Defender: GameScene PlacementGrid rebuilt and visuals applied.");
    }

    public static bool BuildAll()
    {
        EnsureFolder(TowerFolder);
        EnsureFolder(MapFolder);
        EnsureFolder(Path.GetDirectoryName(RegistryPath));

        var towerLv1 = LoadSprite(TowerLv1Path) ?? GreyboxSprites.Square;
        var towerLv2 = LoadSprite(TowerLv2Path) ?? towerLv1;
        var towerLv3 = LoadSprite(TowerLv3Path, TowerLv3SpriteName) ?? towerLv2;
        var placementEmpty = LoadSprite(PlacementEmptyPath) ?? GreyboxSprites.Square;
        var placementAvailable = LoadSprite(PlacementAvailablePath) ?? placementEmpty;
        var coreSprite = LoadSprite(CoreSpritePath) ?? GreyboxSprites.Circle;
        var ringSprite = LoadSprite(RangeRingSpritePath)
            ?? LoadSprite(CircleSpritePath)
            ?? GreyboxSprites.Ring;
        var waypointSprite = LoadSprite(CircleSpritePath) ?? GreyboxSprites.Circle;
        var spawnSprite = LoadSprite(SpawnPortalPath) ?? waypointSprite;

        var beatTower = CreateBeatTowerPrefab(towerLv1, ringSprite);
        var placementCell = CreatePlacementCellPrefab(placementEmpty);
        var core = CreateCorePrefab(coreSprite);
        var pathMarker = CreatePathWaypointPrefab(waypointSprite);
        var spawnMarker = CreateSpawnPointPrefab(spawnSprite);

        SaveRegistry(beatTower, placementCell, core, pathMarker, spawnMarker,
            placementEmpty, placementAvailable, towerLv1, towerLv2, towerLv3, coreSprite, ringSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return beatTower != null && placementCell != null;
    }

    static GameObject CreateBeatTowerPrefab(Sprite bodySprite, Sprite ringSprite)
    {
        var path = $"{TowerFolder}/BeatTower.prefab";
        return SaveFreshPrefab(path, "BeatTower", go =>
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = bodySprite;
            sr.color = Color.white;
            sr.sortingOrder = 10;
            go.transform.localScale = Vector3.one * Tower.BaseVisualScale;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = Tower.HoverRadiusAtBaseScale;

            var tower = go.AddComponent<Tower>();
            tower.towerType = TowerType.Beat;

            go.AddComponent<BeatTower>();
            go.AddComponent<TowerClickTarget>();
            go.AddComponent<TowerFireRecoil>();
            go.AddComponent<TowerRangeVisualizer>();

            var ringGo = new GameObject("RangeRing");
            ringGo.transform.SetParent(go.transform, false);
            var ringSr = ringGo.AddComponent<SpriteRenderer>();
            ringSr.sprite = ringSprite;
            ringSr.sortingOrder = 1;
            ringSr.color = new Color(1f, 1f, 1f, 0.22f);
            float targetDiameter = tower.range * 2f;
            float spriteDiameter = ringSprite != null
                ? Mathf.Max(ringSprite.bounds.size.x, ringSprite.bounds.size.y)
                : 1f;
            float ringScale = spriteDiameter > 0.0001f ? targetDiameter / spriteDiameter : targetDiameter;
            ringGo.transform.localScale = new Vector3(ringScale, ringScale, 1f);
        });
    }

    static GameObject CreatePlacementCellPrefab(Sprite tileSprite)
    {
        var path = $"{MapFolder}/TowerPlacementCell.prefab";
        return SaveFreshPrefab(path, "TowerPlacementCell", go =>
        {
            go.transform.localScale = Vector3.one * 0.85f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tileSprite;
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.isTrigger = false;

            go.AddComponent<TowerPlacementCell>();
        });
    }

    static GameObject CreateCorePrefab(Sprite coreArt)
    {
        var path = $"{MapFolder}/Core.prefab";
        return SaveFreshPrefab(path, "Core", go =>
        {
            go.transform.position = MapLayout.Active.CorePosition;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = coreArt;
            sr.sortingOrder = 8;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            go.AddComponent<BaseHealth>();
        });
    }

    static GameObject CreatePathWaypointPrefab(Sprite sprite)
    {
        var path = $"{MapFolder}/PathWaypointMarker.prefab";
        return SaveFreshPrefab(path, "PathWaypointMarker", go =>
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 1f, 1f, 0.35f);
            sr.sortingOrder = -1;
            go.transform.localScale = Vector3.one * 0.25f;
        });
    }

    static GameObject CreateSpawnPointPrefab(Sprite sprite)
    {
        var path = $"{MapFolder}/SpawnPointMarker.prefab";
        return SaveFreshPrefab(path, "SpawnPointMarker", go =>
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 2;
            go.transform.localScale = Vector3.one * 0.5f;
        });
    }

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

    static void SaveRegistry(
        GameObject beatTower,
        GameObject placementCell,
        GameObject core,
        GameObject pathMarker,
        GameObject spawnMarker,
        Sprite placementEmpty,
        Sprite placementAvailable,
        Sprite towerLv1,
        Sprite towerLv2,
        Sprite towerLv3,
        Sprite coreArt,
        Sprite ringArt)
    {
        var registry = AssetDatabase.LoadAssetAtPath<MapPrefabRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<MapPrefabRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        registry.SetPrefabs(beatTower, placementCell, core, pathMarker, spawnMarker);
        registry.SetSprites(
            placementEmpty, placementAvailable, towerLv1, towerLv2, towerLv3, coreArt, ringArt);
        EditorUtility.SetDirty(registry);
    }

    static Sprite LoadSprite(string assetPath, string spriteName = null)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>();
        if (!string.IsNullOrEmpty(spriteName))
        {
            var named = sprites.FirstOrDefault(sprite => sprite.name == spriteName);
            if (named != null)
                return named;
        }

        return sprites.FirstOrDefault()
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
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
