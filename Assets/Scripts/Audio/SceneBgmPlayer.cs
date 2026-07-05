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

    AudioSource _source;
    bool _pauseSubscribed;

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
        ApplyVolume();
    }

    void Start()
    {
        TrySubscribePause();
        if (playOnAwake && !ShouldDeferPlayForMatchCountdown())
            Play();
    }

    static bool ShouldDeferPlayForMatchCountdown()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.name == SceneNames.Game
            && FindAnyObjectByType<GameStartCountdownUI>() != null;
    }

    void OnDestroy()
    {
        UnsubscribePause();
    }

    void OnValidate()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source != null)
        {
            _source.loop = loop;
            ApplyClip();
            ApplyVolume();
        }
    }

    public void Play()
    {
        if (_source == null || bgmClip == null)
            return;

        ApplyClip();
        ApplyVolume();

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
        _source.volume = paused ? volume * pauseVolumeMultiplier : volume;
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
}
