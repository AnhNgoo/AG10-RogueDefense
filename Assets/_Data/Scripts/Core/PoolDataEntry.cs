using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// SERIALIZABLE CLASS: Dữ liệu cấu hình cho 1 Pool.
/// Sử dụng trong PoolConfig (List<PoolDataEntry>).
/// </summary>
[System.Serializable]
public class PoolDataEntry
{
    [TableColumnWidth(120, Resizable = false)]
    [Tooltip("Loại Pool (phải unique trong toàn bộ config)")]
    public PoolType poolType = PoolType.None;

    [TableColumnWidth(150, Resizable = false)]
    [Required, AssetsOnly]
    [Tooltip("Prefab gốc (phải có component implement IPoolable)")]
    public GameObject prefab;

    [TableColumnWidth(80, Resizable = false)]
    [Range(1, 100)]
    [Tooltip("Số lượng khởi tạo ban đầu (Warmup)")]
    public int initialSize = 10;

    [TableColumnWidth(80, Resizable = false)]
    [Range(10, 500)]
    [Tooltip("Số lượng tối đa (giới hạn để tránh lag)")]
    public int maxSize = 50;

    [TableColumnWidth(120, Resizable = false)]
    [Tooltip("Tên GameObject cha chứa pool này (để gọn Hierarchy)")]
    public string parentName = "Pool_Default";

    #region Validation

    /// <summary>
    /// Kiểm tra config có hợp lệ không.
    /// </summary>
    public bool IsValid(out string errorMessage)
    {
        if (poolType == PoolType.None)
        {
            errorMessage = $"PoolType chưa được set!";
            return false;
        }

        if (prefab == null)
        {
            errorMessage = $"Prefab is null cho Pool '{poolType}'!";
            return false;
        }

        IPoolable poolable = prefab.GetComponent<IPoolable>();
        if (poolable == null)
        {
            errorMessage = $"Prefab '{prefab.name}' không có component implement IPoolable!";
            return false;
        }

        if (poolable.PoolType != poolType)
        {
            errorMessage = $"PoolType mismatch! Config: {poolType}, Component: {poolable.PoolType}";
            return false;
        }

        if (initialSize <= 0 || maxSize < initialSize)
        {
            errorMessage = $"Invalid size settings! initialSize={initialSize}, maxSize={maxSize}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    #endregion
}
