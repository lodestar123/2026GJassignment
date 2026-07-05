using System;
using UnityEngine;

/// <summary>
/// 슬롯 1칸 해금 규칙 — slotIndex는 PlacementGrid의 Slot_N 과 동일.
/// </summary>
[Serializable]
public struct PlacementSlotUnlockRule
{
    [Tooltip("Slot_N 의 N (0부터)")]
    public int slotIndex;

    public bool unlockedAtStart;

    [Min(0)]
    public int unlockGoldCost;
}
