using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 스프라이트 교체 없이 greybox 기반 전투 연출 — 포탄, 피격, 파티클, 팝업, 링.
/// </summary>
public class CombatVfxService : MonoBehaviour
{
    public static CombatVfxService Instance { get; private set; }

    Transform _poolRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _poolRoot = new GameObject("CombatVfxPool").transform;
        _poolRoot.SetParent(transform);
        ScreenShake.EnsureOnMainCamera();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayTowerShot(Vector3 from, Vector3 to, TowerType towerType, float damage, Transform towerTransform = null)
    {
        StartCoroutine(ProjectileRoutine(from, to, GetShotColor(towerType), damage));
        if (towerTransform != null)
            towerTransform.GetComponent<TowerFireRecoil>()?.Punch();
        SimpleAudio.Instance?.PlayTowerFire(towerType);
    }

    public void ReportDamage(EnemyHealth enemy, float amount, Vector3? sourcePos = null)
    {
        if (enemy == null)
            return;

        var pos = enemy.transform.position;
        if (enemy.IsAlive)
            StartCoroutine(HitFlashRoutine(enemy));
        SpawnHitParticles(pos, GetDamageColor(amount));
        SpawnDamagePopup(pos, amount);
        ScreenShake.Instance?.Shake(amount >= 8f ? 0.12f : 0.05f, 0.12f);
        SimpleAudio.Instance?.PlayEnemyHit(amount);
    }

    public void ReportDeath(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        var pos = enemy.transform.position;
        SpawnDeathBurst(pos, enemy.kind);
        SimpleAudio.Instance?.PlayEnemyDeath();
    }

    public void PlayOverloadRing(Vector3 center, float radius)
    {
        StartCoroutine(ExpandRingRoutine(center, radius, new Color(1f, 0.35f, 0.3f, 0.85f)));
        ScreenShake.Instance?.Shake(0.15f, 0.18f);
        SimpleAudio.Instance?.PlayOverloadStrike();
    }

    public void PlayGoldPulsePopup(Vector3 worldPos, int gold)
    {
        StartCoroutine(FloatingTextRoutine(worldPos + Vector3.up * 0.3f, $"+{gold}G", new Color(1f, 0.85f, 0.2f)));
        SimpleAudio.Instance?.PlayGoldPulse();
    }

    /// <summary>Perfect 마디 — Core·타워·맵 연동 연출.</summary>
    public void PlayRhythmPerfectSuccess(CommandType type, int goldReward, TowerRegistry towers)
    {
        Vector3 corePos = GetCorePosition();

        BaseHealth.Instance?.GetComponent<CoreBeatPulse>()?.PulseRhythmPerfect(type);

        var color = GetRhythmCommandColor(type);
        StartCoroutine(ExpandRingRoutine(corePos, 2.35f, color));
        SpawnHitParticles(corePos, color, 16, 0.58f);
        ScreenShake.Instance?.Shake(GetRhythmShakeIntensity(type), 0.2f);

        if (towers != null && towers.BeatTowers.Count > 0)
        {
            foreach (var beat in towers.BeatTowers)
                StartCoroutine(RhythmCoreLinkRoutine(corePos, beat.transform.position, color));
        }

        if (type == CommandType.GoldPulse && goldReward > 0)
            PlayGoldFlyToCore(towers, corePos, goldReward);
    }

    public void PlayRhythmSalvoShot(Vector3 from, Vector3 to, TowerType towerType, float damage, Transform towerTransform)
    {
        StartCoroutine(RhythmSalvoProjectileRoutine(from, to, GetRhythmSalvoColor(towerType)));
        towerTransform?.GetComponent<TowerFireRecoil>()?.Punch();
        SimpleAudio.Instance?.PlayTowerFire(towerType);
    }

    static float GetRhythmShakeIntensity(CommandType type) => type switch
    {
        CommandType.GoldPulse => 0.17f,
        CommandType.RhythmShot => 0.16f,
        CommandType.OverloadStrike => 0.14f,
        _ => 0.12f
    };

    static Vector3 GetCorePosition()
    {
        if (BaseHealth.Instance != null)
            return BaseHealth.Instance.transform.position;
        return Vector3.zero;
    }

    static Color GetRhythmCommandColor(CommandType type) => type switch
    {
        CommandType.GoldPulse => new Color(1f, 0.88f, 0.28f, 0.88f),
        CommandType.RhythmShot => new Color(0.92f, 0.94f, 1f, 0.85f),
        CommandType.OverloadStrike => new Color(1f, 0.38f, 0.32f, 0.88f),
        CommandType.ChainZap => new Color(1f, 0.78f, 0.22f, 0.88f),
        CommandType.TempoUp => new Color(0.4f, 0.88f, 1f, 0.85f),
        CommandType.TempoDown => new Color(0.65f, 0.58f, 1f, 0.85f),
        _ => new Color(1f, 0.85f, 0.25f, 0.85f)
    };

    static Color GetRhythmSalvoColor(TowerType type)
    {
        var baseColor = GetShotColor(type);
        return Color.Lerp(baseColor, new Color(1f, 0.92f, 0.45f), 0.45f);
    }

    void PlayGoldFlyToCore(TowerRegistry towers, Vector3 corePos, int totalGold)
    {
        var spawns = new List<Vector3>();
        if (towers != null)
        {
            foreach (var beat in towers.BeatTowers)
                spawns.Add(beat.transform.position + Vector3.up * 0.15f);
        }

        if (spawns.Count == 0)
            spawns.Add(corePos + Vector3.up * 1.6f);

        int perTower = totalGold / spawns.Count;
        int remainder = totalGold % spawns.Count;
        bool playedAudio = false;

        for (int i = 0; i < spawns.Count; i++)
        {
            int amount = perTower + (i == 0 ? remainder : 0);
            if (amount <= 0)
                continue;

            StartCoroutine(GoldFlyToCoreRoutine(spawns[i], corePos, amount, !playedAudio));
            playedAudio = true;
        }
    }

    IEnumerator GoldFlyToCoreRoutine(Vector3 from, Vector3 to, int gold, bool playAudio)
    {
        var go = new GameObject("GoldFly");
        go.transform.SetParent(_poolRoot);
        go.transform.position = from;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = $"+{gold}G";
        tmp.fontSize = gold >= 20 ? 4.2f : 3.6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.9f, 0.28f, 1f);
        if (BeatDefenderFonts.Pretendard != null)
            tmp.font = BeatDefenderFonts.Pretendard;

        if (playAudio)
            SimpleAudio.Instance?.PlayGoldPulse();

        const float duration = 0.62f;
        float elapsed = 0f;
        Vector3 start = from;
        Vector3 end = to + Vector3.up * 0.2f;
        Vector3 control = (start + end) * 0.5f + Vector3.up * 0.85f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float eased = t * t * (3f - 2f * t);
            go.transform.position = QuadraticBezier(start, control, end, eased);
            float scale = Mathf.Lerp(1.15f, 0.75f, t);
            go.transform.localScale = Vector3.one * scale;
            tmp.color = new Color(1f, 0.9f, 0.28f, 1f - t * 0.35f);
            yield return null;
        }

        SpawnHitParticles(end, new Color(1f, 0.88f, 0.25f, 1f), 6, 0.38f);
        Destroy(go);
    }

    IEnumerator RhythmCoreLinkRoutine(Vector3 core, Vector3 towerPos, Color color)
    {
        var linkColor = new Color(color.r, color.g, color.b, 0.82f);
        var go = new GameObject("RhythmCoreLink");
        go.transform.SetParent(_poolRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Circle;
        sr.color = linkColor;
        sr.sortingOrder = 19;
        go.transform.localScale = Vector3.one * 0.12f;

        const float duration = 0.14f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = Vector3.Lerp(core, towerPos, t);
            sr.color = new Color(linkColor.r, linkColor.g, linkColor.b, linkColor.a * (1f - t));
            yield return null;
        }

        Destroy(go);
    }

    IEnumerator RhythmSalvoProjectileRoutine(Vector3 from, Vector3 to, Color color)
    {
        var go = new GameObject("RhythmSalvoShot");
        go.transform.SetParent(_poolRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Circle;
        sr.color = color;
        sr.sortingOrder = 27;
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.24f;

        const float duration = 0.08f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = Vector3.Lerp(from, to, t);
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.24f, 0.16f, t);
            yield return null;
        }

        SpawnHitParticles(to, color, 8, 0.42f);
        Destroy(go);
    }

    static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    public void PlayGoldSpendPopup(Vector3 worldPos, int gold)
    {
        var spendColor = new Color(1f, 0.42f, 0.35f, 1f);
        StartCoroutine(FloatingTextRoutine(worldPos + Vector3.up * 0.25f, $"-{gold}G", spendColor, 0.65f, 0.85f));
        SpawnGoldSpendParticles(worldPos, gold);
        SimpleAudio.Instance?.PlayGoldSpend();
    }

    void SpawnGoldSpendParticles(Vector3 pos, int gold)
    {
        int count = Mathf.Clamp(gold / 5, 4, 10);
        var coinColor = new Color(1f, 0.82f, 0.22f, 1f);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("SpendCoin");
            go.transform.SetParent(_poolRoot);
            go.transform.position = pos + (Vector3)Random.insideUnitCircle * 0.12f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GreyboxSprites.Square;
            sr.color = coinColor;
            sr.sortingOrder = 24;
            go.transform.localScale = Vector3.one * Random.Range(0.07f, 0.11f);
            Vector2 vel = Vector2.up * Random.Range(0.55f, 1.05f)
                + Random.insideUnitCircle * 0.18f;
            StartCoroutine(GoldSpendParticleRoutine(go, sr, vel));
        }
    }

    IEnumerator GoldSpendParticleRoutine(GameObject go, SpriteRenderer sr, Vector2 velocity)
    {
        float life = Random.Range(0.28f, 0.42f);
        float elapsed = 0f;
        Color start = sr.color;
        Vector3 startScale = go.transform.localScale;
        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / life;
            go.transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 0.94f;
            go.transform.localScale = startScale * (1f - t * 0.35f);
            sr.color = new Color(start.r, start.g, start.b, start.a * (1f - t));
            yield return null;
        }

        Destroy(go);
    }

    public void PlayCoreHit(Vector3 corePos)
    {
        ScreenShake.Instance?.Shake(0.22f, 0.28f);
        SpawnHitParticles(corePos, new Color(1f, 0.2f, 0.15f, 1f), 16, 0.55f);
        SimpleAudio.Instance?.PlayCoreHit();
    }

    public void PlayCoreCrisisPulse(Vector3 corePos)
    {
        StartCoroutine(ExpandRingRoutine(corePos, 1.2f, new Color(1f, 0.15f, 0.1f, 0.65f)));
        ScreenShake.Instance?.Shake(0.1f, 0.18f);
    }

    public void PlayTowerPlaced(Vector3 pos, TowerType type)
    {
        StartCoroutine(ExpandRingRoutine(pos, 0.55f, GetShotColor(type)));
        SimpleAudio.Instance?.PlayTowerFire(type);
    }

    public void PlaySkillSuccess(CommandType type)
    {
        SimpleAudio.Instance?.PlaySkill(type);
    }

    public void PlayFeverActivated()
    {
        SimpleAudio.Instance?.PlayFeverActivate();
    }

    public void PlayChainZapBurst(Vector3 pos, bool isPrimary)
    {
        var color = isPrimary
            ? new Color(1f, 0.85f, 0.25f, 0.95f)
            : new Color(1f, 0.65f, 0.2f, 0.85f);
        SpawnHitParticles(pos, color, isPrimary ? 10 : 6, isPrimary ? 0.5f : 0.35f);
        if (isPrimary)
            StartCoroutine(ExpandRingRoutine(pos, 0.5f, new Color(1f, 0.75f, 0.15f, 0.75f)));
    }

    public void PlayChainZapLink(Vector3 from, Vector3 to)
    {
        StartCoroutine(ChainLinkRoutine(from, to));
    }

    IEnumerator ChainLinkRoutine(Vector3 from, Vector3 to)
    {
        var color = new Color(1f, 0.78f, 0.2f, 1f);
        var go = new GameObject("ChainLink");
        go.transform.SetParent(_poolRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Circle;
        sr.color = color;
        sr.sortingOrder = 26;
        go.transform.localScale = Vector3.one * 0.14f;

        const float duration = 0.08f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        SpawnHitParticles(to, color, 4, 0.3f);
        Destroy(go);
    }

    IEnumerator ProjectileRoutine(Vector3 from, Vector3 to, Color color, float damage)
    {
        var go = new GameObject("Projectile");
        go.transform.SetParent(_poolRoot);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Circle;
        sr.color = color;
        sr.sortingOrder = 25;
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.18f;

        const float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        SpawnHitParticles(to, color, 6, 0.35f);
        Destroy(go);
    }

    IEnumerator HitFlashRoutine(EnemyHealth enemy)
    {
        if (enemy == null)
            yield break;

        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null)
            yield break;

        Color original = sr.color;
        sr.color = new Color(1f, 0.35f, 0.35f, original.a);
        float t = 0f;
        const float duration = 0.1f;
        while (t < duration)
        {
            if (enemy == null || sr == null)
                yield break;

            t += Time.deltaTime;
            sr.color = Color.Lerp(new Color(1f, 0.35f, 0.35f, original.a), original, t / duration);
            yield return null;
        }

        if (enemy != null && sr != null)
            sr.color = original;
    }

    void SpawnHitParticles(Vector3 pos, Color color, int count = 10, float speed = 0.45f)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("HitParticle");
            go.transform.SetParent(_poolRoot);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GreyboxSprites.Square;
            sr.color = color;
            sr.sortingOrder = 22;
            go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);
            Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(speed * 0.5f, speed);
            StartCoroutine(ParticleRoutine(go, sr, vel));
        }
    }

    void SpawnDeathBurst(Vector3 pos, EnemyKind kind)
    {
        Color color = kind switch
        {
            EnemyKind.Elite => new Color(1f, 0.75f, 0.15f, 1f),
            EnemyKind.Downbeat => new Color(0.85f, 0.35f, 1f, 1f),
            _ => new Color(0.35f, 0.75f, 1f, 1f)
        };

        int particles = kind switch
        {
            EnemyKind.Elite => 22,
            EnemyKind.Downbeat => 18,
            _ => 12
        };

        float ring = kind switch
        {
            EnemyKind.Elite => 1.45f,
            EnemyKind.Downbeat => 1.2f,
            _ => 0.75f
        };

        SpawnHitParticles(pos, color, particles, 0.7f);
        StartCoroutine(ExpandRingRoutine(pos, ring, color));
    }

    public void PlayEliteRegenPulse(Vector3 pos)
    {
        var color = new Color(0.35f, 1f, 0.55f, 0.75f);
        SpawnHitParticles(pos, color, 8, 0.45f);
        StartCoroutine(ExpandRingRoutine(pos, 0.5f, color));
    }

    IEnumerator ParticleRoutine(GameObject go, SpriteRenderer sr, Vector2 velocity)
    {
        float life = Random.Range(0.18f, 0.32f);
        float elapsed = 0f;
        Color start = sr.color;
        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            go.transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 0.92f;
            float a = 1f - (elapsed / life);
            sr.color = new Color(start.r, start.g, start.b, start.a * a);
            yield return null;
        }

        Destroy(go);
    }

    IEnumerator ExpandRingRoutine(Vector3 center, float maxRadius, Color color)
    {
        var go = new GameObject("RingVfx");
        go.transform.SetParent(_poolRoot);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GreyboxSprites.Ring;
        sr.color = color;
        sr.sortingOrder = 20;

        const float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0.2f, maxRadius * 2f, t);
            go.transform.localScale = Vector3.one * scale;
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
            yield return null;
        }

        Destroy(go);
    }

    void SpawnDamagePopup(Vector3 pos, float damage)
    {
        StartCoroutine(FloatingTextRoutine(
            pos + Vector3.up * 0.15f, FormatDamagePopup(damage), GetDamageColor(damage)));
    }

    static string FormatDamagePopup(float damage) =>
        damage >= 10f ? damage.ToString("0") : damage.ToString("0.#");

    IEnumerator FloatingTextRoutine(
        Vector3 pos,
        string text,
        Color color,
        float duration = 0.55f,
        float rise = 0.65f)
    {
        var go = new GameObject("FloatingText");
        go.transform.SetParent(_poolRoot);
        go.transform.position = pos;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = text.StartsWith("-") ? 3.6f : 3.2f;
        tmp.fontStyle = text.StartsWith("-") ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        if (BeatDefenderFonts.Pretendard != null)
            tmp.font = BeatDefenderFonts.Pretendard;

        float elapsed = 0f;
        Vector3 start = pos;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = start + Vector3.up * (t * rise);
            tmp.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }

        Destroy(go);
    }

    static Color GetShotColor(TowerType type) => type switch
    {
        TowerType.Strike => new Color(1f, 0.45f, 0.35f),
        TowerType.Boost => new Color(1f, 0.75f, 0.2f),
        _ => new Color(0.95f, 0.95f, 1f)
    };

    static Color GetDamageColor(float damage) =>
        damage >= 8f ? new Color(1f, 0.45f, 0.25f)
        : damage >= 4f ? new Color(1f, 0.75f, 0.35f)
        : new Color(1f, 0.95f, 0.55f);
}
