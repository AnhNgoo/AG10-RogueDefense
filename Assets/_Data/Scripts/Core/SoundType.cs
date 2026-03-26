/// <summary>
/// Enum định nghĩa tất cả các loại âm thanh.
/// </summary>
public enum SoundType
{
    None = 0,

    // === MUSIC ===
    MenuMusic,
    GameplayMusic,

    // === SFX UI ===
    ButtonClick,
    ButtonHover,
    MenuOpen,
    MenuClose,

    // === SFX GAMEPLAY ===
    TowerPlace,
    TowerUpgrade,
    TowerShoot,
    EnemyHit,
    EnemyDeath,
    WaveStart,
    WaveComplete,
    BaseHit,
    Coin,
    Victory,
    Lose
}
