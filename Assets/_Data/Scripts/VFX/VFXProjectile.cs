using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// VFX PROJECTILE: Viên đạn bay từ Tower đến Enemy.
/// ZERO PHYSICS: Không dùng Rigidbody/Collider, chỉ nội suy vị trí (Interpolation).
/// Tích hợp Object Pooling (IPoolable) để tái sử dụng.
/// </summary>
public class VFXProjectile : MonoBehaviour, IPoolable
{
    #region IPoolable Implementation

    [Header("Pool Configuration")]
    [SerializeField] private PoolType _poolType = PoolType.BulletNormal;

    public PoolType PoolType => _poolType;

    #endregion

    #region Configuration

    [Header("Particle System")]
    [SerializeField] private ParticleSystem[] _particleSystems;

    [Header("Trail Renderer")]
    [Tooltip("Mảng các TrailRenderer (sử dụng cho đạn Water, Fire trail, etc.)")]
    [SerializeField] private TrailRenderer[] _trails;

    [Header("Rotation")]
    [Tooltip("Xoay viên đạn theo hướng bay (visual effect)")]
    [SerializeField] private bool _rotateTowardsTarget = true;

    #endregion

    #region Runtime Variables

    private Transform _target;          // Mục tiêu bay tới
    private float _speed;               // Tốc độ bay (units/second)
    private bool _isFlying = false;     // Cờ đang bay
    private Vector3 _lastKnownPosition; // Vị trí cuối cùng của target (Phantom Target Fix)

    #endregion

    #region Public API

    /// <summary>
    /// Khởi động viên đạn bay về phía target.
    /// Gọi bởi Tower sau khi spawn từ Pool.
    /// </summary>
    /// <param name="startPos">Vị trí xuất phát (Muzzle Point)</param>
    /// <param name="target">Transform của Enemy</param>
    /// <param name="speed">Tốc độ bay (units/giây)</param>
    public void Fire(Vector3 startPos, Transform target, float speed)
    {
        // Setup vị trí ban đầu
        transform.position = startPos;

        // Lưu target và speed
        _target = target;
        _speed = speed;

        // CRITICAL: Lưu vị trí cuối cùng của target (Phantom Target Fix)
        _lastKnownPosition = target.position;

        // Bắt đầu bay
        _isFlying = true;

        // Xoay viên đạn nhìn về phía target (nếu bật)
        if (_rotateTowardsTarget && _target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        // Chỉ update khi đang bay
        if (!_isFlying) return;

        // 1. CRITICAL: Cập nhật vị trí cuối cùng NẾU target còn sống và còn active (Phantom Target Fix)
        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            _lastKnownPosition = _target.position;
        }
        else
        {
            // Nếu target đã chết/vào pool -> Xóa reference để không truy cập vào Transform bị reset về (0,0,0) nữa
            _target = null;
        }

        // 2. Tính khoảng cách đến điểm đến (là _lastKnownPosition, không phải _target.position)
        float distanceToTarget = Vector3.Distance(transform.position, _lastKnownPosition);

        // 3. Nếu đã đến đích -> Return to pool
        if (distanceToTarget < 0.1f)
        {
            ReturnToPool();
            return;
        }

        // 4. Di chuyển về phía _lastKnownPosition (Interpolation, không dùng Physics)
        transform.position = Vector3.MoveTowards(
            transform.position,
            _lastKnownPosition,
            _speed * Time.deltaTime
        );

        // 5. Xoay viên đạn nhìn về _lastKnownPosition (nếu bật)
        if (_rotateTowardsTarget)
        {
            Vector3 direction = (_lastKnownPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    #endregion

    #region IPoolable Lifecycle

    /// <summary>
    /// Gọi khi spawn từ Pool.
    /// Tự động Play tất cả Particle Systems và Clear TrailRenderer.
    /// </summary>
    public void OnSpawnFromPool()
    {
        // CRITICAL: Clear Trail TRƯỚC KHI SetActive(true) để tránh streak bug
        if (_trails != null)
        {
            foreach (var trail in _trails)
            {
                if (trail != null)
                {
                    trail.Clear(); // Xóa vệt trail cũ
                    trail.emitting = true; // Bật emitting
                }
            }
        }

        gameObject.SetActive(true);

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

        // Reset cờ bay
        _isFlying = false;
    }

    /// <summary>
    /// Gọi khi return về Pool.
    /// Tự động Stop và Clear tất cả Particle Systems và TrailRenderer.
    /// </summary>
    public void OnReturnToPool()
    {
        // Stop bay
        _isFlying = false;

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

        // Stop và Clear tất cả TrailRenderer
        if (_trails != null)
        {
            foreach (var trail in _trails)
            {
                if (trail != null)
                {
                    trail.emitting = false; // Tắt emitting
                    trail.Clear(); // Xóa vệt trail
                }
            }
        }

        // Reset references
        _target = null;

        gameObject.SetActive(false);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Trả viên đạn về Pool (gọi nội bộ khi đến target hoặc target mất).
    /// </summary>
    private void ReturnToPool()
    {
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

    #endregion

    #region Auto-Find Visuals

#if UNITY_EDITOR
    [ContextMenu("Auto-Find Visuals (Particles + Trails)")]
    private void AutoFindVisuals()
    {
        // Tự động tìm tất cả ParticleSystem
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        Debug.Log($"[VFXProjectile] Found {_particleSystems.Length} Particle Systems.");

        // Tự động tìm tất cả TrailRenderer
        _trails = GetComponentsInChildren<TrailRenderer>(true);
        Debug.Log($"[VFXProjectile] Found {_trails.Length} Trail Renderers.");

        // Log tổng kết
        Debug.Log($"[VFXProjectile] ✓ Auto-Find Complete: {_particleSystems.Length} Particles + {_trails.Length} Trails");
    }
#endif

    #endregion
}
