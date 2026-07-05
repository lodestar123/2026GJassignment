using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 입력 felt 보정. Baseline(0.24s) + 플레이어 조정(0 = baseline).
/// 설정 UI "감도 0" → 적용 offset 0.24s. OnBeat 연출은 baseline만 (BeatClock).
/// </summary>
public class RhythmInputSettings : MonoBehaviour
{
    public const string PlayerPrefsAdjustmentKey = "BeatDefender.InputOffsetAdjustment";
    public const string LegacyAbsoluteOffsetKey = "BeatDefender.InputOffsetSeconds";
    const string BaselineVersionKey = "BeatDefender.InputOffsetBaselineVersion";
    const int CurrentBaselineVersion = 2;

    /// <summary>튜닝된 기본 offset — 플레이어 감도 0일 때 적용.</summary>
    public const float BaselineInputOffsetSeconds = 0.24f;

    /// <summary><see cref="BaselineInputOffsetSeconds"/> 와 동일 — 하위 호환 alias.</summary>
    public const float DefaultInputOffsetSeconds = BaselineInputOffsetSeconds;

    /// <summary>플레이어 감도 조정 범위(±). 0 = baseline.</summary>
    public const float MinInputOffsetAdjustment = -0.1f;
    public const float MaxInputOffsetAdjustment = 0.1f;

    public static RhythmInputSettings Instance { get; private set; }

    public event Action<float> OnInputOffsetChanged;

    [SerializeField]
    [Tooltip("플레이어 감도 조정. 0 = Baseline(0.24s).")]
    float inputOffsetAdjustment;

    [SerializeField]
    [Tooltip("켜면 PlayerPrefs 조정값이 씬 기본값보다 우선합니다.")]
    bool loadFromPlayerPrefsOnAwake = true;

    [SerializeField]
    KeyCode[] additionalReservedKeys = Array.Empty<KeyCode>();

    public IReadOnlyList<KeyCode> AdditionalReservedKeys => additionalReservedKeys;

    /// <summary>플레이어 감도 조정(0 = baseline).</summary>
    public float InputOffsetAdjustment => inputOffsetAdjustment;

    /// <summary>실제 적용 offset = Baseline + Adjustment (판정·타임라인).</summary>
    public float InputOffsetSeconds => BaselineInputOffsetSeconds + inputOffsetAdjustment;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (loadFromPlayerPrefsOnAwake)
            LoadAdjustmentFromPlayerPrefs();

        ApplyReservedKeys();
    }

    void Start()
    {
        Debug.Log(
            $"[RhythmInputSettings] offset={InputOffsetSeconds:0.###}s " +
            $"(baseline {BaselineInputOffsetSeconds:0.###}s + adj {inputOffsetAdjustment:0.###}s)");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public float AdjustTapTime(float rawTime) => rawTime - InputOffsetSeconds;

    public static float GetAppliedOffsetSeconds()
    {
        return Instance != null ? Instance.InputOffsetSeconds : DefaultInputOffsetSeconds;
    }

    /// <summary>판정·타임라인 felt 마디 원점 — wall MeasureStart + offset (= 사이클 지연).</summary>
    public static float GetRhythmMeasureOrigin(float measureStartTime)
    {
        return measureStartTime + GetAppliedOffsetSeconds();
    }

    /// <summary>판정·타임라인 felt 마디 내 경과.</summary>
    public static float GetFeltElapsedInMeasure(float wallTime, float measureStartTime)
    {
        return wallTime - GetRhythmMeasureOrigin(measureStartTime);
    }

    /// <summary>설정 UI — 감도 0이면 baseline(0.24s).</summary>
    public void SetInputOffsetAdjustment(float adjustment, bool persist)
    {
        float clamped = ClampAdjustment(adjustment);
        if (Mathf.Approximately(inputOffsetAdjustment, clamped))
            return;

        inputOffsetAdjustment = clamped;
        OnInputOffsetChanged?.Invoke(InputOffsetSeconds);

        if (persist)
        {
            PlayerPrefs.SetFloat(PlayerPrefsAdjustmentKey, inputOffsetAdjustment);
            PlayerPrefs.SetInt(BaselineVersionKey, CurrentBaselineVersion);
        }
    }

    /// <summary>절대 offset 직접 지정(테스트·레거시). adjustment = absolute − baseline.</summary>
    public void SetInputOffsetSeconds(float absoluteSeconds, bool persist)
    {
        SetInputOffsetAdjustment(absoluteSeconds - BaselineInputOffsetSeconds, persist);
    }

    public static float ClampAdjustment(float adjustment)
    {
        return Mathf.Clamp(adjustment, MinInputOffsetAdjustment, MaxInputOffsetAdjustment);
    }

    /// <summary>절대 offset clamp — baseline ± adjustment 범위.</summary>
    public static float ClampOffset(float absoluteSeconds)
    {
        return BaselineInputOffsetSeconds + ClampAdjustment(absoluteSeconds - BaselineInputOffsetSeconds);
    }

    void LoadAdjustmentFromPlayerPrefs()
    {
        int version = PlayerPrefs.GetInt(BaselineVersionKey, 0);

        if (version < CurrentBaselineVersion)
        {
            if (version == 1)
            {
                inputOffsetAdjustment = PlayerPrefs.HasKey(PlayerPrefsAdjustmentKey)
                    ? ClampAdjustment(PlayerPrefs.GetFloat(PlayerPrefsAdjustmentKey, 0f))
                    : 0f;
            }
            else if (PlayerPrefs.HasKey(PlayerPrefsAdjustmentKey))
            {
                inputOffsetAdjustment = ClampAdjustment(
                    PlayerPrefs.GetFloat(PlayerPrefsAdjustmentKey, 0f));
            }
            else if (PlayerPrefs.HasKey(LegacyAbsoluteOffsetKey))
            {
                float legacyAbsolute = PlayerPrefs.GetFloat(LegacyAbsoluteOffsetKey);
                inputOffsetAdjustment = ClampAdjustment(legacyAbsolute - BaselineInputOffsetSeconds);
                PlayerPrefs.DeleteKey(LegacyAbsoluteOffsetKey);
            }

            PlayerPrefs.SetFloat(PlayerPrefsAdjustmentKey, inputOffsetAdjustment);
            PlayerPrefs.SetInt(BaselineVersionKey, CurrentBaselineVersion);
            return;
        }

        if (PlayerPrefs.HasKey(PlayerPrefsAdjustmentKey))
        {
            inputOffsetAdjustment = ClampAdjustment(
                PlayerPrefs.GetFloat(PlayerPrefsAdjustmentKey, 0f));
        }
    }

    void OnValidate()
    {
        inputOffsetAdjustment = ClampAdjustment(inputOffsetAdjustment);
        if (Instance == this)
            ApplyReservedKeys();
    }

    void ApplyReservedKeys()
    {
        if (additionalReservedKeys == null)
            return;

        foreach (KeyCode k in additionalReservedKeys)
            RhythmKeyFilter.RegisterReservedKey(k);
    }
}
