using UnityEngine;

[DefaultExecutionOrder(50)]
public class SimpleAudio : MonoBehaviour
{
    public static SimpleAudio Instance { get; private set; }

    [Range(0f, 1f)] public float metronomeVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;

    [Header("Metronome - downbeat")]
    public float regularTickHz = 880f;
    public float downbeatTickHz = 1320f;
    [Range(1f, 2f)] public float downbeatVolumeMultiplier = 1.5f;
    public float regularTickDuration = 0.04f;
    public float downbeatTickDuration = 0.06f;

    AudioSource _source;
    bool _subscribed;
    float _rhythmVolumeMultiplier = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        GameSettings.ApplyTo(this);
        GameSettings.OnSfxVolumeChanged += OnSfxVolumeChanged;
        GameSettings.OnMetronomeVolumeChanged += OnMetronomeVolumeChanged;
    }

    void OnDestroy()
    {
        GameSettings.OnSfxVolumeChanged -= OnSfxVolumeChanged;
        GameSettings.OnMetronomeVolumeChanged -= OnMetronomeVolumeChanged;
        TryUnsubscribe();
        if (Instance == this)
            Instance = null;
    }

    void OnSfxVolumeChanged(float value) => sfxVolume = value;
    void OnMetronomeVolumeChanged(float value) => metronomeVolume = value;

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();
    void OnDisable() => TryUnsubscribe();

    void TrySubscribe()
    {
        if (_subscribed || BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnBeat -= PlayMetronomeTick;
        BeatClock.Instance.OnBeat += PlayMetronomeTick;
        _subscribed = true;
    }

    void TryUnsubscribe()
    {
        if (!_subscribed || BeatClock.Instance == null)
            return;

        BeatClock.Instance.OnBeat -= PlayMetronomeTick;
        _subscribed = false;
    }

    public void PlayMetronomeTick()
    {
        if (_source == null)
            return;

        bool downbeat = BeatClock.Instance != null && BeatClock.Instance.IsDownbeat;

        float frequency = downbeat ? downbeatTickHz : regularTickHz;
        float duration = downbeat ? downbeatTickDuration : regularTickDuration;
        float volume = metronomeVolume * (downbeat ? downbeatVolumeMultiplier : 1f) * _rhythmVolumeMultiplier;
        float tempoScale = BeatClock.Instance != null ? BeatClock.Instance.TempoScale : 1f;
        float pitch = tempoScale > 0f ? 1f / tempoScale : 1f;

        PlayBeep(frequency, duration, volume, pitch);
    }

    public void SetRhythmVolumeMultiplier(float multiplier)
    {
        _rhythmVolumeMultiplier = Mathf.Clamp01(multiplier);
    }

    public void PlayTapFeedback(TapTimingQuality quality)
    {
        switch (quality)
        {
            case TapTimingQuality.Perfect:
                PlayBeep(1760f, 0.032f, sfxVolume * 0.5f, 1.08f);
                break;
            case TapTimingQuality.Good:
                PlayBeep(1180f, 0.026f, sfxVolume * 0.34f, 1f);
                break;
            default:
                PlayBeep(260f, 0.038f, sfxVolume * 0.26f, 0.88f);
                break;
        }
    }

    public void PlayJudgment(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect:
                PlayBeep(1560f, 0.05f, sfxVolume * 0.55f, 1f);
                break;
            case JudgmentResult.Good:
                PlayBeep(1180f, 0.04f, sfxVolume * 0.42f, 1f);
                break;
            case JudgmentResult.Miss:
            case JudgmentResult.Cooldown:
            case JudgmentResult.NoTower:
                PlayBeep(220f, 0.07f, sfxVolume * 0.35f, 0.85f);
                break;
        }
    }

    public void PlayTowerFire(bool fever = false)
    {
        float hz = fever ? 920f : 760f;
        float vol = fever ? sfxVolume * 0.34f : sfxVolume * 0.25f;
        PlayBeep(hz, fever ? 0.042f : 0.035f, vol, fever ? 1.2f : 1.1f);
    }

    public void PlayEnemyHit(float damage, bool fever = false)
    {
        float hz = fever
            ? (damage >= 8f ? 380f : 520f)
            : (damage >= 8f ? 280f : 420f);
        float vol = fever ? sfxVolume * 0.3f : sfxVolume * 0.22f;
        PlayBeep(hz, fever ? 0.038f : 0.03f, vol, fever ? 1.12f : 1f);
    }

    public void PlayEnemyDeath()
    {
        PlayBeep(180f, 0.08f, sfxVolume * 0.3f, 0.8f);
    }

    public void PlayCoreHit()
    {
        PlayBeep(110f, 0.12f, sfxVolume * 0.45f, 0.7f);
    }

    public void PlayGoldPulse()
    {
        PlayBeep(980f, 0.06f, sfxVolume * 0.35f, 1.25f);
    }

    public void PlayGoldSpend()
    {
        PlayBeep(420f, 0.05f, sfxVolume * 0.28f, 0.85f);
    }

    public void PlayFeverActivate()
    {
        float vol = sfxVolume * 0.34f;
        PlayBeep(520f, 0.08f, vol * 0.9f, 1f);
        PlayBeep(880f, 0.1f, vol, 1f);
        PlayBeep(1180f, 0.12f, vol * 0.85f, 1f);
    }

    public void PlayOverloadStrike()
    {
        PlayBeep(240f, 0.1f, sfxVolume * 0.4f, 0.75f);
    }

    public void PlaySkill(CommandType type)
    {
        float hz = type switch
        {
            CommandType.RhythmShot => 880f,
            CommandType.ChainZap => 640f,
            CommandType.OverloadStrike => 360f,
            CommandType.TempoUp => 920f,
            CommandType.TempoDown => 480f,
            _ => 660f
        };
        PlayBeep(hz, 0.045f, sfxVolume * 0.32f, 1.05f);
    }

    void PlayBeep(float frequency, float duration, float volume, float pitch = 1f)
    {
        if (_source == null || volume <= 0f)
            return;

        int sampleRate = 44100;
        int sampleLength = Mathf.CeilToInt(sampleRate * duration);
        var clip = AudioClip.Create("beep", sampleLength, 1, sampleRate, false);
        var data = new float[sampleLength];

        for (int i = 0; i < sampleLength; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (t / duration);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * pitch * t) * envelope * volume;
        }

        clip.SetData(data, 0);
        _source.PlayOneShot(clip);
    }
}
