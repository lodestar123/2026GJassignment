using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬별 루프 BGM — Inspector에서 AudioClip 교체.
/// </summary>
[DisallowMultipleComponent]
public class SceneBgmPlayer : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] float volume = 0.45f;
    [SerializeField] bool loop = true;
    [SerializeField] bool playOnAwake = true;

    [Header("Pause")]
    [SerializeField] bool duckOnPause = true;
    [SerializeField] [Range(0f, 1f)] float pauseVolumeMultiplier = 0.4f;

    [Header("Tempo")]
    [Tooltip("TempoUp/Down 시 BeatClock·메트로놈과 같이 pitch 연동")]
    [SerializeField] bool syncPitchToTempo = true;

    AudioSource _source;
    bool _pauseSubscribed;
    bool _tempoSubscribed;

    public float BgmStartTimeUnscaled { get; private set; }
    public bool IsPlaying => _source != null && _source.isPlaying;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = loop;
        _source.spatialBlend = 0f;
        _source.priority = 128;
        ApplyClip();
        GameSettings.ApplyTo(this);
        GameSettings.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        ApplyVolume();
    }

    void Start()
    {
        TrySubscribePause();
        TrySubscribeTempo();
        if (playOnAwake && !ShouldDeferPlayForMatchCountdown())
            Play();
    }

    void Update()
    {
        if (syncPitchToTempo && !_tempoSubscribed)
            TrySubscribeTempo();
    }

    static bool ShouldDeferPlayForMatchCountdown()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.name == SceneNames.Game
            && FindAnyObjectByType<GameStartCountdownUI>() != null;
    }

    void OnDestroy()
    {
        GameSettings.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        UnsubscribePause();
        UnsubscribeTempo();
    }

    public void RefreshVolumeFromSettings() => ApplyVolume();

    void HandleBgmVolumeChanged(float _) => ApplyVolume();

    void OnValidate()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source != null)
        {
            _source.loop = loop;
            ApplyClip();
            ApplyVolume();
            ApplyTempoPitch();
        }
    }

    public void Play()
    {
        if (_source == null || bgmClip == null)
            return;

        ApplyClip();
        ApplyVolume();
        ApplyTempoPitch();

        if (!_source.isPlaying)
        {
            _source.Play();
            BgmStartTimeUnscaled = Time.unscaledTime;
        }
    }

    public void Stop()
    {
        if (_source != null && _source.isPlaying)
            _source.Stop();
    }

    void ApplyClip()
    {
        if (_source == null)
            return;

        _source.clip = bgmClip;
    }

    void ApplyVolume()
    {
        if (_source == null)
            return;

        bool paused = duckOnPause && PauseController.Instance != null && PauseController.Instance.IsPaused;
        float master = GameSettings.BgmVolume;
        float duck = paused ? pauseVolumeMultiplier : 1f;
        _source.volume = master * volume * duck;
    }

    void TrySubscribePause()
    {
        if (!duckOnPause || _pauseSubscribed || PauseController.Instance == null)
            return;

        PauseController.Instance.OnPauseChanged += HandlePauseChanged;
        _pauseSubscribed = true;
        ApplyVolume();
    }

    void UnsubscribePause()
    {
        if (!_pauseSubscribed || PauseController.Instance == null)
            return;

        PauseController.Instance.OnPauseChanged -= HandlePauseChanged;
        _pauseSubscribed = false;
    }

    void HandlePauseChanged(bool _)
    {
        ApplyVolume();
    }

    void TrySubscribeTempo()
    {
        if (!syncPitchToTempo || _tempoSubscribed || TempoController.Instance == null)
            return;

        TempoController.Instance.OnTempoChanged += ApplyTempoPitch;
        _tempoSubscribed = true;
        ApplyTempoPitch();
    }

    void UnsubscribeTempo()
    {
        if (!_tempoSubscribed || TempoController.Instance == null)
            return;

        TempoController.Instance.OnTempoChanged -= ApplyTempoPitch;
        _tempoSubscribed = false;
    }

    void ApplyTempoPitch()
    {
        if (_source == null || !syncPitchToTempo)
            return;

        float scale = TempoController.Instance != null ? TempoController.Instance.CurrentScale : 1f;
        _source.pitch = scale > 0f ? 1f / scale : 1f;
    }
}
