using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy AI Prototype sử dụng Waypoint-based Movement (không dùng Physics).
/// Di chuyển qua danh sách waypoints bằng Vector3.MoveTowards (tối ưu performance).
/// FSM đơn giản: Spawning -> Moving -> ReachedBase -> Dead.
/// </summary>
public class EnemyAI_Prototype : MonoBehaviour
{
    #region State Machine

    public enum EnemyState
    {
        Spawning,       // Vừa spawn, chuẩn bị di chuyển
        Moving,         // Đang di chuyển theo waypoints
        ReachedBase,    // Đã đến Home (gây damage)
        Dead            // Đã chết (bị bắn hoặc đã damage xong)
    }

    private EnemyState currentState = EnemyState.Spawning;

    #endregion

    #region Movement Configuration

    [Header("Movement Settings")]
    [Tooltip("Tốc độ di chuyển (Unity units/giây)")]
    public float moveSpeed = 3f;

    [Tooltip("Khoảng cách đủ gần để coi như đã đến waypoint (tránh overshoot)")]
    public float waypointReachThreshold = 0.1f;

    #endregion

    #region Private Fields

    private List<Vector3> pathWaypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;

    #endregion

    #region Setup

    /// <summary>
    /// Khởi tạo Enemy với đường đi (gọi từ bên ngoài khi spawn).
    /// </summary>
    public void Setup(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogError("[EnemyAI] Setup failed: Path is null or empty!");
            Destroy(gameObject);
            return;
        }

        pathWaypoints = path;
        currentWaypointIndex = 0;
        currentState = EnemyState.Moving;

        // Đặt Enemy tại điểm đầu tiên
        transform.position = pathWaypoints[0];

        Debug.Log($"[EnemyAI] Setup complete. Total waypoints: {pathWaypoints.Count}");
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
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
    private void UpdateMovement()
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
                Debug.Log($"[EnemyAI] Reached waypoint {currentWaypointIndex}/{pathWaypoints.Count}");
            }
        }
    }

    /// <summary>
    /// Xử lý khi Enemy đến Home Base.
    /// </summary>
    private void HandleReachedBase()
    {
        Debug.Log("[EnemyAI] Reached Home Base! Dealing damage...");

        // TODO: Gọi GameManager để trừ máu Player
        // GameManager.Instance.TakeDamage(damageAmount);

        // Chuyển sang trạng thái Dead và hủy object
        currentState = EnemyState.Dead;
        Destroy(gameObject, 0.5f); // Delay 0.5s để có thể play animation/effect
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
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
