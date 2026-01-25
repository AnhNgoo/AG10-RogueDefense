using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// SCRIPTABLE OBJECT: Cấu hình tổng hợp cho toàn bộ Object Pool trong game.
/// Chứa List các PoolDataEntry (Enemy, Bullet, VFX, etc.)
/// 
/// WORKFLOW:
/// 1. Tạo file này 1 lần: Right-click → Create → AG10 → Pool Config
/// 2. Thêm các pool vào Table (Odin Inspector)
/// 3. Kéo thả file này vào ObjectPoolManager trong Inspector
/// </summary>
[CreateAssetMenu(fileName = "PoolConfig", menuName = "AG10/Pool Config", order = 0)]
public class PoolConfig : SerializedScriptableObject
{
    #region Pool Configurations

    [TabGroup("Tabs", "Pools")]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true, DrawScrollView = true, MaxScrollViewHeight = 600)]
    [Tooltip("Danh sách tất cả Pools trong game (Enemy, Bullet, VFX, UI, etc.)")]
    public List<PoolDataEntry> pools = new List<PoolDataEntry>();

    #endregion

    #region Runtime Info

    [TabGroup("Tabs", "Info"), ReadOnly, ShowInInspector]
    public int TotalPools => pools?.Count ?? 0;

    [TabGroup("Tabs", "Info"), ReadOnly, ShowInInspector]
    public string ConfigStatus
    {
        get
        {
            if (pools == null || pools.Count == 0)
                return "⚠ No pools configured!";

            int validCount = 0;
            int invalidCount = 0;

            foreach (var entry in pools)
            {
                if (entry.IsValid(out _))
                    validCount++;
                else
                    invalidCount++;
            }

            if (invalidCount == 0)
                return $"✓ All {validCount} pools valid";
            else
                return $"⚠ {validCount} valid, {invalidCount} invalid";
        }
    }

    #endregion

    #region Validation

    [TabGroup("Tabs", "Validation")]
    [Button("Validate All Pools", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void ValidateAllPools()
    {
        if (pools == null || pools.Count == 0)
        {
            Debug.LogError("[PoolConfig] No pools configured!");
            return;
        }

        int validCount = 0;
        int invalidCount = 0;
        HashSet<PoolType> usedTypes = new HashSet<PoolType>();

        for (int i = 0; i < pools.Count; i++)
        {
            PoolDataEntry entry = pools[i];

            // Kiểm tra tính hợp lệ
            if (!entry.IsValid(out string errorMessage))
            {
                Debug.LogError($"[PoolConfig] Pool [{i}]: {errorMessage}");
                invalidCount++;
                continue;
            }

            // Kiểm tra duplicate PoolType
            if (usedTypes.Contains(entry.poolType))
            {
                Debug.LogError($"[PoolConfig] Pool [{i}]: Duplicate PoolType '{entry.poolType}'!");
                invalidCount++;
                continue;
            }

            usedTypes.Add(entry.poolType);
            validCount++;
        }

        if (invalidCount == 0)
        {
            Debug.Log($"[PoolConfig] ✓ Validation complete! All {validCount} pools are valid.");
        }
        else
        {
            Debug.LogWarning($"[PoolConfig] ⚠ Validation complete: {validCount} valid, {invalidCount} invalid.");
        }
    }

    [TabGroup("Tabs", "Validation")]
    [Button("Add Default Pools", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
    private void AddDefaultPools()
    {
        if (pools == null)
            pools = new List<PoolDataEntry>();

        // Thêm các pool mặc định (giúp người dùng khởi động nhanh)
        pools.Add(new PoolDataEntry
        {
            poolType = PoolType.EnemyBasic,
            prefab = null,
            initialSize = 30,
            maxSize = 100,
            parentName = "Pool_Enemies"
        });

        pools.Add(new PoolDataEntry
        {
            poolType = PoolType.BulletNormal,
            prefab = null,
            initialSize = 50,
            maxSize = 200,
            parentName = "Pool_Projectiles"
        });

        pools.Add(new PoolDataEntry
        {
            poolType = PoolType.VFX_Explosion,
            prefab = null,
            initialSize = 20,
            maxSize = 50,
            parentName = "Pool_VFX"
        });

        pools.Add(new PoolDataEntry
        {
            poolType = PoolType.DamagePopup,
            prefab = null,
            initialSize = 30,
            maxSize = 100,
            parentName = "Pool_UI"
        });

        Debug.Log("[PoolConfig] ✓ Added 4 default pools. Assign prefabs in Inspector!");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Tìm Pool Entry theo PoolType.
    /// </summary>
    public PoolDataEntry GetPoolEntry(PoolType poolType)
    {
        if (pools == null)
            return null;

        return pools.Find(entry => entry.poolType == poolType);
    }

    /// <summary>
    /// Kiểm tra PoolType có tồn tại trong config không.
    /// </summary>
    public bool HasPoolType(PoolType poolType)
    {
        return GetPoolEntry(poolType) != null;
    }

    #endregion
}
