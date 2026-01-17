using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp tạo đường chính từ Nhà Chính ra ngoài bản đồ
/// </summary>
public class MainPathGenerator
{
    private const int INITIAL_PATH_LENGTH = WorldConfig.INITIAL_PATH_LENGTH;

    private MapManager _mapManager;
    private HomeGenerator _homeGenerator;
    public List<Vector2Int> PathPositions { get; private set; }

    /// <summary>
    /// Constructor khởi tạo MainPathGenerator
    /// </summary>
    public MainPathGenerator(MapManager mapManager, HomeGenerator homeGenerator)
    {
        _mapManager = mapManager;
        _homeGenerator = homeGenerator;
        PathPositions = new List<Vector2Int>();
    }

    /// <summary>
    /// Tạo đường chính từ Nhà Chính ra ngoài bản đồ
    /// </summary>
    public void GeneratePath()
    {
        PathPositions.Clear();

        Vector2Int currentPos = _homeGenerator.GetExitTilePosition();
        Vector2Int dir = GetDirectionVector(_homeGenerator.SelectedExitDirection);

        for (int i = 0; i < INITIAL_PATH_LENGTH; i++)
        {
            if (!_mapManager.IsPositionInBounds(currentPos))
                break;

            TileData tile = _mapManager.GetOrCreateTile(currentPos);

            tile.SetPath();

            PathPositions.Add(currentPos);

            currentPos += dir;
        }

    }
    /// <summary>
    /// Lấy vector hướng từ Enum ExitDirection
    /// </summary>
    /// <param name="direction">Hướng thoát ra từ Nhà Chính</param>
    /// <returns>Vector2Int đại diện cho hướng</returns>
    private Vector2Int GetDirectionVector(ExitDirection direction)
    {
        return direction switch
        {
            ExitDirection.North => Vector2Int.up,
            ExitDirection.East => Vector2Int.right,
            ExitDirection.South => Vector2Int.down,
            ExitDirection.West => Vector2Int.left,
            _ => Vector2Int.up,
        };
    }
}
