using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌측 Rhythm Scroll — 패턴 카드 · 선택 · CD · Tab 확대/축소.
/// 제목·설명·박자 도형은 Inspector / Hierarchy에서 직접 지정.
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

        [Tooltip("카드 제목 (예: 일반 공격). 비우면 TitleText 내용을 덮어쓰지 않음.")]
        public string Title;

        [Tooltip("짧은 설명. 비우면 PatternText는 숨기고 PatternVisual만 사용 가능.")]
        public string Description;

        [Tooltip("박자 도형 등 직접 만든 UI 자식. Tab 축소 시 함께 숨김.")]
        public GameObject PatternVisual;
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

#if UNITY_EDITOR
    void OnValidate()
    {
        if (cards == null || !isActiveAndEnabled)
            return;

        foreach (var card in cards)
            ApplyCardCopy(card);

        ApplyExpandedState();
    }
#endif

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

        _subscribed = cooldowns != null || selector != null;
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
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(_expanded ? expandedWidth : collapsedWidth, panelRect.sizeDelta.y);

        if (cards == null)
            return;

        foreach (var card in cards)
        {
            if (card?.Root == null)
                continue;

            bool showDescription = ShouldShowDescription(card);
            if (card.PatternText != null)
                card.PatternText.gameObject.SetActive(_expanded && showDescription);

            if (card.PatternVisual != null)
                card.PatternVisual.SetActive(_expanded);

            if (card.CooldownText != null)
                card.CooldownText.gameObject.SetActive(_expanded);
        }
    }

    static bool ShouldShowDescription(PatternCard card) =>
        !string.IsNullOrEmpty(card.Description)
        || (card.PatternVisual == null && card.PatternText != null && !string.IsNullOrEmpty(card.PatternText.text));

    void RefreshAll()
    {
        if (cards == null)
            return;

        foreach (var card in cards)
        {
            if (card?.Root == null)
                continue;

            if (card.AccentBar != null)
                card.AccentBar.color = GetAccent(card.Type);

            ApplyCardCopy(card);
        }

        RefreshCooldowns();
        RefreshSelectionVisual();
        ApplyExpandedState();
    }

    void ApplyCardCopy(PatternCard card)
    {
        if (card.TitleText != null && !string.IsNullOrEmpty(card.Title))
            card.TitleText.text = card.Title;

        if (card.PatternText != null && !string.IsNullOrEmpty(card.Description))
            card.PatternText.text = card.Description;
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
        CommandType.ChainZap => BoostColor,
        CommandType.TempoUp => TempoUpColor,
        CommandType.TempoDown => TempoDownColor,
        _ => Color.gray
    };
}
