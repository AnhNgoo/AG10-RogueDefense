using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;

/// <summary>
/// Cấu hình nâng cấp tháp (Upgrade Config).
/// Định nghĩa số liệu tăng cho mỗi lần nâng cấp tháp.
/// </summary>
[System.Serializable]
public struct TowerUpgradeConfig
{
    [Tooltip("Tăng giá tiền cho lần nâng cấp tiếp theo")]
    public int costIncrease;

    [Tooltip("Tăng sát thương mỗi lần nâng cấp")]
    public float damageIncrease;

    [Tooltip("Tăng tầm bắn mỗi lần nâng cấp")]
    public float attackRangeIncrease;

    [Tooltip("Giảm Fire Rate (thời gian hồi giữa các phát) để bắn nhanh hơn")]
    public float fireRateDecrease;

    [Tooltip("Tăng tốc độ bay của viên đạn")]
    public float bulletSpeedIncrease;
}

/// <summary>
/// Base class cho tất cả các loại Tháp.
/// Kiến trúc:
/// - Kế thừa MonoBehaviour và IPoolable để tích hợp với Object Pooling.
/// - Abstract class cho phép mở rộng: Fire, Water, Earth, Wind.
/// - Data-Based Validation qua WorldMapManager.
/// </summary>
public abstract class TowerBase : MonoBehaviour, IPoolable
{
    #region Static Tracking (For Cleanup)

    /// <summary>
    /// Danh sách TẤT CẢ towers đang active trong game.
    /// Dùng để cleanup khi restart/load scene mới.
    /// </summary>
    private static readonly List<TowerBase> ActiveTowersList = new List<TowerBase>();

    #endregion

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
    [Tooltip("Chi phí nâng cấp tháp lên cấp tiếp theo")]
    [SerializeField] protected int _upgradeCost = 150;

    [BoxGroup("Stats")]
    [Tooltip("Tầm bắn của tháp (Unity Units)")]
    [SerializeField] protected float _attackRange = 5f;

    [BoxGroup("Stats")]
    [Tooltip("Sát thương mỗi đòn")]
    [SerializeField] protected float _damage = 10f;

    [BoxGroup("Stats")]
    [Tooltip("Thời gian hồi giữa các đòn tấn công (Fire Rate)")]
    [SerializeField] protected float _fireRate = 1f;

    [Title("Upgrade Configuration")]
    [BoxGroup("Stats")]
    [Tooltip("Cấu hình nâng cấp tháp (tăng damage, range, giảm fire rate, etc.)")]
    [SerializeField]
    protected TowerUpgradeConfig _upgradeConfig = new TowerUpgradeConfig
    {
        costIncrease = 50,
        damageIncrease = 5f,
        attackRangeIncrease = 0.5f,
        fireRateDecrease = 0.1f,
        bulletSpeedIncrease = 2f
    };

    [BoxGroup("Stats")]
    [Tooltip("Cấp độ tối đa của tháp (VD: 3 = có thể nâng cấp 2 lần từ Lv1 -> Lv3)")]
    [SerializeField] protected int _maxLevel = 3;

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
    public int UpgradeCost => _upgradeCost;
    public float AttackRange => _attackRange;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public float BulletSpeed => bulletSpeed;
    public Transform MuzzlePoint => _muzzlePoint;
    public int CurrentLevel { get; private set; } = 1;
    public int MaxLevel => _maxLevel;

    /// <summary>
    /// Tổng số tiền đã bỏ ra cho tháp này (BuildCost + tất cả UpgradeCost).
    /// USAGE: Dùng để tính giá Sell = 80% TotalSpent (công bằng với số cấp đã nâng).
    /// </summary>
    public int TotalSpent { get; private set; }

    #endregion

    #region Observer Pattern - Tower Events

    /// <summary>
    /// Event bắn ra khi tháp vừa nâng cấp xong.
    /// OBSERVER PATTERN: TowerEditUI sẽ subscribe để tự động refresh UI.
    /// </summary>
    public event Action OnTowerUpgraded;

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
        // CRITICAL: Kiểm tra cả activeInHierarchy để tránh xoay theo quái đã vào pool
        if (_currentTarget != null && !_currentTarget.IsDead && _currentTarget.gameObject.activeInHierarchy && _visualTransform != null)
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

            // CRITICAL: Kiểm tra cả activeInHierarchy trước khi bắn (tránh Ghost Bullet)
            if (_currentTarget != null && !_currentTarget.IsDead && _currentTarget.gameObject.activeInHierarchy)
            {
                Shoot(_currentTarget);
                _fireTimer = 0f; // Reset timer
            }
            else
            {
                // Quái đã chết/vào pool giữa chừng -> Reset target
                _currentTarget = null;
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

        // Reset tổng tiền đã bỏ ra (chỉ bằng BuildCost lúc mới đặt)
        TotalSpent = _buildCost;

        // Thêm vào danh sách active towers (tracking)
        if (!ActiveTowersList.Contains(this))
        {
            ActiveTowersList.Add(this);
        }
    }

    /// <summary>
    /// Gọi khi tháp trả về pool.
    /// </summary>
    public virtual void OnReturnToPool()
    {
        _isActive = false;
        gameObject.SetActive(false);

        // Xóa khỏi danh sách active towers
        if (ActiveTowersList.Contains(this))
        {
            ActiveTowersList.Remove(this);
        }
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

        // Play SFX khi tháp bắn
        AudioManager.Instance?.PlaySFX(SoundType.TowerShoot);

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
    /// 1. Lưu vị trí cuối cùng của quái (phòng trường hợp quái chết giữa chừng).
    /// 2. Chờ hitTime giây (thời gian đạn bay).
    /// 3. Kiểm tra target còn sống không.
    /// 4. Nếu còn -> Gây sát thương và spawn VFX Hit.
    /// 5. Nếu chết giữa chừng -> Spawn VFX "xịt" ở vị trí cuối cùng cho thật.
    /// </summary>
    private async UniTaskVoid HitTargetSequence(EnemyBase target, float hitTime)
    {
        // CRITICAL: Lưu lại vị trí hiện tại của quái làm vị trí dự phòng (Ghost Bullet Fix)
        Vector3 lastKnownPosition = target.Position;

        // Đợi thời gian đạn bay
        await UniTask.Delay(System.TimeSpan.FromSeconds(hitTime));

        // Kiểm tra target còn tồn tại và chưa chết
        if (target != null && !target.IsDead && target.gameObject.activeInHierarchy)
        {
            // GÂY SÁT THƯƠNG cho target
            target.TakeDamage(_damage);

            // SPAWN VFX HIT tại vị trí target
            Vector3 hitPosition = target.Position;
            ObjectPoolManager.Instance.Spawn(hitVFXType, hitPosition, Quaternion.identity);

            // Play SFX khi trúng enemy
            AudioManager.Instance?.PlaySFX(SoundType.EnemyHit);
        }
        else
        {
            // Target đã chết giữa chừng (bị Tower khác giết hoặc vào pool)
            // -> Vẫn spawn VFX nổ "xịt" ở vị trí cuối cùng cho thật (visual feedback)
            ObjectPoolManager.Instance.Spawn(hitVFXType, lastKnownPosition, Quaternion.identity);
        }
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
    /// Logic: Kiểm tra tiền, trừ tiền, nâng cấp stats.
    /// </summary>
    public void OnUpgradeClicked()
    {
        // Kiểm tra đã đạt max level chưa
        if (CurrentLevel >= _maxLevel)
        {
            Debug.Log($"[TowerBase] {_towerName} đã đạt cấp độ tối đa (Lv{_maxLevel})!");
            return;
        }

        // Kiểm tra và trừ tiền
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendGold(_upgradeCost))
        {
            // Nâng cấp thành công
            Debug.Log($"[TowerBase] {_towerName} đã được nâng cấp thành công! Chi phí: {_upgradeCost} Gold");

            // CỘNG ĐỒN CÁC CHỈ SỐ (Incremental Upgrade)
            _damage += _upgradeConfig.damageIncrease;
            _attackRange += _upgradeConfig.attackRangeIncrease;
            _fireRate = Mathf.Max(0.1f, _fireRate - _upgradeConfig.fireRateDecrease); // Đảm bảo không âm
            bulletSpeed += _upgradeConfig.bulletSpeedIncrease;

            // Cộng vào tổng tiền đã bỏ ra (để tính Sell giá cao hơn)
            TotalSpent += _upgradeCost;

            // Tăng giá nâng cấp lần tiếp theo
            _upgradeCost += _upgradeConfig.costIncrease;

            // Tăng level
            CurrentLevel++;

            Debug.Log($"[TowerBase] {_towerName} giờ ở Lv{CurrentLevel}: DMG={_damage:F1}, Range={_attackRange:F1}, FireRate={_fireRate:F2}s");

            // Bắn event để UI cập nhật
            OnTowerUpgraded?.Invoke();

            // Cập nhật Range Indicator (nếu đang bật Edit Mode)
            if (_isEditModeActive && _rangeIndicator != null)
            {
                _rangeIndicator.localScale = new Vector3(_attackRange * 2f, _attackRange * 2f, _attackRange * 2f);
            }
        }
        else
        {
            // Không đủ tiền
            Debug.Log($"[TowerBase] Không đủ tiền nâng cấp {_towerName}! Cần: {_upgradeCost} Gold");

            // Feedback UI (optional)
            // Có thể gọi CurrencyUI.ShowInsufficientFeedback() nếu cần
        }
    }

    /// <summary>
    /// Callback khi player bấm nút Sell.
    /// Logic: Trả 80% TỔNG TIỀN đã bỏ ra (bao gồm cả Upgrade), FreeTile, trả tháp về pool.
    /// </summary>
    public void OnSellClicked()
    {
        // Tính tiền hoàn (80% TotalSpent - công bằng với số cấp đã nâng)
        int refund = Mathf.RoundToInt(TotalSpent * 0.8f);
        Debug.Log($"[TowerBase] {_towerName} đã được bán! Hoàn tiền: {refund} Gold");

        // Thêm tiền vào CurrencyManager
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(refund);
        }

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

    #region Static Cleanup

    /// <summary>
    /// XÓA TẤT CẢ towers đang active (trả về pool).
    /// CRITICAL: Gọi trước khi load scene mới để tránh ghost towers.
    /// Sử dụng: GameEndUI.RestartLevel(), SceneTransition, etc.
    /// </summary>
    public static void ClearAllTowers()
    {
        // Tạo list tạm để tránh modify collection while iterating
        List<TowerBase> towersToClearing = new List<TowerBase>(ActiveTowersList);

        Debug.Log($"[TowerBase] Clearing {towersToClearing.Count} active towers...");

        foreach (TowerBase tower in towersToClearing)
        {
            if (tower != null && tower.gameObject.activeInHierarchy)
            {
                // Trả tower về pool (sẽ tự động remove khỏi ActiveTowersList)
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.ReturnToPool(tower.gameObject);
                }
                else
                {
                    // Fallback: Destroy nếu không có pool
                    tower.gameObject.SetActive(false);
                }
            }
        }

        // Đảm bảo list sạch hoàn toàn
        ActiveTowersList.Clear();

        // Xóa dữ liệu occupied tiles trong WorldMapManager
        if (WorldMapManager.Instance != null)
        {
            WorldMapManager.Instance.ClearOccupiedTiles();
        }

        Debug.Log("[TowerBase] All towers cleared!");
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
