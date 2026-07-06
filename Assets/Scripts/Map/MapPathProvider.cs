using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 Path_S1 / Path_S2 자식(P0, P1…) 웨이포인트를 적 경로·스폰으로 사용.
/// MapLayout Y자 fallback은 씬 경로가 없을 때만 사용합니다.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MapPathProvider : MonoBehaviour
{
    public const string P0WaypointName = "P0";
    public const string PathS1Name = "Path_S1";
    public const string PathS2Name = "Path_S2";

    public static MapPathProvider Instance { get; private set; }

    [SerializeField] Transform pathS1Root;
    [SerializeField] Transform pathS2Root;

    [Header("Spawn P0 (optional override)")]
    [Tooltip("비우면 정렬된 첫 웨이포인트(P0) 위치를 사용합니다.")]
    [SerializeField] Transform spawnP0S1;
    [Tooltip("비우면 정렬된 첫 웨이포인트(P0) 위치를 사용합니다.")]
    [SerializeField] Transform spawnP0S2;

    static bool _warnedMissingS1;
    static bool _warnedMissingS2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        _warnedMissingS1 = false;
        _warnedMissingS2 = false;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsurePathRoots();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    void OnValidate() => EnsurePathRoots();
#endif

    void EnsurePathRoots()
    {
        if (pathS1Root == null)
            pathS1Root = FindPathRoot(PathS1Name);
        if (pathS2Root == null)
            pathS2Root = FindPathRoot(PathS2Name);
    }

    Transform FindPathRoot(string rootName)
    {
        foreach (Transform child in transform)
        {
            if (child.name == rootName)
                return child;
        }

        var deep = transform.Find(rootName);
        if (deep != null)
            return deep;

        var global = GameObject.Find(rootName);
        return global != null ? global.transform : null;
    }

    public Vector2 GetSpawnForLeftSpawn()
    {
        var path = GetPathForLeftSpawn();
        return path.Length > 0 ? path[0] : MapLayout.SpawnS1;
    }

    public Vector2 GetSpawnForRightSpawn()
    {
        var path = GetPathForRightSpawn();
        return path.Length > 0 ? path[0] : MapLayout.SpawnS2;
    }

    public Vector2[] GetPathForLeftSpawn() =>
        ResolvePath(pathS1Root, spawnP0S1, MapLayout.Active.PathFromS1, leftSide: true);

    public Vector2[] GetPathForRightSpawn() =>
        ResolvePath(pathS2Root, spawnP0S2, MapLayout.Active.PathFromS2, leftSide: false);

    Vector2[] ResolvePath(Transform pathRoot, Transform spawnOverride, Vector2[] layoutFallback, bool leftSide)
    {
        EnsurePathRoots();
        if (pathRoot == null)
            pathRoot = leftSide ? pathS1Root : pathS2Root;

        var scenePath = BuildScenePath(pathRoot);
        if (scenePath.Length > 0)
        {
            if (spawnOverride != null)
                scenePath[0] = spawnOverride.position;
            return scenePath;
        }

        if (leftSide)
        {
            if (!_warnedMissingS1)
            {
                Debug.LogWarning(
                    "[MapPathProvider] Path_S1 웨이포인트 없음 — MapLayout Y자 fallback 사용. " +
                    "Hierarchy: --- Map --- / Path_S1 / P0… 확인.");
                _warnedMissingS1 = true;
            }
        }
        else if (!_warnedMissingS2)
        {
            Debug.LogWarning(
                "[MapPathProvider] Path_S2 웨이포인트 없음 — MapLayout Y자 fallback 사용. " +
                "Hierarchy: --- Map --- / Path_S2 / P0… 확인.");
            _warnedMissingS2 = true;
        }

        return layoutFallback;
    }

    public static Vector2[] BuildScenePath(Transform pathRoot)
    {
        if (pathRoot == null || pathRoot.childCount == 0)
            return System.Array.Empty<Vector2>();

        var waypoints = CollectSortedWaypoints(pathRoot);
        var points = new Vector2[waypoints.Count];
        for (int i = 0; i < waypoints.Count; i++)
            points[i] = waypoints[i].position;

        return points;
    }

    public static Vector2[] GetScenePathOrLayout(string pathRootName, Vector2[] layoutFallback)
    {
        var root = GameObject.Find(pathRootName)?.transform;
        var scenePath = BuildScenePath(root);
        return scenePath.Length > 0 ? scenePath : layoutFallback;
    }

    static List<Transform> CollectSortedWaypoints(Transform pathRoot)
    {
        var waypoints = new List<Transform>(pathRoot.childCount);
        for (int i = 0; i < pathRoot.childCount; i++)
            waypoints.Add(pathRoot.GetChild(i));

        waypoints.Sort(CompareWaypointTransform);
        return waypoints;
    }

    static int CompareWaypointTransform(Transform a, Transform b)
    {
        bool hasA = TryParseWaypointIndex(a.name, out int indexA);
        bool hasB = TryParseWaypointIndex(b.name, out int indexB);

        if (hasA && hasB)
            return indexA.CompareTo(indexB);

        if (hasA != hasB)
            return hasA ? -1 : 1;

        return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
    }

    public static bool TryParseWaypointIndex(string name, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(name) || name[0] != 'P' || name.Length <= 1)
            return false;

        return int.TryParse(name.Substring(1), out index) && index >= 0;
    }
}
