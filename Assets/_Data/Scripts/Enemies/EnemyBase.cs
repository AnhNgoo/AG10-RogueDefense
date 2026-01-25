using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BASE CLASS: Enemy cơ bản với FSM và Waypoint Movement.
/// Open/Closed Principle: Open for extension (kế thừa), Closed for modification (không sửa core logic).
/// OBJECT POOLING: Implement IPoolable để tái sử dụng thay vì Destroy.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IPoolable
{
    #region State Machine

    public enum EnemyState
    {
        Spawning,       // Vừa spawn, chuẩn bị di chuyển
        Moving,         // Đang di chuyển theo waypoints
        ReachedBase,    // Đã đến Home (gây damage)
        Dead            // Đã chết (bị bắn hoặc đã damage xong)
    }

    protected EnemyState currentState = EnemyState.Spawning;

    #endregion

    #region Movement Configuration

    [Header("Movement Settings")]
    [Tooltip("Tốc độ di chuyển (Unity units/giây)")]
    public float moveSpeed = 3f;

    [Tooltip("Khoảng cách đủ gần để coi như đã đến waypoint (tránh overshoot)")]
    public float waypointReachThreshold = 0.1f;

    #endregion

    #region Protected Fields

    protected List<Vector3> pathWaypoints = new List<Vector3>();
    protected int currentWaypointIndex = 0;

    [Header("Pooling")]
    [Tooltip("Loại Pool (PHẢI khớp với PoolType trong PoolData)")]
    public PoolType enemyType = PoolType.EnemyBasic;

    #endregion

    #region Setup & Lifecycle

    /// <summary>
    /// Khởi tạo Enemy với đường đi (gọi từ bên ngoài khi spawn).
    /// TEMPLATE METHOD: Gọi các hook methods cho subclass override.
    /// </summary>
    public virtual void Setup(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] Setup failed: Path is null or empty!");
            Destroy(gameObject);
            return;
        }

        pathWaypoints = path;
        currentWaypointIndex = 0;
        currentState = EnemyState.Moving;

        // Đặt Enemy tại điểm đầu tiên
        transform.position = pathWaypoints[0];

        // QUAN TRỌNG: Random tốc độ để tránh chồng chéo khi nhiều enemy cùng đường
        moveSpeed *= Random.Range(0.8f, 1.2f);

        // Hook: Cho subclass custom logic khi spawn
        OnSpawnComplete();

        Debug.Log($"[{GetType().Name}] Setup complete. Total waypoints: {pathWaypoints.Count}, Speed: {moveSpeed:F2}");
    }

    /// <summary>
    /// Hook Method: Override để thực hiện logic custom khi enemy vừa spawn xong.
    /// </summary>
    protected virtual void OnSpawnComplete() { }

    protected virtual void Update()
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
                HandleReachedBase();
                break;

            case EnemyState.Dead:
                // Không làm gì, chờ Destroy
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

        // Quay mặt theo hướng di chuyển (optional, cho đẹp)
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
    /// Hook Method: Override để xử lý khi đến waypoint.
    /// </summary>
    protected virtual void OnReachWaypoint(int waypointIndex) { }

    /// <summary>
    /// Xử lý khi Enemy đến Home Base.
    /// TEMPLATE METHOD: Gọi hook OnReachBase() cho subclass.
    /// </summary>
    protected virtual void HandleReachedBase()
    {
        Debug.Log($"[{GetType().Name}] Reached Home Base! Dealing damage...");

        // Hook: Cho subclass xử lý damage logic
        OnReachBase();

        // Chuyển sang trạng thái Dead và RETURN TO POOL (không Destroy)
        currentState = EnemyState.Dead;
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }

    /// <summary>
    /// Hook Method: Override để xử lý logic khi enemy đến Base.
    /// </summary>
    protected virtual void OnReachBase()
    {
        // TODO: Gọi GameManager để trừ máu Player
        // GameManager.Instance.TakeDamage(damageAmount);
    }

    #endregion

    #region IPoolable Implementation

    /// <summary>
    /// PoolType property - Bắt buộc phải implement từ IPoolable.
    /// Dùng để ObjectPoolManager biết trả về đúng pool.
    /// </summary>
    public PoolType PoolType => enemyType;

    /// <summary>
    /// Gọi khi object được lấy ra từ Pool.
    /// Reset trạng thái về như mới (máu đầy, vận tốc = 0, etc.)
    /// </summary>
    public virtual void OnSpawnFromPool()
    {
        // Reset FSM state
        currentState = EnemyState.Spawning;
        currentWaypointIndex = 0;
        pathWaypoints.Clear();

        // Reset tốc độ về mặc định
        moveSpeed = 3f;

        // TODO: Reset máu khi có health system
        // health = maxHealth;

        Debug.Log($"[{GetType().Name}] ✓ Spawned from pool (Type: {enemyType}).");
    }

    /// <summary>
    /// Gọi khi object được trả về Pool.
    /// Cleanup resources (stop Coroutines, detach từ parent, etc.)
    /// </summary>
    public virtual void OnReturnToPool()
    {
        // Stop tất cả Coroutines (nếu có)
        StopAllCoroutines();

        // Reset state
        currentState = EnemyState.Dead;
        pathWaypoints.Clear();
        currentWaypointIndex = 0;

        // Reset position về gốc (tránh object bay ra ngoài map)
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Debug.Log($"[{GetType().Name}] ✓ Returned to pool (Type: {enemyType}).");
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
