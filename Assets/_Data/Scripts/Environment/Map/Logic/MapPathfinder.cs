using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SERVICE: Chịu trách nhiệm tính toán đường đi (Pathfinding) cho Enemy.
/// Single Responsibility: Chỉ xử lý logic tìm đường, không quản lý Map hay Enemy.
/// </summary>
public class MapPathfinder
{
    #region Dependencies

    private readonly MapGenerationSettings settings;
    private readonly Dictionary<Vector2Int, ChunkData> worldChunks;

    #endregion

    #region Constructor

    /// <summary>
    /// Khởi tạo MapPathfinder với dependencies cần thiết.
    /// </summary>
    public MapPathfinder(MapGenerationSettings settings, Dictionary<Vector2Int, ChunkData> worldChunks)
    {
        this.settings = settings;
        this.worldChunks = worldChunks;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Tính đường đi từ một ExitPoint cụ thể về Home (0,0) bằng Backtracking.
    /// Trả về List<Vector3> waypoints (world position) với Y=2.
    /// </summary>
    public List<Vector3> CalculatePathToHome(ChunkData startChunk, Vector2Int startExitTile)
    {
        List<Vector3> path = new List<Vector3>();

        // Backtracking: Từ startChunk về (0,0)
        ChunkData currentChunk = startChunk;

        // Lưu chunk đã thăm để tránh loop vô hạn
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        int maxSteps = 500; // Giới hạn số bước

        // === BƯỚC 1: Thêm ExitPoint (mép chunk) làm điểm xuất phát ===
        Vector3 exitWorldPos = TileToWorldPosition(startChunk.chunkCoord, startExitTile);
        path.Add(exitWorldPos);

        // === BƯỚC 2: Loop cho đến khi về tới Base Chunk (0,0) ===
        while (currentChunk.chunkCoord != Vector2Int.zero)
        {
            // Kiểm tra loop
            if (visited.Contains(currentChunk.chunkCoord))
            {
                Debug.LogError($"[MapPathfinder] Path calculation loop detected at {currentChunk.chunkCoord}!");
                return null;
            }
            visited.Add(currentChunk.chunkCoord);

            // Giới hạn số bước để tránh vòng lặp vô hạn
            if (visited.Count > maxSteps)
            {
                Debug.LogError($"[MapPathfinder] Path calculation exceeded {maxSteps} steps! Breaking.");
                return null;
            }

            // Add waypoint: Center của chunk hiện tại (Tile 4,4)
            Vector3 centerWorldPos = TileToWorldPosition(currentChunk.chunkCoord, new Vector2Int(4, 4));
            path.Add(centerWorldPos);

            // Add waypoint: EntryPoint của chunk hiện tại (điểm vào chunk này)
            Vector3 entryWorldPos = TileToWorldPosition(currentChunk.chunkCoord, currentChunk.entryPoint);
            path.Add(entryWorldPos);

            // QUAN TRỌNG: Tính parent direction từ EntryPoint
            Vector2Int parentDir = GetParentDirection(currentChunk.entryPoint);
            Vector2Int parentCoord = currentChunk.chunkCoord + parentDir;

            // Kiểm tra parent chunk có tồn tại không
            if (!worldChunks.TryGetValue(parentCoord, out ChunkData parentChunk))
            {
                Debug.LogError($"[MapPathfinder] Path calculation failed: Parent chunk {parentCoord} not found (current: {currentChunk.chunkCoord}, entry: {currentChunk.entryPoint}, dir: {parentDir})!");
                return null;
            }

            // Di chuyển đến parent chunk
            currentChunk = parentChunk;
        }

        // === BƯỚC 3: Đã về đến Base Chunk (0,0) -> Thêm Center của Base vào cuối path ===
        Vector3 homeCenter = TileToWorldPosition(Vector2Int.zero, new Vector2Int(4, 4));
        path.Add(homeCenter);

        // === BƯỚC 4: Nâng tất cả waypoints lên Y=2 (Enemy di chuyển trên không) ===
        for (int i = 0; i < path.Count; i++)
        {
            path[i] = new Vector3(path[i].x, 2f, path[i].z);
        }

        Debug.Log($"[MapPathfinder] ✓ Path calculated: {path.Count} waypoints from exit {startExitTile} in chunk {startChunk.chunkCoord} to Home.");
        return path;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Tính hướng Parent Chunk từ tọa độ EntryPoint (tile cục bộ 0-8).
    /// EntryPoint là điểm vào chunk hiện tại -> Parent nằm ở hướng ngược lại.
    /// </summary>
    public Vector2Int GetParentDirection(Vector2Int entryPoint)
    {
        // Entry ở mép dưới (y = 0) -> Parent nằm phía Nam (Down)
        if (entryPoint.y == 0) return Vector2Int.down;
        // Entry ở mép trên (y = 8) -> Parent nằm phía Bắc (Up)
        if (entryPoint.y == 8) return Vector2Int.up;
        // Entry ở mép trái (x = 0) -> Parent nằm phía Tây (Left)
        if (entryPoint.x == 0) return Vector2Int.left;
        // Entry ở mép phải (x = 8) -> Parent nằm phía Đông (Right)
        if (entryPoint.x == 8) return Vector2Int.right;

        Debug.LogError($"[MapPathfinder] Invalid EntryPoint: {entryPoint}. Must be on edge (0 or 8).");
        return Vector2Int.zero; // Fallback
    }

    /// <summary>
    /// Tính hướng di chuyển từ chunk hiện tại dựa trên tọa độ ExitPoint.
    /// </summary>
    public Vector2Int GetDirectionFromEdgeTile(Vector2Int exitPoint)
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
}
