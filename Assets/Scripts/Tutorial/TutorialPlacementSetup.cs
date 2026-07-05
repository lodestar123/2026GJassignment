using UnityEngine;

/// <summary>
/// TutorialScene — bake된 TutorialPlacement / TowerTypeSelect 참조.
/// </summary>
[DefaultExecutionOrder(-110)]
public class TutorialPlacementSetup : MonoBehaviour
{
    public static TutorialPlacementSetup Instance { get; private set; }

    [SerializeField] GameObject worldRoot;
    [SerializeField] GameObject towerSelectRoot;
    [SerializeField] Transform towersRoot;
    [SerializeField] int tutorialStartingGold = 50;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveRefs();
        EnsureTowerPlacer();
        EnsureGold();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResolveRefs()
    {
        worldRoot ??= GameObject.Find("TutorialPlacement");
        towerSelectRoot ??= GameObject.Find("TowerTypeSelect");
        towersRoot ??= worldRoot != null
            ? worldRoot.transform.Find("Towers")
            : null;
    }

    public void SetVisible(bool visible)
    {
        if (worldRoot != null)
            worldRoot.SetActive(visible);

        if (towerSelectRoot != null)
            towerSelectRoot.SetActive(visible);

        if (visible)
        {
            EnsureTowerPlacer();
            TowerPlacer.Instance?.EnsureGridBound();
        }
    }

    void EnsureGold()
    {
        var resources = ResourceManager.Instance ?? FindAnyObjectByType<ResourceManager>();
        if (resources == null)
            return;

        if (resources.Gold < tutorialStartingGold)
            resources.AddGold(tutorialStartingGold - resources.Gold);
    }

    void EnsureTowerPlacer()
    {
        var placer = TowerPlacer.Instance
            ?? FindAnyObjectByType<TowerPlacer>()
            ?? GameObject.Find("--- Systems ---")?.AddComponent<TowerPlacer>();

        if (placer != null && towersRoot != null)
            placer.SetTowerRoot(towersRoot);
    }
}
