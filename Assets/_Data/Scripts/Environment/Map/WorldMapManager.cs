using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

/// <summary>
/// ORCHESTRATOR: Điều phối Map Generation và Visualization.
/// Vai trò: Quản lý state, gọi MapGenerator để sinh dữ liệu, gọi MapVisualizer để hiển thị.
/// Không chứa logic tính toán map (đã tách sang MapGenerator) hay logic render (đã tách sang MapVisualizer).
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

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Enemy Testing"), Required]
    [Tooltip("Prefab Enemy để test (tạm thời)")]
    public GameObject enemyPrefab;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Enemy Testing")]
    [Tooltip("Bật để tự động spawn Enemy khi expand chunk")]
    public bool autoSpawnEnemyOnExpand = true;

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

    // Dependencies (Injected)
    private MapGenerator mapGenerator;
    private MapVisualizer mapVisualizer;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Tạo Enemy Prefab tạm thời nếu chưa gán
        if (enemyPrefab == null)
        {
            CreateTemporaryEnemyPrefab();
        }

        GenerateWorld();
    }

    /// <summary>
    /// Tạo Enemy Prefab đơn giản (Cube đỏ) cho testing.
    /// </summary>
    private void CreateTemporaryEnemyPrefab()
    {
        GameObject tempEnemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tempEnemy.name = "TempEnemyPrefab";
        tempEnemy.transform.localScale = new Vector3(1f, 2f, 1f); // Hình người

        // Đổi màu đỏ
        Renderer renderer = tempEnemy.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }

        // Xóa collider mặc định, thêm CapsuleCollider (trigger)
        Destroy(tempEnemy.GetComponent<BoxCollider>());
        CapsuleCollider capsule = tempEnemy.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.height = 2f;
        capsule.radius = 0.5f;

        // Thêm EnemyAI_Prototype component
        tempEnemy.AddComponent<EnemyAI_Prototype>();

        // Chuyển thành Prefab runtime (không lưu vào Assets)
        enemyPrefab = tempEnemy;
        tempEnemy.SetActive(false); // Ẩn đi, chỉ dùng để Instantiate

        Debug.Log("[WorldMapManager] Created temporary Enemy Prefab for testing.");
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

        // Khởi tạo Dependencies (Dependency Injection)
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
                // Tính hướng đi ra từ ExitPoint
                Vector2Int direction = GetDirectionFromEdgeTile(exitPoint);

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

        // Hiển thị chunk này thông qua MapVisualizer
        mapVisualizer.VisualizeChunk(chunkToExpand);

        Debug.Log($"[WorldMapManager] Expanded chunk {chunkToExpand.chunkCoord}. Remaining hidden: {hiddenChunks.Count}");

        // ========================================
        // SPAWN ENEMY KHI EXPAND CHUNK
        // ========================================
        if (autoSpawnEnemyOnExpand && enemyPrefab != null)
        {
            SpawnEnemyAtChunk(chunkToExpand);
        }
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

        // Visualize tất cả chunks còn lại thông qua MapVisualizer
        foreach (var chunk in hiddenChunks)
        {
            mapVisualizer.VisualizeChunk(chunk);
            visualizedCoords.Add(chunk.chunkCoord);
        }

        hiddenChunks.Clear();
        Debug.Log($"[WorldMapManager] Expanded {count} chunks. All chunks now visible.");
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

        Debug.Log($"[WorldMapManager] Base Chunk visualized. {hiddenChunks.Count} chunks hidden. Use 'Expand One Chunk' button to reveal.");
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

    /// <summary>
    /// Tính hướng di chuyển từ chunk hiện tại dựa trên tọa độ ExitPoint.
    /// </summary>
    private Vector2Int GetDirectionFromEdgeTile(Vector2Int exitPoint)
    {
        // Exit ở cạnh trên (y = 8) -> Đi lên (Up)
        if (exitPoint.y == 8) return Vector2Int.up;
        // Exit ở cạnh dưới (y = 0) -> Đi xuống (Down)
        if (exitPoint.y == 0) return Vector2Int.down;
        // Exit ở cạnh phải (x = 8) -> Đi phải (Right)
        if (exitPoint.x == 8) return Vector2Int.right;
        // Exit ở cạnh trái (x = 0) -> Đi trái (Left)
        if (exitPoint.x == 0) return Vector2Int.left;
        return Vector2Int.zero; // Fallback (không nên xảy ra)
    }

    /// <summary>
    /// Spawn Enemy tại EndPoint của chunk và tính đường đi về Home.
    /// </summary>
    private void SpawnEnemyAtChunk(ChunkData chunk)
    {
        // Tính path từ chunk này về Home (0,0)
        List<Vector3> pathToHome = CalculatePathToHome(chunk);

        if (pathToHome == null || pathToHome.Count == 0)
        {
            Debug.LogWarning($"[WorldMapManager] Cannot spawn Enemy: No path to Home from chunk {chunk.chunkCoord}");
            return;
        }

        // Spawn Enemy tại điểm đầu tiên (EndPoint của chunk)
        Vector3 spawnPosition = pathToHome[0] + Vector3.up * 0.5f; // Y+0.5 để nổi trên đất
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemyObj.name = $"Enemy_Chunk_{chunk.chunkCoord.x}_{chunk.chunkCoord.y}";

        // Setup Enemy với path
        EnemyAI_Prototype enemyAI = enemyObj.GetComponent<EnemyAI_Prototype>();
        if (enemyAI != null)
        {
            enemyAI.Setup(pathToHome);
        }
        else
        {
            Debug.LogError("[WorldMapManager] EnemyPrefab không có component EnemyAI_Prototype!");
            Destroy(enemyObj);
        }
    }

    /// <summary>
    /// Tính đường đi từ chunk hiện tại về Home (0,0) bằng Backtracking.
    /// Trả về List<Vector3> waypoints (world position).
    /// </summary>
    private List<Vector3> CalculatePathToHome(ChunkData startChunk)
    {
        List<Vector3> path = new List<Vector3>();

        // Backtracking: Từ startChunk về (0,0)
        Vector2Int currentCoord = startChunk.chunkCoord;
        ChunkData currentChunk = startChunk;

        // Lưu chunk đã thăm để tránh loop vô hạn
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        while (currentCoord != Vector2Int.zero)
        {
            // Kiểm tra loop
            if (visited.Contains(currentCoord))
            {
                Debug.LogError($"[WorldMapManager] Path calculation loop detected at {currentCoord}!");
                return null;
            }
            visited.Add(currentCoord);

            // Thêm waypoint: Center của chunk hiện tại
            Vector3 centerWorldPos = TileToWorldPosition(currentCoord, new Vector2Int(4, 4));
            path.Add(centerWorldPos);

            // Thêm waypoint: EntryPoint của chunk hiện tại (điểm vào chunk này)
            Vector3 entryWorldPos = TileToWorldPosition(currentCoord, currentChunk.entryPoint);
            path.Add(entryWorldPos);

            // Tìm chunk cha (chunk mà currentChunk nối với qua EntryPoint)
            Vector2Int direction = GetDirectionFromEdgeTile(currentChunk.entryPoint);
            Vector2Int parentCoord = currentCoord - direction; // Ngược lại hướng entry

            // Kiểm tra chunk cha có tồn tại không
            if (!worldChunks.TryGetValue(parentCoord, out ChunkData parentChunk))
            {
                Debug.LogError($"[WorldMapManager] Path calculation failed: Parent chunk {parentCoord} not found!");
                return null;
            }

            // Di chuyển đến chunk cha
            currentCoord = parentCoord;
            currentChunk = parentChunk;

            // Giới hạn số bước để tránh vòng lặp vô hạn
            if (visited.Count > 500)
            {
                Debug.LogError("[WorldMapManager] Path calculation exceeded 500 steps! Breaking.");
                return null;
            }
        }

        // Đã về đến Home (0,0) -> Thêm điểm cuối (Center của Home)
        Vector3 homeCenter = TileToWorldPosition(Vector2Int.zero, new Vector2Int(4, 4));
        path.Add(homeCenter);

        // QUAN TRỌNG: Reverse path vì ta tính ngược từ ngọn về gốc
        path.Reverse();

        Debug.Log($"[WorldMapManager] Path calculated: {path.Count} waypoints from {startChunk.chunkCoord} to Home.");
        return path;
    }

    /// <summary>
    /// Chuyển đổi tọa độ Tile (chunk + tile index) sang World Position.
    /// </summary>
    private Vector3 TileToWorldPosition(Vector2Int chunkCoord, Vector2Int tileCoord)
    {
        // Tính world position của chunk
        Vector3 chunkWorldPos = new Vector3(
            chunkCoord.x * settings.ChunkWorldSize,
            0,
            chunkCoord.y * settings.ChunkWorldSize
        );

        // Tính offset của tile trong chunk
        Vector3 tileOffset = new Vector3(
            (tileCoord.x * settings.tileSize) - settings.CenterOffset,
            0,
            (tileCoord.y * settings.tileSize) - settings.CenterOffset
        );

        return chunkWorldPos + tileOffset;
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
