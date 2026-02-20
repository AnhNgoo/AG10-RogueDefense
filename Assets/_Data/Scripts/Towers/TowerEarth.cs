using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Earth (Pháo Đài) - Tháp phòng thủ, HP cao, sát thương AoE.
/// </summary>
public class TowerEarth : TowerBase
{
    #region IPoolable Implementation

    public override PoolType PoolType => PoolType.TowerEarth;

    #endregion

    #region Inspector Configuration

    [Title("Earth Tower Specific")]
    [BoxGroup("Earth Stats")]
    [Tooltip("Bán kính AoE damage (Unity Units)")]
    [SerializeField] private float _aoeRadius = 2f;

    [BoxGroup("Earth Stats")]
    [Tooltip("HP tối đa của tháp")]
    [SerializeField] private float _maxHP = 200f;

    #endregion

    #region Public Properties

    public float AoeRadius => _aoeRadius;
    public float MaxHP => _maxHP;

    #endregion

    #region Private Fields

    private float _currentHP;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        if (_towerName == "Tower") _towerName = "Earth Tower";
        if (_description == "") _description = "Tháp Đất - Phòng thủ cao, sát thương AoE.";
        _currentHP = _maxHP;
    }

    #endregion

    #region Override Methods

    public override void OnSpawnFromPool()
    {
        base.OnSpawnFromPool();
        _currentHP = _maxHP;
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
    }

    #endregion

    #region Debug Gizmos

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, _aoeRadius);
    }
#endif

    #endregion
}
