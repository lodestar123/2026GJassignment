using UnityEngine;
using UnityEngine.SceneManagement;

static class FeverTimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsureInScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryEnsureInScene(scene);
    }

    static void TryEnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var name = scene.name;
        if (name != SceneNames.Game && name != SceneNames.Tutorial)
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "--- Systems ---")
                continue;

            if (root.GetComponent<FeverTimeController>() == null)
                root.AddComponent<FeverTimeController>();
            if (root.GetComponent<TempoController>() == null)
                root.AddComponent<TempoController>();
            return;
        }
    }
}
