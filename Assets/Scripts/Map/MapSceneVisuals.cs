using UnityEngine;

/// <summary>
/// 씬에 bake된 Core·경로·배치 슬롯에 MapPrefabRegistry 스프라이트 반영.
/// </summary>
public static class MapSceneVisuals
{
    public static void ApplyAll()
    {
        var registry = MapPrefabRegistry.Get();
        if (registry == null)
        {
            Debug.LogWarning(
                "[MapSceneVisuals] MapPrefabRegistry not found. " +
                "Assets/Resources/BeatDefender/MapPrefabRegistry.asset 확인.");
            return;
        }

        ApplyCore(registry);
        ApplyPathMarkers(registry);
        ApplyPlacementCells();
    }

    static void ApplyCore(MapPrefabRegistry registry)
    {
        if (registry.CoreSprite == null)
            return;

        var core = BaseHealth.Instance ?? Object.FindAnyObjectByType<BaseHealth>();
        var sr = core != null ? core.GetComponent<SpriteRenderer>() : null;
        if (sr == null)
            return;

        sr.sprite = registry.CoreSprite;
        sr.color = Color.white;
    }

    static void ApplyPathMarkers(MapPrefabRegistry registry)
    {
        var pathSprite = GetPrefabBodySprite(registry.PathWaypointMarker);
        if (pathSprite != null)
        {
            ApplySpriteToChildren("Path_S1", pathSprite);
            ApplySpriteToChildren("Path_S2", pathSprite);
        }

        var spawnSprite = GetPrefabBodySprite(registry.SpawnPointMarker);
        if (spawnSprite == null)
            return;

        ApplySpriteToChildren("Spawn_S1", spawnSprite);
        ApplySpriteToChildren("Spawn_S2", spawnSprite);
    }

    static void ApplyPlacementCells()
    {
        foreach (var cell in Object.FindObjectsByType<TowerPlacementCell>(FindObjectsInactive.Include))
        {
            cell.RefreshFromRegistry();
        }
    }

    static Sprite GetPrefabBodySprite(GameObject prefab)
    {
        return prefab != null ? prefab.GetComponent<SpriteRenderer>()?.sprite : null;
    }

    static void ApplySpriteToChildren(string rootName, Sprite sprite)
    {
        var root = GameObject.Find(rootName)?.transform;
        if (root == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            var sr = root.GetChild(i).GetComponent<SpriteRenderer>();
            if (sr == null)
                continue;

            sr.sprite = sprite;
            sr.color = Color.white;
        }
    }
}
