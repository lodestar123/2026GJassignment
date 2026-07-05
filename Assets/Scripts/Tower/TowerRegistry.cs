using System.Collections.Generic;
using UnityEngine;

public class TowerRegistry : MonoBehaviour
{
    public static TowerRegistry Instance { get; private set; }

    public IReadOnlyList<BeatTower> BeatTowers => _beatTowers;
    public bool HasAnyTower => _beatTowers.Count > 0;

    readonly List<BeatTower> _beatTowers = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Refresh()
    {
        _beatTowers.Clear();

        foreach (var beat in FindObjectsByType<BeatTower>(FindObjectsSortMode.None))
            _beatTowers.Add(beat);
    }

    public void RegisterTower(Tower tower)
    {
        if (tower == null)
            return;

        var beat = tower.GetComponent<BeatTower>();
        if (beat != null)
            Register(beat);
    }

    public void UnregisterTower(Tower tower)
    {
        if (tower == null)
            return;

        var beat = tower.GetComponent<BeatTower>();
        if (beat != null)
            Unregister(beat);
    }

    public void Register(BeatTower beatTower)
    {
        if (beatTower != null && !_beatTowers.Contains(beatTower))
            _beatTowers.Add(beatTower);
    }

    public void Unregister(BeatTower beatTower)
    {
        _beatTowers.Remove(beatTower);
    }
}
