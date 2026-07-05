using UnityEngine;

/// <summary>
/// 경로 선두 판별 — Y가 작을수록 본진에 가까움 (MAP 하단 Core).
/// </summary>
public class EnemyPathProgress : MonoBehaviour
{
    public float Progress => transform.position.y;
}
