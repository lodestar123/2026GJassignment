using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 하이어라키 Path_S1 / Path_S2 자식 위치를 적 경로로 사용. 없으면 MapLayout fallback.
/// </summary>
public class MapPathProvider : MonoBehaviour
{
    public static MapPathProvider Instance { get; private set; }

    [SerializeField] Transform pathS1Root;
    [SerializeField] Transform pathS2Root;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (pathS1Root == null)
            pathS1Root = transform.Find("Path_S1");
        if (pathS2Root == null)
            pathS2Root = transform.Find("Path_S2");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Vector2[] GetPathForLeftSpawn() => GetPath(pathS1Root, MapLayout.Active.PathFromS1);

    public Vector2[] GetPathForRightSpawn() => GetPath(pathS2Root, MapLayout.Active.PathFromS2);

    static Vector2[] GetPath(Transform root, Vector2[] fallback)
    {
        if (root == null || root.childCount == 0)
            return fallback;

        var points = new List<Vector2>(root.childCount);
        for (int i = 0; i < root.childCount; i++)
            points.Add(root.GetChild(i).position);

        return points.ToArray();
    }
}
