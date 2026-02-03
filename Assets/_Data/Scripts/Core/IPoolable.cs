/// <summary>
/// Interface cho các object có thể được pooling.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Loại pool mà object này thuộc về.
    /// </summary>
    PoolType PoolType { get; }

    /// <summary>
    /// Được gọi khi object được spawn từ pool.
    /// </summary>
    void OnSpawnFromPool();

    /// <summary>
    /// Được gọi khi object được trả về pool.
    /// </summary>
    void OnReturnToPool();
}
