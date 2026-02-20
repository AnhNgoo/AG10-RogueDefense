/// <summary>
/// Enum định nghĩa tất cả các loại Pool.
/// </summary>
public enum PoolType
{
    None = 0,

    // === ENEMIES ===
    EnemyBasic = 100,
    EnemyFast = 101,
    EnemySlow = 102,
    EnemyBoss = 103,

    // === TOWERS ===
    TowerFire = 150,    // Nấm Đỏ - Tấn công nhanh, gây DOT
    TowerWater = 151,   // Thông Máy - Làm chậm địch
    TowerEarth = 152,   // Pháo Đài - Sát thương AoE, phòng thủ
    TowerWind = 153,    // Cối Tròn - Multi-target, tốc độ cao

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
