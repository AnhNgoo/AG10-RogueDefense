using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

/// <summary>
/// ORCHESTRATOR: Điều phối Map Generation và Visualization.
/// Vai trò: Quản lý state, gọi MapGenerator để sinh dữ liệu, gọi MapVisualizer để hiển thị.
/// Không chứa logic tính toán map (đã tách sang MapGenerator) hay logic render (đã tách sang MapVisualizer).
/// SOLID: Tuân thủ Single Responsibility (chỉ orchestrate) và Dependency Inversion (inject services).
/// </summary>
public class WorldMapManager : SerializedMonoBehaviour
{
    #region Inspector Configuration

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/References"), Required]
    [Tooltip("File cấu hình chứa các tham số sinh map và Prefabs")]
    [InlineEditor(InlineEditorModes.LargePreview)]
    public MapGenerationSettings settings;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/References"), Required]
    [Tooltip("Database chứa các seed đã kiểm chứng (fallback mechanism)")]
    [InlineEditor(InlineEditorModes.SmallPreview)]
    public MapSeedDatabase seedDatabase;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Seed Control")]
    [Tooltip("Bật để sử dụng Random Seed mỗi lần generate")]
    public bool useRandomSeed = true;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Seed Control")]
    [Tooltip("Seed cố định (nếu useRandomSeed = false)")]
    [HideIf("useRandomSeed")]
    public int seed = 12345;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Debug Mode")]
    [Tooltip("Bật để test với một seed cụ thể (không retry)")]
    public bool useSpecificSeed = false;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Debug Mode")]
    [Tooltip("Seed debug (chỉ dùng khi useSpecificSeed = true)")]
    [ShowIf("useSpecificSeed")]
    public int specificSeed = 0;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Visualization")]
    [Tooltip("Hiển thị Gizmos trong Scene View")]
    public bool showGizmos = true;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int CurrentSeed => seed;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int TotalChunks => worldChunks?.Count ?? 0;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public string GenerationStatus => TotalChunks >= (settings?.minChunks ?? 120) ? "✓ Valid Map" : "⚠ Incomplete";

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int HiddenChunks => hiddenChunks?.Count ?? 0;

    #endregion

    #region Private Fields

    // State Management
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>();
    private List<ChunkData> hiddenChunks = new List<ChunkData>();
    private List<Vector2Int> visualizedCoords = new List<Vector2Int>();

    // Dependencies (Injected - SOLID Dependency Inversion Principle)
    private MapGenerator mapGenerator;
    private MapVisualizer mapVisualizer;
    private MapPathfinder mapPathfinder;
    private EnemySpawner enemySpawner;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        GenerateWorld();
    }

    #endregion

    #region Public API

    [TabGroup("Tabs", "General"), Button("Generate Map", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void GenerateWorld()
    {
        if (settings == null)
        {
            Debug.LogError("[WorldMapManager] MapGenerationSettings chưa được gán! Hủy bỏ.");
            return;
        }

        // ========================================
        // DEPENDENCY INJECTION (SOLID Principle)
        // ========================================
        mapGenerator = new MapGenerator(settings);
        mapVisualizer = new MapVisualizer(settings, transform);

        // Xóa map cũ trước khi sinh map mới
        ClearVisuals();

        // === MODE 1: DEBUG WITH SPECIFIC SEED ===
        if (useSpecificSeed)
        {
            seed = specificSeed;
            Random.InitState(seed);

            Dictionary<Vector2Int, ChunkData> result = mapGenerator.GenerateMapData();
            bool debugSuccess = (result != null);

            if (debugSuccess)
            {
                worldChunks = result;
                InitializeDependencies(); // Khởi tạo Pathfinder và Spawner
                InitializeVisualization();
                Debug.Log($"[DEBUG MODE] Map generated with specific seed: {seed}. Total chunks: {worldChunks.Count}");
            }
            else
            {
                Debug.LogWarning($"[DEBUG MODE] Map generation failed with seed {seed}.");
            }
            return;
        }

        // === MODE 2: AUTO-GENERATION WITH RETRY ===
        int maxAttempts = settings.maxRetryAttempts;
        int attempts = 0;
        bool success = false;

        // Auto-retry until we get a good map
        while (!success && attempts < maxAttempts)
        {
            attempts++;

            // Setup seed
            if (useRandomSeed)
            {
                seed = Random.Range(0, 999999);
            }
            Random.InitState(seed);

            // Try to generate map using MapGenerator
            Dictionary<Vector2Int, ChunkData> result = mapGenerator.GenerateMapData();
            success = (result != null);

            if (!success)
            {
                Debug.LogWarning($"Map generation attempt {attempts} failed. Retrying with new seed...");
                seed++; // Try next seed
            }
            else
            {
                worldChunks = result;
                InitializeDependencies(); // Khởi tạo Pathfinder và Spawner sau khi có worldChunks
                InitializeVisualization();
                Debug.Log($"✓ Map generated successfully with Seed: {seed} (Attempts: {attempts}, Chunks: {worldChunks.Count})");
            }
        }

        // === FALLBACK MECHANISM ===
        if (!success)
        {
            Debug.LogError($"Failed to generate valid map after {maxAttempts} attempts.");

            // Try to use known good seed from database
            if (seedDatabase != null && seedDatabase.HasSeeds())
            {
                int fallbackSeed = seedDatabase.GetRandomSeed();
                seed = fallbackSeed;
                Random.InitState(seed);

                Dictionary<Vector2Int, ChunkData> result = mapGenerator.GenerateMapData();
                bool fallbackSuccess = (result != null);

                if (fallbackSuccess)
                {
                    worldChunks = result;
                    InitializeDependencies();
                    InitializeVisualization();
                    Debug.LogWarning($"⚠ Generation failed, used FALLBACK seed: {fallbackSeed}. Chunks: {worldChunks.Count}");
                }
                else
                {
                    Debug.LogError($"Even fallback seed {fallbackSeed} failed! Check seedDatabase quality.");
                }
            }
            else
            {
                Debug.LogError("No seedDatabase available for fallback!");
            }
        }
    }

    [TabGroup("Tabs", "General"), Button("Expand One Chunk", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
    [EnableIf("@hiddenChunks != null && hiddenChunks.Count > 0")]
    public void ExpandOneChunk()
    {
        if (hiddenChunks == null || hiddenChunks.Count == 0)
        {
            Debug.LogWarning("[WorldMapManager] Không còn chunk ẩn để mở rộng!");
            return;
        }

        // ========================================
        // THUẬT TOÁN: Tìm chunk dựa trên ExitPoint của chunk đã visualize (Connection-based)
        // ========================================
        ChunkData chunkToExpand = null;

        // Duyệt qua các chunk đã visualize
        foreach (var visCoord in visualizedCoords)
        {
            // Lấy chunk data từ worldChunks
            if (!worldChunks.TryGetValue(visCoord, out ChunkData visChunk))
                continue;

            // Duyệt qua các ExitPoint của chunk này
            foreach (var exitPoint in visChunk.exitPoints)
            {
                // Tính hướng đi ra từ ExitPoint (DELEGATION TO MAPPATHFINDER)
                Vector2Int direction = mapPathfinder.GetDirectionFromEdgeTile(exitPoint);

                // Tính tọa độ chunk hàng xóm
                Vector2Int neighborCoord = visCoord + direction;

                // Kiểm tra xem neighbor có trong hiddenChunks không
                ChunkData candidate = hiddenChunks.FirstOrDefault(c => c.chunkCoord == neighborCoord);
                if (candidate != null)
                {
                    // Tìm thấy chunk nối với đường đi -> Ưu tiên cao nhất
                    chunkToExpand = candidate;
                    break; // Thoát vòng lặp exitPoints
                }
            }

            if (chunkToExpand != null) break; // Thoát vòng lặp visualizedCoords
        }

        // Fallback: Nếu không tìm thấy kết nối đường đi, lấy chunk gần nhất
        if (chunkToExpand == null)
        {
            chunkToExpand = hiddenChunks.OrderBy(c => Mathf.Abs(c.chunkCoord.x) + Mathf.Abs(c.chunkCoord.y)).First();
            Debug.LogWarning("[WorldMapManager] Không tìm thấy chunk có kết nối đường đi. Fallback lấy chunk gần nhất.");
        }

        // Xóa khỏi hidden list và thêm vào visualized list
        hiddenChunks.Remove(chunkToExpand);
        visualizedCoords.Add(chunkToExpand.chunkCoord);

        // Hiển thị chunk này thông qua MapVisualizer (DELEGATION)
        mapVisualizer.VisualizeChunk(chunkToExpand);

        // ========================================
        // SẮP XẾP LẠI HIDDEN CHUNKS ĐỂ ƯU TIÊN NHÁNH ANH EM
        // ========================================
        // Chunk vừa mở có thể là ngã 3 -> Sắp xếp lại để các nhánh kế tiếp được mở ngay
        hiddenChunks = hiddenChunks.OrderBy(c =>
        {
            // Ưu tiên: Chunks nối trực tiếp với visualized chunks (kế tiếp nhánh)
            bool isDirectNeighbor = visualizedCoords.Any(visCoord =>
            {
                if (!worldChunks.TryGetValue(visCoord, out ChunkData visChunk)) return false;
                foreach (var exit in visChunk.exitPoints)
                {
                    Vector2Int dir = mapPathfinder.GetDirectionFromEdgeTile(exit);
                    if (visCoord + dir == c.chunkCoord) return true;
                }
                return false;
            });

            if (isDirectNeighbor) return 0; // Ưu tiên cao nhất
            return Mathf.Abs(c.chunkCoord.x) + Mathf.Abs(c.chunkCoord.y); // Manhattan distance
        }).ToList();

        // ========================================
        // SPAWN WAVE (DELEGATION TO ENEMYSPAWNER)
        // ========================================
        if (enemySpawner != null)
        {
            enemySpawner.StartNextWave(); // Delegation - Bắt đầu Wave mới
        }

        Debug.Log($"[WorldMapManager] ✓ Expanded chunk {chunkToExpand.chunkCoord}. Remaining hidden: {hiddenChunks.Count}");
    }

    [TabGroup("Tabs", "General"), Button("Expand All Chunks", ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.4f)]
    [EnableIf("@hiddenChunks != null && hiddenChunks.Count > 0")]
    public void ExpandAllChunks()
    {
        if (hiddenChunks == null || hiddenChunks.Count == 0)
        {
            Debug.LogWarning("[WorldMapManager] Không còn chunk ẩn để mở rộng!");
            return;
        }

        int count = hiddenChunks.Count;

        // Visualize tất cả chunks còn lại thông qua MapVisualizer (DELEGATION)
        foreach (var chunk in hiddenChunks)
        {
            mapVisualizer.VisualizeChunk(chunk);
            visualizedCoords.Add(chunk.chunkCoord);
        }

        hiddenChunks.Clear();
        Debug.Log($"[WorldMapManager] ✓ Expanded {count} chunks. All chunks now visible.");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Khởi tạo visualization: Hiển thị Base Chunk, ẩn tất cả chunks còn lại.
    /// </summary>
    private void InitializeVisualization()
    {
        hiddenChunks.Clear();
        visualizedCoords.Clear();

        // Visualize chỉ Base Chunk (0,0)
        if (worldChunks.TryGetValue(Vector2Int.zero, out ChunkData targetBaseChunk))
        {
            mapVisualizer.VisualizeChunk(targetBaseChunk);
            visualizedCoords.Add(Vector2Int.zero); // Track Base Chunk đã hiển thị
        }

        // Tất cả chunks còn lại đưa vào hidden list
        foreach (var chunk in worldChunks.Values)
        {
            if (chunk.chunkCoord != Vector2Int.zero)
            {
                hiddenChunks.Add(chunk);
            }
        }

        Debug.Log($"[WorldMapManager] ✓ Base Chunk visualized. {hiddenChunks.Count} chunks hidden. Use 'Expand One Chunk' button to reveal.");
    }

    /// <summary>
    /// Khởi tạo các dependencies sau khi có worldChunks.
    /// Dependency Injection: MapPathfinder và EnemySpawner nhận worldChunks.
    /// </summary>
    private void InitializeDependencies()
    {
        // Khởi tạo MapPathfinder với settings và worldChunks
        mapPathfinder = new MapPathfinder(settings, worldChunks);

        // Khởi tạo EnemySpawner (MonoBehaviour)
        GameObject spawnerObj = new GameObject("EnemySpawner");
        spawnerObj.transform.SetParent(transform);
        enemySpawner = spawnerObj.AddComponent<EnemySpawner>();
        enemySpawner.Initialize(mapPathfinder, worldChunks, visualizedCoords);

        Debug.Log("[WorldMapManager] ✓ Dependencies initialized (MapPathfinder + EnemySpawner).");
    }

    /// <summary>
    /// Xóa toàn bộ visual cũ trong scene.
    /// </summary>
    private void ClearVisuals()
    {
        if (mapVisualizer != null)
        {
            mapVisualizer.ClearVisuals();
        }

        hiddenChunks.Clear();
        visualizedCoords.Clear();
        Debug.Log("[WorldMapManager] Cleared all visuals.");
    }

    #endregion

    #region Gizmos Visualization

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (settings == null) return;
        if (worldChunks == null || worldChunks.Count == 0) return;

        foreach (var kvp in worldChunks)
        {
            ChunkData chunk = kvp.Value;
            Vector2Int chunkCoord = kvp.Key;

            // Calculate Chunk world position
            Vector3 chunkWorldPos = new Vector3(
                chunkCoord.x * settings.ChunkWorldSize,
                0,
                chunkCoord.y * settings.ChunkWorldSize
            );

            // Draw each tile
            for (int x = 0; x < settings.chunkSize; x++)
            {
                for (int z = 0; z < settings.chunkSize; z++)
                {
                    TileType tile = chunk.tiles[x, z];

                    // Calculate tile world position
                    Vector3 tileLocalPos = new Vector3(
                        (x * settings.tileSize) - settings.CenterOffset,
                        0,
                        (z * settings.tileSize) - settings.CenterOffset
                    );
                    Vector3 tileWorldPos = chunkWorldPos + tileLocalPos;

                    // Set Gizmo color based on tile type
                    switch (tile)
                    {
                        case TileType.Ground:
                            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gray
                            break;
                        case TileType.Path:
                            Gizmos.color = new Color(1f, 1f, 0f, 0.8f); // Yellow
                            break;
                        case TileType.Home:
                            Gizmos.color = new Color(0f, 1f, 0f, 0.6f); // Green
                            break;
                        case TileType.StartPoint:
                            Gizmos.color = new Color(1f, 1f, 0f, 1f); // Bright Yellow
                            break;
                        case TileType.EndPoint:
                            Gizmos.color = new Color(1f, 1f, 0f, 1f); // Bright Yellow
                            break;
                    }

                    // Draw tile as cube
                    Gizmos.DrawCube(tileWorldPos, new Vector3(settings.tileSize * 0.9f, 0.1f, settings.tileSize * 0.9f));
                }
            }
        }
    }

    #endregion
}
