/// <summary>
/// Enum định nghĩa các loại âm thanh trong game
/// </summary>
public enum SoundType
{
    None = 0,
    
    // === MUSIC ===
    MenuMusic,
    GameplayMusic,
    VictoryMusic,
    DefeatMusic,
    
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
    GameOver
}
