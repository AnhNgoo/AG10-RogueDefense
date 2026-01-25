using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject chứa toàn bộ cấu hình cho hệ thống sinh map.
/// Tách biệt settings khỏi MonoBehaviour để dễ quản lý và tái sử dụng.
/// </summary>
[CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "AG10/Map Generation Settings")]
public class MapGenerationSettings : SerializedScriptableObject
{
    [BoxGroup("Chunk Configuration"), LabelText("Chunk Size (Grid)")]
    [Tooltip("Kích thước Chunk (số ô vuông 1 chiều). Mặc định: 9x9")]
    [Range(5, 15)]
    public int chunkSize = 9;

    [BoxGroup("Chunk Configuration"), LabelText("Tile Size (Unity Units)")]
    [Tooltip("Kích thước 1 Tile trong Unity world space. Mặc định: 2.0")]
    [Range(1f, 5f)]
    public float tileSize = 2f;

    [BoxGroup("World Boundaries"), LabelText("Min Coordinate")]
    [Tooltip("Tọa độ Chunk nhỏ nhất (âm). Mặc định: -6")]
    public int minCoord = -6;

    [BoxGroup("World Boundaries"), LabelText("Max Coordinate")]
    [Tooltip("Tọa độ Chunk lớn nhất (dương). Mặc định: 5")]
    public int maxCoord = 5;

    [BoxGroup("Generation Rules"), LabelText("Branch Rate")]
    [Tooltip("Tỷ lệ tạo nhánh phụ (0.0 = không nhánh, 1.0 = nhánh tối đa). Khuyến nghị: 0.1")]
    [Range(0f, 1f)]
    public float branchRate = 0.1f;

    [BoxGroup("Quality Assurance"), LabelText("Minimum Chunks")]
    [Tooltip("Số Chunk tối thiểu để map được coi là hợp lệ. Mặc định: 120")]
    [MinValue(1)]
    public int minChunks = 120;

    [BoxGroup("Quality Assurance"), LabelText("Max Retry Attempts")]
    [Tooltip("Số lần thử lại tối đa khi sinh map không đạt yêu cầu. Mặc định: 100")]
    [MinValue(10)]
    public int maxRetryAttempts = 100;

    [BoxGroup("Prefabs"), Required]
    [Tooltip("Prefab cho đường đi (Path)")]
    public GameObject pathPrefab;

    [BoxGroup("Prefabs"), Required]
    [Tooltip("Prefab cho lớp đất nền (Dirt)")]
    public GameObject dirtPrefab;

    [BoxGroup("Prefabs"), Required]
    [Tooltip("Prefab cho lớp cỏ phủ (Grass)")]
    public GameObject grassPrefab;

    [BoxGroup("Prefabs"), Required]
    [Tooltip("Prefab cho nhà chính (Home Base 3x3)")]
    public GameObject homeBasePrefab;

    [FoldoutGroup("Calculated Values (Read-Only)"), ReadOnly, ShowInInspector]
    public float ChunkWorldSize => chunkSize * tileSize;

    [FoldoutGroup("Calculated Values (Read-Only)"), ReadOnly, ShowInInspector]
    public float CenterOffset => (ChunkWorldSize / 2f) - (tileSize / 2f);

    [FoldoutGroup("Calculated Values (Read-Only)"), ReadOnly, ShowInInspector]
    public int TotalPossibleChunks => (maxCoord - minCoord + 1) * (maxCoord - minCoord + 1);

    [Button("Validate Settings", ButtonSizes.Medium), PropertyOrder(-1)]
    private void ValidateSettings()
    {
        if (minChunks > TotalPossibleChunks)
        {
            Debug.LogWarning($"[MapGenerationSettings] minChunks ({minChunks}) vượt quá số Chunk có thể tạo ({TotalPossibleChunks})!");
        }
        else
        {
            Debug.Log($"[MapGenerationSettings] ✓ Settings hợp lệ. World: {maxCoord - minCoord + 1}x{maxCoord - minCoord + 1} chunks.");
        }
    }
}
