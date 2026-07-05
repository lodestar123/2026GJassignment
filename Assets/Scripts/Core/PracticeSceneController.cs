using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PracticeScene — Standalone(Start) vs Additive(Pause) 분기.
/// </summary>
public class PracticeSceneController : MonoBehaviour
{
    [SerializeField] GameObject standaloneRoot;
    [SerializeField] GameObject overlayRoot;
    [SerializeField] Button standaloneExitButton;
    [SerializeField] Button overlayCloseButton;
    [SerializeField] TextMeshProUGUI infoText;

    bool _isOverlay;

    void Awake()
    {
        _isOverlay = IsLoadedAsOverlay();

        if (_isOverlay)
            SetupOverlayMode();
        else
            SetupStandaloneMode();
    }

    static bool IsLoadedAsOverlay()
    {
        if (SceneManager.sceneCount <= 1)
            return false;

        var game = SceneManager.GetSceneByName(SceneNames.Game);
        return game.IsValid() && game.isLoaded;
    }

    void SetupOverlayMode()
    {
        if (standaloneRoot != null)
            standaloneRoot.SetActive(false);

        EnsureOverlayCanvas();
        DisableLocalDuplicateSystems();

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        if (infoText != null)
            infoText.text = "박자 연습 (Pause 중)\nGameScene 리듬/Rail 사용, CD 없음";

        if (overlayCloseButton != null)
            overlayCloseButton.onClick.AddListener(CloseOverlay);
    }

    void EnsureOverlayCanvas()
    {
        if (overlayRoot == null || overlayRoot.GetComponentInParent<Canvas>() != null)
            return;

        var canvasGo = new GameObject("OverlayCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();
        SceneManager.MoveGameObjectToScene(canvasGo, gameObject.scene);
        overlayRoot.transform.SetParent(canvasGo.transform, false);
    }

    void SetupStandaloneMode()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
        if (standaloneRoot != null)
            standaloneRoot.SetActive(true);

        SkillCooldownController.Instance?.SetCooldownsDisabled(true);

        if (infoText != null)
            infoText.text = "4패턴 연습, CD 비활성, 적 없음";

        if (standaloneExitButton != null)
            standaloneExitButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Start));
    }

    void DisableLocalDuplicateSystems()
    {
        var scene = gameObject.scene;

        foreach (var root in scene.GetRootGameObjects())
        {
            switch (root.name)
            {
                case "Main Camera":
                case "EventSystem":
                case "--- Systems ---":
                case "--- UI ---":
                    root.SetActive(false);
                    break;
            }
        }

        if (BeatClock.Instance != null && BeatClock.Instance.gameObject.scene == scene)
            BeatClock.Instance.gameObject.SetActive(false);

        SkillCooldownController.Instance?.SetCooldownsDisabled(true);
    }

    void CloseOverlay()
    {
        PracticeSceneLoader.Instance?.UnloadPracticeOverlay();
    }
}
