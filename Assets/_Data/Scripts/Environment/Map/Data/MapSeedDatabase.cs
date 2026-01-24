using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Database lưu trữ các Seed đã được kiểm chứng tạo ra map tốt.
/// Dùng làm fallback khi random generation thất bại sau nhiều lần thử.
/// </summary>
[CreateAssetMenu(fileName = "MapSeedDatabase", menuName = "AG10/Map Seed Database")]
public class MapSeedDatabase : SerializedScriptableObject
{
    [BoxGroup("Seed Storage")]
    [Tooltip("Danh sách các Seed đã kiểm chứng (>= minChunks requirement)")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    public List<int> knownGoodSeeds = new List<int>();

    [FoldoutGroup("Database Info"), ReadOnly, ShowInInspector]
    public int TotalSeeds => knownGoodSeeds?.Count ?? 0;

    [FoldoutGroup("Database Info"), ReadOnly, ShowInInspector]
    public string Status => HasSeeds() ? $"✓ {TotalSeeds} seeds available" : "⚠ Empty database";

    /// <summary>
    /// Kiểm tra xem database có seed nào không.
    /// </summary>
    public bool HasSeeds()
    {
        return knownGoodSeeds != null && knownGoodSeeds.Count > 0;
    }

    /// <summary>
    /// Lấy một seed ngẫu nhiên từ database.
    /// </summary>
    public int GetRandomSeed()
    {
        if (!HasSeeds())
        {
            Debug.LogWarning("[MapSeedDatabase] Database trống! Trả về seed mặc định.");
            return 12345;
        }

        return knownGoodSeeds[Random.Range(0, knownGoodSeeds.Count)];
    }

    /// <summary>
    /// Thêm seed vào database (tránh trùng lặp).
    /// </summary>
    public void AddSeed(int seed)
    {
        if (!knownGoodSeeds.Contains(seed))
        {
            knownGoodSeeds.Add(seed);
            Debug.Log($"[MapSeedDatabase] ✓ Đã thêm seed {seed}. Tổng: {knownGoodSeeds.Count}");
        }
        else
        {
            Debug.LogWarning($"[MapSeedDatabase] Seed {seed} đã tồn tại trong database.");
        }
    }

    [BoxGroup("Actions"), Button("Clear All Seeds", ButtonSizes.Large)]
    [GUIColor(1f, 0.5f, 0.5f)]
    private void ClearAllSeeds()
    {
        if (knownGoodSeeds.Count == 0)
        {
            Debug.LogWarning("[MapSeedDatabase] Database đã trống.");
            return;
        }

        knownGoodSeeds.Clear();
        Debug.Log("[MapSeedDatabase] Đã xóa toàn bộ seeds.");
    }

    [BoxGroup("Actions"), Button("Sort Seeds", ButtonSizes.Medium)]
    private void SortSeeds()
    {
        if (knownGoodSeeds.Count == 0)
        {
            Debug.LogWarning("[MapSeedDatabase] Không có seed để sort.");
            return;
        }

        knownGoodSeeds.Sort();
        Debug.Log($"[MapSeedDatabase] Đã sắp xếp {knownGoodSeeds.Count} seeds.");
    }

    [BoxGroup("Actions"), Button("Remove Duplicates", ButtonSizes.Medium)]
    private void RemoveDuplicates()
    {
        int originalCount = knownGoodSeeds.Count;
        knownGoodSeeds = new List<int>(new HashSet<int>(knownGoodSeeds));
        int removed = originalCount - knownGoodSeeds.Count;

        if (removed > 0)
        {
            Debug.Log($"[MapSeedDatabase] Đã xóa {removed} seed trùng lặp.");
        }
        else
        {
            Debug.Log("[MapSeedDatabase] Không có seed trùng lặp.");
        }
    }
}
