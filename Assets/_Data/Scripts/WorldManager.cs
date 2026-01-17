using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    //Các thành phầnh con
    private MapManager _mapManager;
    private HomeGenerator _homeGenerator;
    private MainPathGenerator _mainPathGenerator;
    private WorldExpander _worldExpander;
    private BaseTerrainGenerator _terrainGenerator;

    //Dữ liệu quản lý thế giới
    private Dictionary<Vector2Int, ChunkData> _chunks = new Dictionary<Vector2Int, ChunkData>();

    //// Danh sách các điểm chờ mở rộng (Exit Nodes)
    private List<PathNode> _activeExitNodes = new List<PathNode>();

    private const int CHUNK_SIZE = WorldConfig.CHUNK_SIZE;

    void Start()
    {
        InitializeWorld();
    }

    void Update()
    {
        // Test expand bằng phím Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExpandWorld();
        }
    }

    private void InitializeWorld()
    {
        _mapManager = new MapManager();
        _homeGenerator = new HomeGenerator(_mapManager);
        _mainPathGenerator = new MainPathGenerator(_mapManager, _homeGenerator);
        _worldExpander = new WorldExpander(_mapManager);
        _terrainGenerator = new BaseTerrainGenerator(_mapManager);

        //Sinh nhà chính chunk(0,0)
        _homeGenerator.GenerateHome();

        // Đăng ký Chunk (0,0) - Home
        Vector2Int homeMin = new Vector2Int(-WorldConfig.HALF_SIZE, -WorldConfig.HALF_SIZE);
        Vector2Int homeMax = homeMin + new Vector2Int(WorldConfig.CHUNK_SIZE - 1, WorldConfig.CHUNK_SIZE - 1);

        RegisterChunk(Vector2Int.zero, homeMin, homeMax);
        // Lấp đầy Chunk (0,0)
        _terrainGenerator.GenerateBaseTerrainForChunk(homeMin, homeMax);

        // 2. Sinh đường chính
        _mainPathGenerator.GeneratePath();

        // 3. Lấy Exit Node đầu tiên từ đường chính
        if (_mainPathGenerator.PathPositions.Count > 0)
        {
            Vector2Int lastPos = _mainPathGenerator.PathPositions[_mainPathGenerator.PathPositions.Count - 1];
            Vector2Int dir = GetDirectionVector(_homeGenerator.SelectedExitDirection);

            // QUAN TRỌNG: Node này thuộc Chunk (0,0)
            _activeExitNodes.Add(new PathNode(lastPos, dir, Vector2Int.zero));
        }
    }

    private void ExpandWorld()
    {
        if (_activeExitNodes.Count == 0) return;

        PathNode exitNode = _activeExitNodes[0];
        _activeExitNodes.RemoveAt(0);

        // Tính Chunk mới sẽ là gì
        Vector2Int nextChunkCoord = exitNode.ChunkCoord + exitNode.Direction;

        // KIỂM TRA TRÙNG CHUNK
        if (_chunks.ContainsKey(nextChunkCoord))
        {
            Debug.LogWarning($"Chunk {nextChunkCoord} đã tồn tại! Cần cơ chế nối đường (WorldPathConnector).");
            return;
        }

        // Gọi mở rộng
        var result = _worldExpander.ExpandFromExit(exitNode);

        if (result != null)
        {
            // Đăng ký chunk mới
            RegisterChunk(result.ChunkCoord, result.ChunkMin, result.ChunkMax);

            // Thêm exit nodes
            _activeExitNodes.AddRange(result.ExitNodes);

            // Lấp đầy đất CHỈ CHO CHUNK MỚI
            _terrainGenerator.GenerateBaseTerrainForChunk(result.ChunkMin, result.ChunkMax);
        }
    }

    private void RegisterChunk(Vector2Int coord, Vector2Int min, Vector2Int max)
    {
        ChunkData newChunk = new ChunkData(coord, min, max);
        _chunks.Add(coord, newChunk);
    }
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
