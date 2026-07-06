using UnityEngine;

/// <summary>
/// 타워 사거리 링 — BeatTower 프리팹 Inspector의 rangeRingSprite / RangeRing 자식 사용.
/// </summary>
[RequireComponent(typeof(Tower))]
[DefaultExecutionOrder(100)]
public class TowerRangeVisualizer : MonoBehaviour
{
    [SerializeField] float alpha = 0.22f;
    [SerializeField] Sprite rangeRingSprite;

    Tower _tower;
    SpriteRenderer _ring;

    void Awake()
    {
        _tower = GetComponent<Tower>();
        CreateRing();
        ApplyRingSprite();
    }

    void Start() => RefreshRingScale();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_tower == null)
            _tower = GetComponent<Tower>();

        CreateRing();
        ApplyRingSprite();
        RefreshRingScale();
    }
#endif

    public void SetRangeRingSprite(Sprite sprite) => rangeRingSprite = sprite;

    public void RefreshRingScale() => Refresh();

    void CreateRing()
    {
        if (_ring != null)
            return;

        var existing = transform.Find("RangeRing");
        if (existing != null)
        {
            _ring = existing.GetComponent<SpriteRenderer>();
            return;
        }

        var go = new GameObject("RangeRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _ring = go.AddComponent<SpriteRenderer>();
        _ring.sortingOrder = 1;
    }

    void ApplyRingSprite()
    {
        if (_ring == null)
            return;

        if (rangeRingSprite != null)
            _ring.sprite = rangeRingSprite;

        SpriteRendererUtility.EnsureSpriteMaterial(_ring);
    }

    void Refresh()
    {
        if (_tower == null || _ring == null)
            return;

        float targetDiameter = _tower.Range * 2f;
        float spriteDiameter = GetSpriteWorldDiameter(_ring.sprite);
        float scale = spriteDiameter > 0.0001f
            ? targetDiameter / spriteDiameter
            : targetDiameter;

        _ring.transform.localScale = new Vector3(scale, scale, 1f);
        _ring.color = new Color(1f, 1f, 1f, alpha);
    }

    static float GetSpriteWorldDiameter(Sprite sprite)
    {
        if (sprite == null)
            return 1f;

        var size = sprite.bounds.size;
        return Mathf.Max(size.x, size.y);
    }
}
