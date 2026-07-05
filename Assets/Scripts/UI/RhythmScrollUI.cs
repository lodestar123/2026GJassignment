using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌측 Rhythm Scroll — 4패턴 카드 · CD 표시 · Tab 확대/축소.
/// </summary>
public class RhythmScrollUI : MonoBehaviour
{
    [System.Serializable]
    public class PatternCard
    {
        public CommandType Type;
        public GameObject Root;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI PatternText;
        public TextMeshProUGUI CooldownText;
        public Image AccentBar;
    }

    [SerializeField] RectTransform panelRect;
    [SerializeField] PatternCard[] cards;
    [SerializeField] float expandedWidth = 280f;
    [SerializeField] float collapsedWidth = 48f;

    bool _expanded = true;
    bool _subscribed;

    static readonly Color GoldColor = new(1f, 0.84f, 0.31f);
    static readonly Color ShotColor = new(0.92f, 0.92f, 0.92f);
    static readonly Color StrikeColor = new(0.94f, 0.33f, 0.31f);
    static readonly Color BoostColor = new(0.81f, 0.58f, 0.85f);

    void OnEnable()
    {
        TrySubscribe();
        RefreshAll();
        ApplyExpandedState();
    }

    void Start()
    {
        TrySubscribe();
        RefreshAll();
    }

    void OnDisable() => TryUnsubscribe();

    void Update()
    {
        if (!_subscribed)
            TrySubscribe();

        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleExpanded();
    }

    void TrySubscribe()
    {
        if (_subscribed)
            return;

        var cooldowns = SkillCooldownController.Instance
            ?? FindAnyObjectByType<SkillCooldownController>();
        if (cooldowns != null)
        {
            cooldowns.OnCooldownsChanged -= RefreshCooldowns;
            cooldowns.OnCooldownsChanged += RefreshCooldowns;
        }

        _subscribed = cooldowns != null;
    }

    void TryUnsubscribe()
    {
        var cooldowns = SkillCooldownController.Instance
            ?? FindAnyObjectByType<SkillCooldownController>();
        if (cooldowns != null)
            cooldowns.OnCooldownsChanged -= RefreshCooldowns;

        _subscribed = false;
    }

    public void ToggleExpanded()
    {
        _expanded = !_expanded;
        ApplyExpandedState();
    }

    void ApplyExpandedState()
    {
        if (panelRect == null)
            return;

        panelRect.sizeDelta = new Vector2(_expanded ? expandedWidth : collapsedWidth, panelRect.sizeDelta.y);

        if (cards == null)
            return;

        foreach (var card in cards)
        {
            if (card?.Root == null)
                continue;

            if (card.PatternText != null)
                card.PatternText.gameObject.SetActive(_expanded);
            if (card.CooldownText != null)
                card.CooldownText.gameObject.SetActive(_expanded);
        }
    }

    void RefreshAll()
    {
        if (cards == null || BeatClock.Instance == null)
            return;

        float md = BeatClock.Instance.EffectiveMeasureDuration;

        foreach (var card in cards)
        {
            if (card?.Root == null)
                continue;

            card.AccentBar.color = GetAccent(card.Type);
            card.TitleText.text = GetTitle(card.Type);
            card.PatternText.text = GetPatternHint(card.Type, md);
        }

        RefreshCooldowns();
    }

    void RefreshCooldowns()
    {
        if (cards == null)
            return;

        var cooldowns = SkillCooldownController.Instance;
        foreach (var card in cards)
        {
            if (card?.CooldownText == null)
                continue;

            if (card.Type == CommandType.OverloadStrike)
            {
                float rem = cooldowns != null ? cooldowns.GetRemaining(CommandType.OverloadStrike) : 0f;
                card.CooldownText.text = rem > 0f ? $"CD {rem:0.0}s" : "";
            }
            else if (card.Type == CommandType.BPMBoost)
            {
                float rem = cooldowns != null ? cooldowns.GetRemaining(CommandType.BPMBoost) : 0f;
                card.CooldownText.text = rem > 0f ? $"CD {rem:0.0}s" : "";
            }
            else
            {
                card.CooldownText.text = "";
            }
        }
    }

    static Color GetAccent(CommandType type) => type switch
    {
        CommandType.GoldPulse => GoldColor,
        CommandType.RhythmShot => ShotColor,
        CommandType.OverloadStrike => StrikeColor,
        CommandType.BPMBoost => BoostColor,
        _ => Color.gray
    };

    static string GetTitle(CommandType type) => type switch
    {
        CommandType.GoldPulse => "Gold",
        CommandType.RhythmShot => "Shot",
        CommandType.OverloadStrike => "Strike",
        CommandType.BPMBoost => "Boost",
        _ => type.ToString()
    };

    static string GetPatternHint(CommandType type, float md) => type switch
    {
        CommandType.GoldPulse => $"2 taps: 0, {md * 0.5f:0.##}s",
        CommandType.RhythmShot => $"3 taps: 0, {md * 0.5f:0.##}, {md * 0.75f:0.##}s",
        CommandType.OverloadStrike => "5 taps",
        CommandType.BPMBoost => "1 tap @ downbeat",
        _ => ""
    };
}
