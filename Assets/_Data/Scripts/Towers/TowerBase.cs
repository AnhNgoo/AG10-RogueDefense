using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;

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

    [BoxGroup("Visual")]
    [Tooltip("Offset xoay để compensate model 3D bị sai trục (Y = 180 nếu model quay lưng)")]
    [SerializeField] protected Vector3 visualRotationOffset = new Vector3(0, 180f, 0);

    [BoxGroup("Visual")]
    [Tooltip("Tốc độ xoay nòng súng (càng cao càng nhanh)")]
    [Range(1f, 30f)]
    [SerializeField] protected float turnSpeed = 10f;

    [Title("Combat System")]
    [BoxGroup("Combat")]
    [Tooltip("Tốc độ bay của viên đạn (Unity units/giây)")]
    [Range(5f, 50f)]
    [SerializeField] protected float bulletSpeed = 20f;

    [BoxGroup("Combat")]
    [Tooltip("Loại đạn (PoolType) sẽ spawn từ Pool")]
    [SerializeField] protected PoolType bulletType = PoolType.BulletNormal;

    [BoxGroup("Combat")]
    [Tooltip("VFX hiệu ứng nổ khi đạn chạm Enemy")]
    [SerializeField] protected PoolType hitVFXType = PoolType.VFX_Hit;

    [Title("Edit Mode UI")]
    [BoxGroup("Edit Mode")]
    [Tooltip("Canvas World Space chứa nút Upgrade/Sell")]
    [SerializeField] private GameObject _editCanvas;

    [BoxGroup("Edit Mode")]
    [Tooltip("Transform vòng tròn tầm bắn (Cylinder hoặc Quad)")]
    [SerializeField] private Transform _rangeIndicator;

    [BoxGroup("Edit Mode")]
    [Tooltip("Bật Billboard effect để Canvas luôn hướng về Camera (tránh lật ngang khi Orbit)")]
    [SerializeField] private bool _enableBillboard = true;

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
    private bool _isEditModeActive = false;
    private Vector2Int _tileCoord; // Tọa độ tile của tháp (lưu để FreeTile khi bán)

    // Combat State
    private float _fireTimer = 0f;          // Timer giữa các đòn bắn
    private EnemyBase _currentTarget = null; // Mục tiêu hiện tại

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

        // Ẩn Edit UI mặc định
        if (_editCanvas != null)
        {
            _editCanvas.SetActive(false);
        }

        if (_rangeIndicator != null)
        {
            _rangeIndicator.gameObject.SetActive(false);
        }
    }

    protected virtual void LateUpdate()
    {
        // Billboard Effect: EditCanvas luôn hướng về Camera khi Edit Mode bật
        if (_enableBillboard && _isEditModeActive && _editCanvas != null && _editCanvas.activeInHierarchy)
        {
            if (Camera.main != null)
            {
                _editCanvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    protected virtual void Update()
    {
        // Chỉ hoạt động khi Tower active
        if (!_isActive) return;

        // XOAY NÒNG SÚNG BÁM THEO TARGET (Lerp mượt mà)
        if (_currentTarget != null && !_currentTarget.IsDead && _visualTransform != null)
        {
            // Tính hướng từ nòng súng tới target (bỏ trục Y để không ngửa lên trời)
            Vector3 direction = _currentTarget.Position - _visualTransform.position;
            direction.y = 0f;

            // Chỉ xoay nếu direction hợp lệ
            if (direction != Vector3.zero)
            {
                // Tính rotation cơ bản (nhìn về target)
                Quaternion baseRotation = Quaternion.LookRotation(direction);

                // ĐÚNG: Nhân Quaternion để áp offset (không cộng Euler angles!)
                Quaternion targetRotation = baseRotation * Quaternion.Euler(visualRotationOffset);

                // CHỈ LẤY TRỤC Y - Giữ nguyên X và Z của visual (tránh nòng súng bị ngửa/nghiêng)
                float targetY = targetRotation.eulerAngles.y;
                Quaternion onlyYRotation = Quaternion.Euler(0f, targetY, 0f);

                // Xoay mượt mà bằng Lerp (tránh giật cục)
                _visualTransform.rotation = Quaternion.Lerp(
                    _visualTransform.rotation,
                    onlyYRotation,
                    Time.deltaTime * turnSpeed
                );
            }
        }

        // Tăng Fire Timer
        _fireTimer += Time.deltaTime;

        // Kiểm tra có thể bắn chưa (Fire Rate)
        if (_fireTimer >= _fireRate)
        {
            // Tìm mục tiêu tốt nhất
            _currentTarget = GetBestTarget();

            // Nếu có target hợp lệ -> Bắn
            if (_currentTarget != null)
            {
                Shoot(_currentTarget);
                _fireTimer = 0f; // Reset timer
            }
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

    #region Combat System

    /// <summary>
    /// Tìm mục tiêu tốt nhất để bắn (ZERO PHYSICS - không dùng OverlapSphere).
    /// Logic: Chọn con gần nhất VÀ đi xa nhất (Progress Score cao nhất).
    /// </summary>
    protected EnemyBase GetBestTarget()
    {
        if (EnemyBase.ActiveEnemiesList == null || EnemyBase.ActiveEnemiesList.Count == 0)
            return null;

        EnemyBase bestTarget = null;
        float bestScore = float.MinValue;
        float attackRangeSqr = _attackRange * _attackRange; // Dùng sqrMagnitude cho performance

        // Duyệt qua tất cả enemies đang active
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemiesList)
        {
            // Bỏ qua nếu enemy null, đã chết, hoặc gameObject không active
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                continue;

            // Tính khoảng cách bình phương (sqrMagnitude nhanh hơn Distance)
            Vector3 toEnemy = enemy.Position - transform.position;
            float distanceSqr = toEnemy.sqrMagnitude;

            // Bỏ qua nếu nằm ngoài tầm bắn
            if (distanceSqr > attackRangeSqr)
                continue;

            // Tính Progress Score: Waypoint Index cao hơn = ưu tiên hơn
            // Trừ khoảng cách đến waypoint tiếp theo để có độ chính xác cao
            float distanceToNextWaypoint = Vector3.Distance(enemy.Position, enemy.GetNextWaypointPosition());
            float progressScore = enemy.CurrentWaypointIndex * 100f - distanceToNextWaypoint;

            // Chọn con có Progress Score cao nhất (gần đích nhất)
            if (progressScore > bestScore)
            {
                bestScore = progressScore;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Bắn vào mục tiêu.
    /// Logic:
    /// 1. Spawn viên đạn từ Pool tại _muzzlePoint.
    /// 2. Khởi chạy UniTask để xử lý sát thương sau khi đạn bay đến.
    /// LƯU Ý: Logic xoay nòng súng đã được di chuyển vào Update() để bám theo target liên tục.
    /// </summary>
    protected virtual void Shoot(EnemyBase target)
    {
        if (target == null || target.IsDead) return;

        // 1. SPAWN VIÊN ĐẠN từ Pool
        Vector3 spawnPosition = _muzzlePoint != null ? _muzzlePoint.position : transform.position;

        GameObject bulletObj = ObjectPoolManager.Instance.Spawn(
            bulletType,
            spawnPosition,
            Quaternion.identity
        );

        if (bulletObj == null)
        {
            Debug.LogWarning($"[TowerBase] Không thể spawn bullet type: {bulletType}");
            return;
        }

        // Get VFXProjectile component và gọi Fire()
        VFXProjectile projectile = bulletObj.GetComponent<VFXProjectile>();
        if (projectile != null)
        {
            projectile.Fire(spawnPosition, target.transform, bulletSpeed);
        }
        else
        {
            Debug.LogError($"[TowerBase] Bullet prefab thiếu component VFXProjectile!");
            ObjectPoolManager.Instance.ReturnToPool(bulletObj);
            return;
        }

        // 2. KHỞI CHẠY PREDICTABLE HIT SEQUENCE (UniTask)
        float distanceToTarget = Vector3.Distance(spawnPosition, target.Position);
        float hitTime = distanceToTarget / bulletSpeed; // Thời gian đạn bay

        HitTargetSequence(target, hitTime).Forget();
    }

    /// <summary>
    /// Sequence xử lý sát thương sau khi đạn bay đến (PREDICTABLE HIT).
    /// Logic:
    /// 1. Chờ hitTime giây (thời gian đạn bay).
    /// 2. Kiểm tra target còn sống không.
    /// 3. Nếu còn -> Gây sát thương và spawn VFX Hit.
    /// </summary>
    private async UniTaskVoid HitTargetSequence(EnemyBase target, float hitTime)
    {
        // Đợi thời gian đạn bay
        await UniTask.Delay(System.TimeSpan.FromSeconds(hitTime));

        // Kiểm tra target còn tồn tại và chưa chết
        if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
        {
            // Target đã chết giữa chừng (bị Tower khác giết) -> Bỏ qua
            return;
        }

        // GÂY SÁT THƯƠNG cho target
        target.TakeDamage(_damage);

        // SPAWN VFX HIT tại vị trí target
        Vector3 hitPosition = target.Position;
        ObjectPoolManager.Instance.Spawn(hitVFXType, hitPosition, Quaternion.identity);

        // TODO: Play hit sound effect
        // AudioManager.Instance.PlaySFX("TowerHit");
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

    /// <summary>
    /// Lưu tọa độ tile của tháp (để FreeTile khi bán).
    /// Gọi bởi TowerPlacementManager sau khi đặt tháp.
    /// </summary>
    public void SetTileCoordinate(Vector2Int tileCoord)
    {
        _tileCoord = tileCoord;
    }

    /// <summary>
    /// Bật/Tắt Edit Mode (hiện/ẩn Canvas Upgrade/Sell + Range Indicator).
    /// Gọi bởi TowerInteractionManager khi player click vào tháp.
    /// </summary>
    public void ToggleEditMode(bool isOn)
    {
        _isEditModeActive = isOn;

        // Hiện/Ẩn Edit Canvas
        if (_editCanvas != null)
        {
            _editCanvas.SetActive(isOn);

            // Gán Event Camera cho World Space Canvas (CRITICAL cho Mobile Touch)
            if (isOn)
            {
                Canvas canvas = _editCanvas.GetComponent<Canvas>();
                if (canvas != null && canvas.worldCamera == null)
                {
                    canvas.worldCamera = Camera.main;
                }
            }
        }

        // Hiện/Ẩn Range Indicator
        if (_rangeIndicator != null)
        {
            _rangeIndicator.gameObject.SetActive(isOn);

            // Đặt kích thước vòng tròn theo attack range
            if (isOn)
            {
                _rangeIndicator.localScale = new Vector3(_attackRange * 2f, _attackRange * 2f, _attackRange * 2f);
            }
        }
    }

    /// <summary>
    /// Callback khi player bấm nút Upgrade.
    /// TODO: Implement nâng cấp tháp logic (tăng stats, trừ tiền).
    /// </summary>
    public void OnUpgradeClicked()
    {
        Debug.Log($"[TowerBase] {_towerName} đã được nâng cấp!");
        // TODO: Kiểm tra tiền, nâng cấp stats, trừ tiền
    }

    /// <summary>
    /// Callback khi player bấm nút Sell.
    /// Logic: Trả 50% tiền xây, FreeTile trong WorldMapManager, trả tháp về pool.
    /// </summary>
    public void OnSellClicked()
    {
        // Tính tiền hoàn (50% BuildCost)
        int refund = Mathf.RoundToInt(_buildCost * 0.5f);
        Debug.Log($"[TowerBase] {_towerName} đã được bán! Hoàn tiền: {refund}");

        // TODO: Thêm tiền vào Player Gold (cần tham chiếu ResourceManager hoặc GameManager)
        // ResourceManager.Instance.AddGold(refund);

        // Giải phóng tile trong WorldMapManager
        if (WorldMapManager.Instance != null)
        {
            WorldMapManager.Instance.FreeTile(_tileCoord);
        }

        // Tắt Edit Mode
        ToggleEditMode(false);

        // Trả tháp về pool
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            // Fallback: Destroy nếu không có pool
            Destroy(gameObject);
        }
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
