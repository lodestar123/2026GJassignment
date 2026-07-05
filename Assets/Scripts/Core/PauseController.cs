using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ESC Pause — gameplay timeScale=0 · BeatClock·메트로놈 유지 · rhythm vol 40%.
/// </summary>
[DefaultExecutionOrder(-90)]
public class PauseController : MonoBehaviour
{
    public static PauseController Instance { get; private set; }

    public bool IsPaused { get; private set; }

    public event Action<bool> OnPauseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        PauseMenuUI.Resolve()?.EnsureInitialized();
        SettingsPanelUI.Resolve()?.EnsureInitialized();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (ResultScreenUI.Instance != null && ResultScreenUI.Instance.IsVisible)
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return;

        var settings = SettingsPanelUI.Resolve();
        if (settings != null && settings.IsVisible)
        {
            settings.Hide();
            return;
        }

        TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;
        SimpleAudio.Instance?.SetRhythmVolumeMultiplier(0.4f);
        PauseMenuUI.Resolve()?.Show();
        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = 1f;
        SimpleAudio.Instance?.SetRhythmVolumeMultiplier(1f);
        PauseMenuUI.Resolve()?.Hide();
        OnPauseChanged?.Invoke(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.Start);
    }
}
