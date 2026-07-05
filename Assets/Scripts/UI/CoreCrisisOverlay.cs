using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Core HP 1일 때 화면 붉은 비네트 + 박자 심장박 흔들림.
/// </summary>
public class CoreCrisisOverlay : MonoBehaviour
{
    public static CoreCrisisOverlay Instance { get; private set; }

    [SerializeField] CanvasGroup vignette;
    [SerializeField] Image vignetteImage;

    BaseHealth _core;
    bool _crisisActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnGameScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != SceneNames.Game)
            return;

        if (FindAnyObjectByType<CoreCrisisOverlay>(FindObjectsInactive.Include) != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var go = new GameObject("CoreCrisisOverlay");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();
        go.AddComponent<CoreCrisisOverlay>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildVignette();
        SetCrisisActive(false);
    }

    void Start()
    {
        _core = BaseHealth.Instance ?? FindAnyObjectByType<BaseHealth>();
        if (_core != null)
        {
            _core.OnHpChanged += HandleHpChanged;
            HandleHpChanged(_core.CurrentHp, _core.MaxHp);
        }

        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat += OnBeat;
    }

    void OnDestroy()
    {
        if (_core != null)
            _core.OnHpChanged -= HandleHpChanged;

        if (BeatClock.Instance != null)
            BeatClock.Instance.OnBeat -= OnBeat;

        if (Instance == this)
            Instance = null;
    }

    void BuildVignette()
    {
        if (vignette != null)
            return;

        var rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        vignette = gameObject.AddComponent<CanvasGroup>();
        vignette.interactable = false;
        vignette.blocksRaycasts = false;

        var imageGo = new GameObject("Vignette");
        imageGo.transform.SetParent(transform, false);
        var imageRt = imageGo.AddComponent<RectTransform>();
        imageRt.anchorMin = Vector2.zero;
        imageRt.anchorMax = Vector2.one;
        imageRt.offsetMin = Vector2.zero;
        imageRt.offsetMax = Vector2.zero;

        vignetteImage = imageGo.AddComponent<Image>();
        vignetteImage.color = new Color(0.55f, 0.02f, 0.02f, 0.28f);
        vignetteImage.raycastTarget = false;
    }

    void HandleHpChanged(int current, int max)
    {
        SetCrisisActive(current == 1 && max > 1);
    }

    void SetCrisisActive(bool active)
    {
        _crisisActive = active;
        if (vignette != null)
            vignette.gameObject.SetActive(active);
    }

    void Update()
    {
        if (!_crisisActive || vignette == null || vignetteImage == null)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f);
        float alpha = Mathf.Lerp(0.14f, 0.34f, pulse);
        var color = vignetteImage.color;
        color.a = alpha;
        vignetteImage.color = color;
    }

    void OnBeat()
    {
        if (!_crisisActive)
            return;

        ScreenShake.Instance?.Shake(0.06f, 0.14f);
    }
}
