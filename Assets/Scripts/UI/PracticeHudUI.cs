using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PracticeScene 상단 — GameHud secondary line과 동일한 리듬 상태 표시.
/// </summary>
public class PracticeHudUI : MonoBehaviour, IRuntimeSceneUi
{
    [SerializeField] TextMeshProUGUI statusLine;
    [SerializeField] Image background;

    SkillCooldownController _cooldowns;

    public void EnsureSceneHierarchy()
    {
        if (statusLine == null)
            BuildUi();
    }

    void Awake()
    {
        _cooldowns = FindAnyObjectByType<SkillCooldownController>();
        EnsureSceneHierarchy();
    }

    void OnEnable()
    {
        if (BeatClock.Instance != null)
        {
            BeatClock.Instance.OnTimingChanged -= RefreshTiming;
            BeatClock.Instance.OnTimingChanged += RefreshTiming;
        }

        var cooldowns = SkillCooldownController.Instance ?? _cooldowns;
        if (cooldowns != null)
        {
            cooldowns.OnCooldownsChanged -= Refresh;
            cooldowns.OnCooldownsChanged += Refresh;
        }

        RefreshTiming(0f);
    }

    void OnDisable()
    {
        if (BeatClock.Instance != null)
            BeatClock.Instance.OnTimingChanged -= RefreshTiming;

        var cooldowns = SkillCooldownController.Instance ?? _cooldowns;
        if (cooldowns != null)
            cooldowns.OnCooldownsChanged -= Refresh;
    }

    void Update()
    {
        if (Time.frameCount % 15 == 0)
            RefreshTiming(0f);
    }

    void Refresh() => RefreshTiming(0f);

    void RefreshTiming(float _)
    {
        if (statusLine == null)
            return;

        var cooldowns = SkillCooldownController.Instance ?? _cooldowns;
        statusLine.text = GameHudUI.BuildRhythmSecondaryLine(cooldowns);
    }

    void BuildUi()
    {
        var root = transform as RectTransform;
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -28f);
        root.sizeDelta = new Vector2(GameHudUI.HudBarWidth, 36f);

        if (background == null)
        {
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            background = bgGo.AddComponent<Image>();
            background.sprite = GreyboxSprites.Square;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.08f, 0.08f, 0.11f, 0.88f);
            background.raycastTarget = false;
        }

        if (statusLine == null)
        {
            var textGo = new GameObject("StatusLine");
            textGo.transform.SetParent(transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
            statusLine = textGo.AddComponent<TextMeshProUGUI>();
            statusLine.fontSize = 18f;
            statusLine.alignment = TextAlignmentOptions.Center;
            statusLine.raycastTarget = false;
            if (BeatDefenderFonts.Pretendard != null)
                statusLine.font = BeatDefenderFonts.Pretendard;
        }
    }
}
