using UnityEngine;

/// <summary>
/// 타워 호버 → TowerSellUI.
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerClickTarget : MonoBehaviour
{
    Tower _tower;

    void Awake() => _tower = GetComponent<Tower>();

    void OnMouseEnter()
    {
        if (!CanInteract())
            return;

        TowerSellUI.Resolve()?.Show(_tower);
    }

    bool CanInteract()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return false;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return false;

        return true;
    }
}
