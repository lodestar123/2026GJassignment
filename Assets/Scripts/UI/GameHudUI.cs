using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 HUD — Time / HP / Gold 카드 + 하단 보조 정보.
/// </summary>
public class GameHudUI : MonoBehaviour
{
    [Header("HP Card")]
    [SerializeField] TextMeshProUGUI hpTitle;
    [SerializeField] TextMeshProUGUI hpValue;
    [SerializeField] TextMeshProUGUI hpHearts;
    [SerializeField] Image hpBarFill;
    [SerializeField] Image hpCardBg;

    [Header("Gold Card")]
    [SerializeField] TextMeshProUGUI goldTitle;
    [SerializeField] TextMeshProUGUI goldValue;
    [SerializeField] TextMeshProUGUI goldSub;
    [SerializeField] Image goldCardBg;

    [Header("Timer Card")]
    [SerializeField] TextMeshProUGUI timerTitle;
    [SerializeField] TextMeshProUGUI timerValue;
    [SerializeField] Image timerBarFill;
    [SerializeField] Image timerCardBg;

    [Header("Secondary")]
    [SerializeField] TextMeshProUGUI secondaryLine;

    [Header("Legacy (pre-card layout)")]
    [SerializeField] TextMeshProUGUI statusLine;
    [SerializeField] TextMeshProUGUI detailLine;

    static readonly Color HpAccent = new(0.95f, 0.32f, 0.32f, 1f);
    static readonly Color GoldAccent = new(1f, 0.82f, 0.28f, 1f);
    static readonly Color TimerAccent = new(0.45f, 0.85f, 1f, 1f);
    static readonly Color TimerUrgentAccent = new(1f, 0.55f, 0.25f, 1f);
    static readonly Color CardBgNormal = new(0.08f, 0.08f, 0.11f, 0.88f);
    static readonly Color CardBgCrisis = new(0.2f, 0.05f, 0.07f, 0.92f);
    public const float HudBarWidth = 512f;
    public const float HudBarHeight = 106f;
    const float HudTitleFontSize = 14f;
    const float HudValueFontSize = 40f;
    const float HudSubFontSize = 22f;
    const float HudSecondaryFontSize = 18f;
    const float LegacyStatusFontSize = 28f;
    const float LegacyDetailFontSize = 22f;
    const string Sep = " | ";

    ResourceManager _resources;
    SkillCooldownController _cooldowns;
    RunStats _stats;
    BaseHealth _core;
    GameManager _game;

    float _goldSpendFlashUntil;
    int _lastGoldSpent;

    bool UsesCardLayout => hpValue != null && goldValue != null && timerValue != null;

    void Awake()
    {
        _resources = FindAnyObjectByType<ResourceManager>();
        _cooldowns = FindAnyObjectByType<SkillCooldownController>();
        _stats = FindAnyObjectByType<RunStats>();
        _core = FindAnyObjectByType<BaseHealth>();
        _game = FindAnyObjectByType<GameManager>();

        if (!UsesCardLayout)
            TryResolveCardRefs();

        ApplyHudLayout();
        ApplyHudTypography();
    }

    void ApplyHudTypography()
    {
        if (UsesCardLayout)
        {
            SetFontSize(hpTitle, HudTitleFontSize);
            SetFontSize(hpValue, HudValueFontSize);
            SetFontSize(hpHearts, HudSubFontSize);
            SetFontSize(goldTitle, HudTitleFontSize);
            SetFontSize(goldValue, HudValueFontSize + 2f);
            SetFontSize(goldSub, HudSubFontSize - 3f);
            SetFontSize(timerTitle, HudTitleFontSize);
            SetFontSize(timerValue, HudValueFontSize + 2f);
            SetFontSize(secondaryLine, HudSecondaryFontSize);
            return;
        }

        SetFontSize(statusLine, LegacyStatusFontSize);
        SetFontSize(detailLine, LegacyDetailFontSize);
    }

    static void SetFontSize(TextMeshProUGUI tmp, float size)
    {
        if (tmp == null)
            return;

        tmp.fontSize = size;
        tmp.fontSizeMax = size;
        tmp.fontSizeMin = size;
    }

    void ApplyHudLayout()
    {
        var rootRect = transform as RectTransform;
        if (rootRect == null)
            return;

        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.sizeDelta = new Vector2(HudBarWidth, HudBarHeight);

        var cardsRow = transform.Find("CardsRow") as RectTransform;
        if (cardsRow != null)
        {
            cardsRow.anchorMin = Vector2.zero;
            cardsRow.anchorMax = Vector2.one;
            cardsRow.offsetMin = new Vector2(0f, 28f);
            cardsRow.offsetMax = new Vector2(0f, -4f);

            ApplyCardSlot(cardsRow, "TimerCard", 0f, 0.32f, 0);
            ApplyCardSlot(cardsRow, "HpCard", 0.34f, 0.66f, 1);
            ApplyCardSlot(cardsRow, "GoldCard", 0.68f, 1f, 2);

            if (secondaryLine != null)
            {
                secondaryLine.alignment = TextAlignmentOptions.Center;
                var rt = secondaryLine.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.offsetMin = new Vector2(0f, 4f);
                rt.offsetMax = new Vector2(0f, 26f);
            }

            return;
        }

        if (statusLine != null)
            statusLine.alignment = TextAlignmentOptions.Center;

        if (detailLine != null)
            detailLine.alignment = TextAlignmentOptions.Center;
    }

    static void ApplyCardSlot(RectTransform row, string cardName, float minX, float maxX, int siblingIndex)
    {
        var card = row.Find(cardName) as RectTransform;
        if (card == null)
            return;

        card.anchorMin = new Vector2(minX, 0f);
        card.anchorMax = new Vector2(maxX, 1f);
        card.offsetMin = new Vector2(2f, 0f);
        card.offsetMax = new Vector2(-2f, 0f);
        card.SetSiblingIndex(siblingIndex);
    }

    void TryResolveCardRefs()
    {
        var cards = transform.Find("CardsRow");
        if (cards == null)
            return;

        hpTitle ??= FindTmp(cards, "HpCard/Title");
        hpValue ??= FindTmp(cards, "HpCard/Value");
        hpHearts ??= FindTmp(cards, "HpCard/Hearts");
        hpBarFill ??= FindImg(cards, "HpCard/Bar/Fill");
        hpCardBg ??= cards.Find("HpCard")?.GetComponent<Image>();

        goldTitle ??= FindTmp(cards, "GoldCard/Title");
        goldValue ??= FindTmp(cards, "GoldCard/Value");
        goldSub ??= FindTmp(cards, "GoldCard/Sub");
        goldCardBg ??= cards.Find("GoldCard")?.GetComponent<Image>();

        timerTitle ??= FindTmp(cards, "TimerCard/Title");
        timerValue ??= FindTmp(cards, "TimerCard/Value");
        timerBarFill ??= FindImg(cards, "TimerCard/Bar/Fill");
        timerCardBg ??= cards.Find("TimerCard")?.GetComponent<Image>();

        secondaryLine ??= FindTmp(transform, "SecondaryLine");
    }

    static TextMeshProUGUI FindTmp(Transform root, string path) =>
        root.Find(path)?.GetComponent<TextMeshProUGUI>();

    static Image FindImg(Transform root, string path) =>
        root.Find(path)?.GetComponent<Image>();

    void OnEnable()
    {
        if (_resources != null)
        {
            _resources.OnGoldChanged -= Refresh;
            _resources.OnGoldChanged += Refresh;
            _resources.OnGoldSpent -= OnGoldSpent;
            _resources.OnGoldSpent += OnGoldSpent;
        }

        if (_core != null)
        {
            _core.OnHpChanged -= OnHpChanged;
            _core.OnHpChanged += OnHpChanged;
        }

        if (_game != null)
        {
            _game.OnTimerChanged -= OnTimerChanged;
            _game.OnTimerChanged += OnTimerChanged;
        }

        if (BeatClock.Instance != null)
        {
            BeatClock.Instance.OnTimingChanged -= OnTimingChanged;
            BeatClock.Instance.OnTimingChanged += OnTimingChanged;
        }

        var cooldowns = SkillCooldownController.Instance ?? _cooldowns;
        if (cooldowns != null)
        {
            cooldowns.OnCooldownsChanged -= OnCooldownsChanged;
            cooldowns.OnCooldownsChanged += OnCooldownsChanged;
        }

        if (_stats != null)
        {
            _stats.OnStatsChanged -= OnStatsChanged;
            _stats.OnStatsChanged += OnStatsChanged;
        }

        Refresh(0);
    }

    void OnDisable()
    {
        if (_resources != null)
        {
            _resources.OnGoldChanged -= Refresh;
            _resources.OnGoldSpent -= OnGoldSpent;
        }

        if (_core != null)
            _core.OnHpChanged -= OnHpChanged;

        if (_game != null)
            _game.OnTimerChanged -= OnTimerChanged;

        if (BeatClock.Instance != null)
            BeatClock.Instance.OnTimingChanged -= OnTimingChanged;

        var cooldowns = SkillCooldownController.Instance ?? _cooldowns;
        if (cooldowns != null)
            cooldowns.OnCooldownsChanged -= OnCooldownsChanged;

        if (_stats != null)
            _stats.OnStatsChanged -= OnStatsChanged;
    }

    void Update() => RefreshPeriodic();

    void OnGoldSpent(int amount)
    {
        _lastGoldSpent = amount;
        _goldSpendFlashUntil = Time.unscaledTime + 0.55f;
        Refresh(_resources != null ? _resources.Gold : 0);
    }

    void OnCooldownsChanged() => Refresh(_resources != null ? _resources.Gold : 0);
    void OnTimingChanged(float _) => Refresh(_resources != null ? _resources.Gold : 0);
    void OnHpChanged(int _, int __) => Refresh(_resources != null ? _resources.Gold : 0);
    void OnTimerChanged(float _) => Refresh(_resources != null ? _resources.Gold : 0);
    void OnStatsChanged() => Refresh(_resources != null ? _resources.Gold : 0);

    void RefreshPeriodic()
    {
        bool crisis = _core != null && _core.CurrentHp == 1 && _core.MaxHp > 1;
        bool goldFlash = _goldSpendFlashUntil > Time.unscaledTime;
        if (!crisis && !goldFlash && Time.frameCount % 15 != 0)
            return;

        Refresh(_resources != null ? _resources.Gold : 0);
    }

    void Refresh(int gold)
    {
        if (UsesCardLayout)
            RefreshCards(gold);
        else
            RefreshLegacy(gold);
    }

    void RefreshCards(int gold)
    {
        int hp = _core != null ? _core.CurrentHp : BaseHealth.DefaultMaxHp;
        int maxHp = _core != null ? _core.MaxHp : BaseHealth.DefaultMaxHp;
        float remaining = _game != null ? _game.RemainingSeconds : GameManager.MatchDurationSeconds;

        bool crisis = hp == 1 && maxHp > 1;
        float crisisPulse = crisis ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f) : 0f;
        bool spendFlash = _goldSpendFlashUntil > Time.unscaledTime;
        bool timerUrgent = remaining <= 30f;

        if (hpTitle != null)
            hpTitle.text = "CORE HP";

        if (hpValue != null)
        {
            Color hpColor = crisis
                ? (crisisPulse > 0.75f ? new Color(1f, 0.09f, 0.27f) : HpAccent)
                : HpAccent;
            hpValue.color = hpColor;
            hpValue.text = $"{hp}<size=60%><color=#AAAAAA>/{maxHp}</color></size>";
        }

        if (hpHearts != null)
            hpHearts.text = FormatHearts(hp, maxHp, crisis);

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = maxHp > 0 ? (float)hp / maxHp : 0f;
            hpBarFill.color = crisis ? Color.Lerp(HpAccent, Color.white, crisisPulse * 0.35f) : HpAccent;
        }

        if (hpCardBg != null)
            hpCardBg.color = crisis ? Color.Lerp(CardBgCrisis, CardBgNormal, crisisPulse * 0.25f) : CardBgNormal;

        if (goldTitle != null)
            goldTitle.text = "GOLD";

        if (goldValue != null)
        {
            goldValue.color = spendFlash ? new Color(1f, 0.32f, 0.32f) : GoldAccent;
            goldValue.text = $"{gold}<size=55%>G</size>";
        }

        if (goldSub != null)
        {
            goldSub.text = spendFlash
                ? $"<color=#FF8A65>-{_lastGoldSpent}G</color>"
                : string.Empty;
        }

        if (timerTitle != null)
            timerTitle.text = "TIME LEFT";

        if (timerValue != null)
        {
            timerValue.color = timerUrgent ? TimerUrgentAccent : Color.white;
            timerValue.text = FormatTimer(remaining);
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = remaining / GameManager.MatchDurationSeconds;
            timerBarFill.color = timerUrgent ? TimerUrgentAccent : TimerAccent;
        }

        if (timerCardBg != null)
            timerCardBg.color = CardBgNormal;

        if (goldCardBg != null)
            goldCardBg.color = CardBgNormal;

        if (secondaryLine != null)
            secondaryLine.text = BuildSecondaryLine(gold);
    }

    void RefreshLegacy(int gold)
    {
        if (statusLine == null)
            return;

        float bpm = BeatClock.Instance != null ? BeatClock.Instance.CurrentBpm : 120f;
        bool fever = FeverTimeController.Instance != null && FeverTimeController.Instance.IsFeverActive;
        float feverLeft = FeverTimeController.Instance != null
            ? FeverTimeController.Instance.FeverRemaining
            : 0f;
        float tempoScale = BeatClock.Instance != null ? BeatClock.Instance.TempoScale : 1f;

        string bpmLine = fever
            ? $"<color=#FFB74D><b>FEVER</b> DMG x{FeverTimeController.DamageMultiplier:0.#}</color> ({feverLeft:0.0}s)"
            : tempoScale != 1f
                ? $"{bpm:0} BPM <size=85%>(x{tempoScale:0.##})</size>"
                : $"{bpm:0} BPM";

        float strikeCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.OverloadStrike) : 0f;
        float chainCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.ChainZap) : 0f;

        int hp = _core != null ? _core.CurrentHp : BaseHealth.DefaultMaxHp;
        int maxHp = _core != null ? _core.MaxHp : BaseHealth.DefaultMaxHp;

        string timerLine = FormatTimerRich(_game != null ? _game.RemainingSeconds : GameManager.MatchDurationSeconds);

        bool spendFlash = _goldSpendFlashUntil > Time.unscaledTime;
        string goldLine = spendFlash
            ? $"<color=#FF5252><b>{gold}G</b></color> <size=85%><color=#FF8A65>(-{_lastGoldSpent}G)</color></size>"
            : $"<b>{gold}G</b>";

        bool crisis = hp == 1 && maxHp > 1;
        float crisisPulse = crisis ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f) : 0f;
        string hpLine = crisis
            ? $"<color=#FF5252><b>!! HP {hp}/{maxHp} !!</b></color>"
            : $"<b>HP {hp}/{maxHp}</b>";
        if (crisis && crisisPulse > 0.75f)
            hpLine = $"<color=#FF1744><b>!! HP {hp}/{maxHp} !!</b></color>";

        statusLine.text =
            $"{timerLine}{Sep}{hpLine}{Sep}{goldLine}{Sep}{bpmLine}{Sep}" +
            $"Strike {FormatCd(strikeCd)}{Sep}Chain {FormatCd(chainCd)}";

        if (detailLine != null)
        {
            int towers = TowerPlacer.Instance != null ? TowerPlacer.Instance.TowerCount : 0;
            int perfect = _stats != null ? _stats.PerfectCount : 0;
            int good = _stats != null ? _stats.GoodCount : 0;
            int miss = _stats != null ? _stats.MissCount : 0;

            detailLine.text =
                $"Judge P{perfect} G{good} M{miss}{Sep}Towers {towers}{Sep}" +
                $"Tower: {(TowerSelection.HasSelection ? TowerSelection.Selected.ToString() : "None")}";
        }
    }

    string BuildSecondaryLine(int gold)
    {
        float bpm = BeatClock.Instance != null ? BeatClock.Instance.CurrentBpm : 120f;
        bool fever = FeverTimeController.Instance != null && FeverTimeController.Instance.IsFeverActive;
        float feverLeft = FeverTimeController.Instance != null
            ? FeverTimeController.Instance.FeverRemaining
            : 0f;
        float tempoScale = BeatClock.Instance != null ? BeatClock.Instance.TempoScale : 1f;

        string bpmPart = fever
            ? $"<color=#FFB74D><b>FEVER</b> x{FeverTimeController.DamageMultiplier:0.#}</color> ({feverLeft:0.0}s)"
            : tempoScale != 1f
                ? $"{bpm:0} BPM <size=90%>(x{tempoScale:0.##})</size>"
                : $"{bpm:0} BPM";

        float strikeCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.OverloadStrike) : 0f;
        float chainCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.ChainZap) : 0f;

        int perfect = _stats != null ? _stats.PerfectCount : 0;
        int good = _stats != null ? _stats.GoodCount : 0;
        int miss = _stats != null ? _stats.MissCount : 0;
        int towers = TowerPlacer.Instance != null ? TowerPlacer.Instance.TowerCount : 0;

        string towerSel = TowerSelection.HasSelection ? TowerSelection.Selected.ToString() : "None";

        return
            $"{bpmPart}{Sep}Strike {FormatCdRich(strikeCd)}{Sep}Chain {FormatCdRich(chainCd)}{Sep}" +
            $"P{perfect} G{good} M{miss}{Sep}Towers {towers}{Sep}{towerSel}";
    }

    static string FormatHearts(int hp, int maxHp, bool crisis)
    {
        var sb = new System.Text.StringBuilder(maxHp * 4);
        for (int i = 0; i < maxHp; i++)
        {
            if (i > 0)
                sb.Append(' ');

            if (i < hp)
            {
                string color = crisis ? "#FF1744" : "#FF5252";
                sb.Append($"<color={color}>*</color>");
            }
            else
            {
                sb.Append("<color=#555555>-</color>");
            }
        }

        return sb.ToString();
    }

    static string FormatTimer(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int min = total / 60;
        int sec = total % 60;
        return $"{min}:{sec:00}";
    }

    static string FormatTimerRich(float seconds) => $"<b>{FormatTimer(seconds)}</b>";

    static string FormatCd(float seconds) =>
        seconds > 0f ? $"{seconds:0.0}s" : "Ready";

    static string FormatCdRich(float seconds) =>
        seconds > 0f
            ? $"<color=#AAAAAA>{seconds:0.0}s</color>"
            : "<color=#69F0AE>Ready</color>";
}
