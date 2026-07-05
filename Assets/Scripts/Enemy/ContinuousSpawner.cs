using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2분 스폰 — BALANCE §7.1 · 필드 max · 시작 3s delay · 구간별 급변 대신 smooth ramp.
/// </summary>
public class ContinuousSpawner : MonoBehaviour
{
    public const int FieldCap = 85;

    [SerializeField] Transform enemyRoot;
    [SerializeField] EnemyPrefabRegistry prefabRegistry;
    [SerializeField] float startDelaySeconds = GameManager.SpawnGraceSeconds;

    float _elapsed;
    float _spawnTimer;
    bool _spawnFromS1 = true;
    bool _started;
    float _spawnIntervalScale = 1f;

    readonly List<EnemyHealth> _alive = new();

    void Awake()
    {
        if (prefabRegistry == null)
            prefabRegistry = EnemyPrefabRegistry.Instance;

        ValidatePrefabSetup();

        if (GetComponent<EliteSpawnDirector>() == null)
            gameObject.AddComponent<EliteSpawnDirector>();
        if (GetComponent<MatchMilestoneDirector>() == null)
            gameObject.AddComponent<MatchMilestoneDirector>();
    }

    void ValidatePrefabSetup()
    {
        if (prefabRegistry == null)
        {
            Debug.LogWarning(
                "[ContinuousSpawner] EnemyPrefabRegistry를 찾지 못했습니다. " +
                "Assets/Resources/BeatDefender/EnemyPrefabRegistry.asset 이 있는지 확인하세요. " +
                "greybox 적으로 스폰됩니다.");
            return;
        }

        if (!prefabRegistry.HasPrefab(EnemyKind.EighthNote))
            Debug.LogWarning("[ContinuousSpawner] Eighth Note 프리팹 슬롯이 비어 있습니다.", prefabRegistry);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return;

        _elapsed += Time.deltaTime;
        if (_elapsed < startDelaySeconds)
            return;

        if (!_started)
        {
            _started = true;
            _spawnTimer = 0f;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f || _alive.Count >= FieldCap)
            return;

        var phase = EvaluateSpawn(_elapsed);

        if (phase.Simultaneous)
        {
            SpawnFromSide(MapLayout.SpawnS1, phase);
            if (_alive.Count < FieldCap)
                SpawnFromSide(MapLayout.SpawnS2, phase);
        }
        else
        {
            var spawn = _spawnFromS1 ? MapLayout.SpawnS1 : MapLayout.SpawnS2;
            SpawnFromSide(spawn, phase);
            _spawnFromS1 = !_spawnFromS1;
        }

        _spawnTimer = phase.Interval * _spawnIntervalScale;
    }

    /// <summary>값이 작을수록 스폰 간격 단축 (Last Stand 등).</summary>
    public void ApplySpawnPressure(float intervalScale)
    {
        _spawnIntervalScale = Mathf.Min(_spawnIntervalScale, Mathf.Clamp(intervalScale, 0.25f, 1f));
    }

    void SpawnFromSide(Vector2 spawnPos, SpawnPhase phase)
    {
        for (int i = 0; i < phase.CountPerSide && _alive.Count < FieldCap; i++)
            TrySpawn(spawnPos, phase);
    }

    void TrySpawn(Vector2 spawnPos, SpawnPhase phase)
    {
        if (_alive.Count >= FieldCap)
            return;

        var kind = Random.value < phase.DownbeatChance ? EnemyKind.Downbeat : EnemyKind.EighthNote;
        var waypoints = spawnPos.x < 0f
            ? (MapPathProvider.Instance != null
                ? MapPathProvider.Instance.GetPathForLeftSpawn()
                : MapLayout.PathFromS1)
            : (MapPathProvider.Instance != null
                ? MapPathProvider.Instance.GetPathForRightSpawn()
                : MapLayout.PathFromS2);
        SpawnEnemy(spawnPos, waypoints, kind);
    }

    public void SpawnEliteWave(EliteTier tier)
    {
        var pathLeft = MapPathProvider.Instance != null
            ? MapPathProvider.Instance.GetPathForLeftSpawn()
            : MapLayout.PathFromS1;
        var pathRight = MapPathProvider.Instance != null
            ? MapPathProvider.Instance.GetPathForRightSpawn()
            : MapLayout.PathFromS2;

        SpawnEnemy(MapLayout.SpawnS1, pathLeft, EnemyKind.Elite, tier);
        SpawnEnemy(MapLayout.SpawnS2, pathRight, EnemyKind.Elite, tier);
    }

    void SpawnEnemy(Vector2 spawnPos, Vector2[] waypoints, EnemyKind kind, EliteTier eliteTier = EliteTier.None)
    {
        var go = InstantiateEnemy(kind, spawnPos);
        go.name = GetEnemyObjectName(kind, eliteTier);

        var health = go.GetComponent<EnemyHealth>();
        if (health == null)
            health = go.AddComponent<EnemyHealth>();

        float lane = EnemyMovement.RollLaneOffset(kind);
        var movement = go.GetComponent<EnemyMovement>();
        if (movement == null)
            movement = go.AddComponent<EnemyMovement>();

        movement.moveSpeed = GetMoveSpeed(kind, eliteTier);
        movement.Initialize(waypoints, kind, lane);

        health.Configure(kind, eliteTier);
        health.OnDied += HandleEnemyDied;

        if (go.GetComponent<EnemyPathProgress>() == null)
            go.AddComponent<EnemyPathProgress>();
        if (go.GetComponent<EnemyBeatBounce>() == null)
            go.AddComponent<EnemyBeatBounce>();

        if (kind == EnemyKind.Elite)
        {
            var elite = go.GetComponent<EliteEnemyBehavior>() ?? go.AddComponent<EliteEnemyBehavior>();
            elite.Initialize(eliteTier);
            ApplyEliteVisual(go.transform, go.GetComponent<SpriteRenderer>(), eliteTier);
        }

        _alive.Add(health);
    }

    GameObject InstantiateEnemy(EnemyKind kind, Vector2 spawnPos)
    {
        var registry = prefabRegistry != null ? prefabRegistry : EnemyPrefabRegistry.Instance;
        var prefab = registry != null ? registry.GetPrefab(kind) : null;

        if (prefab != null)
        {
            var instance = Instantiate(prefab, spawnPos, Quaternion.identity, enemyRoot);
            return instance;
        }

        Debug.LogWarning($"[ContinuousSpawner] {kind} 프리팹 없음 → legacy greybox 스폰.");
        return CreateLegacyEnemy(kind, spawnPos);
    }

    GameObject CreateLegacyEnemy(EnemyKind kind, Vector2 spawnPos)
    {
        var go = new GameObject("Enemy");
        if (enemyRoot != null)
            go.transform.SetParent(enemyRoot);
        go.transform.position = spawnPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = kind == EnemyKind.Elite ? 6 : 5;
        ApplyLegacyVisual(go.transform, sr, kind);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = kind == EnemyKind.Elite ? 0.58f : 0.5f;

        return go;
    }

    static string GetEnemyObjectName(EnemyKind kind, EliteTier tier)
    {
        if (kind != EnemyKind.Elite)
            return kind == EnemyKind.Downbeat ? "Downbeat" : "EighthNote";

        return tier == EliteTier.Wave90 ? "Elite90" : "Elite60";
    }

    static void ApplyEliteVisual(Transform transform, SpriteRenderer sr, EliteTier tier)
    {
        if (sr == null)
            return;

        sr.color = tier == EliteTier.Wave90
            ? new Color(1f, 0.45f, 0.2f)
            : new Color(1f, 0.78f, 0.22f);
        transform.localScale = Vector3.one * (tier == EliteTier.Wave90 ? 0.78f : 0.72f);
    }

    static void ApplyLegacyVisual(Transform transform, SpriteRenderer sr, EnemyKind kind)
    {
        sr.sprite = GreyboxSprites.Enemy;

        if (kind == EnemyKind.Elite)
        {
            sr.color = new Color(1f, 0.78f, 0.22f);
            transform.localScale = Vector3.one * 0.72f;
            return;
        }

        sr.color = kind == EnemyKind.Downbeat
            ? new Color(0.67f, 0.28f, 0.74f)
            : new Color(0.26f, 0.65f, 0.96f);
        transform.localScale = kind == EnemyKind.Downbeat
            ? Vector3.one * 0.55f
            : Vector3.one * 0.45f;
    }

    static float GetMoveSpeed(EnemyKind kind, EliteTier tier)
    {
        if (kind == EnemyKind.Elite)
            return tier == EliteTier.Wave90
                ? EnemyBalance.EliteWave90Speed
                : EnemyBalance.EliteWave60Speed;

        return kind == EnemyKind.Downbeat
            ? EnemyBalance.DownbeatSpeed
            : EnemyBalance.EighthNoteSpeed;
    }

    void HandleEnemyDied(EnemyHealth enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        _alive.Remove(enemy);

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddGold(enemy.goldReward);

        RunStats.Instance?.RecordEnemyKill(enemy.kind, enemy.eliteTier);
        Destroy(enemy.gameObject);
    }

    static SpawnPhase EvaluateSpawn(float elapsed)
    {
        float spawnTime = Mathf.Max(0f, elapsed - GameManager.SpawnGraceSeconds);
        float t = Mathf.Clamp01(spawnTime / GameManager.MatchDurationSeconds);
        float ramp = SmoothStep(t);

        float interval = Mathf.Lerp(1.75f, 0.55f, ramp);
        float downbeatChance = Mathf.Lerp(0.05f, 0.35f, ramp);
        float simultaneousChance = SmoothStep(Mathf.InverseLerp(0.25f, 0.8f, t));
        bool simultaneous = Random.value < simultaneousChance;
        float countRamp = SmoothStep(Mathf.InverseLerp(0.6f, 0.95f, t));
        int countPerSide = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 2f, countRamp)), 1, 2);

        return new SpawnPhase(interval, downbeatChance, simultaneous, countPerSide);
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);

    readonly struct SpawnPhase
    {
        public readonly float Interval;
        public readonly float DownbeatChance;
        public readonly bool Simultaneous;
        public readonly int CountPerSide;

        public SpawnPhase(float interval, float downbeatChance, bool simultaneous, int countPerSide)
        {
            Interval = interval;
            DownbeatChance = downbeatChance;
            Simultaneous = simultaneous;
            CountPerSide = Mathf.Max(1, countPerSide);
        }
    }
}
