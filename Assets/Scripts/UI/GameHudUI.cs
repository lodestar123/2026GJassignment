using TMPro;

using UnityEngine;



/// <summary>

/// 상단 HUD — HP · Gold · BPM · CD · 2:00 타이머.

/// </summary>

public class GameHudUI : MonoBehaviour

{

    [SerializeField] TextMeshProUGUI statusLine;

    [SerializeField] TextMeshProUGUI detailLine;



    ResourceManager _resources;

    SkillCooldownController _cooldowns;

    RunStats _stats;

    BaseHealth _core;

    GameManager _game;



    void Awake()

    {

        _resources = FindAnyObjectByType<ResourceManager>();

        _cooldowns = FindAnyObjectByType<SkillCooldownController>();

        _stats = FindAnyObjectByType<RunStats>();

        _core = FindAnyObjectByType<BaseHealth>();

        _game = FindAnyObjectByType<GameManager>();

    }



    void OnEnable()

    {

        if (_resources != null)

        {

            _resources.OnGoldChanged -= Refresh;

            _resources.OnGoldChanged += Refresh;

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

            _resources.OnGoldChanged -= Refresh;



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



    void OnCooldownsChanged() => Refresh(_resources != null ? _resources.Gold : 0);

    void OnTimingChanged(float _) => Refresh(_resources != null ? _resources.Gold : 0);

    void OnHpChanged(int _, int __) => Refresh(_resources != null ? _resources.Gold : 0);

    void OnTimerChanged(float _) => Refresh(_resources != null ? _resources.Gold : 0);

    void OnStatsChanged() => Refresh(_resources != null ? _resources.Gold : 0);



    void RefreshPeriodic()

    {

        if (Time.frameCount % 15 != 0)

            return;



        Refresh(_resources != null ? _resources.Gold : 0);

    }



    void Refresh(int gold)

    {

        if (statusLine == null)

            return;



        float bpm = BeatClock.Instance != null ? BeatClock.Instance.CurrentBpm : 120f;

        bool boosted = BeatClock.Instance != null && BeatClock.Instance.IsBoosted;

        float boostLeft = BeatClock.Instance != null ? BeatClock.Instance.BoostRemaining : 0f;



        float strikeCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.OverloadStrike) : 0f;

        float boostCd = _cooldowns != null ? _cooldowns.GetRemaining(CommandType.BPMBoost) : 0f;



        int hp = _core != null ? _core.CurrentHp : BaseHealth.DefaultMaxHp;

        int maxHp = _core != null ? _core.MaxHp : BaseHealth.DefaultMaxHp;



        int perfect = _stats != null ? _stats.PerfectCount : 0;

        int good = _stats != null ? _stats.GoodCount : 0;

        int miss = _stats != null ? _stats.MissCount : 0;



        string bpmLine = boosted

            ? $"<color=#FFB74D>{bpm:0} BPM</color> ({boostLeft:0.0}s)"

            : $"{bpm:0} BPM";



        string timerLine = FormatTimer(_game != null ? _game.RemainingSeconds : GameManager.MatchDurationSeconds);



        statusLine.text =

            $"<b>HP {hp}/{maxHp}</b>  |  " +

            $"<b>{gold}G</b>  |  {timerLine}  |  {bpmLine}  |  " +

            $"Strike {FormatCd(strikeCd)}  |  Boost {FormatCd(boostCd)}";



        if (detailLine != null)

        {

            int towers = TowerPlacer.Instance != null ? TowerPlacer.Instance.TowerCount : 0;

            detailLine.text =

                $"Judge P{perfect} G{good} M{miss}  |  " +

                $"Towers {towers}/{TowerPlacer.MaxTowers}  |  " +

                $"Tower: {(TowerSelection.HasSelection ? TowerSelection.Selected.ToString() : "None")}";

        }

    }



    static string FormatTimer(float seconds)

    {

        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));

        int min = total / 60;

        int sec = total % 60;

        return $"<b>{min}:{sec:00}</b>";

    }



    static string FormatCd(float seconds)

    {

        return seconds > 0f ? $"{seconds:0.0}s" : "Ready";

    }

}


