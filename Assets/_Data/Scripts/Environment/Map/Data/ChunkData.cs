using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// Dữ liệu của một Chunk 9x9 trong World Map.
/// Chứa thông tin về tiles, entry point, exit points.
/// </summary>
[System.Serializable]
public class ChunkData
{
    [BoxGroup("Chunk Info")]
    [ReadOnly, Tooltip("Tọa độ Chunk trong World Grid")]
    public Vector2Int chunkCoord;

    [BoxGroup("Tile Data")]
    [TableMatrix(SquareCells = true, HideColumnIndices = true, HideRowIndices = true)]
    [Tooltip("Grid 9x9 chứa các loại Tile")]
    public TileType[,] tiles = new TileType[9, 9];

    [BoxGroup("Path Connections")]
    [ReadOnly, Tooltip("Điểm vào của Chunk (nơi đường đi từ Chunk khác đến)")]
    public Vector2Int entryPoint;

    [BoxGroup("Path Connections")]
    [ReadOnly, Tooltip("Danh sách các điểm ra (đường đi sang Chunk khác)")]
    public List<Vector2Int> exitPoints = new List<Vector2Int>();

    public ChunkData(Vector2Int coord)
    {
        chunkCoord = coord;
        
        // Khởi tạo tất cả tiles là Ground
        for (int x = 0; x < 9; x++)
        {
            for (int z = 0; z < 9; z++)
            {
                tiles[x, z] = TileType.Ground;
            }
        }
    }
}
