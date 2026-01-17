using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trạng Thái của Chunk
/// </summary>
public enum ChunkState
{
    Hidden,
    Active,
    Complete
}

public class ChunkData
{
    public Vector2Int Coordinates; //Tọa độ logic của Chunk
    public Vector2Int WorldMin; //Tọa độ min của Chunk trên bản đồ
    public Vector2Int WorldMax; //Tọa độ max của Chunk trên bản đồ

    public ChunkState State;

    public List<PathNode> EntryPoints = new List<PathNode>(); //Danh sách các điểm Entry của Chunk
    public List<PathNode> ExitPoints = new List<PathNode>();  //Danh sách các điểm Exit của Chunk

    public ChunkData(Vector2Int coord, Vector2Int min, Vector2Int max)
    {
        Coordinates = coord;
        WorldMin = min;
        WorldMax = max;
        State = ChunkState.Active;
    }
}
