using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerRegistry : MonoBehaviour
{
    public static TowerRegistry Instance { get; private set; }

    public IReadOnlyList<BeatTower> BeatTowers => _beatTowers;
    public IReadOnlyList<Tower> StrikeTowers => _strikeTowers;
    public IReadOnlyList<Tower> BoostTowers => _boostTowers;

    readonly List<BeatTower> _beatTowers = new();
    readonly List<Tower> _strikeTowers = new();
    readonly List<Tower> _boostTowers = new();

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
        _strikeTowers.Clear();
        _boostTowers.Clear();

        foreach (var beat in FindObjectsByType<BeatTower>(FindObjectsSortMode.None))
            _beatTowers.Add(beat);

        foreach (var tower in FindObjectsByType<Tower>(FindObjectsSortMode.None))
        {
            switch (tower.towerType)
            {
                case TowerType.Strike:
                    _strikeTowers.Add(tower);
                    break;
                case TowerType.Boost:
                    _boostTowers.Add(tower);
                    break;
            }
        }
    }

    public void RegisterTower(Tower tower)
    {
        if (tower == null)
            return;

        switch (tower.towerType)
        {
            case TowerType.Beat:
                var beat = tower.GetComponent<BeatTower>();
                if (beat != null && !_beatTowers.Contains(beat))
                    _beatTowers.Add(beat);
                break;
            case TowerType.Strike:
                if (!_strikeTowers.Contains(tower))
                    _strikeTowers.Add(tower);
                break;
            case TowerType.Boost:
                if (!_boostTowers.Contains(tower))
                    _boostTowers.Add(tower);
                break;
        }
    }

    public void UnregisterTower(Tower tower)
    {
        if (tower == null)
            return;

        var beat = tower.GetComponent<BeatTower>();
        if (beat != null)
            _beatTowers.Remove(beat);

        _strikeTowers.Remove(tower);
        _boostTowers.Remove(tower);
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
