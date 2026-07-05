using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// StartScene — 게임 시작 · 박자 연습 · 종료.
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [SerializeField] Button startGameButton;
    [SerializeField] Button practiceButton;
    [SerializeField] Button quitButton;
    [SerializeField] TextMeshProUGUI titleText;

    void Awake()
    {
        Time.timeScale = 1f;

        if (titleText != null)
            titleText.text = "Beat Defender";

        if (startGameButton != null)
            startGameButton.onClick.AddListener(() => SceneManager.LoadScene(GameSettings.ActiveGameSceneName));

        if (practiceButton != null)
            practiceButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Practice));

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
