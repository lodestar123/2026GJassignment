using System.Collections.Generic;
using UnityEngine;

public class CommandEffectController : MonoBehaviour
{
    public const float OverloadStrikeDamage = 8f;
    public const float BpmBoostTowerDamage = 4f;
    public const float RhythmShotDamage = 2f;
    public const float BpmBoostDuration = 6f;

    TowerRegistry _towers;

    void Awake()
    {
        _towers = GetComponent<TowerRegistry>();
        if (_towers == null)
            _towers = FindAnyObjectByType<TowerRegistry>();
    }

    public void Apply(CommandType type, JudgmentResult judgment)
    {
        if (judgment == JudgmentResult.Miss || judgment == JudgmentResult.Cooldown
            || judgment == JudgmentResult.NoTower)
            return;

        switch (type)
        {
            case CommandType.GoldPulse:
                ApplyGoldPulse();
                break;
            case CommandType.RhythmShot:
                ApplyRhythmShot();
                break;
            case CommandType.OverloadStrike:
                ApplyOverloadStrike();
                break;
            case CommandType.BPMBoost:
                ApplyBpmBoost();
                break;
        }

    }

    void ApplyGoldPulse()
    {
        if (ResourceManager.Instance == null)
            return;

        ResourceManager.Instance.AddGold(ResourceManager.GoldPulseReward);
        var hudPos = Camera.main != null
            ? Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.88f, 10f))
            : Vector3.zero;
        CombatVfxService.Instance?.PlayGoldPulsePopup(hudPos, ResourceManager.GoldPulseReward);
        Debug.Log($"[Rhythm] GoldPulse +{ResourceManager.GoldPulseReward}G (총 {ResourceManager.Instance.Gold}G)");
    }

    void ApplyRhythmShot()
    {
        if (_towers == null)
            return;

        int fired = 0;
        foreach (var beatTower in _towers.BeatTowers)
        {
            if (beatTower.FireOnce(RhythmShotDamage))
                fired++;
        }

        Debug.Log($"[Rhythm] RhythmShot — BeatTower {fired}기 즉시 사격 ({RhythmShotDamage} dmg)");
    }

    void ApplyOverloadStrike()
    {
        if (_towers == null || _towers.StrikeTowers.Count == 0)
        {
            Debug.Log("[Rhythm] OverloadStrike — StrikeTower 없음 (데미지 없음)");
            return;
        }

        var hit = new HashSet<EnemyHealth>();
        float ringRadius = Tower.DefaultRange;
        Vector3 ringCenter = _towers.StrikeTowers[0].transform.position;
        foreach (var strike in _towers.StrikeTowers)
        {
            ringCenter = strike.transform.position;
            ringRadius = strike.Range;
            strike.CollectEnemiesInRange(hit);
        }

        CombatVfxService.Instance?.PlayOverloadRing(ringCenter, ringRadius);

        foreach (var enemy in hit)
            enemy.TakeDamage(OverloadStrikeDamage);

        Debug.Log($"[Rhythm] OverloadStrike — {hit.Count}적 × {OverloadStrikeDamage} dmg");
    }

    void ApplyBpmBoost()
    {
        BeatClock.Instance?.SetBoost(BpmBoostDuration);

        if (_towers != null && _towers.BoostTowers.Count > 0)
        {
            var hit = new HashSet<EnemyHealth>();
            foreach (var boost in _towers.BoostTowers)
                boost.CollectEnemiesInRange(hit);

            foreach (var enemy in hit)
                enemy.TakeDamage(BpmBoostTowerDamage);

            float bpm = BeatClock.Instance != null ? BeatClock.Instance.CurrentBpm : 0f;
            Debug.Log($"[Rhythm] BPMBoost — {bpm:0} BPM for {BpmBoostDuration}s + {hit.Count} enemies x {BpmBoostTowerDamage} dmg");
        }
        else
        {
            float bpm = BeatClock.Instance != null ? BeatClock.Instance.CurrentBpm : 0f;
            Debug.Log($"[Rhythm] BPMBoost — {bpm:0} BPM for {BpmBoostDuration}s (no BoostTower, no range dmg)");
        }
    }
}
