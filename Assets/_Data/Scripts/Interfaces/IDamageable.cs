using UnityEngine;

/// <summary>
/// Interface cho các đối tượng có thể nhận sát thương.
/// SOLID: Interface Segregation Principle - Tách riêng logic nhận sát thương.
/// Được implement bởi: Enemy, Boss, Destructible Objects.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Nhận sát thương từ nguồn bên ngoài (Tháp, Spell, Trap, etc.).
    /// </summary>
    /// <param name="amount">Lượng sát thương</param>
    void TakeDamage(float amount);

    /// <summary>
    /// Kiểm tra đối tượng đã chết chưa.
    /// Dùng để Targeting System bỏ qua target đã chết.
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// Vị trí hiện tại của đối tượng (để tính khoảng cách, bắn đạn).
    /// Dùng để Tower biết vị trí mục tiêu.
    /// </summary>
    Vector3 Position { get; }
}
