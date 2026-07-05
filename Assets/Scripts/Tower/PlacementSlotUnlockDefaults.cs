/// <summary>
/// 맵별 기본 슬롯 해금 — PlacementGrid.slotUnlockRules 에 복사·참고용.
/// Slot_N 의 N = slotIndex (MapLayout.PlacementSlots 순서).
/// </summary>
public static class PlacementSlotUnlockDefaults
{
    public const int DefaultLockedCost = 20;

    /// <summary>
    /// Classic 14칸 — 경로 인접 8칸 시작 해금.
    /// 규칙에 없는 슬롯은 잠금 + DefaultLockedCost.
    /// </summary>
    public static readonly PlacementSlotUnlockRule[] ClassicStarter = new[]
    {
        Rule(3), Rule(4), Rule(6), Rule(7), Rule(8), Rule(9), Rule(12), Rule(13),
    };

    static PlacementSlotUnlockRule Rule(int index) => new()
    {
        slotIndex = index,
        unlockedAtStart = true,
        unlockGoldCost = 0,
    };
}
