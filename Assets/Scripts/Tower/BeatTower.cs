using UnityEngine;

[RequireComponent(typeof(Tower))]
public class BeatTower : MonoBehaviour
{
    public const float ActiveDamage = 2f;
    public const float FallbackDamage = 0.6f;
    public const float InputWindowSeconds = 1.2f;

    Tower _tower;
    RhythmInputRecorder _input;

    void Awake()
    {
        _tower = GetComponent<Tower>();
        _tower.towerType = TowerType.Beat;
        _input = FindAnyObjectByType<RhythmInputRecorder>();
        TowerRegistry.Instance?.Register(this);
        TowerRegistry.Instance?.RegisterTower(_tower);
    }

    void Start()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat += OnBeat;
    }

    void OnDestroy()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;
        TowerRegistry.Instance?.Unregister(this);
        TowerRegistry.Instance?.UnregisterTower(_tower);
    }

    void OnBeat()
    {
        if (Time.timeScale <= 0f)
            return;

        float damage = (_input != null && _input.HasRecentInput(InputWindowSeconds))
            ? ActiveDamage
            : FallbackDamage;

        var target = _tower.GetPathLeaderInRange();
        if (target != null)
        {
            CombatVfxService.Instance?.PlayTowerShot(
                transform.position, target.transform.position, _tower.towerType, damage, transform);
            target.TakeDamage(damage);
        }
    }

    public bool FireOnce(float damage)
    {
        var target = _tower.GetPathLeaderInRange();
        if (target == null)
            return false;

        CombatVfxService.Instance?.PlayTowerShot(
            transform.position, target.transform.position, _tower.towerType, damage, transform);
        target.TakeDamage(damage);
        return true;
    }
}
