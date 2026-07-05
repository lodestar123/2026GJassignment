using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 승리/패배 결과 — BALANCE §10 · FLOW §8.
/// Host는 항상 활성, panelRoot만 Show/Hide.
/// </summary>
public class ResultScreenUI : MonoBehaviour
{
    public static ResultScreenUI Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI detailText;
    [SerializeField] Button restartButton;
    [SerializeField] Button titleButton;

    bool _initialized;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureInitialized();
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    void Start() => Subscribe();

    public static ResultScreenUI Resolve()
    {
        if (Instance != null)
            return Instance;

        var found = FindAnyObjectByType<ResultScreenUI>(FindObjectsInactive.Include);
        found?.EnsureInitialized();
        return found;
    }

    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (panelRoot == null)
            panelRoot = transform.Find("Panel")?.gameObject ?? gameObject;

        if (restartButton != null)
            restartButton.onClick.AddListener(() => PauseController.Instance?.RestartGame());

        if (titleButton != null)
            titleButton.onClick.AddListener(() => PauseController.Instance?.GoToTitle());

        Subscribe();
    }

    void Subscribe()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnVictory -= ShowVictory;
        GameManager.Instance.OnDefeat -= ShowDefeat;
        GameManager.Instance.OnVictory += ShowVictory;
        GameManager.Instance.OnDefeat += ShowDefeat;
    }

    void Unsubscribe()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnVictory -= ShowVictory;
        GameManager.Instance.OnDefeat -= ShowDefeat;
    }

    public void DisplayVictory() => ShowVictory();
    public void DisplayDefeat() => ShowDefeat();

    void ShowVictory()
    {
        var result = ScoreCalculator.Calculate(
            RunStats.Instance,
            BaseHealth.Instance,
            GameManager.Instance,
            victory: true);

        Show(
            $"CLEAR - {result.TotalScore:N0} ({result.Grade})",
            BuildDetail(result, GameManager.Instance, RunStats.Instance, BaseHealth.Instance));
    }

    void ShowDefeat()
    {
        var result = ScoreCalculator.Calculate(
            RunStats.Instance,
            BaseHealth.Instance,
            GameManager.Instance,
            victory: false);

        Show(
            $"GAME OVER - {result.TotalScore:N0} ({result.Grade})",
            BuildDetail(result, GameManager.Instance, RunStats.Instance, BaseHealth.Instance));
    }

    void Show(string title, string detail)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (PauseController.Instance != null && PauseController.Instance.IsPaused)
            PauseController.Instance.Resume();

        Time.timeScale = 0f;

        if (titleText != null)
            titleText.text = title;
        if (detailText != null)
            detailText.text = detail;
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    static string BuildDetail(
        ScoreCalculator.Result result,
        GameManager game,
        RunStats stats,
        BaseHealth core)
    {
        int hp = core != null ? core.CurrentHp : 0;
        int eighth = stats != null ? stats.EighthNoteKills : 0;
        int downbeat = stats != null ? stats.DownbeatKills : 0;
        int perfect = stats != null ? stats.PerfectCount : 0;
        int good = stats != null ? stats.GoodCount : 0;
        int miss = stats != null ? stats.MissCount : 0;
        int survivalSec = game != null ? Mathf.FloorToInt(game.ElapsedSeconds) : 0;

        if (result.Cleared)
        {
            return
                $"HP {hp}  |  8th {eighth}  |  Down {downbeat}\n" +
                $"P{perfect} G{good} M{miss}\n" +
                $"Surv +{result.SurvivalBonus}  Def +{result.DefenseBonus}  " +
                $"Fight +{result.CombatBonus}  Rhythm +{result.RhythmBonus}";
        }

        return
            $"Survived {survivalSec}s  |  8th {eighth}  |  Down {downbeat}\n" +
            $"P{perfect} G{good} M{miss}\n" +
            $"Fight +{result.CombatBonus}  Rhythm +{result.RhythmBonus}  Time +{result.TimeBonus}";
    }
}
