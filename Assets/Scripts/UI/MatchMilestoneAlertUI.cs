using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 매치 마일스톤 배너 (2.8s). GameScene Canvas에 자동 생성.
/// </summary>
public class MatchMilestoneAlertUI : MonoBehaviour
{
    public static MatchMilestoneAlertUI Instance { get; private set; }

    [SerializeField] TextMeshProUGUI alertText;
    [SerializeField] float displaySeconds = 2.8f;

    float _hideAt = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != SceneNames.Game)
            return;

        if (FindAnyObjectByType<MatchMilestoneAlertUI>(FindObjectsInactive.Include) != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var go = new GameObject("MatchMilestoneAlertUI");
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<MatchMilestoneAlertUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureAlertText();
        MatchMilestoneDirector.OnMilestoneReached += HandleMilestone;

        if (alertText != null)
            alertText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        MatchMilestoneDirector.OnMilestoneReached -= HandleMilestone;
        if (Instance == this)
            Instance = null;
    }

    void EnsureAlertText()
    {
        if (alertText != null)
            return;

        var go = new GameObject("MilestoneAlert");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -148f);
        rt.sizeDelta = new Vector2(480f, 80f);

        alertText = go.AddComponent<TextMeshProUGUI>();
        alertText.fontSize = 26f;
        alertText.alignment = TextAlignmentOptions.Center;
        alertText.richText = true;
        alertText.raycastTarget = false;
        if (BeatDefenderFonts.Pretendard != null)
            alertText.font = BeatDefenderFonts.Pretendard;
    }

    void Update()
    {
        if (alertText == null || !alertText.gameObject.activeSelf)
            return;

        if (Time.unscaledTime >= _hideAt)
            alertText.gameObject.SetActive(false);
    }

    void HandleMilestone(MatchMilestoneKind kind)
    {
        EnsureAlertText();
        if (alertText == null)
            return;

        alertText.text = kind switch
        {
            MatchMilestoneKind.PressureRising30 =>
                "<color=#81D4FA><b>압박 구간</b></color>\n<size=85%>적 스폰이 점점 빨라집니다</size>",
            MatchMilestoneKind.EliteWarning60 =>
                "<color=#FFB74D><b>!! ELITE 60s 임박 !!</b></color>\n<size=85%>3초 후 — 받는 피해 25% 감소</size>",
            MatchMilestoneKind.EliteWarning90 =>
                "<color=#FF5252><b>!! ELITE 90s 임박 !!</b></color>\n<size=85%>3초 후 — 강박마다 HP +3</size>",
            MatchMilestoneKind.LastStand100 =>
                "<color=#FF5252><b>LAST STAND</b></color>\n<size=85%>마지막 20초 — 스폰 가속!</size>",
            MatchMilestoneKind.FinalPush110 =>
                "<color=#FF1744><b>FINAL PUSH</b></color>\n<size=85%>최종 러시 — 버텨라!</size>",
            _ => ""
        };

        if (string.IsNullOrEmpty(alertText.text))
            return;

        alertText.gameObject.SetActive(true);
        _hideAt = Time.unscaledTime + displaySeconds;
        ScreenShake.Instance?.Shake(0.08f, 0.14f);
    }
}
