using UnityEngine;

[CreateAssetMenu(fileName = "EnemyVisualConfig", menuName = "Beat Defender/Enemy Visual Config")]
public class EnemyVisualConfig : ScriptableObject
{
    public const string ResourcesPath = "BeatDefender/EnemyVisualConfig";

    [SerializeField] Sprite[] eighthNoteWalkCycle;

    public Sprite[] EighthNoteWalkCycle => eighthNoteWalkCycle;

    public void SetEighthNoteWalkCycle(Sprite[] frames) => eighthNoteWalkCycle = frames;
}
