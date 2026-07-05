using System;

/// <summary>타워 배치 모드 — 통합 타워 1종.</summary>
public static class TowerSelection
{
    public static bool IsArmed { get; private set; }

    public static bool HasSelection => IsArmed;

    public static event Action OnChanged;

    public static void ToggleArm()
    {
        IsArmed = !IsArmed;
        OnChanged?.Invoke();
    }

    public static void Arm()
    {
        if (IsArmed)
            return;

        IsArmed = true;
        OnChanged?.Invoke();
    }

    public static void Disarm()
    {
        if (!IsArmed)
            return;

        IsArmed = false;
        OnChanged?.Invoke();
    }
}
