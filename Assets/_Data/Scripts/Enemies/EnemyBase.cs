using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// BASE CLASS: Enemy cơ bản với FSM và Waypoint Movement.
/// Open/Closed Principle: Open for extension (kế thừa), Closed for modification (không sửa core logic).
/// OBJECT POOLING: Implement IPoolable để tái sử dụng thay vì Destroy.
/// HEALTH SYSTEM: Tích hợp HealthComponent và implement IDamageable.
/// Refactored with Odin Inspector for better Visualization.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IPoolable, IDamageable
{
    #region State Machine

    public enum EnemyState
    {
        Spawning,       // Vừa spawn, chuẩn bị di chuyển
        Moving,         // Đang di chuyển theo waypoints
        ReachedBase,    // Đã đến Home (gây damage)
        Dead            // Đã chết
    }

    [Title("Status")]
    [EnumToggleButtons]
    [ShowInInspector, ReadOnly]
    protected EnemyState currentState = EnemyState.Spawning;

    #endregion

    #region Static Tracking (For Tower AI)

    /// <summary>
    /// Danh sách TẤT CẢ enemies đang active trong game.
    /// Tower sẽ duyệt list này để tìm target (ZERO PHYSICS - không dùng OverlapSphere).
    /// </summary>
    public static readonly List<EnemyBase> ActiveEnemiesList = new List<EnemyBase>();

    #endregion

    #region Movement Configuration

    [Title("Configuration", "Movement & Stats", TitleAlignment = TitleAlignments.Centered)]
    [BoxGroup("Stats")]
    [HorizontalGroup("Stats/Split")]
    [Range(1f, 10f)]
    [LabelWidth(100), SuffixLabel("units/s")]
    [Tooltip("Tốc độ di chuyển (Unity units/giây)")]
    public float moveSpeed = 3f;

    [HorizontalGroup("Stats/Split")]
    [LabelWidth(120), SuffixLabel("units")]
    [Tooltip("Khoảng cách đủ gần để coi như đã đến waypoint")]
    public float waypointReachThreshold = 0.1f;

    [BoxGroup("Stats")]
    [Tooltip("Loại Pool (PHẢI khớp với PoolType trong PoolData)")]
    public PoolType enemyType = PoolType.EnemyBasic;

    [BoxGroup("Stats")]
    [LabelWidth(120), SuffixLabel("Gold")]
    [Tooltip("Số vàng nhận được khi tiêu diệt enemy này")]
    [SerializeField] protected int _goldReward = 10;

    [BoxGroup("Stats")]
    [Required]
    [Tooltip("Component quản lý máu (MANDATORY)")]
    [SerializeField] protected HealthComponent healthComponent;

    #endregion

    #region Protected Fields

    protected List<Vector3> pathWaypoints = new List<Vector3>();

    [ShowInInspector, ReadOnly, ProgressBar(0, "TotalWaypoints")]
    [LabelText("Path Progress")]
    protected int currentWaypointIndex = 0;

    protected int TotalWaypoints => pathWaypoints?.Count ?? 0;

    // Cờ ngăn FSM spam HandleReachedBase() mỗi frame
    private bool _isHandlingReachBase = false;

    // Cờ ngăn trừ ActiveEnemies nhiều lần khi chết
    private bool _isCountedAsDead = false;

    #endregion

    #region Public Properties (For Tower AI)

    /// <summary>
    /// Index của waypoint hiện tại (dùng cho Tower AI tính Progress Score).
    /// </summary>
    public int CurrentWaypointIndex => currentWaypointIndex;

    /// <summary>
    /// Lấy vị trí waypoint TIẾP THEO mà enemy đang đi tới.
    /// Dùng cho Tower AI để dự đoán vị trí enemy trong tương lai.
    /// </summary>
    public Vector3 GetNextWaypointPosition()
    {
        if (pathWaypoints == null || pathWaypoints.Count == 0)
            return transform.position;

        if (currentWaypointIndex >= pathWaypoints.Count)
            return transform.position; // Đã đến cuối path

        return pathWaypoints[currentWaypointIndex];
    }

    #endregion

    #region Setup & Lifecycle

    /// <summary>
    /// Khởi tạo Enemy với đường đi (Path).
    /// TEMPLATE METHOD: Gọi các hook methods cho subclass override.
    /// </summary>
    public virtual void Setup(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] Setup failed: Path is null or empty!");
            Destroy(gameObject); // Fallback destroy if setup fails hard
            return;
        }

        pathWaypoints = path;
        currentWaypointIndex = 0;
        currentState = EnemyState.Moving;

        // Đặt Enemy tại điểm đầu tiên
        transform.position = pathWaypoints[0];

        // Hook: Cho subclass custom logic khi spawn
        OnSpawnComplete();
    }

    /// <summary>
    /// ĐẶT MÁU CUSTOM cho Enemy (từ WaveManager - HP scaling theo wave).
    /// PHẢI GỌI SAU Setup() nhưng TRƯỚC khi bắt đầu di chuyển.
    /// </summary>
    /// <param name="maxHP">Máu tối đa (đã tính bởi WaveManager)</param>
    public void SetCustomHealth(float maxHP)
    {
        if (healthComponent != null)
        {
            healthComponent.Initialize(maxHP); // Overload nhận customMaxHealth
            Debug.Log($"[{GetType().Name}] Custom HP set: {maxHP:F0}");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] HealthComponent is NULL! Cannot set custom HP.");
        }
    }

    /// <summary>
    /// Hook Method: Override để thực hiện logic custom khi enemy vừa spawn xong.
    /// </summary>
    protected virtual void OnSpawnComplete() { }

    /// <summary>
    /// Sử dụng FixedUpdate cho di chuyển để đảm bảo chuyển động ổn định.
    /// KHÔNG phụ thuộc FPS như Update().
    /// </summary>
    protected virtual void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Spawning:
                // Chờ Setup() được gọi
                break;

            case EnemyState.Moving:
                UpdateMovement();
                break;

            case EnemyState.ReachedBase:
                // Chỉ gọi HandleReachedBase() 1 LẦN DUY NHẤT khi chuyển state
                if (!_isHandlingReachBase)
                {
                    _isHandlingReachBase = true;
                    HandleReachedBase();
                }
                break;

            case EnemyState.Dead:
                // Không làm gì, chờ ReturnToPool
                break;
        }
    }

    #endregion

    #region Movement Logic

    /// <summary>
    /// Di chuyển Enemy theo waypoints bằng Interpolation (không dùng Physics).
    /// </summary>
    protected virtual void UpdateMovement()
    {
        if (currentWaypointIndex >= pathWaypoints.Count)
        {
            // Đã đến waypoint cuối cùng -> Reached Base
            currentState = EnemyState.ReachedBase;
            return;
        }

        // Lấy waypoint hiện tại
        Vector3 targetWaypoint = pathWaypoints[currentWaypointIndex];

        // Di chuyển tới waypoint (Interpolation)
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWaypoint,
            moveSpeed * Time.deltaTime
        );

        // Quay mặt theo hướng di chuyển
        Vector3 direction = (targetWaypoint - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Kiểm tra đã đến waypoint chưa
        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
        if (distanceToWaypoint <= waypointReachThreshold)
        {
            // Đã đến waypoint -> Chuyển sang waypoint tiếp theo
            currentWaypointIndex++;

            if (currentWaypointIndex < pathWaypoints.Count)
            {
                OnReachWaypoint(currentWaypointIndex);
            }
        }
    }

    /// <summary>
    /// Xử lý khi Enemy đến đích (Game Over hoặc trừ máu người chơi).
    /// </summary>
    protected virtual void OnReachWaypoint(int waypointIndex) { }

    /// <summary>
    /// Di chuyển Enemy hướng tới Waypoint tiếp theo.
    /// TEMPLATE METHOD: Gọi hook OnReachBase() cho subclass.
    /// </summary>
    protected virtual void HandleReachedBase()
    {
        Debug.Log($"[{GetType().Name}] Reached Home Base! Dealing damage...");

        // Hook: Cho subclass xử lý damage logic
        OnReachBase();

        // CRITICAL: Giảm số lượng enemy đang active (chỉ 1 lần)
        if (!_isCountedAsDead)
        {
            _isCountedAsDead = true;
            EnemySpawner.ActiveEnemies--;
            Debug.Log($"[{GetType().Name}] ActiveEnemies giảm xuống: {EnemySpawner.ActiveEnemies}");
        }

        // Chuyển sang trạng thái Dead và RETURN TO POOL (không Destroy)
        currentState = EnemyState.Dead;
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }

    /// <summary>
    /// Hook Method: Override để xử lý logic khi enemy đến Base.
    /// GÂY SÁT THƯƠNG cho Nhà Chính.
    /// </summary>
    protected virtual void OnReachBase()
    {
        // Gây sát thương cho Nhà Chính (OBSERVER PATTERN: UI tự động cập nhật)
        if (BaseHealthManager.Instance != null)
        {
            BaseHealthManager.Instance.TakeDamage(1);
        }
        else
        {
            Debug.LogWarning("[EnemyBase] BaseHealthManager không tồn tại! Không thể trừ máu Base.");
        }
    }

    #endregion

    #region IDamageable Implementation

    /// <summary>
    /// Nhận sát thương từ Tower, Spell, etc.
    /// Delegate sang HealthComponent.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(amount);

            // FIX: Chỉ gọi visual feedback nếu enemy VẪN CÒN SỐNG sau khi bị trừ máu
            // Tránh lỗi NullReference khi enemy chết ngay lập tức (máu <= 0)
            if (!IsDead)
            {
                OnTakeDamage(amount);
            }
        }
    }

    /// <summary>
    /// Kiểm tra đã chết chưa (IDamageable).
    /// </summary>
    public bool IsDead => healthComponent != null && healthComponent.IsDead;

    /// <summary>
    /// Vị trí hiện tại (IDamageable).
    /// </summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// Hook Method: Override để xử lý visual feedback khi nhận damage.
    /// Ví dụ: Play animation TakeDamage, spawn blood VFX, shake model, etc.
    /// </summary>
    protected virtual void OnTakeDamage(float amount) { }

    #endregion

    #region Health System

    /// <summary>
    /// Xử lý khi Enemy chết (gọi bởi HealthComponent.OnDeath event).
    /// Giảm số lượng ActiveEnemies VÀ Return về Pool.
    /// </summary>
    private void HandleDeath()
    {
        // CRITICAL: Chỉ giảm ActiveEnemies 1 LẦN DUY NHẤT
        if (!_isCountedAsDead)
        {
            _isCountedAsDead = true;
            EnemySpawner.ActiveEnemies--;
            Debug.Log($"[{GetType().Name}] Chết! ActiveEnemies giảm xuống: {EnemySpawner.ActiveEnemies}");

            // THƯỞNG TIỀN cho người chơi khi tiêu diệt enemy
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddGold(_goldReward);
            }
        }

        // Hook: Cho subclass xử lý logic khi chết (spawn VFX, drop loot, etc.)
        OnDie();

        // Chuyển state sang Dead
        currentState = EnemyState.Dead;

        // Return về Pool
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }

    /// <summary>
    /// Hook Method: Override để xử lý logic khi enemy chết.
    /// Ví dụ: Spawn death VFX, drop gold, play death sound, etc.
    /// </summary>
    protected virtual void OnDie() { }

    #endregion

    #region IPoolable Implementation

    /// <summary>
    /// PoolType property - Bắt buộc phải implement từ IPoolable.
    /// </summary>
    public PoolType PoolType => enemyType;

    /// <summary>
    /// Gọi khi lấy ra khỏi Pool. Reset máu, trạng thái.
    /// </summary>
    public virtual void OnSpawnFromPool()
    {
        // Reset FSM state
        currentState = EnemyState.Spawning;
        currentWaypointIndex = 0;
        pathWaypoints.Clear();

        // Reset cờ HandleReachedBase
        _isHandlingReachBase = false;

        // CRITICAL: Reset cờ đếm chết
        _isCountedAsDead = false;

        // Reset tốc độ về mặc định
        moveSpeed = 3f;

        // ANTI-STACKING: Random tốc độ mỗi lần spawn để tránh quái đi trùng khít
        moveSpeed *= Random.Range(0.8f, 1.2f);

        // Khởi tạo Health Component
        if (healthComponent != null)
        {
            healthComponent.Initialize();

            // Subscribe vào event OnDeath
            healthComponent.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] HealthComponent null! Không thể spawn enemy.");
        }

        // CRITICAL: Thêm vào Static List để Tower AI có thể tracking
        if (!ActiveEnemiesList.Contains(this))
        {
            ActiveEnemiesList.Add(this);
        }

        Debug.Log($"[{GetType().Name}] ✓ Spawned from pool (Type: {enemyType}, Speed: {moveSpeed:F2}).");
    }

    /// <summary>
    /// Gọi khi object được trả về Pool.
    /// </summary>
    public virtual void OnReturnToPool()
    {
        // CRITICAL: Xóa khỏi Static List
        if (ActiveEnemiesList.Contains(this))
        {
            ActiveEnemiesList.Remove(this);
        }

        // Unsubscribe khỏi HealthComponent events
        if (healthComponent != null)
        {
            healthComponent.OnDeath -= HandleDeath;
        }

        // Stop tất cả Coroutines (nếu có)
        StopAllCoroutines();

        // Reset state
        currentState = EnemyState.Dead;
        pathWaypoints.Clear();
        currentWaypointIndex = 0;

        // Reset cờ HandleReachedBase
        _isHandlingReachBase = false;

        // Reset cờ đếm chết
        _isCountedAsDead = false;

        // Reset position về gốc (tránh object bay ra ngoài map)
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    #endregion

    #region Debug Visualization

    protected virtual void OnDrawGizmos()
    {
        if (pathWaypoints == null || pathWaypoints.Count == 0) return;

        // Vẽ đường đi trong Scene View
        Gizmos.color = Color.red;
        for (int i = 0; i < pathWaypoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathWaypoints[i], pathWaypoints[i + 1]);
        }

        // Vẽ các waypoints
        Gizmos.color = Color.yellow;
        foreach (var waypoint in pathWaypoints)
        {
            Gizmos.DrawSphere(waypoint, 0.3f);
        }

        // Highlight waypoint hiện tại
        if (currentWaypointIndex < pathWaypoints.Count)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pathWaypoints[currentWaypointIndex], 0.5f);
        }
    }

    #endregion
}
