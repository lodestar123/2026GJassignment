using UnityEngine;

/// <summary>
/// 60s: 장갑(피해 25%↓) · 90s: 강박마다 HP 회복.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EliteEnemyBehavior : MonoBehaviour
{
    EnemyHealth _health;
    SpriteRenderer _sprite;
    Color _baseColor;
    float _pulse;

    public EliteTier Tier { get; private set; }

    public void Initialize(EliteTier tier)
    {
        Tier = tier;
        _health = GetComponent<EnemyHealth>();
        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite != null)
            _baseColor = _sprite.color;

        if (tier == EliteTier.Wave90 && BeatClock.Instance != null)
            BeatClock.Instance.OnBeat += OnBeat;
    }

    void OnDestroy()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;
    }

    void Update()
    {
        if (_sprite == null)
            return;

        _pulse += Time.deltaTime * 3f;
        float glow = 0.85f + Mathf.Sin(_pulse) * 0.15f;
        _sprite.color = new Color(_baseColor.r * glow, _baseColor.g * glow, _baseColor.b * glow, _baseColor.a);
    }

    void OnBeat()
    {
        if (Tier != EliteTier.Wave90 || _health == null || !_health.IsAlive)
            return;

        if (BeatClock.Instance == null || !BeatClock.Instance.IsDownbeat)
            return;

        _health.Heal(EnemyBalance.EliteWave90RegenPerDownbeat);
        CombatVfxService.Instance?.PlayEliteRegenPulse(transform.position);
    }

    public float ModifyIncomingDamage(float amount)
    {
        if (Tier == EliteTier.Wave60)
            return amount * EnemyBalance.EliteWave60DamageTaken;

        return amount;
    }
}
