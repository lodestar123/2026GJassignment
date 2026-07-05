using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Tower : MonoBehaviour
{
    public const float DefaultRange = 2.5f;

    public TowerType towerType = TowerType.Beat;
    public float range = DefaultRange;

    CircleCollider2D _rangeCollider;

    public float Range => range;

    void Awake()
    {
        _rangeCollider = GetComponent<CircleCollider2D>();
        _rangeCollider.isTrigger = false;
        ApplyRange();
        if (GetComponent<TowerRangeVisualizer>() == null)
            gameObject.AddComponent<TowerRangeVisualizer>();
        if (GetComponent<TowerFireRecoil>() == null)
            gameObject.AddComponent<TowerFireRecoil>();
    }

    void OnValidate()
    {
        ApplyRange();
    }

    void ApplyRange()
    {
        if (_rangeCollider == null)
            _rangeCollider = GetComponent<CircleCollider2D>();

        if (_rangeCollider != null)
        {
            _rangeCollider.isTrigger = false;
            _rangeCollider.radius = range;
        }
    }

    public bool IsEnemyInRange(EnemyHealth enemy)
    {
        if (enemy == null || !enemy.IsAlive)
            return false;

        return GetSqrDistanceTo(enemy.transform.position) <= range * range;
    }

    float GetSqrDistanceTo(Vector3 worldPoint)
    {
        var delta = (Vector2)worldPoint - (Vector2)transform.position;
        return delta.sqrMagnitude;
    }

    public void CollectEnemiesInRange(HashSet<EnemyHealth> results)
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null && enemy.IsAlive && IsEnemyInRange(enemy))
                results.Add(enemy);
        }
    }

    public EnemyHealth GetPathLeaderInRange()
    {
        EnemyHealth best = null;
        float bestProgress = float.MaxValue;

        var hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null || !enemy.IsAlive || !IsEnemyInRange(enemy))
                continue;

            var path = enemy.GetComponent<EnemyPathProgress>();
            float progress = path != null ? path.Progress : hit.transform.position.y;
            if (progress < bestProgress)
            {
                bestProgress = progress;
                best = enemy;
            }
        }

        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = towerType switch
        {
            TowerType.Beat => Color.white,
            TowerType.Strike => Color.red,
            TowerType.Boost => new Color(1f, 0.6f, 0f),
            _ => Color.gray
        };
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
