using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TutorialScene — StartScene에서 단독 로드. PracticeScene 대체.
/// </summary>
public class TutorialSceneController : MonoBehaviour
{
    [SerializeField] GameObject standaloneRoot;
    [SerializeField] Button exitButton;
    [SerializeField] TextMeshProUGUI legacyInfoText;

    void Awake()
    {
        MigrateFromLegacyPractice();

        if (standaloneRoot != null)
            standaloneRoot.SetActive(true);

        SkillCooldownController.Instance?.SetCooldownsDisabled(true);

        if (legacyInfoText != null)
            legacyInfoText.gameObject.SetActive(false);

        if (GetComponent<TutorialController>() == null)
            gameObject.AddComponent<TutorialController>();

        if (exitButton != null)
            exitButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Start));
    }

    static void MigrateFromLegacyPractice()
    {
        var flow = GameObject.Find("TutorialFlow") ?? GameObject.Find("PracticeFlow");
        if (flow != null)
        {
            flow.name = "TutorialFlow";
            StripLegacyFlowComponents(flow);
        }

        var overlay = GameObject.Find("OverlayRoot");
        if (overlay != null)
            overlay.SetActive(false);
    }

    static void StripLegacyFlowComponents(GameObject flow)
    {
        foreach (var mb in flow.GetComponents<MonoBehaviour>())
        {
            if (mb is TutorialSceneController or TutorialController or TutorialPlacementSetup)
                continue;

            Destroy(mb);
        }
    }
}
