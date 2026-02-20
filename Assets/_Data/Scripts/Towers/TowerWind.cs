using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Wind (Cối Tròn) - Tháp tốc độ cao, multi-target, Fire Rate nhanh.
/// </summary>
public class TowerWind : TowerBase
{
    #region IPoolable Implementation

    public override PoolType PoolType => PoolType.TowerWind;

    #endregion

    #region Inspector Configuration

    [Title("Wind Tower Specific")]
    [BoxGroup("Wind Stats")]
    [Tooltip("Số mục tiêu tối đa có thể tấn công cùng lúc")]
    [SerializeField, Range(1, 5)] private int _maxTargets = 3;

    [BoxGroup("Wind Stats")]
    [Tooltip("Độ ưu tiên tấn công mục tiêu gần nhất")]
    [SerializeField] private bool _prioritizeClosest = true;

    #endregion

    #region Public Properties

    public int MaxTargets => _maxTargets;
    public bool PrioritizeClosest => _prioritizeClosest;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        if (_towerName == "Tower") _towerName = "Wind Tower";
        if (_description == "") _description = "Tháp Gió - Tấn công cực nhanh, multi-target.";
    }

    #endregion

    #region Override Methods

    public override void OnSpawnFromPool()
    {
        base.OnSpawnFromPool();
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
    }

    #endregion
}
