using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌측 Rhythm Scroll — 4패턴 카드 · 선택 · CD · Tab 확대/축소.
/// </summary>
public class RhythmScrollUI : MonoBehaviour
{
    [System.Serializable]
    public class PatternCard
    {
        public CommandType Type;
        public GameObject Root;
        public Button SelectButton;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI PatternText;
        public TextMeshProUGUI CooldownText;
        public Image AccentBar;
        public Image Background;
    }

    [SerializeField] RectTransform panelRect;
    [SerializeField] PatternCard[] cards;
    [SerializeField] float expandedWidth = 280f;
    [SerializeField] float collapsedWidth = 48f;

    static readonly Color NormalBg = new(0.08f, 0.08f, 0.1f, 0.92f);
    static readonly Color SelectedBg = new(0.14f, 0.16f, 0.22f, 0.98f);
    static readonly Color GoldColor = new(1f, 0.84f, 0.31f);
    static readonly Color ShotColor = new(0.92f, 0.92f, 0.92f);
    static readonly Color StrikeColor = new(0.94f, 0.33f, 0.31f);
    static readonly Color BoostColor = new(0.81f, 0.58f, 0.85f);
    static readonly Color TempoUpColor = new(0.35f, 0.85f, 1f);
    static readonly Color TempoDownColor = new(0.62f, 0.55f, 0.95f);

    bool _expanded = true;
    bool _subscribed;
    bool _buttonsWired;

    void OnEnable()
    {
        WireCardButtons();
        TrySubscribe();
        RefreshAll();
        ApplyExpandedState();
    }

    void Start()
    {
        WireCardButtons();
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

    void WireCardButtons()
    {
        if (_buttonsWired || cards == null)
            return;

        foreach (var card in cards)
        {
            if (card?.Root == null)
                continue;

            if (card.SelectButton == null)
                card.SelectButton = card.Root.GetComponent<Button>();

            if (card.Background == null)
                card.Background = card.Root.GetComponent<Image>();

            if (card.SelectButton == null)
            {
                if (card.Background != null)
                    card.Background.raycastTarget = true;
                card.SelectButton = card.Root.AddComponent<Button>();
                if (card.Background != null)
                    card.SelectButton.targetGraphic = card.Background;
            }

            var type = card.Type;
            card.SelectButton.onClick.RemoveAllListeners();
            card.SelectButton.onClick.AddListener(() => SelectPattern(type));
        }

        _buttonsWired = true;
    }

    void SelectPattern(CommandType type)
    {
        var selector = RhythmPatternSelector.Instance
            ?? RhythmCommandDetector.Instance?.GetComponent<RhythmPatternSelector>();
        selector?.SetSelected(type);
        RefreshSelectionVisual();
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

        var selector = RhythmPatternSelector.Instance
            ?? FindAnyObjectByType<RhythmPatternSelector>();
        if (selector != null)
        {
            selector.OnSelectionChanged -= HandleSelectionChanged;
            selector.OnSelectionChanged += HandleSelectionChanged;
        }

        var tempo = TempoController.Instance ?? FindAnyObjectByType<TempoController>();
        if (tempo != null)
        {
            tempo.OnTempoChanged -= RefreshTempoStacks;
            tempo.OnTempoChanged += RefreshTempoStacks;
            tempo.OnTempoChanged -= RefreshPatternHints;
            tempo.OnTempoChanged += RefreshPatternHints;
        }

        _subscribed = cooldowns != null || selector != null || tempo != null;
    }

    void TryUnsubscribe()
    {
        var cooldowns = SkillCooldownController.Instance
            ?? FindAnyObjectByType<SkillCooldownController>();
        if (cooldowns != null)
            cooldowns.OnCooldownsChanged -= RefreshCooldowns;

        var selector = RhythmPatternSelector.Instance
            ?? FindAnyObjectByType<RhythmPatternSelector>();
        if (selector != null)
            selector.OnSelectionChanged -= HandleSelectionChanged;

        var tempo = TempoController.Instance ?? FindAnyObjectByType<TempoController>();
        if (tempo != null)
        {
            tempo.OnTempoChanged -= RefreshTempoStacks;
            tempo.OnTempoChanged -= RefreshPatternHints;
        }

        _subscribed = false;
    }

    void HandleSelectionChanged(CommandType type) => RefreshSelectionVisual();

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
            card.PatternText.text = RhythmPatternLibrary.FormatPatternHint(card.Type, md);
        }

        RefreshCooldowns();
        RefreshTempoStacks();
        RefreshSelectionVisual();
    }

    void RefreshPatternHints()
    {
        if (cards == null || BeatClock.Instance == null)
            return;

        float md = BeatClock.Instance.EffectiveMeasureDuration;
        foreach (var card in cards)
        {
            if (card?.PatternText == null)
                continue;

            card.PatternText.text = RhythmPatternLibrary.FormatPatternHint(card.Type, md);
        }
    }

    void RefreshTempoStacks()
    {
        if (cards == null)
            return;

        var tempo = TempoController.Instance;
        foreach (var card in cards)
        {
            if (card?.CooldownText == null)
                continue;

            if (card.Type == CommandType.TempoUp)
            {
                int stacks = tempo != null ? tempo.FastStacks : 0;
                card.CooldownText.text = stacks > 0
                    ? $"Fast x{stacks}/{TempoController.MaxStacksPerDirection}"
                    : "";
            }
            else if (card.Type == CommandType.TempoDown)
            {
                int stacks = tempo != null ? tempo.SlowStacks : 0;
                card.CooldownText.text = stacks > 0
                    ? $"Slow x{stacks}/{TempoController.MaxStacksPerDirection}"
                    : "";
            }
        }
    }

    void RefreshSelectionVisual()
    {
        if (cards == null)
            return;

        var selected = RhythmPatternSelector.Instance != null
            ? RhythmPatternSelector.Instance.Selected
            : CommandType.GoldPulse;

        foreach (var card in cards)
        {
            if (card?.Background == null)
                continue;

            bool isSelected = card.Type == selected;
            card.Background.color = isSelected ? SelectedBg : NormalBg;

            if (card.AccentBar != null)
            {
                var accentRt = card.AccentBar.rectTransform;
                accentRt.sizeDelta = new Vector2(isSelected ? 10f : 6f, accentRt.sizeDelta.y);
            }
        }
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
            else if (card.Type == CommandType.ChainZap)
            {
                float rem = cooldowns != null ? cooldowns.GetRemaining(CommandType.ChainZap) : 0f;
                card.CooldownText.text = rem > 0f ? $"CD {rem:0.0}s" : "";
            }
            else if (card.Type != CommandType.TempoUp && card.Type != CommandType.TempoDown)
            {
                card.CooldownText.text = "";
            }
        }

        RefreshTempoStacks();
    }

    static Color GetAccent(CommandType type) => type switch
    {
        CommandType.GoldPulse => GoldColor,
        CommandType.RhythmShot => ShotColor,
        CommandType.OverloadStrike => StrikeColor,
        CommandType.ChainZap => BoostColor,
        CommandType.TempoUp => TempoUpColor,
        CommandType.TempoDown => TempoDownColor,
        _ => Color.gray
    };

    static string GetTitle(CommandType type) => type switch
    {
        CommandType.GoldPulse => "Gold",
        CommandType.RhythmShot => "Shot",
        CommandType.OverloadStrike => "Strike",
        CommandType.ChainZap => "Chain",
        CommandType.TempoUp => "Fast",
        CommandType.TempoDown => "Slow",
        _ => type.ToString()
    };
}
