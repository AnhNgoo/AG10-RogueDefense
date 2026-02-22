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
    BulletNormal = 200,     // Đạn thường (generic)
    BulletPiercing = 201,   // Đạn xuyên giáp
    Arrow = 202,            // Mũi tên
    Rocket = 203,           // Tên lửa

    // === PROJECTILES (ELEMENTAL) ===
    BulletFire = 210,       // Đạn Lửa (Fire Tower)
    BulletWater = 211,      // Đạn Nước (Water Tower)
    BulletEarth = 212,      // Đạn Đất (Earth Tower)
    BulletWind = 213,       // Đạn Gió (Wind Tower)

    // === VFX ===
    VFX_Explosion = 300,    // Vụ nổ chung
    VFX_Hit = 301,          // Va chạm chung
    VFX_Muzzle = 302,       // Hiệu ứng nòng súng
    VFX_Death = 303,        // Chết chung

    // === VFX (ELEMENTAL HIT) ===
    VFX_HitFire = 310,      // Va chạm Lửa
    VFX_HitWater = 311,     // Va chạm Nước
    VFX_HitEarth = 312,     // Va chạm Đất
    VFX_HitWind = 313,      // Va chạm Gió

    // === UI ===
    DamagePopup = 400,
    FloatingText = 401,

    // === MISC ===
    Coin = 500,
    PowerUp = 501
}
