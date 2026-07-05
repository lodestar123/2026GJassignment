using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 튜토리얼 단계 진행 — 리듬·패턴·타워·승리 조건을 순차 안내.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TutorialController : MonoBehaviour
{
    static readonly TutorialStep[] Steps =
    {
        new(
            "Beat Defender",
            "120 BPM 박자에 맞춰 <b>왼손</b>으로 리듬 패턴을 입력하고, "
            + "<b>오른손</b>으로 타워를 배치해 Core를 지키는 탑뷰 디펜스입니다.\n\n"
            + "목표: <b>2분(120초) 생존</b>. Core HP가 0이 되면 패배합니다.",
            TutorialAdvanceMode.Manual),

        new(
            "박자와 타임라인",
            "화면 하단 <color=#59E0FF>타임라인</color>의 playhead가 박자를 보여줍니다.\n"
            + "<b>Space</b>(또는 다른 리듬 키)를 박자에 맞춰 한 번 눌러보세요.\n"
            + "가이드 선 근처면 <color=#FFD54F>PERFECT</color> / <color=#8CF29E>GOOD</color> 마커가 찍힙니다.",
            TutorialAdvanceMode.WaitFirstGoodTap),

        new(
            "골드 펄스 (GoldPulse)",
            "Scroll에서 <color=#FFD54F>GoldPulse</color>가 선택되어 있습니다.\n"
            + "마디 안에서 <b>0초, 0.5초</b> 두 박(쿵! 쿵!)을 맞춰 입력하세요.\n"
            + "성공하면 <b>+10 골드</b> - 쿨타임 없이 반복 가능합니다.",
            TutorialAdvanceMode.WaitGoldPulseSuccess,
            CommandType.GoldPulse),

        new(
            "패턴 선택 (Scroll)",
            "마우스 <b>휠</b>로 Scroll 카드를 바꿔 다른 패턴을 선택할 수 있습니다.\n"
            + "한 번 휠을 돌려 다른 패턴으로 바꿔 보세요.\n"
            + "<b>Tab</b>으로 Scroll을 확대할 수도 있습니다.",
            TutorialAdvanceMode.WaitPatternChange),

        new(
            "리듬 샷 (RhythmShot)",
            "Scroll에서 <color=#EEEEEE>RhythmShot</color>을 선택하세요.\n"
            + "<b>0 → 0.25 → 0.5초</b> 세 박(딴! 딴! 따!)을 입력하면 "
            + "배치된 <b>평타 타워</b>가 즉시 1발 사격합니다.\n"
            + "튜토리얼에서는 타워 없이도 패턴 연습이 가능합니다.",
            TutorialAdvanceMode.WaitRhythmShotSuccess,
            CommandType.RhythmShot),

        new(
            "타워 배치",
            "우측 하단 <b>Tower</b> 버튼을 눌러 배치 모드를 켠 뒤, "
            + "화면 중앙의 <color=#59E066>초록 슬롯</color>을 클릭해 타워를 설치하세요.\n"
            + "- 타워 클릭 - <b>판매</b> (50% 환급)\n"
            + "- <b>Space 입력 중에도</b> 마우스로 배치 가능 (양손 협응)",
            TutorialAdvanceMode.WaitTowerPlaced),

        new(
            "강공격 (OverloadStrike)",
            "Scroll에서 <color=#F05550>OverloadStrike</color>를 선택하세요.\n"
            + "<b>0 → 0.5 → 0.75초</b> 세 박 입력 시 Strike 타워 사거리 안 적에게 "
            + "광역 피해(8 dmg)를 줍니다. 쿨타임 <b>10초</b>.\n"
            + "튜토리얼에서는 CD가 꺼져 있으니 연습해 보세요.",
            TutorialAdvanceMode.WaitOverloadSuccess,
            CommandType.OverloadStrike),

        new(
            "체인, 템포 스킬",
            "<color=#CF94D9>ChainZap</color> - 6타 연타, 범위 연쇄 피해\n"
            + "<color=#59D9FF>TempoUp</color> - 0, 0.25s 2타, 6초간 BPM 가속\n"
            + "<color=#9F8CF2>TempoDown</color> - 0, 0.75s 2타, 템포 감속\n\n"
            + "가속 중에도 <b>적 이동, 스폰 속도는 변하지 않습니다</b>. "
            + "평타 타워 사격만 박자에 맞춰 빨라집니다.",
            TutorialAdvanceMode.Manual),

        new(
            "일시정지, 설정",
            "게임 중 <b>ESC</b> - Pause.\n"
            + "- 적, 타워, 타이머는 멈추지만 <b>BeatClock, 메트로놈, Rail</b>은 계속 흐릅니다.\n"
            + "- Rhythm 볼륨은 40%로 줄어듭니다.\n"
            + "Pause에서 재시작, 설정, 타이틀 복귀가 가능합니다.",
            TutorialAdvanceMode.Manual),

        new(
            "튜토리얼 완료",
            "이제 본편 <b>GameScene</b>에서 2분 생존에 도전하세요!\n"
            + "PERFECT / GOOD / MISS가 점수와 피버에 반영됩니다.\n\n"
            + "「게임 시작」으로 바로 플레이할 수 있습니다.",
            TutorialAdvanceMode.Manual),
    };

    [SerializeField] TutorialUI ui;

    RhythmCommandDetector _detector;
    RhythmPatternSelector _selector;
    TowerPlacer _placer;
    int _stepIndex;
    bool _waitingAdvance;
    CommandType _patternAtStepStart;
    Coroutine _autoAdvanceRoutine;

    void Awake()
    {
        ui ??= GetComponentInChildren<TutorialUI>(true);
        ui?.ResolveRefs();
    }

    void Start()
    {
        _detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        _selector = RhythmPatternSelector.Instance ?? FindAnyObjectByType<RhythmPatternSelector>();
        _placer = TowerPlacer.Instance ?? FindAnyObjectByType<TowerPlacer>();

        EnsurePlacementSetup();

        SkillCooldownController.Instance?.SetCooldownsDisabled(true);

        if (ui != null)
        {
            ui.OnNextClicked += HandleNext;
            ui.OnSkipClicked += CompleteAndExit;
        }

        SubscribeEvents();
        ShowCurrentStep();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();

        if (ui != null)
        {
            ui.OnNextClicked -= HandleNext;
            ui.OnSkipClicked -= CompleteAndExit;
        }
    }

    void SubscribeEvents()
    {
        if (_detector != null)
        {
            _detector.OnTapTimingFeedback += HandleTapFeedback;
            _detector.OnCommandResolved += HandleCommandResolved;
        }

        if (_selector != null)
            _selector.OnSelectionChanged += HandleSelectionChanged;
    }

    void UnsubscribeEvents()
    {
        if (_detector != null)
        {
            _detector.OnTapTimingFeedback -= HandleTapFeedback;
            _detector.OnCommandResolved -= HandleCommandResolved;
        }

        if (_selector != null)
            _selector.OnSelectionChanged -= HandleSelectionChanged;
    }

    void ShowCurrentStep()
    {
        if (_stepIndex >= Steps.Length)
        {
            CompleteAndExit();
            return;
        }

        _waitingAdvance = false;
        var step = Steps[_stepIndex];
        _patternAtStepStart = _selector != null ? _selector.Selected : CommandType.GoldPulse;

        if (step.HighlightPattern != CommandType.None && _selector != null)
            _selector.SetSelected(step.HighlightPattern);

        bool showNext = step.AdvanceMode == TutorialAdvanceMode.Manual
            || _stepIndex == Steps.Length - 1;

        var placementSetup = TutorialPlacementSetup.Instance
            ?? FindAnyObjectByType<TutorialPlacementSetup>();
        placementSetup?.SetVisible(step.AdvanceMode == TutorialAdvanceMode.WaitTowerPlaced);

        ui?.ShowStep(step, _stepIndex, Steps.Length, showNext);
        ui?.SetNextInteractable(showNext);
    }

    void Update()
    {
        TryAdvanceOnTowerPlaced();
    }

    void TryAdvanceOnTowerPlaced()
    {
        if (_waitingAdvance || _stepIndex >= Steps.Length)
            return;

        if (Steps[_stepIndex].AdvanceMode != TutorialAdvanceMode.WaitTowerPlaced)
            return;

        _placer ??= TowerPlacer.Instance ?? FindAnyObjectByType<TowerPlacer>();
        if (_placer != null && _placer.TowerCount > 0)
            ScheduleAutoAdvance(0.6f);
    }

    void EnsurePlacementSetup()
    {
        var setup = TutorialPlacementSetup.Instance
            ?? FindAnyObjectByType<TutorialPlacementSetup>();

        setup?.ResolveRefs();
    }

    void HandleNext()
    {
        if (_stepIndex >= Steps.Length - 1)
        {
            CompleteAndStartGame();
            return;
        }

        AdvanceStep();
    }

    void HandleTapFeedback(float _, TapTimingQuality quality)
    {
        if (_waitingAdvance || _stepIndex >= Steps.Length)
            return;

        if (Steps[_stepIndex].AdvanceMode != TutorialAdvanceMode.WaitFirstGoodTap)
            return;

        if (quality is TapTimingQuality.Perfect or TapTimingQuality.Good)
            ScheduleAutoAdvance(0.6f);
    }

    void HandleCommandResolved(CommandType type, JudgmentResult judgment)
    {
        if (_waitingAdvance || _stepIndex >= Steps.Length)
            return;

        if (judgment is not (JudgmentResult.Perfect or JudgmentResult.Good))
            return;

        var mode = Steps[_stepIndex].AdvanceMode;
        bool match = mode switch
        {
            TutorialAdvanceMode.WaitGoldPulseSuccess => type == CommandType.GoldPulse,
            TutorialAdvanceMode.WaitRhythmShotSuccess => type == CommandType.RhythmShot,
            TutorialAdvanceMode.WaitOverloadSuccess => type == CommandType.OverloadStrike,
            _ => false
        };

        if (match)
            ScheduleAutoAdvance(0.8f);
    }

    void HandleSelectionChanged(CommandType type)
    {
        if (_waitingAdvance || _stepIndex >= Steps.Length)
            return;

        if (Steps[_stepIndex].AdvanceMode != TutorialAdvanceMode.WaitPatternChange)
            return;

        if (type != _patternAtStepStart)
            ScheduleAutoAdvance(0.5f);
    }

    void ScheduleAutoAdvance(float delay)
    {
        if (_waitingAdvance)
            return;

        _waitingAdvance = true;
        ui?.SetNextInteractable(true);

        if (_autoAdvanceRoutine != null)
            StopCoroutine(_autoAdvanceRoutine);

        _autoAdvanceRoutine = StartCoroutine(AutoAdvanceAfter(delay));
    }

    IEnumerator AutoAdvanceAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _autoAdvanceRoutine = null;

        if (_stepIndex >= Steps.Length - 1)
        {
            CompleteAndStartGame();
            yield break;
        }

        AdvanceStep();
    }

    void AdvanceStep()
    {
        _waitingAdvance = false;
        _stepIndex++;
        ShowCurrentStep();
    }

    void CompleteAndExit()
    {
        TutorialProgress.MarkComplete();
        SceneManager.LoadScene(SceneNames.Start);
    }

    void CompleteAndStartGame()
    {
        TutorialProgress.MarkComplete();
        SceneManager.LoadScene(GameSettings.ActiveGameSceneName);
    }
}
