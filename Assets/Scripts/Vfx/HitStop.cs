using System.Collections;
using UnityEngine;

/// <summary>짧은 시간 정지 — 피버 발동 등 임팩트용.</summary>
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    Coroutine _routine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Ensure()
    {
        if (Instance != null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        if (cam.GetComponent<HitStop>() == null)
            cam.gameObject.AddComponent<HitStop>();
    }

    public static void Brief(float realDuration, float frozenScale = 0.12f)
    {
        Ensure();
        Instance?.Trigger(realDuration, frozenScale);
    }

    void Trigger(float realDuration, float frozenScale)
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(BriefRoutine(realDuration, frozenScale));
    }

    IEnumerator BriefRoutine(float realDuration, float frozenScale)
    {
        if (realDuration <= 0f)
            yield break;

        float saved = Time.timeScale;
        if (saved > 0f)
            Time.timeScale = Mathf.Clamp(frozenScale, 0.02f, saved);

        yield return new WaitForSecondsRealtime(realDuration);

        if (Time.timeScale <= frozenScale + 0.001f)
            Time.timeScale = saved > 0f ? saved : 1f;

        _routine = null;
    }
}
