using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Lớp Logic thuần chịu trách nhiệm sinh dữ liệu bản đồ (không có logic hiển thị).
/// Nhận MapGenerationSettings làm đầu vào, trả về Dictionary<Vector2Int, ChunkData>.
/// </summary>
public class MapGenerator
{
    private MapGenerationSettings settings;

    public MapGenerator(MapGenerationSettings settings)
    {
        this.settings = settings;
    }

    /// <summary>
    /// Thử sinh map một lần. Trả về Dictionary nếu thành công, null nếu thất bại.
    /// </summary>
    public Dictionary<Vector2Int, ChunkData> GenerateMapData()
    {
        Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>();

        // ========================================
        // ========================================
        // GIAI ĐOẠN 1: KHỞI TẠO HÌNH CHỮ THẬP CỨNG
        // ========================================

        // Bước 1.1: Tạo Chunk Gốc (0,0)
        ChunkData baseChunk = new ChunkData(Vector2Int.zero);

        // Vẽ Nhà Chính 3x3
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

        // Bước 1.2: Tạo CHỈ 1 hàng xóm ngẫu nhiên (lối ra duy nhất từ nhà)
        Vector2Int[] allDirections = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Queue<ChunkData> queue = new Queue<ChunkData>();

        // Chọn MỘT hướng ngẫu nhiên cho lối ra
        Vector2Int chosenDir = allDirections[Random.Range(0, allDirections.Length)];

        // Tính tọa độ hàng xóm
        Vector2Int neighborCoord = Vector2Int.zero + chosenDir;

        // Tạo chunk hàng xóm duy nhất
        ChunkData firstNeighbor = new ChunkData(neighborCoord);

        // Thiết lập kết nối giữa Gốc và Hàng xóm duy nhất này
        Vector2Int exitFromBase = GetCenterEdgeTile(chosenDir);
        Vector2Int entryToNeighbor = GetCenterEdgeTile(-chosenDir);

        // Chunk Gốc: Thêm CHỈ MỘT lối ra
        baseChunk.exitPoints.Add(exitFromBase);

        // Hàng xóm: Đặt lối vào trỏ ngược lại Gốc
        firstNeighbor.entryPoint = entryToNeighbor;

        // Thêm hàng xóm vào thế giới
        worldChunks.Add(neighborCoord, firstNeighbor);

        // Thêm hàng xóm duy nhất vào hàng đợi để bắt đầu mở rộng
        queue.Enqueue(firstNeighbor);

        // ========================================
        // GIAI ĐOẠN 2: LẤP ĐẦY THẾ GIỚI (BFS)
        // ========================================

        while (queue.Count > 0)
        {
            ChunkData currentChunk = queue.Dequeue();

            // Tìm tất cả các hướng mở rộng hợp lệ
            List<Vector2Int> validDirections = new List<Vector2Int>();

            foreach (var dir in allDirections)
            {
                Vector2Int nextCoord = currentChunk.chunkCoord + dir;

                // Kiểm tra biên
                if (nextCoord.x < settings.minCoord || nextCoord.x > settings.maxCoord ||
                    nextCoord.y < settings.minCoord || nextCoord.y > settings.maxCoord)
                    continue;

                // Kiểm tra đã tồn tại chưa
                if (worldChunks.ContainsKey(nextCoord))
                    continue;

                validDirections.Add(dir);
            }

            // CHIẾN THUẬT RẮN THÔNG MINH: Luôn cố gắng đi tiếp, uốn cong khi bị chặn
            List<Vector2Int> selectedDirections = new List<Vector2Int>();

            if (validDirections.Count > 0)
            {
                // Bước 1: Tính hướng "tiến" (dựa trên hướng vào)
                Vector2Int forwardDir = GetForwardDirection(currentChunk.entryPoint);

                // Bước 2: Chọn hướng CHÍNH (tiếp tục đường đi chính)
                Vector2Int primaryDir;

                if (validDirections.Contains(forwardDir))
                {
                    // Ưu tiên đi thẳng (rắn đi tiếp)
                    primaryDir = forwardDir;
                }
                else
                {
                    // Bị chặn phía trước - PHẢI chọn hướng khác (rắn uốn cong)
                    // Xáo trộn và chọn cái đầu tiên để đảm bảo ngẫu nhiên
                    primaryDir = validDirections.OrderBy(x => Random.value).First();
                }

                selectedDirections.Add(primaryDir);

                // Bước 3: Cân nhắc RẼ NHÁNH (đường phụ)
                // Loại bỏ hướng chính khỏi danh sách hợp lệ
                List<Vector2Int> remainingDirections = validDirections.Where(d => d != primaryDir).ToList();

                if (remainingDirections.Count > 0 && Random.value < settings.branchRate)
                {
                    // Tạo nhánh với xác suất thấp
                    Vector2Int branchDir = remainingDirections.OrderBy(x => Random.value).First();
                    selectedDirections.Add(branchDir);
                }

                // Đảm bảo: Không bao giờ quá 2 hướng (Tối đa hình chữ T)
            }

            // Tạo chunk theo các hướng đã chọn
            foreach (var dir in selectedDirections)
            {
                Vector2Int newCoord = currentChunk.chunkCoord + dir;
                ChunkData newChunk = new ChunkData(newCoord);

                // Thiết lập kết nối
                Vector2Int exitFromCurrent = GetCenterEdgeTile(dir);
                Vector2Int entryToNew = GetCenterEdgeTile(-dir);

                currentChunk.exitPoints.Add(exitFromCurrent);
                newChunk.entryPoint = entryToNew;

                worldChunks.Add(newCoord, newChunk);
                queue.Enqueue(newChunk);
            }
        }

        // ========================================
        // GIAI ĐOẠN 3: HẬU XỬ LÝ & VẼ ĐƯỜNG
        // ========================================

        foreach (var chunk in worldChunks.Values)
        {
            // Sửa ngõ cụt: Tạo lối ra giả đối diện với lối vào
            if (chunk.chunkCoord != Vector2Int.zero && chunk.exitPoints.Count == 0)
            {
                Vector2Int fakeExit = GetOppositeEdge(chunk.entryPoint);
                chunk.exitPoints.Add(fakeExit);
            }

            // Vẽ đường đi trên các ô
            GenerateStarPath(chunk);
        }

        // Kiểm tra xem bản đồ có đạt yêu cầu chất lượng không
        bool success = worldChunks.Count >= settings.minChunks;

        return success ? worldChunks : null;
    }

    #region Path Drawing

    /// <summary>
    /// Vẽ đường đi hình sao từ Entry -> Center -> tất cả Exits.
    /// </summary>
    private void GenerateStarPath(ChunkData chunk)
    {
        Vector2Int center = new Vector2Int(4, 4);

        // Lối vào -> Tâm (bỏ qua cho chunk gốc bắt đầu tại tâm)
        if (chunk.chunkCoord != Vector2Int.zero)
        {
            chunk.tiles[chunk.entryPoint.x, chunk.entryPoint.y] = TileType.StartPoint;
            DrawStraightLine(chunk, chunk.entryPoint, center);
        }

        // Tâm -> Tất cả lối ra
        foreach (var exit in chunk.exitPoints)
        {
            DrawStraightLine(chunk, center, exit);
            chunk.tiles[exit.x, exit.y] = TileType.EndPoint;
        }

        // Đảm bảo tâm được đánh dấu là đường đi
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

        // Di chuyển theo trục X trước
        while (current.x != to.x)
        {
            current.x += (int)Mathf.Sign(to.x - current.x);
            SetPathTile(chunk, current);
        }

        // Sau đó di chuyển theo trục Y
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
        // Cho phép đường đi đè lên Đất và Nhà (để đi qua căn cứ)
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
        // Tìm cạnh đối diện với điểm vào
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
        // Lối vào ở dưới -> Hướng tới là LÊN
        if (entryPoint.y == 0) return Vector2Int.up;
        if (entryPoint.y == 8) return Vector2Int.down;
        if (entryPoint.x == 0) return Vector2Int.right;
        if (entryPoint.x == 8) return Vector2Int.left;
        return Vector2Int.up; // Default fallback
    }

    #endregion
}
