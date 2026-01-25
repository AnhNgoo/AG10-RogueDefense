/// <summary>
/// ENUM: Các loại Pool trong game.
/// Mở rộng khi thêm loại object mới (Enemy, Bullet, VFX, etc.)
/// </summary>
public enum PoolType
{
    None = 0,

    // === ENEMIES ===
    EnemyBasic = 100,
    EnemyFast = 101,
    EnemySlow = 102,
    EnemyBoss = 103,

    // === PROJECTILES ===
    BulletNormal = 200,
    BulletPiercing = 201,
    Arrow = 202,
    Rocket = 203,

    // === VFX ===
    VFX_Explosion = 300,
    VFX_Hit = 301,
    VFX_Muzzle = 302,
    VFX_Death = 303,

    // === UI ===
    DamagePopup = 400,
    FloatingText = 401,

    // === MISC ===
    Coin = 500,
    PowerUp = 501
}
