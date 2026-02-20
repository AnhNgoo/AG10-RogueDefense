using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Base class cho tất cả các loại Tháp.
/// Kiến trúc:
/// - Kế thừa MonoBehaviour và IPoolable để tích hợp với Object Pooling.
/// - Abstract class cho phép mở rộng: Fire, Water, Earth, Wind.
/// - Data-Based Validation qua WorldMapManager.
/// </summary>
public abstract class TowerBase : MonoBehaviour, IPoolable
{
    #region IPoolable Implementation

    /// <summary>
    /// Loại pool của tháp (override trong subclass).
    /// </summary>
    public abstract PoolType PoolType { get; }

    #endregion

    #region Inspector Configuration

    [Title("Tower Base Configuration")]
    [BoxGroup("Stats")]
    [Tooltip("Tên hiển thị của tháp")]
    [SerializeField] protected string _towerName = "Tower";

    [BoxGroup("Stats")]
    [Tooltip("Mô tả ngắn gọn về tháp")]
    [SerializeField, TextArea(2, 3)] protected string _description = "";

    [BoxGroup("Stats")]
    [Tooltip("Chi phí xây dựng ban đầu")]
    [SerializeField] protected int _buildCost = 100;

    [BoxGroup("Stats")]
    [Tooltip("Tầm bắn của tháp (Unity Units)")]
    [SerializeField] protected float _attackRange = 5f;

    [BoxGroup("Stats")]
    [Tooltip("Sát thương mỗi đòn")]
    [SerializeField] protected float _damage = 10f;

    [BoxGroup("Stats")]
    [Tooltip("Thời gian hồi giữa các đòn tấn công (Fire Rate)")]
    [SerializeField] protected float _fireRate = 1f;

    [BoxGroup("Visual")]
    [Tooltip("Transform đầu nòng súng (vị trí spawn đạn)")]
    [SerializeField] protected Transform _muzzlePoint;

    [BoxGroup("Visual")]
    [Tooltip("Transform mô hình tháp (để xoay hướng về mục tiêu)")]
    [SerializeField] protected Transform _visualTransform;

    #endregion

    #region Public Properties

    public string TowerName => _towerName;
    public string Description => _description;
    public int BuildCost => _buildCost;
    public float AttackRange => _attackRange;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public Transform MuzzlePoint => _muzzlePoint;

    #endregion

    #region Private Fields

    // Tower State
    protected bool _isActive = false;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // Đảm bảo Visual Transform tồn tại
        if (_visualTransform == null)
        {
            _visualTransform = transform;
        }

        if (_muzzlePoint == null)
        {
            _muzzlePoint = transform;
            Debug.LogWarning($"[TowerBase] {_towerName} không có Muzzle Point! Dùng Transform gốc.", gameObject);
        }
    }

    #endregion

    #region IPoolable Methods

    /// <summary>
    /// Gọi khi tháp spawn từ pool.
    /// </summary>
    public virtual void OnSpawnFromPool()
    {
        _isActive = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Gọi khi tháp trả về pool.
    /// </summary>
    public virtual void OnReturnToPool()
    {
        _isActive = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Khởi tạo vị trí tháp (gọi bởi TowerPlacementManager).
    /// </summary>
    public virtual void Initialize(Vector3 position)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
    }

    #endregion

    #region Debug Gizmos

#if UNITY_EDITOR
    [BoxGroup("Debug")]
    [Tooltip("Hiển thị attack range trong Scene View")]
    [SerializeField] private bool _showAttackRange = true;

    protected virtual void OnDrawGizmosSelected()
    {
        if (!_showAttackRange) return;

        // Vẽ attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
#endif

    #endregion
}
