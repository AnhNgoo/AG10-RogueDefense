using UnityEngine;

public class BaseTerrainGenerator
{
    private MapManager _mapManager;
    private const float CHANCE_FOREST = WorldConfig.CHANCE_FOREST;
    private const float CHANCE_WATER = WorldConfig.CHANCE_WATER;

    // Chiều cao hiển thị
    private const int TERRAIN_VISUAL_HEIGHT = 1;

    public BaseTerrainGenerator(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// Chỉ lấp đầy địa hình trong phạm vi Chunk được chỉ định
    /// </summary>
    public void GenerateBaseTerrainForChunk(Vector2Int min, Vector2Int max)
    {
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Lấy hoặc tạo tile (mặc định Dirt)
                TileData tile = _mapManager.GetOrCreateTile(pos);

                // Nếu đã có Path hoặc Home -> Bỏ qua
                if (tile.GroundType == GroundType.Path || tile.StructureType == StructureType.Home)
                {
                    continue;
                }

                // Nếu chưa có Surface -> Random phủ Cỏ/Rừng
                if (tile.SurfaceType == SurfaceType.None)
                {
                    float rnd = Random.value;
                    if (rnd < CHANCE_WATER) tile.SetSurface(SurfaceType.Water);
                    else if (rnd < CHANCE_WATER + CHANCE_FOREST) tile.SetSurface(SurfaceType.Forest);
                    else tile.SetSurface(SurfaceType.Grass);
                }
            }
        }
    }
}