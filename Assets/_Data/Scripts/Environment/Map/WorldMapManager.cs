using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

/// <summary>
/// ORCHESTRATOR: Điều phối Map Generation và Visualization.
/// Vai trò: Quản lý state, gọi MapGenerator để sinh dữ liệu, gọi MapVisualizer để hiển thị.
/// Không chứa logic tính toán map (đã tách sang MapGenerator) hay logic render (đã tách sang MapVisualizer).
/// SOLID: Tuân thủ Single Responsibility (chỉ orchestrate) và Dependency Inversion (inject services).
/// SINGLETON: Global access cho ChunkExpandNode và Tower system.
/// </summary>
public class WorldMapManager : SerializedMonoBehaviour
{
    #region Singleton Pattern

    public static WorldMapManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton Setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[WorldMapManager] Instance đã tồn tại! Hủy duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion

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

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Expand Node"), Required]
    [Tooltip("Prefab của World Space UI Node để người chơi click và mở rộng chunk")]
    [SerializeField] private GameObject _expandNodePrefab;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int CurrentSeed => seed;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int TotalChunks => worldChunks?.Count ?? 0;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public string GenerationStatus => TotalChunks >= (settings?.minChunks ?? 120) ? "✓ Valid Map" : "⚠ Incomplete";

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int HiddenChunks => hiddenChunks?.Count ?? 0;

    [TabGroup("Tabs", "General"), BoxGroup("Tabs/General/Runtime Info"), ReadOnly, ShowInInspector]
    public int OccupiedTiles => _occupiedTiles?.Count ?? 0;

    #endregion

    #region Private Fields

    // State Management
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>();
    private List<ChunkData> hiddenChunks = new List<ChunkData>();
    private List<Vector2Int> visualizedCoords = new List<Vector2Int>();

    // Tower Placement Tracking (NEW - Quản lý tile đã có tháp)
    private Dictionary<Vector2Int, bool> _occupiedTiles = new Dictionary<Vector2Int, bool>();

    // Expand Node Management (World Space UI)
    private List<ChunkExpandNode> _activeExpandNodes = new List<ChunkExpandNode>();

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

    [TabGroup("Tabs", "General"), Button("Expand One Chunk (Auto)", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
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

        // Gọi method chung để expand
        ExpandChunk(chunkToExpand);
    }

    /// <summary>
    /// Mở rộng một chunk cụ thể (Gọi bởi ExpandOneChunk hoặc ChunkExpandNode).
    /// REFACTORED: Tách logic expand ra method riêng để ChunkExpandNode có thể gọi trực tiếp.
    /// </summary>
    public void ExpandChunk(ChunkData chunkToExpand)
    {
        if (chunkToExpand == null)
        {
            Debug.LogError("[WorldMapManager] ExpandChunk: Chunk null!");
            return;
        }

        if (!hiddenChunks.Contains(chunkToExpand))
        {
            Debug.LogWarning($"[WorldMapManager] Chunk {chunkToExpand.chunkCoord} không nằm trong hidden list!");
            return;
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
        // CẬP NHẬT EXPAND NODES (Spawn nodes mới tại các chunk có thể mở rộng)
        // ========================================
        UpdateExpandNodes();

        // ========================================
        // SPAWN WAVE (DELEGATION TO WAVEMANAGER)
        // ========================================
        if (enemySpawner != null && WaveManager.Instance != null)
        {
            // Lấy danh sách End Chunks từ EnemySpawner
            List<ChunkData> endChunks = enemySpawner.GetAllEndChunks();

            // Gửi lệnh spawn wave cho WaveManager (SOLID: WaveManager = Brain, EnemySpawner = Executor)
            // Dependency Injection: Pass enemySpawner reference để WaveManager không cần Singleton pattern
            WaveManager.Instance.StartNextWave(endChunks, enemySpawner);
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

    #region Public Getters (Data Access)

    /// <summary>
    /// Lấy chunk data theo coordinate.
    /// Sử dụng bởi CameraController để auto-align.
    /// </summary>
    public ChunkData GetChunk(Vector2Int coord)
    {
        if (worldChunks != null && worldChunks.TryGetValue(coord, out ChunkData chunk))
        {
            return chunk;
        }
        return null;
    }

    /// <summary>
    /// Kiểm tra chunk đã được mở (visualize) chưa.
    /// Sử dụng bởi TowerPlacementManager để ngăn đặt tháp ngoài vùng đã mở.
    /// </summary>
    public bool IsChunkVisualized(Vector2Int chunkCoord)
    {
        return visualizedCoords != null && visualizedCoords.Contains(chunkCoord);
    }

    #endregion

    #region Tower Placement Support (NEW)

    /// <summary>
    /// Kiểm tra xem tile coordinate có đang bị chiếm bởi tháp không.
    /// Used by TowerPlacementManager để validate vị trí đặt tháp.
    /// </summary>
    public bool IsTileOccupied(Vector2Int tileCoord)
    {
        return _occupiedTiles.ContainsKey(tileCoord) && _occupiedTiles[tileCoord];
    }

    /// <summary>
    /// Đánh dấu tile coordinate đã có tháp.
    /// Gọi bởi TowerPlacementManager khi đặt tháp thành công.
    /// </summary>
    public void MarkTileOccupied(Vector2Int tileCoord)
    {
        if (!_occupiedTiles.ContainsKey(tileCoord))
        {
            _occupiedTiles.Add(tileCoord, true);
        }
        else
        {
            _occupiedTiles[tileCoord] = true;
        }

        Debug.Log($"[WorldMapManager] Tile {tileCoord} đã được đánh dấu occupied.");
    }

    /// <summary>
    /// Giải phóng tile coordinate (khi bán/phá hủy tháp).
    /// Gọi bởi TowerPlacementManager hoặc TowerManager khi remove tháp.
    /// </summary>
    public void FreeTile(Vector2Int tileCoord)
    {
        if (_occupiedTiles.ContainsKey(tileCoord))
        {
            _occupiedTiles[tileCoord] = false;
            Debug.Log($"[WorldMapManager] Tile {tileCoord} đã được giải phóng.");
        }
    }

    /// <summary>
    /// Clear tất cả occupied tiles (khi reset map hoặc new game).
    /// </summary>
    public void ClearOccupiedTiles()
    {
        _occupiedTiles.Clear();
        Debug.Log("[WorldMapManager] Đã clear tất cả occupied tiles.");
    }

    /// <summary>
    /// Lấy TileType từ Global Tile Coordinate.
    /// Used by TowerPlacementManager để validate loại tile (Ground, Path, etc.).
    /// FIXED: Helper method giúp TowerPlacementManager convert tọa độ chính xác với CenterOffset.
    /// </summary>
    /// <param name="globalTileCoord">Global Tile Coordinate (chunkCoord * chunkSize + localTileIndex)</param>
    /// <returns>TileType của tile đó, hoặc TileType.EndPoint nếu tile không tồn tại.</returns>
    public TileType GetTileType(Vector2Int globalTileCoord)
    {
        if (settings == null)
        {
            Debug.LogError("[WorldMapManager] MapGenerationSettings null! Không thể lấy TileType.");
            return TileType.EndPoint;
        }

        // ========================================
        // BƯỚC 1: Phân tách Global Tile Coord -> Chunk Coord & Local Tile Index
        // ========================================
        int chunkX = Mathf.FloorToInt((float)globalTileCoord.x / settings.chunkSize);
        int chunkZ = Mathf.FloorToInt((float)globalTileCoord.y / settings.chunkSize);

        int localX = globalTileCoord.x - (chunkX * settings.chunkSize);
        int localZ = globalTileCoord.y - (chunkZ * settings.chunkSize);

        // ========================================
        // BƯỚC 2: Lấy ChunkData từ worldChunks
        // ========================================
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkZ);

        if (worldChunks != null && worldChunks.TryGetValue(chunkCoord, out ChunkData chunk))
        {
            // Clamp local index về range hợp lệ (0 đến chunkSize-1)
            int safeLocalX = Mathf.Clamp(localX, 0, settings.chunkSize - 1);
            int safeLocalZ = Mathf.Clamp(localZ, 0, settings.chunkSize - 1);

            // Trả về TileType
            return chunk.tiles[safeLocalX, safeLocalZ];
        }

        // ========================================
        // BƯỚC 3: Tile không tồn tại trong map -> Trả về EndPoint như marker
        // ========================================
        return TileType.EndPoint; // Marker cho Tile không hợp lệ
    }

    /// <summary>
    /// Tính toán CHÍNH XÁC tọa độ tâm (World Position) của một tile dựa trên Data.
    /// QUAN TRỌNG: Đây là phương pháp ĐÚNG để lấy vị trí đặt tháp, không dựa vào Mesh (vì mesh đã bị gộp).
    /// CÔNG THỨC: Đồng bộ 100% với OnDrawGizmos() để đảm bảo tháp nằm đúng tâm ô lưới.
    /// FIXED: Bây giờ tính đúng CAO ĐỘ Y dựa trên TileType (Ground=1f, Path=0f).
    /// </summary>
    /// <param name="tileCoord">Global Tile Coordinate (chunkCoord * chunkSize + localTileIndex)</param>
    /// <returns>World Position 3D của tâm tile (Bao gồm đúng cao độ Y)</returns>
    public Vector3 GetTileCenterWorldPosition(Vector2Int tileCoord)
    {
        if (settings == null)
        {
            Debug.LogError("[WorldMapManager] MapGenerationSettings null! Không thể tính tile center.");
            return Vector3.zero;
        }

        // ========================================
        // BƯỚC 1: Phân tách Global Tile Coord -> Chunk Coord & Local Tile Index
        // ========================================
        int chunkX = Mathf.FloorToInt((float)tileCoord.x / settings.chunkSize);
        int chunkZ = Mathf.FloorToInt((float)tileCoord.y / settings.chunkSize);

        int localX = tileCoord.x - (chunkX * settings.chunkSize);
        int localZ = tileCoord.y - (chunkZ * settings.chunkSize);

        // ========================================
        // BƯỚC 1.5: Lấy TileType để xác định cao độ Y (CRITICAL FIX)
        // ========================================
        float tileHeightY = 0f; // Mặc định cho Path
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkZ);

        if (worldChunks != null && worldChunks.TryGetValue(chunkCoord, out ChunkData chunk))
        {
            // Clamp local index về range hợp lệ
            int safeLocalX = Mathf.Clamp(localX, 0, settings.chunkSize - 1);
            int safeLocalZ = Mathf.Clamp(localZ, 0, settings.chunkSize - 1);

            TileType tileType = chunk.tiles[safeLocalX, safeLocalZ];

            // Xác định cao độ Y dựa trên TileType
            switch (tileType)
            {
                case TileType.Ground:
                    tileHeightY = 1f; // Ground (cỏ) cao hơn Path
                    break;
                case TileType.Path:
                case TileType.Home:
                case TileType.StartPoint:
                case TileType.EndPoint:
                default:
                    tileHeightY = 0f; // Path và các loại khác ở mặt đất
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[WorldMapManager] Không tìm thấy chunk {chunkCoord} khi tính tile center! Fallback Y=0.");
        }

        // ========================================
        // BƯỚC 2: Tính World Position của Chunk
        // ========================================
        Vector3 chunkWorldPos = new Vector3(
            chunkX * settings.ChunkWorldSize,
            0, // Chunk origin luôn ở Y=0, cao độ tile sẽ cộng sau
            chunkZ * settings.ChunkWorldSize
        );

        // ========================================
        // BƯỚC 3: Tính Local Position của Tile trong Chunk (X, Z) + CAO ĐỘ Y
        // CÔNG THỨC QUAN TRỌNG: (localIndex * tileSize) - CenterOffset
        // ========================================
        Vector3 tileLocalPos = new Vector3(
            (localX * settings.tileSize) - settings.CenterOffset,
            tileHeightY, // CAO ĐỘ THỰC TẾ của tile (Ground=1f, Path=0f)
            (localZ * settings.tileSize) - settings.CenterOffset
        );

        // ========================================
        // BƯỚC 4: Tổng hợp World Position tuyệt đối
        // ========================================
        Vector3 tileCenterWorld = chunkWorldPos + tileLocalPos;

        return tileCenterWorld;
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
        // Tất cả chunks còn lại đưa vào hidden list
        foreach (var chunk in worldChunks.Values)
        {
            if (chunk.chunkCoord != Vector2Int.zero)
            {
                hiddenChunks.Add(chunk);
            }
        }

        // Spawn Expand Nodes tại các chunk có thể mở rộng
        UpdateExpandNodes();

        // KHỞI TẠO WAVEMANAGER: Tính MaxWaves dựa trên số lượng hidden chunks
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.Initialize(hiddenChunks.Count);
            Debug.Log($"[WorldMapManager] ✓ WaveManager initialized with {hiddenChunks.Count} max waves.");
        }

        Debug.Log($"[WorldMapManager] ✓ Base Chunk visualized. {hiddenChunks.Count} chunks hidden. Use 'Expand One Chunk' button to reveal.");
    }

    /// <summary>
    /// Cập nhật World Space UI Expand Nodes tại các chunk có thể mở rộng.
    /// THUẬT TOÁN: Duyệt exitPoints của visualizedCoords -> Tính neighbor coords -> Kiểm tra nằm trong hiddenChunks.
    /// Spawn node tại tâm chunk (tile [4,4]) + Y offset 2f để nổi lên trên mặt đất.
    /// </summary>
    private void UpdateExpandNodes()
    {
        // ========================================
        // BƯỚC 1: XÓA TẤT CẢ NODES CŨ
        // ========================================
        foreach (var node in _activeExpandNodes)
        {
            if (node != null && node.gameObject != null)
            {
                Destroy(node.gameObject);
            }
        }
        _activeExpandNodes.Clear();

        // ========================================
        // BƯỚC 2: KIỂM TRA PREFAB VÀ HIDDEN CHUNKS
        // ========================================
        if (_expandNodePrefab == null)
        {
            Debug.LogWarning("[WorldMapManager] Expand Node Prefab chưa được gán! Không thể spawn nodes.");
            return;
        }

        if (hiddenChunks == null || hiddenChunks.Count == 0)
        {
            // Không còn chunk ẩn -> Không cần nodes
            return;
        }

        // ========================================
        // BƯỚC 3: PHÁT HIỆN CÁC CHUNK CÓ THỂ MỞ RỘNG
        // ========================================
        HashSet<Vector2Int> expandableCoords = new HashSet<Vector2Int>(); // Dùng HashSet để tránh duplicate

        // Duyệt qua tất cả chunks đã visualize
        foreach (var visCoord in visualizedCoords)
        {
            // Lấy chunk data
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
                bool isExpandable = hiddenChunks.Any(c => c.chunkCoord == neighborCoord);

                if (isExpandable)
                {
                    // Đây là chunk có thể mở rộng -> Thêm vào set
                    expandableCoords.Add(neighborCoord);
                }
            }
        }

        // ========================================
        // BƯỚC 4: SPAWN NODES TẠI CÁC EXPANDABLE CHUNKS
        // ========================================
        foreach (var expandCoord in expandableCoords)
        {
            // Lấy ChunkData tương ứng
            ChunkData targetChunk = hiddenChunks.FirstOrDefault(c => c.chunkCoord == expandCoord);
            if (targetChunk == null)
            {
                Debug.LogWarning($"[WorldMapManager] Không tìm thấy ChunkData cho coord {expandCoord}!");
                continue;
            }

            // Tính vị trí node: Tâm chunk (tile 4,4) + offset Y=2f
            Vector2Int centerTileCoord = expandCoord * settings.chunkSize + new Vector2Int(4, 4);
            Vector3 nodePosition = GetTileCenterWorldPosition(centerTileCoord) + Vector3.up * 2f;

            // Instantiate node
            GameObject nodeObj = Instantiate(_expandNodePrefab, nodePosition, Quaternion.identity, transform);
            nodeObj.name = $"ExpandNode_{expandCoord.x}_{expandCoord.y}";

            // Initialize node với chunk target
            ChunkExpandNode nodeComponent = nodeObj.GetComponent<ChunkExpandNode>();
            if (nodeComponent != null)
            {
                nodeComponent.Initialize(targetChunk, expandCoord);
                _activeExpandNodes.Add(nodeComponent);
            }
            else
            {
                Debug.LogError($"[WorldMapManager] Prefab {_expandNodePrefab.name} không có component ChunkExpandNode!");
                Destroy(nodeObj);
            }
        }

        Debug.Log($"[WorldMapManager] ✓ Updated Expand Nodes: {_activeExpandNodes.Count} nodes spawned.");
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
