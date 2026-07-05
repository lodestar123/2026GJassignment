using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PERFECT 연속 콤보 — 피버(16) 진행도를 큰 숫자로 표시.
/// </summary>
public class FeverComboUI : MonoBehaviour
{
    public static FeverComboUI Instance { get; private set; }

    [SerializeField] TextMeshProUGUI comboValueText;
    [SerializeField] TextMeshProUGUI comboLabelText;
    [SerializeField] Image progressFill;

    const float RestOffsetX = -18f;
    const float RestOffsetY = 0f;

    RectTransform _root;
    int _streak;
    int _required = FeverTimeController.RequiredPerfectStreak;
    Coroutine _pulseRoutine;
    Coroutine _breakRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsureInActiveScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryEnsureInActiveScene();

    static void TryEnsureInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != SceneNames.Game)
            return;

        if (FindAnyObjectByType<FeverComboUI>(FindObjectsInactive.Include) != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var go = new GameObject("FeverComboUI", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        go.AddComponent<FeverComboUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
        RefreshDisplay(false);
    }

    void Start()
    {
        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
        {
            fever.OnStreakChanged -= HandleStreakChanged;
            fever.OnStreakChanged += HandleStreakChanged;
            HandleStreakChanged(fever.PerfectStreak, FeverTimeController.RequiredPerfectStreak);
        }
    }

    void OnDestroy()
    {
        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
            fever.OnStreakChanged -= HandleStreakChanged;

        if (Instance == this)
            Instance = null;
    }

    void BuildUi()
    {
        _root = transform as RectTransform;
        if (_root == null)
            _root = gameObject.AddComponent<RectTransform>();

        _root.anchorMin = new Vector2(1f, 0.5f);
        _root.anchorMax = new Vector2(1f, 0.5f);
        _root.pivot = new Vector2(1f, 0.5f);
        _root.anchoredPosition = new Vector2(RestOffsetX, RestOffsetY);
        _root.sizeDelta = new Vector2(112f, 96f);

        transform.SetAsLastSibling();

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bg = bgGo.AddComponent<Image>();
        bg.sprite = GreyboxSprites.Square;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.06f, 0.1f, 0.72f);
        bg.raycastTarget = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0.5f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, -6f);
        labelRt.sizeDelta = new Vector2(0f, 22f);
        comboLabelText = labelGo.AddComponent<TextMeshProUGUI>();
        comboLabelText.text = "COMBO";
        comboLabelText.fontSize = 14f;
        comboLabelText.fontStyle = FontStyles.Bold;
        comboLabelText.alignment = TextAlignmentOptions.Center;
        comboLabelText.color = new Color(1f, 0.78f, 0.35f, 0.9f);
        comboLabelText.raycastTarget = false;
        if (BeatDefenderFonts.Pretendard != null)
            comboLabelText.font = BeatDefenderFonts.Pretendard;

        var valueGo = new GameObject("Value");
        valueGo.transform.SetParent(transform, false);
        var valueRt = valueGo.AddComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0f, 0.28f);
        valueRt.anchorMax = new Vector2(1f, 0.82f);
        valueRt.offsetMin = Vector2.zero;
        valueRt.offsetMax = Vector2.zero;
        comboValueText = valueGo.AddComponent<TextMeshProUGUI>();
        comboValueText.fontSize = 42f;
        comboValueText.fontStyle = FontStyles.Bold;
        comboValueText.alignment = TextAlignmentOptions.Center;
        comboValueText.raycastTarget = false;
        if (BeatDefenderFonts.Pretendard != null)
            comboValueText.font = BeatDefenderFonts.Pretendard;

        var barBgGo = new GameObject("BarBg");
        barBgGo.transform.SetParent(transform, false);
        var barBgRt = barBgGo.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.08f, 0f);
        barBgRt.anchorMax = new Vector2(0.92f, 0f);
        barBgRt.pivot = new Vector2(0.5f, 0f);
        barBgRt.anchoredPosition = new Vector2(0f, 8f);
        barBgRt.sizeDelta = new Vector2(0f, 6f);
        var barBg = barBgGo.AddComponent<Image>();
        barBg.sprite = GreyboxSprites.Square;
        barBg.type = Image.Type.Sliced;
        barBg.color = new Color(1f, 1f, 1f, 0.12f);
        barBg.raycastTarget = false;

        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(barBgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        progressFill = fillGo.AddComponent<Image>();
        progressFill.sprite = GreyboxSprites.Square;
        progressFill.color = new Color(1f, 0.72f, 0.18f, 0.95f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.raycastTarget = false;
    }

    void HandleStreakChanged(int streak, int required)
    {
        int previous = _streak;
        _streak = streak;
        _required = Mathf.Max(1, required);
        RefreshDisplay(streak > previous);

        if (streak == 0 && previous > 0)
            PlayBreakFeedback();
    }

    void RefreshDisplay(bool increased)
    {
        if (comboValueText == null)
            return;

        comboValueText.text = _streak.ToString();
        float t = _required > 0 ? (float)_streak / _required : 0f;
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(t);

        var hot = new Color(1f, 0.55f, 0.12f);
        var warm = new Color(1f, 0.78f, 0.35f);
        var idle = new Color(0.75f, 0.75f, 0.82f);
        comboValueText.color = _streak >= _required * 0.75f ? hot
            : _streak > 0 ? warm
            : idle;

        if (comboLabelText != null)
            comboLabelText.text = _streak > 0 ? $"COMBO / {_required}" : "PERFECT";

        if (increased)
            PlayIncrementPulse();
    }

    void PlayIncrementPulse()
    {
        if (_root == null)
            return;

        if (_pulseRoutine != null)
            StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine(1.18f, 0.12f));
    }

    void PlayBreakFeedback()
    {
        if (_root == null)
            return;

        if (_breakRoutine != null)
            StopCoroutine(_breakRoutine);
        _breakRoutine = StartCoroutine(BreakRoutine());
    }

    IEnumerator PulseRoutine(float peakScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = t < 0.45f
                ? Mathf.Lerp(1f, peakScale, t / 0.45f)
                : Mathf.Lerp(peakScale, 1f, (t - 0.45f) / 0.55f);
            _root.localScale = Vector3.one * scale;
            yield return null;
        }

        _root.localScale = Vector3.one;
        _pulseRoutine = null;
    }

    IEnumerator BreakRoutine()
    {
        if (comboValueText != null)
            comboValueText.color = new Color(1f, 0.35f, 0.35f);

        float elapsed = 0f;
        const float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float shake = Mathf.Sin(elapsed * 48f) * (1f - elapsed / duration) * 4f;
            _root.anchoredPosition = new Vector2(RestOffsetX + shake, RestOffsetY);
            yield return null;
        }

        _root.anchoredPosition = new Vector2(RestOffsetX, RestOffsetY);
        RefreshDisplay(false);
        _breakRoutine = null;
    }
}
