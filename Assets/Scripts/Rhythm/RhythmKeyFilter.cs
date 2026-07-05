using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리듬 입력으로 쓸 수 있는 키 판별. ESC·Tab 등 예약 키 제외.
/// PauseController 등 다른 시스템은 <see cref="RegisterReservedKey"/>로 추가 등록.
/// </summary>
public static class RhythmKeyFilter
{
    static readonly HashSet<KeyCode> ReservedKeys = new()
    {
        KeyCode.Escape,
        KeyCode.Tab,
    };

    static readonly KeyCode[] PollOrder = BuildPollOrder();

    public static IReadOnlyCollection<KeyCode> Reserved => ReservedKeys;

    public static void RegisterReservedKey(KeyCode key)
    {
        if (key != KeyCode.None)
            ReservedKeys.Add(key);
    }

    public static void UnregisterReservedKey(KeyCode key)
    {
        ReservedKeys.Remove(key);
    }

    public static bool IsRhythmKey(KeyCode key)
    {
        if (key == KeyCode.None || ReservedKeys.Contains(key))
            return false;

        if (IsModifierKey(key) || IsMouseKey(key) || IsJoystickKey(key))
            return false;

        return true;
    }

    /// <summary>이번 프레임 리듬 입력 키가 눌렸으면 true (프레임당 1회).</summary>
    public static bool TryGetRhythmKeyDown(out KeyCode key)
    {
        key = KeyCode.None;

        if (!Input.anyKeyDown)
            return false;

        foreach (KeyCode k in PollOrder)
        {
            if (!IsRhythmKey(k) || !Input.GetKeyDown(k))
                continue;

            key = k;
            return true;
        }

        return false;
    }

    /// <summary>이번 프레임에 눌린 모든 리듬 키를 수집. 반환값은 개수.</summary>
    public static int TryGetRhythmKeysDown(List<KeyCode> buffer)
    {
        buffer?.Clear();

        if (!Input.anyKeyDown)
            return 0;

        int count = 0;
        foreach (KeyCode k in PollOrder)
        {
            if (!IsRhythmKey(k) || !Input.GetKeyDown(k))
                continue;

            buffer?.Add(k);
            count++;
        }

        return count;
    }

    static void MergeAdditionalReservedFromSettings()
    {
        if (RhythmInputSettings.Instance == null)
            return;

        foreach (KeyCode k in RhythmInputSettings.Instance.AdditionalReservedKeys)
            ReservedKeys.Add(k);
    }

    static bool IsModifierKey(KeyCode key)
    {
        return key is KeyCode.LeftShift or KeyCode.RightShift
            or KeyCode.LeftControl or KeyCode.RightControl
            or KeyCode.LeftAlt or KeyCode.RightAlt
            or KeyCode.LeftCommand or KeyCode.RightCommand
            or KeyCode.LeftWindows or KeyCode.RightWindows
            or KeyCode.LeftMeta or KeyCode.RightMeta;
    }

    static bool IsMouseKey(KeyCode key) => key is >= KeyCode.Mouse0 and <= KeyCode.Mouse6;

    static bool IsJoystickKey(KeyCode key)
    {
        return key.ToString().StartsWith("Joystick", StringComparison.Ordinal);
    }

    static KeyCode[] BuildPollOrder()
    {
        var list = new List<KeyCode>((int)KeyCode.Menu);
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if ((int)k <= (int)KeyCode.None)
                continue;

            list.Add(k);
        }

        return list.ToArray();
    }
}
