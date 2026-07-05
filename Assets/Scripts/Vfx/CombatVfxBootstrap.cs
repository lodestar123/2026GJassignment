using UnityEngine;
using UnityEngine.SceneManagement;

static class CombatVfxBootstrap
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
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneNames.Game)
            return;

        if (CombatVfxService.Instance != null)
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "--- Systems ---")
                continue;

            if (root.GetComponent<CombatVfxService>() == null)
                root.AddComponent<CombatVfxService>();
            return;
        }
    }
}
