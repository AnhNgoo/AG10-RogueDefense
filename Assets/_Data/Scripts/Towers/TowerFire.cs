using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Fire (Nấm Đỏ) - Tháp công thích cao, Fire Rate nhanh, gây DOT.
/// </summary>
public class TowerFire : TowerBase
{
    #region IPoolable Implementation

    public override PoolType PoolType => PoolType.TowerFire;

    #endregion

    #region Inspector Configuration

    [Title("Fire Tower Specific")]
    [BoxGroup("Fire Stats")]
    [Tooltip("Sát thương DOT mỗi giây")]
    [SerializeField] private float _dotDamagePerSecond = 5f;

    [BoxGroup("Fire Stats")]
    [Tooltip("Thời gian DOT tồn tại trên enemy (giây)")]
    [SerializeField] private float _dotDuration = 3f;

    #endregion

    #region Public Properties

    public float DotDamagePerSecond => _dotDamagePerSecond;
    public float DotDuration => _dotDuration;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        if (_towerName == "Tower") _towerName = "Fire Tower";
        if (_description == "") _description = "Tháp Lửa - Tấn công nhanh, gây DOT.";
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
