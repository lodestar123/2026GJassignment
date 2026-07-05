/// <summary>
/// 적 스탯 — BALANCE §4 (BeatTower 2dmg × 3발 = 8분음표 처치).
/// </summary>
public static class EnemyBalance
{
    public const float EighthNoteHp = 6f;
    public const float EighthNoteSpeed = 1.15f;
    public const int EighthNoteCoreDamage = 1;
    public const int EighthNoteGold = 6;

    public const float DownbeatHp = 15f;
    public const float DownbeatSpeed = 0.55f;
    public const int DownbeatCoreDamage = 2;
    public const int DownbeatGold = 14;

    // Elite 60s — 장갑
    public const float EliteWave60Hp = 40f;
    public const float EliteWave60Speed = 0.5f;
    public const int EliteWave60CoreDamage = 3;
    public const int EliteWave60Gold = 30;
    public const float EliteWave60DamageTaken = 0.75f;

    // Elite 90s — 강박 회복
    public const float EliteWave90Hp = 55f;
    public const float EliteWave90Speed = 0.58f;
    public const int EliteWave90CoreDamage = 3;
    public const int EliteWave90Gold = 45;
    public const float EliteWave90RegenPerDownbeat = 3f;
}
