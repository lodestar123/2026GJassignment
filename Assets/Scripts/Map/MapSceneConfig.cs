using UnityEngine;

/// <summary>
/// 씬별 활성 맵 레이아웃 — GameScene(Classic) / GameSceneSpiral(SpiralIslands) 등.
/// </summary>
public class MapSceneConfig : MonoBehaviour
{
    public static MapSceneConfig Instance { get; private set; }

    [SerializeField] MapLayoutKind layoutKind = MapLayoutKind.Classic;

    public MapLayoutKind LayoutKind => layoutKind;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
