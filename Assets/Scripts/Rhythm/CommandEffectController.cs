using System.Collections.Generic;
using UnityEngine;

public class CommandEffectController : MonoBehaviour
{
    public const float OverloadStrikeDamage = 8f;
    public const float ChainZapPrimaryDamage = 8f;
    public const float ChainZapLinkDamage = 4f;
    public const int ChainZapMaxLinks = 3;

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
            case CommandType.ChainZap:
                ApplyChainZap();
                break;
            case CommandType.TempoUp:
                ApplyTempoUp();
                break;
            case CommandType.TempoDown:
                ApplyTempoDown();
                break;
        }
    }

    void ApplyTempoUp()
    {
        TempoController.Instance?.AddFastStack();
        SimpleAudio.Instance?.PlaySkill(CommandType.TempoUp);
        var tempo = TempoController.Instance;
        Debug.Log($"[Rhythm] TempoUp — fast {tempo?.FastStacks}/{TempoController.MaxStacksPerDirection}, scale {tempo?.CurrentScale:0.##}");
    }

    void ApplyTempoDown()
    {
        TempoController.Instance?.AddSlowStack();
        SimpleAudio.Instance?.PlaySkill(CommandType.TempoDown);
        var tempo = TempoController.Instance;
        Debug.Log($"[Rhythm] TempoDown — slow {tempo?.SlowStacks}/{TempoController.MaxStacksPerDirection}, scale {tempo?.CurrentScale:0.##}");
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

        int fired = 0;
        if (_towers != null)
        {
            foreach (var beatTower in _towers.BeatTowers)
            {
                if (beatTower.FireOnce(beatTower.FallbackDamage))
                    fired++;
            }
        }

        Debug.Log($"[Rhythm] GoldPulse +{ResourceManager.GoldPulseReward}G, BeatTower {fired}기 Fallback 사격 (총 {ResourceManager.Instance.Gold}G)");
    }

    void ApplyRhythmShot()
    {
        if (_towers == null)
            return;

        int fired = 0;
        foreach (var beatTower in _towers.BeatTowers)
        {
            if (beatTower.FireOnce())
                fired++;
        }

        Debug.Log($"[Rhythm] RhythmShot — BeatTower {fired}기 즉시 사격 (Lv별 ActiveDamage)");
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

    void ApplyChainZap()
    {
        if (_towers == null || _towers.BoostTowers.Count == 0)
        {
            Debug.Log("[Rhythm] ChainZap — BoostTower 없음 (데미지 없음)");
            return;
        }

        var inRange = new HashSet<EnemyHealth>();
        var leaders = new List<EnemyHealth>();
        foreach (var boost in _towers.BoostTowers)
        {
            boost.CollectEnemiesInRange(inRange);
            var leader = boost.GetPathLeaderInRange();
            if (leader != null && !leaders.Contains(leader))
                leaders.Add(leader);
        }

        if (leaders.Count == 0)
        {
            Debug.Log("[Rhythm] ChainZap — 범위 내 적 없음");
            return;
        }

        var allEnemies = CollectAllAliveEnemies();
        var damaged = new HashSet<EnemyHealth>();
        int primaryHits = 0;
        int linkHits = 0;

        foreach (var leader in leaders)
        {
            if (!inRange.Contains(leader) || damaged.Contains(leader))
                continue;

            if (ApplyChainFrom(leader, allEnemies, damaged, ref linkHits))
                primaryHits++;
        }

        SimpleAudio.Instance?.PlaySkill(CommandType.ChainZap);
        Debug.Log($"[Rhythm] ChainZap — 선두 {primaryHits} × {ChainZapPrimaryDamage}, 체인 {linkHits} × {ChainZapLinkDamage}");
    }

    bool ApplyChainFrom(
        EnemyHealth leader,
        List<EnemyHealth> allEnemies,
        HashSet<EnemyHealth> damaged,
        ref int linkHits)
    {
        if (leader == null || !leader.IsAlive || damaged.Contains(leader))
            return false;

        damaged.Add(leader);
        leader.TakeDamage(ChainZapPrimaryDamage);
        CombatVfxService.Instance?.PlayChainZapBurst(leader.transform.position, true);

        var current = leader;
        for (int link = 0; link < ChainZapMaxLinks; link++)
        {
            var next = FindNextOnPath(current, allEnemies, damaged);
            if (next == null)
                break;

            damaged.Add(next);
            next.TakeDamage(ChainZapLinkDamage);
            linkHits++;
            CombatVfxService.Instance?.PlayChainZapLink(
                current.transform.position, next.transform.position);
            current = next;
        }

        return true;
    }

    static EnemyHealth FindNextOnPath(
        EnemyHealth from,
        List<EnemyHealth> allEnemies,
        HashSet<EnemyHealth> skip)
    {
        float fromProgress = GetPathProgress(from);
        EnemyHealth best = null;
        float bestProgress = float.MaxValue;

        foreach (var enemy in allEnemies)
        {
            if (enemy == null || !enemy.IsAlive || skip.Contains(enemy))
                continue;

            float progress = GetPathProgress(enemy);
            if (progress <= fromProgress + 0.04f)
                continue;

            if (progress < bestProgress)
            {
                bestProgress = progress;
                best = enemy;
            }
        }

        return best;
    }

    static float GetPathProgress(EnemyHealth enemy)
    {
        if (enemy == null)
            return 0f;

        var path = enemy.GetComponent<EnemyPathProgress>();
        return path != null ? path.Progress : enemy.transform.position.y;
    }

    static List<EnemyHealth> CollectAllAliveEnemies()
    {
        var list = new List<EnemyHealth>();
        foreach (var enemy in Object.FindObjectsByType<EnemyHealth>())
        {
            if (enemy != null && enemy.IsAlive)
                list.Add(enemy);
        }

        return list;
    }
}
