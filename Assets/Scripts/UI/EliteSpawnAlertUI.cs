using TMPro;
using UnityEngine;

/// <summary>
/// 엘리트 웨이브 경고 배너 (2.5s). 씬에 없으면 Canvas에 자동 생성.
/// </summary>
public class EliteSpawnAlertUI : MonoBehaviour, IRuntimeSceneUi
{
    public static EliteSpawnAlertUI Instance { get; private set; }

    [SerializeField] TextMeshProUGUI alertText;
    [SerializeField] float displaySeconds = 2.5f;

    float _hideAt = -1f;

    public void EnsureSceneHierarchy()
    {
        EnsureAlertText();
        if (alertText != null)
            alertText.gameObject.SetActive(false);
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
        EliteSpawnDirector.OnWaveStarted += HandleWaveStarted;
    }

    void OnDestroy()
    {
        EliteSpawnDirector.OnWaveStarted -= HandleWaveStarted;
        if (Instance == this)
            Instance = null;
    }

    void EnsureAlertText()
    {
        if (alertText != null)
            return;

        var go = new GameObject("EliteAlert");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -96f);
        rt.sizeDelta = new Vector2(420f, 72f);

        alertText = go.AddComponent<TextMeshProUGUI>();
        alertText.fontSize = 28f;
        alertText.alignment = TextAlignmentOptions.Center;
        alertText.richText = true;
        alertText.raycastTarget = false;
    }

    void Update()
    {
        if (alertText == null || !alertText.gameObject.activeSelf)
            return;

        if (Time.unscaledTime >= _hideAt)
            alertText.gameObject.SetActive(false);
    }

    void HandleWaveStarted(EliteTier tier)
    {
        EnsureAlertText();
        if (alertText == null)
            return;

        alertText.text = tier switch
        {
            EliteTier.Wave90 => "<color=#FF5252><b>!! ELITE 90s !!</b></color>\n<size=85%>강박마다 HP +3</size>",
            _ => "<color=#FFB74D><b>!! ELITE 60s !!</b></color>\n<size=85%>받는 피해 25% 감소</size>"
        };

        alertText.gameObject.SetActive(true);
        _hideAt = Time.unscaledTime + displaySeconds;
    }
}
