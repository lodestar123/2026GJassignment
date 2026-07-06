using UnityEngine;

/// <summary>
/// 씬 --- Map --- 아래 Spawn_S1 / Spawn_S2 위치 조회.
/// 스폰 위치는 씬에 배치된 루트(또는 Marker 자식)가 기준이며, 없을 때만 MapLayout 기본값을 씁니다.
/// 스프라이트 마커는 자동 생성하지 않습니다 — 씬에 수동 배치하거나 루트 Transform만 사용합니다.
/// </summary>
public static class MapSpawnPointBuilder
{
    public const string SpawnS1RootName = "Spawn_S1";
    public const string SpawnS2RootName = "Spawn_S2";

    public static void EnsureAll()
    {
        var mapRoot = FindMapRoot();
        if (mapRoot == null)
            return;

        var layout = ResolveLayout(mapRoot);
        EnsureRoot(mapRoot, SpawnS1RootName, layout.SpawnS1);
        EnsureRoot(mapRoot, SpawnS2RootName, layout.SpawnS2);
    }

    public static Vector2 GetSpawnS1() => GetSpawnWorldPosition(SpawnS1RootName, MapLayout.SpawnS1);

    public static Vector2 GetSpawnS2() => GetSpawnWorldPosition(SpawnS2RootName, MapLayout.SpawnS2);

    public static Vector2 GetSpawnWorldPosition(string rootName, Vector2 fallback)
    {
        return TryGetMarkerWorldPosition(rootName, out var worldPos) ? worldPos : fallback;
    }

    public static bool TryGetMarkerWorldPosition(string rootName, out Vector2 worldPos)
    {
        worldPos = default;

        var mapRoot = FindMapRoot();
        if (mapRoot == null)
            return false;

        var root = mapRoot.Find(rootName);
        if (root == null)
            return false;

        worldPos = FindSpawnTransform(root).position;
        return true;
    }

    public static Transform FindMapRoot()
    {
        var map = GameObject.Find("--- Map ---");
        return map != null ? map.transform : null;
    }

    static MapLayoutDefinition ResolveLayout(Transform mapRoot)
    {
        var config = mapRoot.GetComponent<MapSceneConfig>();
        return config != null ? MapLayout.Get(config.LayoutKind) : MapLayout.Active;
    }

    static void EnsureRoot(Transform mapRoot, string rootName, Vector2 defaultSpawnPos)
    {
        if (mapRoot.Find(rootName) != null)
            return;

        var go = new GameObject(rootName);
        go.transform.SetParent(mapRoot, false);
        go.transform.localPosition = new Vector3(defaultSpawnPos.x, defaultSpawnPos.y, 0f);
    }

    static Transform FindSpawnTransform(Transform spawnRoot)
    {
        if (spawnRoot == null)
            return null;

        var marker = FindMarkerTransform(spawnRoot);
        return marker != null ? marker : spawnRoot;
    }

    static Transform FindMarkerTransform(Transform spawnRoot)
    {
        if (spawnRoot == null)
            return null;

        var marker = spawnRoot.Find("Marker");
        if (marker != null)
            return marker;

        marker = spawnRoot.Find("1");
        if (marker != null)
            return marker;

        return spawnRoot.childCount > 0 ? spawnRoot.GetChild(0) : null;
    }
}
