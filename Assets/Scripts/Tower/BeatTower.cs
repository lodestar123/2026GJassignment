using UnityEngine;

[RequireComponent(typeof(Tower))]
public class BeatTower : MonoBehaviour
{
    [SerializeField] int level = 1;

    [Header("타워 이미지 (BeatTower 프리팹 Inspector)")]
    [SerializeField] Sprite spriteLevel1;
    [SerializeField] Sprite spriteLevel2;
    [SerializeField] Sprite spriteLevel3;

    Tower _tower;
    int _upgradeGoldSpent;
    bool _subscribed;

    public int Level => level;
    public int UpgradeGoldSpent => _upgradeGoldSpent;
    public float ActiveDamage => BeatTowerUpgrade.GetActiveDamage(level);
    public float FallbackDamage => BeatTowerUpgrade.GetFallbackDamage(level);
    public bool CanUpgrade => BeatTowerUpgrade.CanUpgrade(level);
    public int NextUpgradeCost => BeatTowerUpgrade.GetUpgradeCost(level);

    void Awake()
    {
        _tower = GetComponent<Tower>();
        _tower.towerType = TowerType.Beat;
        level = Mathf.Clamp(level, 1, BeatTowerUpgrade.MaxLevel);
        TowerRegistry.Instance?.Register(this);
        TowerRegistry.Instance?.RegisterTower(_tower);
        ApplyLevelVisual();
    }

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void OnDestroy()
    {
        TryUnsubscribe();
        TowerRegistry.Instance?.Unregister(this);
        TowerRegistry.Instance?.UnregisterTower(_tower);
    }

    void TrySubscribe()
    {
        if (_subscribed || BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnBeat -= OnBeat;
        BeatClock.Instance.OnBeat += OnBeat;
        _subscribed = true;
    }

    void TryUnsubscribe()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;

        _subscribed = false;
    }

    void OnDisable() => TryUnsubscribe();

    public bool TryUpgrade()
    {
        if (!CanUpgrade)
            return false;

        int cost = NextUpgradeCost;
        _upgradeGoldSpent += cost;
        level++;
        ApplyLevelVisual();
        return true;
    }

    public int GetTotalInvestedGold()
    {
        return TowerPlacer.GetCost(TowerType.Beat) + _upgradeGoldSpent;
    }

    public void RefreshFromRegistry() => ApplyLevelVisual();

    public void SetLevelSprites(Sprite level1, Sprite level2, Sprite level3)
    {
        spriteLevel1 = level1;
        spriteLevel2 = level2;
        spriteLevel3 = level3;
    }

    void ApplyLevelVisual()
    {
        float scale = Tower.BaseVisualScale + (level - 1) * 0.06f;
        transform.localScale = Vector3.one * scale;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        var sprite = ResolveSpriteForLevel(level);
        if (sprite != null)
            sr.sprite = sprite;

        sr.color = Color.white;
        if (sr.sortingOrder < 10)
            sr.sortingOrder = 10;

        SpriteRendererUtility.EnsureSpriteMaterial(sr);
        GetComponent<TowerFireRecoil>()?.SyncBaseScale();
        GetComponent<TowerRangeVisualizer>()?.RefreshRingScale();
    }

    Sprite ResolveSpriteForLevel(int lv)
    {
        return lv switch
        {
            1 => spriteLevel1,
            2 => spriteLevel2 != null ? spriteLevel2 : spriteLevel1,
            >= 3 => spriteLevel3 != null ? spriteLevel3 : (spriteLevel2 != null ? spriteLevel2 : spriteLevel1),
            _ => spriteLevel1
        };
    }

    void OnBeat()
    {
        if (Time.timeScale <= 0f)
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return;

        float damage = FallbackDamage;

        var target = _tower.GetPathLeaderInRange();
        if (target != null)
        {
            CombatVfxService.Instance?.PlayTowerShot(
                transform.position, target.transform.position, damage, transform, target);
        }
    }

    public bool FireOnce(float damageOverride = -1f)
    {
        var target = _tower.GetPathLeaderInRange();
        if (target == null)
            return false;

        float damage = damageOverride >= 0f ? damageOverride : ActiveDamage;
        CombatVfxService.Instance?.PlayTowerShot(
            transform.position, target.transform.position, damage, transform, target);
        return true;
    }

    public bool FireRhythmPerfectSalvo(float damageOverride = -1f)
    {
        float damage = damageOverride >= 0f ? damageOverride : ActiveDamage;
        var target = _tower.GetPathLeaderInRange();
        Vector3 to = target != null
            ? target.transform.position
            : transform.position + Vector3.up * 2.4f;

        CombatVfxService.Instance?.PlayRhythmSalvoShot(
            transform.position, to, damage, transform, target);

        return target != null;
    }
}
