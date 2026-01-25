/// <summary>
/// INTERFACE: Các object muốn sử dụng Pooling phải implement interface này.
/// Đảm bảo object được reset đúng cách khi spawn/despawn.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Loại Pool mà object này thuộc về.
    /// Dùng để ObjectPoolManager biết trả về đúng pool.
    /// </summary>
    PoolType PoolType { get; }

    /// <summary>
    /// Gọi khi object được spawn từ Pool.
    /// Reset trạng thái về như mới (máu đầy, vận tốc = 0, etc.)
    /// </summary>
    void OnSpawnFromPool();

    /// <summary>
    /// Gọi khi object được trả về Pool.
    /// Cleanup resources (stop Coroutines, detach từ parent, etc.)
    /// </summary>
    void OnReturnToPool();
}
