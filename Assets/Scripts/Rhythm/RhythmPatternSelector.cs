using System;
using UnityEngine;

/// <summary>
/// 플레이어가 Scroll UI에서 선택한 리듬 커맨드.
/// 미선택(기본) = GoldPulse.
/// </summary>
public class RhythmPatternSelector : MonoBehaviour
{
    public static RhythmPatternSelector Instance { get; private set; }

    [SerializeField] CommandType selected = CommandType.GoldPulse;

    public CommandType Selected => selected;
    public event Action<CommandType> OnSelectionChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (selected == CommandType.None)
            selected = CommandType.GoldPulse;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        HandleMouseWheelSwap();
    }

    void HandleMouseWheelSwap()
    {
        if (!CanSwapWithWheel())
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        // 휠 위 = 이전 패턴, 휠 아래 = 다음 패턴
        CycleSelection(scroll > 0f ? -1 : 1);
    }

    static bool CanSwapWithWheel()
    {
        if (Time.timeScale <= 0f)
            return false;

        if (GameManager.Instance != null && !GameManager.Instance.IsRunning)
            return false;

        if (PauseController.Instance != null && PauseController.Instance.IsPaused)
            return false;

        if (ResultScreenUI.Instance != null && ResultScreenUI.Instance.IsVisible)
            return false;

        return true;
    }

    public void SetSelected(CommandType type)
    {
        if (type == CommandType.None)
            type = CommandType.GoldPulse;

        if (selected == type)
            return;

        selected = type;
        OnSelectionChanged?.Invoke(selected);
    }

    /// <summary>휠 스왑 — direction +1 다음, -1 이전.</summary>
    public void CycleSelection(int direction)
    {
        if (direction == 0)
            return;

        var order = RhythmPatternLibrary.SelectableOrder;
        if (order == null || order.Length == 0)
            return;

        int index = 0;
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] != selected)
                continue;

            index = i;
            break;
        }

        int next = (index + direction) % order.Length;
        if (next < 0)
            next += order.Length;

        SetSelected(order[next]);
    }
}
