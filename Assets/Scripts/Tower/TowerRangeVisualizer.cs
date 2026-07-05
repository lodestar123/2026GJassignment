using UnityEngine;

/// <summary>
/// 타워 사거리 greybox 링 — Play 중 항상 표시.
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerRangeVisualizer : MonoBehaviour
{
    [SerializeField] float alpha = 0.22f;

    Tower _tower;
    SpriteRenderer _ring;

    void Awake()
    {
        _tower = GetComponent<Tower>();
        CreateRing();
    }

    void OnEnable() => Refresh();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_tower == null)
            _tower = GetComponent<Tower>();
        Refresh();
    }
#endif

    void CreateRing()
    {
        if (_ring != null)
            return;

        var go = new GameObject("RangeRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _ring = go.AddComponent<SpriteRenderer>();
        _ring.sprite = GreyboxSprites.Ring;
        _ring.sortingOrder = 1;
        Refresh();
    }

    void Refresh()
    {
        if (_tower == null || _ring == null)
            return;

        float diameter = _tower.Range * 2f;
        _ring.transform.localScale = new Vector3(diameter, diameter, 1f);
        _ring.color = new Color(1f, 1f, 1f, alpha);
    }
}
