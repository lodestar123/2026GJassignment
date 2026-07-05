using UnityEngine;

public sealed class MapLayoutDefinition
{
    public MapLayoutKind Kind { get; set; }
    public string DisplayName { get; set; }
    public Vector2 CorePosition { get; set; }
    public Vector2 SpawnS1 { get; set; }
    public Vector2 SpawnS2 { get; set; }
    public Vector2[] PathFromS1 { get; set; }
    public Vector2[] PathFromS2 { get; set; }
    public Vector2[] PlacementSlots { get; set; }
    public Vector2[] IslandDecorations { get; set; } = System.Array.Empty<Vector2>();
}
