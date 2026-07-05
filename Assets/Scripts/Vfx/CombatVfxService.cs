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

    public void PlayCoreHit(Vector3 corePos)
    {
        ScreenShake.Instance?.Shake(0.22f, 0.28f);
        SpawnHitParticles(corePos, new Color(1f, 0.2f, 0.15f, 1f), 16, 0.55f);
        SimpleAudio.Instance?.PlayCoreHit();
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
        Color color = kind == EnemyKind.Downbeat
            ? new Color(0.85f, 0.35f, 1f, 1f)
            : new Color(0.35f, 0.75f, 1f, 1f);
        SpawnHitParticles(pos, color, kind == EnemyKind.Downbeat ? 18 : 12, 0.7f);
        StartCoroutine(ExpandRingRoutine(pos, kind == EnemyKind.Downbeat ? 1.2f : 0.75f, color));
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
        StartCoroutine(FloatingTextRoutine(pos + Vector3.up * 0.15f, damage.ToString("0"), GetDamageColor(damage)));
    }

    IEnumerator FloatingTextRoutine(Vector3 pos, string text, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.SetParent(_poolRoot);
        go.transform.position = pos;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 3.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        if (BeatDefenderFonts.Pretendard != null)
            tmp.font = BeatDefenderFonts.Pretendard;

        float elapsed = 0f;
        const float duration = 0.55f;
        Vector3 start = pos;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = start + Vector3.up * (t * 0.65f);
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
