using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// StartScene — 게임 시작 · 튜토리얼 · 설정 · 종료.
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [SerializeField] Button startGameButton;
    [SerializeField] Button tutorialButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;
    [SerializeField] TextMeshProUGUI titleText;

    void Awake()
    {
        Time.timeScale = 1f;

        tutorialButton ??= transform.Find("Btn_Tutorial")?.GetComponent<Button>()
            ?? transform.Find("Btn_Practice")?.GetComponent<Button>();
        settingsButton ??= transform.Find("Btn_Settings")?.GetComponent<Button>();

        if (startGameButton != null)
            startGameButton.onClick.AddListener(() => SceneManager.LoadScene(GameSettings.ActiveGameSceneName));

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Tutorial));

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => SettingsPanelUI.Resolve()?.Show());

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
