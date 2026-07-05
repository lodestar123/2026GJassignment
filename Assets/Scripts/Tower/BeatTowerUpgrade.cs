/// <summary>
/// BeatTower Lv1~3 — BALANCE 연동용 정적 표.
/// </summary>
public static class BeatTowerUpgrade
{
    public const int MaxLevel = 3;

    public static float GetActiveDamage(int level) => level switch
    {
        2 => 2.8f,
        3 => 3.6f,
        _ => 2f
    };

    public static float GetFallbackDamage(int level) => level switch
    {
        2 => 0.55f,
        3 => 0.75f,
        _ => 0.35f
    };

    public static int GetUpgradeCost(int currentLevel) => currentLevel switch
    {
        1 => 15,
        2 => 25,
        _ => 0
    };

    public static bool CanUpgrade(int level) => level >= 1 && level < MaxLevel;
}
