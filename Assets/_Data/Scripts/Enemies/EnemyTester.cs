using UnityEngine;

/// <summary>
/// CONCRETE CLASS: Enemy dùng để test (Cube đỏ).
/// Kế thừa EnemyBase và override các hook methods nếu cần.
/// </summary>
public class EnemyTester : EnemyBase
{
    #region Overrides

    protected override void OnSpawnComplete()
    {
        base.OnSpawnComplete();
        // Custom logic cho Tester
    }

    protected override void OnReachWaypoint(int waypointIndex)
    {
        base.OnReachWaypoint(waypointIndex);
        Debug.Log($"[EnemyTester] Reached waypoint {waypointIndex}/{pathWaypoints.Count}");
    }

    protected override void OnReachBase()
    {
        base.OnReachBase();
        // Logic khi đi hết đường
        // TODO: Thực tế sẽ gọi GameManager.TakeDamage()
    }

    #endregion
}
