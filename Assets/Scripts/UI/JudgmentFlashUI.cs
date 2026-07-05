using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// PERFECT / GOOD / MISS / COOLDOWN — 화면 중앙 팝업. FlashText는 하이어라키 자식으로 배치.
/// </summary>
public class JudgmentFlashUI : MonoBehaviour
{
    public static JudgmentFlashUI Instance { get; private set; }

    [SerializeField] TextMeshProUGUI flashText;
    [SerializeField] float displaySeconds = 1.1f;

    Coroutine _hideRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (flashText == null)
            flashText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (flashText != null)
            flashText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    void OnEnable() => Subscribe();
    void Start() => Subscribe();
    void OnDisable() => Unsubscribe();

    void Subscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
        {
            detector.OnCommandResolved -= OnCommandResolved;
            detector.OnCommandResolved += OnCommandResolved;
        }

        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
        {
            fever.OnFeverActivated -= OnFeverActivated;
            fever.OnFeverActivated += OnFeverActivated;
        }
    }

    void Unsubscribe()
    {
        var detector = RhythmCommandDetector.Instance ?? FindAnyObjectByType<RhythmCommandDetector>();
        if (detector != null)
            detector.OnCommandResolved -= OnCommandResolved;

        var fever = FeverTimeController.Instance ?? FindAnyObjectByType<FeverTimeController>();
        if (fever != null)
            fever.OnFeverActivated -= OnFeverActivated;
    }

    void OnFeverActivated()
    {
        if (flashText == null)
            return;

        flashText.text = "<color=#FFB74D><size=120%>FEVER TIME!</size></color>\n<size=55%>DMG x1.5 | 6s</size>";
        flashText.gameObject.SetActive(true);

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideAfter(displaySeconds));
    }

    void OnCommandResolved(CommandType type, JudgmentResult judgment)
    {
        if (flashText == null)
            return;

        string command = type == CommandType.None ? "" : $"\n<size=60%>{type}</size>";
        flashText.text = Format(judgment) + command;
        flashText.gameObject.SetActive(true);

        if (judgment == JudgmentResult.Perfect || judgment == JudgmentResult.Good)
            SimpleAudio.Instance?.PlayJudgment(judgment);

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideAfter(displaySeconds));
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (flashText != null)
            flashText.gameObject.SetActive(false);
        _hideRoutine = null;
    }

    static string Format(JudgmentResult result)
    {
        return result switch
        {
            JudgmentResult.Perfect => "<color=#FFD54F>PERFECT</color>",
            JudgmentResult.Good => "<color=#A5D6A7>GOOD</color>",
            JudgmentResult.Cooldown => "<color=#EF5350>COOLDOWN</color>",
            JudgmentResult.NoTower => "<color=#EF5350>NO TOWER</color>\n<size=50%>타워가 배치되지 않았습니다</size>",
            _ => "<color=#EF5350>MISS</color>"
        };
    }
}
