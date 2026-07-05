using UnityEngine;

/// <summary>
/// Waypoint 경로 이동 + 좌우 lane offset (우루루 군집).
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 3.2f;

    Vector2[] _waypoints;
    int _waypointIndex;
    float _lateralOffset;
    EnemyHealth _health;

    public void Initialize(Vector2[] waypoints, EnemyKind kind, float lateralOffset = 0f)
    {
        _waypoints = waypoints;
        _waypointIndex = 0;
        _lateralOffset = lateralOffset;
        _health = GetComponent<EnemyHealth>();
        _health.Configure(kind);

        if (_waypoints == null || _waypoints.Length == 0)
            return;

        transform.position = GetLateralPoint(0);
    }

    void Update()
    {
        if (Time.timeScale <= 0f || _waypoints == null || _waypoints.Length == 0)
            return;

        if (_waypointIndex >= _waypoints.Length)
        {
            ReachCore();
            return;
        }

        var target = GetLateralPoint(_waypointIndex);
        var pos = (Vector2)transform.position;
        float step = moveSpeed * Time.deltaTime;
        var next = Vector2.MoveTowards(pos, target, step);
        transform.position = next;

        if (Vector2.Distance(next, target) <= 0.04f)
            _waypointIndex++;
    }

    Vector2 GetLateralPoint(int index)
    {
        Vector2 wp = _waypoints[index];
        Vector2 dir = GetSegmentDirection(index);
        Vector2 perp = new Vector2(-dir.y, dir.x);

        float pathT = _waypoints.Length <= 1 ? 0f : index / (float)(_waypoints.Length - 1);
        float spread = Mathf.Lerp(1f, 0.45f, pathT);

        return wp + perp * (_lateralOffset * spread);
    }

    Vector2 GetSegmentDirection(int index)
    {
        if (index > 0)
        {
            Vector2 d = _waypoints[index] - _waypoints[index - 1];
            if (d.sqrMagnitude > 0.0001f)
                return d.normalized;
        }

        if (index + 1 < _waypoints.Length)
        {
            Vector2 d = _waypoints[index + 1] - _waypoints[index];
            if (d.sqrMagnitude > 0.0001f)
                return d.normalized;
        }

        return Vector2.down;
    }

    void ReachCore()
    {
        if (_health == null || !_health.IsAlive)
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
        {
            Destroy(gameObject);
            return;
        }

        var core = BaseHealth.Instance ?? FindAnyObjectByType<BaseHealth>();
        if (core != null && core.IsAlive)
            core.TakeDamage(_health.coreDamage);

        Destroy(gameObject);
    }

    public static float RollLaneOffset(EnemyKind kind)
    {
        return kind == EnemyKind.Downbeat
            ? Random.Range(-0.28f, 0.28f)
            : Random.Range(-0.65f, 0.65f);
    }
}
