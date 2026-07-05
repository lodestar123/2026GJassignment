using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause에서 PracticeScene Additive 로드/언로드.
/// </summary>
public class PracticeSceneLoader : MonoBehaviour
{
    public static PracticeSceneLoader Instance { get; private set; }

    bool _loading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadPracticeFromPause()
    {
        if (_loading || PauseController.Instance == null)
            return;

        SettingsPanelUI.Resolve()?.HidePanelOnly();
        StartCoroutine(LoadRoutine());
    }

    IEnumerator LoadRoutine()
    {
        _loading = true;

        if (SceneManager.GetSceneByName(SceneNames.Practice).isLoaded)
        {
            _loading = false;
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(SceneNames.Practice, LoadSceneMode.Additive);
        while (op != null && !op.isDone)
            yield return null;

        PauseController.Instance.SetPracticeOverlayActive(true);
        _loading = false;
    }

    public void UnloadPracticeOverlay()
    {
        if (_loading)
            return;

        if (!SceneManager.GetSceneByName(SceneNames.Practice).isLoaded)
            return;

        SceneManager.UnloadSceneAsync(SceneNames.Practice);
        PauseController.Instance?.SetPracticeOverlayActive(false);
        PauseMenuUI.Instance?.Show();
    }
}
