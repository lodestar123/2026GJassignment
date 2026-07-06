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
    [SerializeField] TextMeshProUGUI rhythmHighlightText;
    [SerializeField] TextMeshProUGUI metaText;
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

        EnsureExtraTexts();

        if (restartButton != null)
            restartButton.onClick.AddListener(() => PauseController.Instance?.RestartGame());

        if (titleButton != null)
            titleButton.onClick.AddListener(() => PauseController.Instance?.GoToTitle());

        Subscribe();
    }

    void EnsureExtraTexts()
    {
        if (panelRoot == null)
            return;

        var panel = panelRoot.transform;

        rhythmHighlightText ??= panel.Find("RhythmHighlight")?.GetComponent<TextMeshProUGUI>();
        if (rhythmHighlightText == null)
        {
            rhythmHighlightText = CreatePanelText(
                panel,
                "RhythmHighlight",
                new Vector2(0.5f, 0.54f),
                Vector2.zero,
                new Vector2(900f, 72f),
                52f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
        }

        metaText ??= panel.Find("Meta")?.GetComponent<TextMeshProUGUI>();
    }

    static TextMeshProUGUI CreatePanelText(
        Transform panel,
        string name,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(panel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.richText = true;
        tmp.raycastTarget = false;
        BeatDefenderFonts.Apply(tmp);
        return tmp;
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
        var stats = RunStats.Instance;
        var result = ScoreCalculator.Calculate(
            stats,
            BaseHealth.Instance,
            GameManager.Instance,
            victory: true);
        var meta = RunMetaProgress.RecordRun(stats, result);

        Show(
            $"CLEAR - {result.TotalScore:N0}  <size=80%><color=#FFD54F>GRADE {result.Grade}</color></size>",
            BuildRhythmHighlight(result),
            BuildMetaLine(meta),
            BuildDetail(result, GameManager.Instance, stats, BaseHealth.Instance));
    }

    void ShowDefeat()
    {
        var stats = RunStats.Instance;
        var result = ScoreCalculator.Calculate(
            stats,
            BaseHealth.Instance,
            GameManager.Instance,
            victory: false);
        var meta = RunMetaProgress.RecordRun(stats, result);

        Show(
            $"GAME OVER - {result.TotalScore:N0}",
            string.Empty,
            BuildMetaLine(meta, includeCurrentRhythm: true, currentRhythm: result.RhythmAccuracyPercent),
            BuildDetail(result, GameManager.Instance, stats, BaseHealth.Instance));
    }

    void Show(string title, string rhythmHighlight, string meta, string detail)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (PauseController.Instance != null && PauseController.Instance.IsPaused)
            PauseController.Instance.Resume();

        Time.timeScale = 0f;

        if (titleText != null)
            titleText.text = title;

        if (rhythmHighlightText != null)
        {
            rhythmHighlightText.gameObject.SetActive(!string.IsNullOrEmpty(rhythmHighlight));
            rhythmHighlightText.text = rhythmHighlight;
        }

        if (metaText != null)
            metaText.text = meta;

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

    static string BuildRhythmHighlight(ScoreCalculator.Result result)
    {
        if (result.RhythmAccuracyPercent <= 0)
            return string.Empty;

        string color = result.RhythmAccuracyPercent >= 85f ? "#FFD54F"
            : result.RhythmAccuracyPercent >= 70f ? "#81D4FA"
            : "#B0BEC5";

        return $"<color={color}>Rhythm {result.RhythmAccuracyPercent}%</color>";
    }

    static string BuildMetaLine(
        RunMetaProgress.RecordResult meta,
        bool includeCurrentRhythm = false,
        int currentRhythm = 0)
    {
        string scoreLine = meta.NewBestScore
            ? $"<color=#FFD54F><b>NEW PB!</b></color> Score {meta.BestScore:N0}"
            : $"Personal Best {meta.BestScore:N0}";

        string rhythmLabel = meta.NewBestRhythm
            ? $"<color=#FFD54F>Best Rhythm {meta.BestRhythmPercent}%</color>"
            : $"Best Rhythm {meta.BestRhythmPercent}%";

        if (includeCurrentRhythm && currentRhythm > 0)
            rhythmLabel = $"Rhythm {currentRhythm}%  |  {rhythmLabel}";

        string perfectLabel = meta.NewBestPerfect
            ? $"<color=#FFD54F>Most Perfect {meta.BestPerfectCount}</color>"
            : $"Most Perfect {meta.BestPerfectCount}";

        return $"{scoreLine}\n{rhythmLabel}  |  {perfectLabel}";
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
        int elite = stats != null ? stats.EliteKills : 0;
        int perfect = stats != null ? stats.PerfectCount : 0;
        int good = stats != null ? stats.GoodCount : 0;
        int miss = stats != null ? stats.MissCount : 0;
        int survivalSec = game != null ? Mathf.FloorToInt(game.ElapsedSeconds) : 0;

        if (result.Cleared)
        {
            return
                $"HP {hp}  |  8th {eighth}  |  Down {downbeat}  |  Elite {elite}\n" +
                $"P{perfect} G{good} M{miss}\n" +
                $"Surv +{result.SurvivalBonus}  Def +{result.DefenseBonus}  " +
                $"Fight +{result.CombatBonus}  Rhythm +{result.RhythmBonus}";
        }

        return
            $"Survived {survivalSec}s  |  8th {eighth}  |  Down {downbeat}  |  Elite {elite}\n" +
            $"P{perfect} G{good} M{miss}\n" +
            $"Fight +{result.CombatBonus}  Rhythm +{result.RhythmBonus}  Time +{result.TimeBonus}";
    }
}
