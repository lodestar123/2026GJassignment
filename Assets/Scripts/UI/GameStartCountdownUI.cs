using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 매치 시작 전 3·2·1 카운트다운 → BGM → 입력 offset 대기 → 게임 시작.
/// </summary>
public class GameStartCountdownUI : MonoBehaviour, IRuntimeSceneUi
{
    public static GameStartCountdownUI Instance { get; private set; }

    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] Image overlay;
    [SerializeField] float stepSeconds = 1f;
    [SerializeField] int startNumber = 3;
    [SerializeField] float numberFontSize = 128f;

    [Header("BGM → beat sync")]
    [Tooltip("켜면 RhythmInputSettings 적용 offset(baseline+감도)을 대기 시간으로 사용합니다.")]
    [SerializeField] bool useInputOffsetForBgmDelay = true;
    [Min(0f)]
    [Tooltip("useInputOffsetForBgmDelay가 꺼져 있을 때, BGM 재생 후 비트·게임 시작까지 대기(초).")]
    [SerializeField] float bgmStartDelaySeconds = 0.24f;

    Coroutine _routine;
    RectTransform _countRect;

    public bool IsActive { get; private set; }

    public void EnsureSceneHierarchy()
    {
        ResolveRefs();
        if (countText == null)
            BuildUi();
    }

    void ResolveRefs()
    {
        countText ??= transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();
        overlay ??= transform.Find("Overlay")?.GetComponent<Image>();
        _countRect ??= countText != null ? countText.rectTransform : null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSceneHierarchy();
        HideImmediate();
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsRunning)
            return;

        _routine = StartCoroutine(RunCountdown());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    IEnumerator RunCountdown()
    {
        IsActive = true;

        if (overlay != null)
        {
            overlay.enabled = true;
            overlay.raycastTarget = true;
        }

        var bgm = FindAnyObjectByType<SceneBgmPlayer>();
        if (bgm != null && bgm.IsPlaying)
            bgm.Stop();

        if (countText != null)
            countText.gameObject.SetActive(true);

        for (int n = startNumber; n >= 1; n--)
        {
            ShowNumber(n);
            yield return new WaitForSecondsRealtime(stepSeconds);
        }

        HideImmediate();
        IsActive = false;

        if (bgm != null)
            bgm.Play();

        yield return WaitAfterBgmStart(bgm, GetBgmStartDelaySeconds());

        BeatClock.Instance?.ResyncMeasureStart();
        GameManager.Instance?.BeginMatch();
    }

    float GetBgmStartDelaySeconds()
    {
        return useInputOffsetForBgmDelay
            ? RhythmInputSettings.GetAppliedOffsetSeconds()
            : bgmStartDelaySeconds;
    }

    static IEnumerator WaitAfterBgmStart(SceneBgmPlayer bgm, float delaySeconds)
    {
        if (delaySeconds <= 0f)
            yield break;

        if (bgm != null && bgm.IsPlaying)
        {
            float wait = delaySeconds - (Time.unscaledTime - bgm.BgmStartTimeUnscaled);
            if (wait > 0f)
                yield return new WaitForSecondsRealtime(wait);
            yield break;
        }

        yield return new WaitForSecondsRealtime(delaySeconds);
    }

    void ShowNumber(int value)
    {
        if (countText == null)
            return;

        countText.text = value.ToString();
        if (_countRect != null)
            _countRect.localScale = Vector3.one * 1.15f;
    }

    void HideImmediate()
    {
        if (overlay != null)
        {
            overlay.enabled = false;
            overlay.raycastTarget = false;
        }

        if (countText != null)
            countText.gameObject.SetActive(false);
    }

    void BuildUi()
    {
        var root = transform as RectTransform;
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        if (overlay == null)
        {
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(transform, false);
            var overlayRt = overlayGo.AddComponent<RectTransform>();
            Stretch(overlayRt);
            overlay = overlayGo.AddComponent<Image>();
            overlay.sprite = GreyboxSprites.Square;
            overlay.type = Image.Type.Sliced;
            overlay.color = new Color(0f, 0f, 0f, 0.35f);
            overlay.raycastTarget = false;
        }

        if (countText == null)
        {
            var textGo = new GameObject("CountText");
            textGo.transform.SetParent(transform, false);
            _countRect = textGo.AddComponent<RectTransform>();
            Stretch(_countRect);
            countText = textGo.AddComponent<TextMeshProUGUI>();
            countText.alignment = TextAlignmentOptions.Center;
            countText.fontStyle = FontStyles.Bold;
            countText.fontSize = numberFontSize;
            countText.color = new Color(0.45f, 0.88f, 1f, 1f);
            countText.raycastTarget = false;
            if (BeatDefenderFonts.Pretendard != null)
                countText.font = BeatDefenderFonts.Pretendard;
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
