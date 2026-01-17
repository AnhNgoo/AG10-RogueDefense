using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý bản đồ và các Tile trong game
/// </summary>
public class MapManager
{
    //Dictionary lưu trữ dữ liệu các Tile theo vị trí lưới
    private Dictionary<Vector2Int, TileData> _tiles;

    public const int CHUNK_MIN = WorldConfig.CHUNK_MIN;
    public const int CHUNK_MAX = WorldConfig.CHUNK_MAX;

    /// <summary>
    /// Constructor khởi tạo MapManager 
    /// </summary>
    public MapManager()
    {
        _tiles = new Dictionary<Vector2Int, TileData>();
    }

    /// <summary>
    /// Kiểm tra xem vị trí lưới có nằm trong giới hạn bản đồ không
    /// </summary>
    public bool IsPositionInBounds(Vector2Int pos)
    {
        if (pos.x < CHUNK_MIN || pos.x > CHUNK_MAX || pos.y < CHUNK_MIN || pos.y > CHUNK_MAX)
            return false;
        return true;
    }

    /// <summary>
    /// Kiểm tra xem có Tile tại vị trí lưới không
    /// </summary>
    /// <param name="pos"> Vị trí lưới cần kiểm tra</param>
    /// <return> True nếu có Tile, False nếu không</return>
    public bool HasTile(Vector2Int pos)
    {
        return _tiles.ContainsKey(pos);
    }

    /// <summary>
    /// Lấy dữ liệu Tile tại vị trí lưới, nếu không có thì tạo mới Tile Dirt mặc định
    /// </summary>
    public TileData GetOrCreateTile(Vector2Int pos)
    {
        if (!IsPositionInBounds(pos)) return null;

        //Nếu đã có Tile rồi thì trả về là nó
        if (_tiles.TryGetValue(pos, out TileData tile))
        {
            return tile;
        }

        //Nếu chưa có thì tạo mới và trả về mặc định là Dirt
        TileData newTile = new TileData(pos);
        _tiles[pos] = newTile;

        return newTile;
    }

    /// <summary>
    /// Đặt dữ liệu Tile vào bản đồ
    /// </summary>
    /// <param name="tile"> Dữ liệu Tile cần đặt vào bản đồ</param>
    public void SetTile(TileData tile)
    {
        if (tile == null)
        {
            Debug.LogWarning("[MapManager] Tile is null");
            return;
        }

        _tiles[tile.GridPosition] = tile;
    }

    /// <summary>
    /// Lấy tất cả các Tile trong bản đồ
    /// </summary>
    /// <return> Tập hợp tất cả TileData trong bản đồ</return>
    public IEnumerable<TileData> GetAllTiles()
    {
        return _tiles.Values;
    }

    /// <summary>
    /// Lấy danh sách tọa độ để tiện xử lý logic
    /// </summary>
    public IEnumerable<Vector2Int> GetAllPositions() => _tiles.Keys;

    /// <summary>
    /// Xóa tất cả các Tile khỏi bản đồ
    /// </summary>
    public void ClearMap()
    {
        _tiles.Clear();
    }
}
