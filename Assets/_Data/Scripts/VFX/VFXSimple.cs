using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// VFX SIMPLE: Hiệu ứng tĩnh (Hit, Death, Explosion, etc.).
/// Tự động Play Particle System và Return to Pool sau một khoảng thời gian (_lifeTime).
/// Tích hợp Object Pooling (IPoolable) để tái sử dụng.
/// </summary>
public class VFXSimple : MonoBehaviour, IPoolable
{
    #region IPoolable Implementation

    [Header("Pool Configuration")]
    [SerializeField] private PoolType _poolType = PoolType.VFX_Explosion;

    public PoolType PoolType => _poolType;

    #endregion

    #region Configuration

    [Header("Particle System")]
    [SerializeField] private ParticleSystem[] _particleSystems;

    [Header("Lifetime")]
    [Tooltip("Thời gian sống (giây) trước khi tự động Return to Pool")]
    [Range(0.1f, 10f)]
    [SerializeField] private float _lifeTime = 1f;

    #endregion

    #region Runtime Variables

    private CancellationTokenSource _cancellationTokenSource;

    #endregion

    #region IPoolable Lifecycle

    /// <summary>
    /// Gọi khi spawn từ Pool.
    /// Tự động Play Particle Systems và khởi động timer Return to Pool.
    /// </summary>
    public void OnSpawnFromPool()
    {
        gameObject.SetActive(true);

        // Khởi tạo CancellationTokenSource mới
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        // Play tất cả Particle Systems
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                if (ps != null)
                {
                    ps.Clear(); // Clear particles cũ
                    ps.Play();  // Play lại
                }
            }
        }

        // Khởi động timer tự động return to pool
        AutoReturnToPoolAfterLifetime(_cancellationTokenSource.Token).Forget();
    }

    /// <summary>
    /// Gọi khi return về Pool.
    /// Tự động Stop và Clear tất cả Particle Systems.
    /// </summary>
    public void OnReturnToPool()
    {
        // Cancel timer
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        // Stop và Clear tất cả Particle Systems
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        gameObject.SetActive(false);
    }

    #endregion

    #region Auto Return Logic

    /// <summary>
    /// Tự động Return to Pool sau _lifeTime giây.
    /// Sử dụng UniTask với CancellationToken để hủy khi cần.
    /// </summary>
    private async UniTaskVoid AutoReturnToPoolAfterLifetime(CancellationToken cancellationToken)
    {
        try
        {
            // Đợi _lifeTime giây
            await UniTask.Delay(System.TimeSpan.FromSeconds(_lifeTime), cancellationToken: cancellationToken);

            // Kiểm tra GameObject còn active không (safety check)
            if (!gameObject.activeInHierarchy)
                return;

            // Return về Pool
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(gameObject);
            }
            else
            {
                // Fallback: Destroy nếu không có Pool Manager
                Destroy(gameObject);
            }
        }
        catch (System.OperationCanceledException)
        {
            // Task bị cancel (VFX bị return to pool sớm hơn) - không làm gì
        }
    }

    #endregion

    #region Auto-Find Particle Systems

#if UNITY_EDITOR
    [ContextMenu("Auto-Find Particle Systems")]
    private void AutoFindParticleSystems()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        Debug.Log($"[VFXSimple] Found {_particleSystems.Length} Particle Systems.");
    }
#endif

    #endregion
}
