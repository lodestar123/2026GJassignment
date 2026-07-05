using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Tower : MonoBehaviour
{
    public const float DefaultRange = 2.5f;
    public const float BaseVisualScale = 0.6f;
    public const float HoverRadiusAtBaseScale = 0.32f;

    public TowerType towerType = TowerType.Beat;
    public float range = DefaultRange;

    CircleCollider2D _hoverCollider;

    public float Range => range;
    public float HoverRadius => HoverRadiusAtBaseScale * (transform.localScale.x / BaseVisualScale);

    void Awake()
    {
        _hoverCollider = GetComponent<CircleCollider2D>();
        RefreshHoverCollider();
        if (GetComponent<TowerRangeVisualizer>() == null)
            gameObject.AddComponent<TowerRangeVisualizer>();
        if (GetComponent<TowerFireRecoil>() == null)
            gameObject.AddComponent<TowerFireRecoil>();
    }

    void OnValidate()
    {
        RefreshHoverCollider();
    }

    public void RefreshHoverCollider()
    {
        if (_hoverCollider == null)
            _hoverCollider = GetComponent<CircleCollider2D>();

        if (_hoverCollider != null)
        {
            _hoverCollider.isTrigger = false;
            _hoverCollider.radius = HoverRadius;
        }
    }

    void LateUpdate()
    {
        if (_hoverCollider == null)
            return;

        float r = HoverRadius;
        if (!Mathf.Approximately(_hoverCollider.radius, r))
            _hoverCollider.radius = r;
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
