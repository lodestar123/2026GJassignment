using UnityEngine;

/// <summary>
/// 맵 레이아웃 정의 — Classic(Y자 합류).
/// </summary>
public static class MapLayout
{
    public static MapLayoutDefinition Active =>
        MapSceneConfig.Instance != null
            ? Get(MapSceneConfig.Instance.LayoutKind)
            : Classic;

    public static MapLayoutDefinition Get(MapLayoutKind kind) => Classic;

    public static MapLayoutDefinition Classic { get; } = new()
    {
        Kind = MapLayoutKind.Classic,
        DisplayName = "Y자 합류 (본편)",
        CorePosition = new Vector2(0f, -3.5f),
        SpawnS1 = new Vector2(-5f, 4f),
        SpawnS2 = new Vector2(5f, 4f),
        PathFromS1 = new[]
        {
            new Vector2(-5f, 4f),
            new Vector2(-3f, 2f),
            new Vector2(-1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(0f, -2.5f),
            new Vector2(0f, -3.5f)
        },
        PathFromS2 = new[]
        {
            new Vector2(5f, 4f),
            new Vector2(3f, 2f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(0f, -2.5f),
            new Vector2(0f, -3.5f)
        },
        PlacementSlots = new[]
        {
            new Vector2(-4f, 3f), new Vector2(4f, 3f),
            new Vector2(-4f, 2f), new Vector2(-2f, 2f), new Vector2(2f, 2f), new Vector2(4f, 2f),
            new Vector2(-4f, 0f), new Vector2(-2f, 0f), new Vector2(2f, 0f), new Vector2(4f, 0f),
            new Vector2(-4f, -1f), new Vector2(4f, -1f),
            new Vector2(-1f, -2f), new Vector2(1f, -2f)
        }
    };

    public static Vector2 CorePosition => Active.CorePosition;
    public static Vector2 SpawnS1 => Active.SpawnS1;
    public static Vector2 SpawnS2 => Active.SpawnS2;
    public static Vector2 ChokePoint => new Vector2(0f, -1f);
    public static Vector2[] PathFromS1 => Active.PathFromS1;
    public static Vector2[] PathFromS2 => Active.PathFromS2;
    public static Vector2[] PlacementSlots => Active.PlacementSlots;
}
