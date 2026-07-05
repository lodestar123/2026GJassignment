using UnityEngine;

/// <summary>
/// 몬스터 종류별 프리팹 — <b>Project의 EnemyPrefabRegistry.asset</b> Inspector에서 등록.
/// (.cs 코드에 드래그하는 곳이 아님)
/// </summary>
[CreateAssetMenu(fileName = "EnemyPrefabRegistry", menuName = "Beat Defender/Enemy Prefab Registry")]
public class EnemyPrefabRegistry : ScriptableObject
{
    public const string ResourcesPath = "BeatDefender/EnemyPrefabRegistry";

    static EnemyPrefabRegistry _cached;

    [Header("Enemy Prefabs (Project .asset Inspector에서 등록)")]
    [SerializeField] GameObject eighthNote;
    [SerializeField] GameObject downbeat;
    [SerializeField] GameObject elite;

    public static EnemyPrefabRegistry Instance
    {
        get
        {
            if (_cached == null)
                _cached = Resources.Load<EnemyPrefabRegistry>(ResourcesPath);
            return _cached;
        }
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => _cached = null;
#endif

    public GameObject GetPrefab(EnemyKind kind) => kind switch
    {
        EnemyKind.EighthNote => eighthNote,
        EnemyKind.Downbeat => downbeat,
        EnemyKind.Elite => elite,
        _ => null
    };

    public bool HasPrefab(EnemyKind kind) => GetPrefab(kind) != null;

    public void SetPrefabs(GameObject eighth, GameObject down, GameObject eliteEnemy)
    {
        eighthNote = eighth;
        downbeat = down;
        elite = eliteEnemy;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (eighthNote == null || downbeat == null || elite == null)
            Debug.LogWarning(
                "[EnemyPrefabRegistry] 프리팹 슬롯이 비어 있습니다. " +
                "Assets/Resources/BeatDefender/EnemyPrefabRegistry.asset 을 선택해 Inspector에서 등록하세요.",
                this);
    }
#endif
}