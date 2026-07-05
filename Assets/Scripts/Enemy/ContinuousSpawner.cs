using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2분 스폰 — BALANCE §7.1 · 필드 max · 시작 3s delay · 구간별 급변 대신 smooth ramp.
/// </summary>
public class ContinuousSpawner : MonoBehaviour
{
    public const int FieldCap = 85;

    [SerializeField] Transform enemyRoot;
    [SerializeField] float startDelaySeconds = GameManager.SpawnGraceSeconds;

    float _elapsed;
    float _spawnTimer;
    bool _spawnFromS1 = true;
    bool _started;

    readonly List<EnemyHealth> _alive = new();

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

        _spawnTimer = phase.Interval;
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

    void SpawnEnemy(Vector2 spawnPos, Vector2[] waypoints, EnemyKind kind)
    {
        var go = new GameObject(kind == EnemyKind.Downbeat ? "Downbeat" : "EighthNote");
        if (enemyRoot != null)
            go.transform.SetParent(enemyRoot);
        go.transform.position = spawnPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Enemy;
        sr.color = kind == EnemyKind.Downbeat
            ? new Color(0.67f, 0.28f, 0.74f)
            : new Color(0.26f, 0.65f, 0.96f);
        go.transform.localScale = kind == EnemyKind.Downbeat
            ? Vector3.one * 0.55f
            : Vector3.one * 0.45f;
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var health = go.AddComponent<EnemyHealth>();
        health.Configure(kind);
        health.OnDied += HandleEnemyDied;

        float lane = EnemyMovement.RollLaneOffset(kind);
        var movement = go.AddComponent<EnemyMovement>();
        movement.moveSpeed = kind == EnemyKind.Downbeat
            ? EnemyBalance.DownbeatSpeed
            : EnemyBalance.EighthNoteSpeed;
        movement.Initialize(waypoints, kind, lane);

        go.AddComponent<EnemyPathProgress>();
        go.AddComponent<EnemyBeatBounce>();

        _alive.Add(health);
    }

    void HandleEnemyDied(EnemyHealth enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        _alive.Remove(enemy);

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddGold(enemy.goldReward);

        RunStats.Instance?.RecordEnemyKill(enemy.kind);
        Destroy(enemy.gameObject);
    }

    /// <summary>
    /// 0~120s 구간을 0~1로 정규화해 간격·밀도·양쪽 동시 스폰을 smooth step으로 보간.
    /// </summary>
    static SpawnPhase EvaluateSpawn(float elapsed)
    {
        float spawnTime = Mathf.Max(0f, elapsed - GameManager.SpawnGraceSeconds);
        float t = Mathf.Clamp01(spawnTime / GameManager.MatchDurationSeconds);
        float ramp = SmoothStep(t);

        // 1.75s → 0.55s (구간 점프 0.35s 대신 완만히 단축)
        float interval = Mathf.Lerp(1.75f, 0.55f, ramp);

        // 8분:강박 5% → 35%
        float downbeatChance = Mathf.Lerp(0.05f, 0.35f, ramp);

        // ~30s부터 양쪽 동시 스폰 비율 상승, ~96s쯤 거의 항상 동시
        float simultaneousChance = SmoothStep(Mathf.InverseLerp(0.25f, 0.8f, t));
        bool simultaneous = Random.value < simultaneousChance;

        // ~72s부터 측당 2마리로 서서히 증가
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
