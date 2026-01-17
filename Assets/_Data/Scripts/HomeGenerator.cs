using UnityEngine;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// Enum định nghĩa hướng đầu vào của Nhà Chính
/// </summary>
public enum ExitDirection
{
    North, //Y+
    East,   //X+
    South,  //Y-
    West    //X-
}
/// <summary>
/// Lớp lưu trữ dữ liệu của Nhà Chính
/// </summary>
/// <return> Dữ liệu của Nhà Chính</return>
public class HomeGenerator
{
    //Chọn vị trí đầu vào cả nhà chính
    public ExitDirection SelectedExitDirection { get; private set; }

    //Map Manager để truy cập và quản lý các Tile
    private MapManager _mapManager;

    /// <summary>
    /// Constructor khởi tạo HomeGenerator
    /// </summary>
    /// <param name="mapManager"> Tham chiếu đến MapManager</param>
    public HomeGenerator(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// Tạo Nhà Chính tại vị trí trung tâm lưới (0,0)
    /// </summary>
    /// <remarks>Nhà Chính chiếm diện tích 3x3 ô lưới, với vị trí trung tâm là (0,0)</remarks>
    public void GenerateHome()
    {
        RandomizeExitDirection();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                //Đặt StructureType là Home cho các Tile trong khu vực Nhà Chính
                TileData tile = _mapManager.GetOrCreateTile(gridPos);

                bool success = tile.PlaceStructure(StructureType.Home);
                if (!success)
                {
                    Debug.LogWarning($"[HomeGenerator] Failed to place Home at {gridPos}");
                }
            }
        }
    }

    /// <summary>
    /// Chọn ngẫu nhiên hướng đầu ra cho Nhà Chính
    /// </summary>
    /// <returns>Hướng đầu ra đã chọn</returns>
    public void RandomizeExitDirection()
    {
        //Lấy tất cả các giá trị của Enum ExitDirection
        //Chọn ngẫu nhiên một hướng
        var values = Enum.GetValues(typeof(ExitDirection));
        SelectedExitDirection = (ExitDirection)values.GetValue(Random.Range(0, values.Length));
    }

    /// <summary>
    /// Lấy vị trí ô lưới của Tile đầu ra dựa trên hướng đã chọn
    /// </summary>
    /// <returns>Vị trí ô lưới của Tile đầu ra</returns>
    public Vector2Int GetExitTilePosition()
    {
        return SelectedExitDirection switch
        {
            ExitDirection.North => new Vector2Int(0, 2),
            ExitDirection.East => new Vector2Int(2, 0),
            ExitDirection.South => new Vector2Int(0, -2),
            ExitDirection.West => new Vector2Int(-2, 0),
            _ => new Vector2Int(0, 2) //Mặc định là hướng Bắc nếu lỗi
        };
    }
}
