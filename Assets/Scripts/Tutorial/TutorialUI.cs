using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>튜토리얼 안내 패널 — Hierarchy에 bake된 TutorialUI/Panel 참조.</summary>
public class TutorialUI : MonoBehaviour
{
    [SerializeField] CanvasGroup panelGroup;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] TextMeshProUGUI progressText;
    [SerializeField] Button nextButton;
    [SerializeField] Button skipButton;

    public event Action OnNextClicked;
    public event Action OnSkipClicked;

    void Awake()
    {
        ResolveRefs();

        if (nextButton != null)
            nextButton.onClick.AddListener(() => OnNextClicked?.Invoke());

        if (skipButton != null)
            skipButton.onClick.AddListener(() => OnSkipClicked?.Invoke());
    }

    public void ResolveRefs()
    {
        var panel = transform.Find("Panel");
        if (panel == null)
            return;

        panelGroup ??= panel.GetComponent<CanvasGroup>();
        progressText ??= panel.Find("Progress")?.GetComponent<TextMeshProUGUI>();
        titleText ??= panel.Find("Title")?.GetComponent<TextMeshProUGUI>();
        bodyText ??= panel.Find("Body")?.GetComponent<TextMeshProUGUI>();
        nextButton ??= panel.Find("Btn_Next")?.GetComponent<Button>();
        skipButton ??= panel.Find("Btn_Skip")?.GetComponent<Button>();
    }

    public void ShowStep(TutorialStep step, int index, int total, bool showNext)
    {
        if (titleText != null)
            titleText.text = step.Title;

        if (bodyText != null)
            bodyText.text = step.Body;

        if (progressText != null)
            progressText.text = $"{index + 1} / {total}";

        if (nextButton != null)
            nextButton.gameObject.SetActive(showNext);

        if (panelGroup != null)
            panelGroup.alpha = 1f;
    }

    public void SetNextInteractable(bool interactable)
    {
        if (nextButton != null)
            nextButton.interactable = interactable;
    }

    public void Hide()
    {
        if (panelGroup != null)
            panelGroup.alpha = 0f;
    }
}
