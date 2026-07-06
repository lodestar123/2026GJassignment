using UnityEngine;

/// <summary>
/// 프리팹 인스턴스 — 에디터에서는 PrefabUtility, 플레이에서는 Instantiate.
/// </summary>
public static class PrefabSpawnUtility
{
    public static GameObject Instantiate(GameObject prefab, Transform parent)
    {
        if (prefab == null)
            return null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
        return Object.Instantiate(prefab, parent);
    }

    public static GameObject Instantiate(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        var go = Instantiate(prefab, parent);
        if (go == null)
            return null;

        go.transform.SetPositionAndRotation(position, rotation);
        return go;
    }
}
