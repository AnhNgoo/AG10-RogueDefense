using UnityEngine;

/// <summary>
/// Component đơn giản cho VFX để bypass validation IPoolable trong PoolConfig.
/// Tự động Play/Stop ParticleSystem khi spawn/return to pool.
/// </summary>
public class VFXPoolable : MonoBehaviour, IPoolable
{
    [SerializeField] private PoolType _poolType = PoolType.VFX_Death;

    /// <summary>
    /// PoolType property - Bắt buộc phải implement từ IPoolable.
    /// </summary>
    public PoolType PoolType => _poolType;

    /// <summary>
    /// Gọi khi lấy VFX ra khỏi Pool.
    /// Tự động Play tất cả ParticleSystem trong VFX.
    /// </summary>
    public void OnSpawnFromPool()
    {
        // Auto play particle nếu có
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Play();
        }
    }

    /// <summary>
    /// Gọi khi VFX được trả về Pool.
    /// Tự động Stop tất cả ParticleSystem.
    /// </summary>
    public void OnReturnToPool()
    {
        // Stop particle nếu có
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Stop();
            ps.Clear(); // Clear particles để reset state
        }
    }
}
