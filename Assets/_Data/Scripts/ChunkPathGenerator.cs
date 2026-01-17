using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class lưu các mối nối nhỏ trong đường đi
/// </summary>
public class PathNode
{
    public Vector2Int Position;
    public Vector2Int Direction;
    public Vector2Int ChunkCoord; // Tọa độ logic của Chunk chứa Node này
    public PathNode(Vector2Int position, Vector2Int direction, Vector2Int chunkCoord)
    {
        Position = position;
        Direction = direction;
        ChunkCoord = chunkCoord;
    }
}

/// <summary>
/// Kết quả trả về sau khi sinh đường cho 1 chunk
/// </summary>
public class ChunkGeneratorResults
{
    public Vector2Int ChunkCoord;
    public Vector2Int ChunkMin;
    public Vector2Int ChunkMax;

    public List<Vector2Int> AllPathPositions = new List<Vector2Int>();
    public List<PathNode> ExitNodes = new List<PathNode>();
}

public class ChunkPathGenerator
{
    private MapManager _mapManager;

    private const int SEGMENT_LONG = WorldConfig.SEGMENT_LONG;
    private const int SEGMENT_SHORT = WorldConfig.SEGMENT_SHORT;
    private const float CHANCE_SPLIT = WorldConfig.CHANCE_SPLIT;
    private const int MAX_STEPS = WorldConfig.MAX_GENERATION_STEPS;

    private List<Vector2Int> _tempPathPositions;

    public ChunkPathGenerator(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// Sinh đường cho 1 chunk đảm bảo có đường ra
    /// </summary>
    public ChunkGeneratorResults GenerateCompleteChunk(PathNode entryNode, Vector2Int chunkCoord, Vector2Int chunkMin, Vector2Int chunkMax)
    {
        ChunkGeneratorResults results = new ChunkGeneratorResults();

        results.ChunkCoord = chunkCoord;
        results.ChunkMin = chunkMin;
        results.ChunkMax = chunkMax;

        _tempPathPositions = new List<Vector2Int>();
        List<PathNode> activeNodes = new List<PathNode>() { entryNode };

        int safetyCounter = WorldConfig.MAX_GENERATION_STEPS;
        bool hasReachedExit = false;

        while (activeNodes.Count > 0 && safetyCounter > 0)
        {
            safetyCounter--;
            List<PathNode> nextGenerationNodes = new List<PathNode>();

            foreach (var node in activeNodes)
            {
                //Check chạm biên --> Exit
                if (IsOnBoundary(node.Position, chunkMin, chunkMax))
                {
                    results.ExitNodes.Add(new PathNode(node.Position, node.Direction, chunkCoord));
                    hasReachedExit = true;
                    continue;
                }

                //Cố sinh đường
                var newSegment = TryGenerateSegment(node, chunkMin, chunkMax, chunkCoord);

                if (newSegment.Count > 0)
                {
                    nextGenerationNodes.AddRange(newSegment);
                }
                else
                {
                    if (activeNodes.Count == 1 && !hasReachedExit)
                    {
                        //Nếu chỉ còn 1 node và chưa có lối thoát, buộc phải tạo lại đoạn ngắn
                        var forcedExit = ForceExitToNearestBoundary(node, chunkMin, chunkMax, chunkCoord);
                        if (forcedExit != null)
                        {
                            results.ExitNodes.Add(forcedExit);
                            hasReachedExit = true;
                        }
                    }
                }
            }
            activeNodes = nextGenerationNodes;

            //Safety net cuối cùng 
            if (!hasReachedExit && activeNodes.Count > 0)
            {
                //Cụt đường còn lại và buộc phải thoát
                var forced = ForceExitToNearestBoundary(activeNodes[0], chunkMin, chunkMax, chunkCoord);
                if (forced != null) results.ExitNodes.Add(forced);
            }
        }
        //Gán toàn bộ Tile vào kết quả
        results.AllPathPositions = new List<Vector2Int>(_tempPathPositions);
        return results;
    }

    //---------------------- Logic sinh đường ----------------------------
    private List<PathNode> TryGenerateSegment(PathNode node, Vector2Int min, Vector2Int max, Vector2Int coord)
    {
        if (Random.value < CHANCE_SPLIT)
        {
            var split = GenerateSplit(node, min, max, coord);
            if (split != null) return split;
        }

        var longS = GenerateStraight(node, SEGMENT_LONG, min, max, coord);
        if (longS != null) return longS;

        var shortS = GenerateStraight(node, SEGMENT_SHORT, min, max, coord);
        if (shortS != null) return shortS;

        var corner = GenerateCorner(node, min, max, coord);
        if (corner != null) return corner;

        return new List<PathNode>();
    }

    private List<PathNode> GenerateStraight(PathNode node, int length, Vector2Int min, Vector2Int max, Vector2Int coord)
    {
        if (!CanBuildPath(node.Position, node.Direction, length, min, max))
            return null;
        Vector2Int p = node.Position;
        for (int i = 0; i < length; i++)
        {
            p += node.Direction;
            SetPathTile(p);
        }

        return new List<PathNode>() { new PathNode(p, node.Direction, coord) };
    }

    private List<PathNode> GenerateCorner(PathNode node, Vector2Int min, Vector2Int max, Vector2Int coord)
    {
        int pre = 2, post = 3;
        Vector2Int dir = node.Direction;
        Vector2Int turnDir = (Random.value > 0.5f) ? RotateLeft(dir) : RotateRight(dir);

        if (!CanBuildPath(node.Position, dir, pre, min, max)) return null;
        Vector2Int cornerPos = node.Position + dir * pre;
        if (!CanBuildPath(cornerPos, turnDir, post, min, max)) return null;

        Vector2Int p = node.Position;
        for (int i = 0; i < pre; i++) { p += dir; SetPathTile(p); }
        for (int i = 0; i < post; i++) { p += turnDir; SetPathTile(p); }
        return new List<PathNode>() { new PathNode(p, turnDir, coord) };
    }

    private List<PathNode> GenerateSplit(PathNode node, Vector2Int min, Vector2Int max, Vector2Int coord)
    {
        int len = 4;
        Vector2Int dir = node.Direction;
        Vector2Int splitP = node.Position + dir;

        if (!IsPosValid(splitP, min, max) || IsBlocked(splitP)) return null;

        Vector2Int leftDir = RotateLeft(dir);
        Vector2Int rightDir = RotateRight(dir);
        if (!CanBuildPath(splitP, leftDir, len, min, max)) return null;
        if (!CanBuildPath(splitP, rightDir, len, min, max)) return null;
        SetPathTile(splitP);
        Vector2Int lPos = splitP;
        Vector2Int rPos = splitP;
        for (int i = 0; i < len; i++)
        {
            lPos += leftDir;
            SetPathTile(lPos);
            rPos += rightDir;
            SetPathTile(rPos);
        }
        return new List<PathNode>()
        {
            new PathNode(lPos, leftDir, coord),
            new PathNode(rPos, rightDir, coord)
        };
    }

    //---------------------- Các hàm bổ trợ ----------------------------
    /// <summary>
    /// Đặt Tile thành Path
    /// </summary>
    /// <param name="Pos">Vị trí của Tile</param>
    private void SetPathTile(Vector2Int Pos)
    {
        //Set data vào map
        _mapManager.GetOrCreateTile(Pos).SetPath();
        //Lưu vị trí đã tạo đường
        if (!_tempPathPositions.Contains(Pos))
            _tempPathPositions.Add(Pos);
    }

    /// <summary>
    /// Tìm hướng ngắn nhất ra biên để ủi đường
    /// </summary>
    private PathNode ForceExitToNearestBoundary(PathNode node, Vector2Int min, Vector2Int max, Vector2Int coord)
    {
        Vector2Int current = node.Position;

        Vector2Int bestDir = GetDirectionToNearestBoundary(current, min, max);

        int panicLimit = 20;
        while (panicLimit > 0)
        {
            current += bestDir;
            // Đã ra khỏi biên -> Lùi lại lấy điểm biên
            if (!IsPosValid(current, min, max))
                return new PathNode(current - bestDir, bestDir, coord);
            SetPathTile(current);
            if (IsOnBoundary(current, min, max))
                return new PathNode(current, bestDir, coord);
            panicLimit--;
        }
        return null;
    }

    /// <summary>
    /// Tính toán hướng nào gần tường nhất
    /// </summary>
    private Vector2Int GetDirectionToNearestBoundary(Vector2Int pos, Vector2Int min, Vector2Int max)
    {
        int distLeft = pos.x - min.x; // Khoảng cách tới tường trái (West)
        int distRight = max.x - pos.x;  // Khoảng cách tới tường phải (East)
        int distDown = pos.y - min.y;   // Khoảng cách tới tường dưới (South)
        int distUp = max.y - pos.y; // Khoảng cách tới tường trên (North)

        int minDist = Mathf.Min(distLeft, distRight, distDown, distUp);

        if (minDist == distUp) return Vector2Int.up;
        if (minDist == distRight) return Vector2Int.right;
        if (minDist == distDown) return Vector2Int.down;
        return Vector2Int.left;
    }

    //---------------------- Các hàm tiện ích ----------------------------
    /// <summary>
    /// Kiểm tra xem có nằm trong chunk không
    /// </summary>
    private bool IsPosValid(Vector2Int pos, Vector2Int min, Vector2Int max)
    {
        return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
    }
    /// <summary>
    /// Kiểm tra xem đã chạm biên chưa
    /// </summary>
    private bool IsOnBoundary(Vector2Int pos, Vector2Int min, Vector2Int max)
    {
        return pos.x == min.x || pos.x == max.x || pos.y == min.y || pos.y == max.y;
    }
    /// <summary>
    /// Kiểm tra xem ô này có bị cấm xây không
    /// </summary>
    private bool IsBlocked(Vector2Int pos)
    {
        TileData t = _mapManager.GetOrCreateTile(pos);
        // Chặn nếu là Path cũ hoặc có công trình
        return t.GroundType == GroundType.Path || t.StructureType != StructureType.None;
    }
    /// <summary>
    /// Có nên vẽ đường này không
    /// </summary>
    private bool CanBuildPath(Vector2Int startPos, Vector2Int direction, int length, Vector2Int min, Vector2Int max)
    {
        Vector2Int p = startPos;
        for (int i = 0; i < length; i++)
        {
            p += direction;
            if (!IsPosValid(p, min, max) || IsBlocked(p))
                return false;
        }
        return true;
    }
    private Vector2Int RotateLeft(Vector2Int dir)
    {
        return new Vector2Int(-dir.y, dir.x);
    }
    private Vector2Int RotateRight(Vector2Int dir)
    {
        return new Vector2Int(dir.y, -dir.x);
    }
}