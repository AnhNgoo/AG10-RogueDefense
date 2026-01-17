using System.Collections.Generic;
using UnityEngine;

public class WorldExpander
{
    private MapManager _mapManager;
    private ChunkPathGenerator _chunkGen;

    private const int CHUNK_SIZE = WorldConfig.CHUNK_SIZE;

    public WorldExpander(MapManager mapManager)
    {
        _mapManager = mapManager;
        _chunkGen = new ChunkPathGenerator(_mapManager);
    }

    /// <summary>
    /// Hàm mở rộng thế giới. Trả về null nếu hướng đó đã bị chặn bởi chunk khác.
    /// </summary>
    public ChunkGeneratorResults ExpandFromExit(PathNode exitNode)
    {
        // 1. Tính Tọa độ Logic mới (Chunk Coordinate)
        Vector2Int newChunkCoord = exitNode.ChunkCoord + exitNode.Direction;

        // 2. Tính Tọa độ Thực (World Bounds) dựa trên ChunkCoord
        Vector2Int chunkCenter = newChunkCoord * WorldConfig.CHUNK_SIZE;

        Vector2Int chunkMin = chunkCenter - new Vector2Int(WorldConfig.HALF_SIZE, WorldConfig.HALF_SIZE);
        Vector2Int chunkMax = chunkMin + new Vector2Int(WorldConfig.CHUNK_SIZE - 1, WorldConfig.CHUNK_SIZE - 1);

        // 3. Xác định EntryNode (Ngay cạnh ExitNode)
        // EntryNode này sẽ thuộc về Chunk Mới -> Truyền newChunkCoord vào nó
        Vector2Int entryPos = exitNode.Position + exitNode.Direction;
        PathNode entryNode = new PathNode(entryPos, exitNode.Direction, newChunkCoord);

        Debug.Log($"[WorldExpander] Expanding to {newChunkCoord}. Bounds: {chunkMin}-{chunkMax}");

        return _chunkGen.GenerateCompleteChunk(entryNode, newChunkCoord, chunkMin, chunkMax);
    }
}
