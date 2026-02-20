using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Water (Thông Máy) - Tháp làm chậm địch, hiệu ứng Slow mạnh.
/// </summary>
public class TowerWater : TowerBase
{
    #region IPoolable Implementation

    public override PoolType PoolType => PoolType.TowerWater;

    #endregion

    #region Inspector Configuration

    [Title("Water Tower Specific")]
    [BoxGroup("Water Stats")]
    [Tooltip("Tỷ lệ làm chậm (0.0 - 1.0), 0.5 = chậm 50% tốc độ")]
    [SerializeField, Range(0f, 1f)] private float _slowPercentage = 0.3f;

    [BoxGroup("Water Stats")]
    [Tooltip("Thời gian slow tồn tại trên enemy (giây)")]
    [SerializeField] private float _slowDuration = 2f;

    #endregion

    #region Public Properties

    public float SlowPercentage => _slowPercentage;
    public float SlowDuration => _slowDuration;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        if (_towerName == "Tower") _towerName = "Water Tower";
        if (_description == "") _description = "Tháp Nước - Làm chậm địch, hỗ trợ phòng thủ.";
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
