using UnityEngine;

/// <summary>
/// 카메라 흔들림 — 적 피격 / Core 피격.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    Transform _target;
    Vector3 _restLocalPos;
    float _remaining;
    float _intensity;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _target = transform;
        _restLocalPos = _target.localPosition;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void EnsureOnMainCamera()
    {
        if (Instance != null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        if (cam.GetComponent<ScreenShake>() == null)
            cam.gameObject.AddComponent<ScreenShake>();
    }

    public void Shake(float intensity, float duration)
    {
        _intensity = Mathf.Max(_intensity, intensity);
        _remaining = Mathf.Max(_remaining, duration);
    }

    void LateUpdate()
    {
        if (_remaining <= 0f)
        {
            _target.localPosition = _restLocalPos;
            _intensity = 0f;
            return;
        }

        _remaining -= Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_remaining / 0.25f);
        float amp = _intensity * t;
        _target.localPosition = _restLocalPos + (Vector3)Random.insideUnitCircle * amp;
    }
}
