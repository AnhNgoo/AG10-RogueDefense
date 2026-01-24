using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

/// <summary>
/// Quản lý sinh map thế giới theo thuật toán Snake với Base Chunk 4 hướng.
/// Orchestrator điều phối 3 Phase: Init Cross -> BFS Fill -> Post-Process.
/// </summary>
public class WorldMapManager : SerializedMonoBehaviour
{
    #region Inspector Configuration

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/References"), Required]
    [Tooltip("File cấu hình chứa các tham số sinh map")]
    [InlineEditor(InlineEditorModes.SmallPreview)]
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

    #endregion

    #region Private Fields

    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>();

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

        // === MODE 1: DEBUG WITH SPECIFIC SEED ===
        if (useSpecificSeed)
        {
            seed = specificSeed;
            Random.InitState(seed);
            bool debugSuccess = TryGenerateMap();

            if (debugSuccess)
            {
                Debug.Log($"[DEBUG MODE] Map generated with specific seed: {seed}. Total chunks: {worldChunks.Count}");
            }
            else
            {
                Debug.LogWarning($"[DEBUG MODE] Map generation failed with seed {seed}. Only {worldChunks.Count} chunks.");
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

            // Try to generate map
            success = TryGenerateMap();

            if (!success)
            {
                Debug.LogWarning($"Map generation attempt {attempts} failed (only {worldChunks.Count} chunks). Retrying with new seed...");
                seed++; // Try next seed
            }
            else
            {
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

                bool fallbackSuccess = TryGenerateMap();

                if (fallbackSuccess)
                {
                    Debug.LogWarning($"⚠ Generation failed, used FALLBACK seed: {fallbackSeed}. Chunks: {worldChunks.Count}");
                }
                else
                {
                    Debug.LogError($"Even fallback seed {fallbackSeed} failed! Check seedDatabase quality.");
                }
            }
            else
            {
                Debug.LogError("No seedDatabase available for fallback! Keeping last attempt.");
            }
        }
    }

    #endregion

    #region Core Generation Logic

    /// <summary>
    /// Thử sinh map một lần. Trả về true nếu đạt yêu cầu minChunks.
    /// </summary>
    private bool TryGenerateMap()
    {
        worldChunks.Clear();

        // ========================================
        // PHASE 1: HARDCODED CROSS INITIALIZATION
        // ========================================

        // Step 1.1: Create Base Chunk (0,0)
        ChunkData baseChunk = new ChunkData(Vector2Int.zero);

        // Draw Home 3x3
        for (int x = 3; x <= 5; x++)
        {
            for (int z = 3; z <= 5; z++)
            {
                baseChunk.tiles[x, z] = TileType.Home;
            }
        }

        baseChunk.entryPoint = new Vector2Int(4, 4);
        baseChunk.tiles[4, 4] = TileType.StartPoint;

        worldChunks.Add(Vector2Int.zero, baseChunk);

        // Step 1.2: Create ONLY 1 random neighbor (single exit from home)
        Vector2Int[] allDirections = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Queue<ChunkData> queue = new Queue<ChunkData>();

        // Pick ONE random direction for the home exit
        Vector2Int chosenDir = allDirections[Random.Range(0, allDirections.Length)];

        // Calculate neighbor coordinate
        Vector2Int neighborCoord = Vector2Int.zero + chosenDir;

        // Create the single neighbor chunk
        ChunkData firstNeighbor = new ChunkData(neighborCoord);

        // Setup connections between Base and this single Neighbor
        Vector2Int exitFromBase = GetCenterEdgeTile(chosenDir);
        Vector2Int entryToNeighbor = GetCenterEdgeTile(-chosenDir);

        // Base Chunk: Add ONLY ONE exit
        baseChunk.exitPoints.Add(exitFromBase);

        // Neighbor: Set entry pointing back to Base
        firstNeighbor.entryPoint = entryToNeighbor;

        // Add neighbor to world
        worldChunks.Add(neighborCoord, firstNeighbor);

        // Enqueue the single neighbor to start expansion
        queue.Enqueue(firstNeighbor);

        // ========================================
        // PHASE 2: BFS WORLD FILLING
        // ========================================

        while (queue.Count > 0)
        {
            ChunkData currentChunk = queue.Dequeue();

            // Find all valid expansion directions
            List<Vector2Int> validDirections = new List<Vector2Int>();

            foreach (var dir in allDirections)
            {
                Vector2Int nextCoord = currentChunk.chunkCoord + dir;

                // Check boundaries
                if (nextCoord.x < settings.minCoord || nextCoord.x > settings.maxCoord ||
                    nextCoord.y < settings.minCoord || nextCoord.y > settings.maxCoord)
                    continue;

                // Check if already exists
                if (worldChunks.ContainsKey(nextCoord))
                    continue;

                validDirections.Add(dir);
            }

            // SMART SNAKE STRATEGY: Always try to continue, bend when blocked
            List<Vector2Int> selectedDirections = new List<Vector2Int>();

            if (validDirections.Count > 0)
            {
                // Step 1: Calculate "forward" direction (based on entry direction)
                Vector2Int forwardDir = GetForwardDirection(currentChunk.entryPoint);

                // Step 2: Select PRIMARY direction (main path continuation)
                Vector2Int primaryDir;

                if (validDirections.Contains(forwardDir))
                {
                    // Prefer going straight (snake continues forward)
                    primaryDir = forwardDir;
                }
                else
                {
                    // Forward blocked - MUST choose another direction (snake bends)
                    // Shuffle and pick first to ensure randomness
                    primaryDir = validDirections.OrderBy(x => Random.value).First();
                }

                selectedDirections.Add(primaryDir);

                // Step 3: Consider BRANCHING (secondary path)
                // Remove primary from valid list
                List<Vector2Int> remainingDirections = validDirections.Where(d => d != primaryDir).ToList();

                if (remainingDirections.Count > 0 && Random.value < settings.branchRate)
                {
                    // Create branch with low probability
                    Vector2Int branchDir = remainingDirections.OrderBy(x => Random.value).First();
                    selectedDirections.Add(branchDir);
                }

                // Guarantee: Never exceed 2 directions (T-shape max)
            }

            // Create chunks in selected directions
            foreach (var dir in selectedDirections)
            {
                Vector2Int newCoord = currentChunk.chunkCoord + dir;
                ChunkData newChunk = new ChunkData(newCoord);

                // Setup connections
                Vector2Int exitFromCurrent = GetCenterEdgeTile(dir);
                Vector2Int entryToNew = GetCenterEdgeTile(-dir);

                currentChunk.exitPoints.Add(exitFromCurrent);
                newChunk.entryPoint = entryToNew;

                worldChunks.Add(newCoord, newChunk);
                queue.Enqueue(newChunk);
            }
        }

        // ========================================
        // PHASE 3: POST-PROCESSING & PATH DRAWING
        // ========================================

        foreach (var chunk in worldChunks.Values)
        {
            // Fix dead ends: Create fake exit opposite to entry
            if (chunk.chunkCoord != Vector2Int.zero && chunk.exitPoints.Count == 0)
            {
                Vector2Int fakeExit = GetOppositeEdge(chunk.entryPoint);
                chunk.exitPoints.Add(fakeExit);
            }

            // Draw paths on tiles
            GenerateStarPath(chunk);
        }

        // Check if map meets quality requirements
        return worldChunks.Count >= settings.minChunks;
    }

    #endregion

    #region Path Drawing

    /// <summary>
    /// Vẽ đường đi hình sao từ Entry -> Center -> tất cả Exits.
    /// </summary>
    private void GenerateStarPath(ChunkData chunk)
    {
        Vector2Int center = new Vector2Int(4, 4);

        // Entry -> Center (skip for base chunk which starts at center)
        if (chunk.chunkCoord != Vector2Int.zero)
        {
            chunk.tiles[chunk.entryPoint.x, chunk.entryPoint.y] = TileType.StartPoint;
            DrawStraightLine(chunk, chunk.entryPoint, center);
        }

        // Center -> All Exits
        foreach (var exit in chunk.exitPoints)
        {
            DrawStraightLine(chunk, center, exit);
            chunk.tiles[exit.x, exit.y] = TileType.EndPoint;
        }

        // Ensure center is marked as path
        if (chunk.tiles[4, 4] == TileType.Ground)
            chunk.tiles[4, 4] = TileType.Path;
    }

    /// <summary>
    /// Vẽ đường thẳng theo quy tắc L-shape (X-axis trước, Y-axis sau).
    /// </summary>
    private void DrawStraightLine(ChunkData chunk, Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        SetPathTile(chunk, current);

        // Move along X axis first
        while (current.x != to.x)
        {
            current.x += (int)Mathf.Sign(to.x - current.x);
            SetPathTile(chunk, current);
        }

        // Then move along Y axis
        while (current.y != to.y)
        {
            current.y += (int)Mathf.Sign(to.y - current.y);
            SetPathTile(chunk, current);
        }
    }

    /// <summary>
    /// Đặt tile là Path, cho phép ghi đè Ground và Home.
    /// </summary>
    private void SetPathTile(ChunkData chunk, Vector2Int pos)
    {
        // Allow path to overwrite Ground and Home (for paths through home base)
        TileType current = chunk.tiles[pos.x, pos.y];
        if (current == TileType.Ground || current == TileType.Home)
            chunk.tiles[pos.x, pos.y] = TileType.Path;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Lấy tọa độ tile ở giữa cạnh Chunk theo hướng cho trước.
    /// </summary>
    private Vector2Int GetCenterEdgeTile(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return new Vector2Int(4, 8);
        if (dir == Vector2Int.down) return new Vector2Int(4, 0);
        if (dir == Vector2Int.right) return new Vector2Int(8, 4);
        if (dir == Vector2Int.left) return new Vector2Int(0, 4);
        return new Vector2Int(4, 4);
    }

    /// <summary>
    /// Lấy tọa độ cạnh đối diện với Entry Point.
    /// </summary>
    private Vector2Int GetOppositeEdge(Vector2Int entry)
    {
        // Find edge opposite to entry point
        if (entry.y == 0) return new Vector2Int(4, 8); // Bottom -> Top
        if (entry.y == 8) return new Vector2Int(4, 0); // Top -> Bottom
        if (entry.x == 0) return new Vector2Int(8, 4); // Left -> Right
        if (entry.x == 8) return new Vector2Int(0, 4); // Right -> Left
        return new Vector2Int(4, 4);
    }

    /// <summary>
    /// Tính hướng "Forward" dựa trên Entry Point (để Snake tiếp tục thẳng).
    /// </summary>
    private Vector2Int GetForwardDirection(Vector2Int entryPoint)
    {
        // Entry at bottom -> Forward is UP
        if (entryPoint.y == 0) return Vector2Int.up;
        if (entryPoint.y == 8) return Vector2Int.down;
        if (entryPoint.x == 0) return Vector2Int.right;
        if (entryPoint.x == 8) return Vector2Int.left;
        return Vector2Int.up; // Default fallback
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
