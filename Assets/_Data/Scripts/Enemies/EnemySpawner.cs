using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// SERVICE: Quản lý Spawn Enemy với Wave System + Object Pooling (Enum-Based).
/// 
/// PRODUCTION FEATURES:
/// - Wave-based spawning (công thức cân bằng gameplay)
/// - Staggered spawning (spawn rải rác với Coroutine)
/// - Object Pooling integration (Enum-Based)
/// - Balancing formula (Tutorial Wave 1, scaling từ Wave 2+)
/// 
/// WAVE FORMULA:
/// - Wave 1: 1 con (Tutorial)
/// - Wave 2+: Mathf.CeilToInt(waveIndex * 1.5f) + activeEndChunks.Count
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Dependencies

    private MapPathfinder pathfinder;
    private Dictionary<Vector2Int, ChunkData> worldChunks;
    private List<Vector2Int> visualizedCoords;

    #endregion

    #region Wave Configuration

    [TabGroup("Tabs", "Wave"), BoxGroup("Tabs/Wave/Settings")]
    [Tooltip("Wave hiện tại (bắt đầu từ 1)")]
    [ReadOnly, ShowInInspector]
    public int currentWaveIndex = 1;

    [TabGroup("Tabs", "Wave"), BoxGroup("Tabs/Wave/Settings")]
    [Tooltip("Thời gian delay giữa các lần spawn (giây) - Staggering")]
    [Range(0.1f, 3f)]
    public float spawnInterval = 0.5f;

    [TabGroup("Tabs", "Wave"), BoxGroup("Tabs/Wave/Enemy Types")]
    [Tooltip("Loại Enemy spawn cho Wave này (có thể mở rộng thành list)")]
    public PoolType enemyType = PoolType.EnemyBasic;

    [TabGroup("Tabs", "Wave"), BoxGroup("Tabs/Wave/Balancing")]
    [Tooltip("Hệ số nhân cho số lượng quái (Wave scaling từ Wave 2+)")]
    [Range(1f, 3f)]
    public float waveScalingMultiplier = 1.5f;

    [TabGroup("Tabs", "Wave"), BoxGroup("Tabs/Wave/Balancing")]
    [Tooltip("Số quái cố định cho Wave 1 (Tutorial)")]
    [Range(1, 5)]
    public int wave1EnemyCount = 1;

    #endregion

    #region Runtime Info

    [TabGroup("Tabs", "Info"), ReadOnly, ShowInInspector]
    private bool isSpawning = false;

    [TabGroup("Tabs", "Info"), ReadOnly, ShowInInspector]
    private int lastWaveEnemyCount = 0;

    [TabGroup("Tabs", "Info"), ReadOnly, ShowInInspector]
    private int lastWaveEndChunks = 0;

    private Coroutine spawnCoroutine;

    #endregion

    #region Initialization

    /// <summary>
    /// Khởi tạo EnemySpawner với dependencies (Dependency Injection).
    /// </summary>
    public void Initialize(
        MapPathfinder pathfinder,
        Dictionary<Vector2Int, ChunkData> worldChunks,
        List<Vector2Int> visualizedCoords)
    {
        this.pathfinder = pathfinder;
        this.worldChunks = worldChunks;
        this.visualizedCoords = visualizedCoords;

        Debug.Log("[EnemySpawner] ✓ Initialized successfully.");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Bắt đầu Wave mới (gọi từ WorldMapManager khi Expand chunk).
    /// 
    /// GAMEPLAY FLOW: 
    /// Expand Chunk -> StartNextWave -> Spawn enemies dần dần (Staggering) -> Wave Complete.
    /// </summary>
    public void StartNextWave()
    {
        if (isSpawning)
        {
            Debug.LogWarning("[EnemySpawner] Wave đang spawn! Hủy bỏ yêu cầu mới.");
            return;
        }

        // Validation: Kiểm tra ObjectPoolManager đã sẵn sàng chưa
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] ObjectPoolManager chưa khởi tạo! Cannot spawn wave.");
            return;
        }

        // Tìm các End Chunks (điểm spawn hợp lệ)
        List<ChunkData> endChunks = GetAllEndChunks();

        if (endChunks.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No end chunks found! Cannot spawn wave.");
            return;
        }

        // Tính số lượng quái cho Wave này
        int enemyCount = CalculateEnemyCountForWave(endChunks.Count);

        // Lưu thống kê
        lastWaveEnemyCount = enemyCount;
        lastWaveEndChunks = endChunks.Count;

        Debug.Log($"[EnemySpawner] === WAVE {currentWaveIndex} START === ({enemyCount} enemies, {endChunks.Count} spawn points)");

        // Bắt đầu Coroutine spawn (Staggered)
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(endChunks, enemyCount));
    }

    /// <summary>
    /// Dừng spawn ngay lập tức (dùng khi reset game hoặc Game Over).
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        isSpawning = false;
        Debug.Log("[EnemySpawner] Spawn stopped.");
    }

    #endregion

    #region Wave Logic

    /// <summary>
    /// Công thức tính số lượng quái cho Wave hiện tại.
    /// 
    /// BALANCING:
    /// - Wave 1: Cố định (Tutorial) - 1 con để người chơi làm quen.
    /// - Wave 2+: Tăng dần theo công thức: waveIndex * 1.5 + số cửa ra.
    /// 
    /// Ví dụ:
    /// - Wave 1: 1 con
    /// - Wave 2: 2 * 1.5 + 2 = 5 con
    /// - Wave 3: 3 * 1.5 + 3 = 8 con
    /// - Wave 5: 5 * 1.5 + 4 = 12 con
    /// </summary>
    private int CalculateEnemyCountForWave(int activeEndChunksCount)
    {
        // Wave 1: Cố định (Tutorial)
        if (currentWaveIndex == 1)
        {
            return wave1EnemyCount;
        }

        // Wave 2+: Scaling formula
        int scaledCount = Mathf.CeilToInt(currentWaveIndex * waveScalingMultiplier);
        int totalCount = scaledCount + activeEndChunksCount;

        return Mathf.Max(totalCount, 1); // Tối thiểu 1 quái
    }

    /// <summary>
    /// Coroutine: Spawn enemies rải rác (Staggered Spawning).
    /// 
    /// GAMEPLAY BENEFIT:
    /// - Quái đi thành đoàn dài thay vì dính chùm -> Dễ chơi hơn.
    /// - Giảm lag spike (không spawn cùng lúc 50 con).
    /// - Tạo cảm giác wave liên tục, không có khoảng trống.
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(List<ChunkData> endChunks, int enemyCount)
    {
        isSpawning = true;

        // Xáo trộn thứ tự spawn points (tránh spawn luôn ở cùng 1 chỗ)
        List<ChunkData> shuffledEndChunks = endChunks.OrderBy(x => Random.value).ToList();

        int spawnedCount = 0;
        int endChunkIndex = 0;

        // Spawn từng quái một với delay (Staggering)
        while (spawnedCount < enemyCount)
        {
            // Lấy End Chunk để spawn (Round-robin qua tất cả end chunks)
            ChunkData spawnChunk = shuffledEndChunks[endChunkIndex % shuffledEndChunks.Count];

            // Spawn 1 enemy tại chunk này
            SpawnEnemyAtChunk(spawnChunk);

            spawnedCount++;
            endChunkIndex++;

            // Delay trước khi spawn con tiếp theo
            yield return new WaitForSeconds(spawnInterval);
        }

        // Wave hoàn thành -> Tăng Wave Index
        currentWaveIndex++;
        isSpawning = false;

        Debug.Log($"[EnemySpawner] === WAVE {currentWaveIndex - 1} COMPLETE === (Spawned: {spawnedCount})");
    }

    #endregion

    #region Spawn Methods

    /// <summary>
    /// Spawn 1 Enemy tại MỘT ExitPoint ngẫu nhiên của chunk.
    /// Dùng Object Pool Manager (Enum-Based).
    /// </summary>
    private void SpawnEnemyAtChunk(ChunkData chunk)
    {
        // Kiểm tra chunk có exitPoints không
        if (chunk.exitPoints == null || chunk.exitPoints.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] Chunk {chunk.chunkCoord} không có ExitPoint!");
            return;
        }

        // Lọc ra các ExitPoints CHƯA KẾT NỐI (trỏ ra vùng chưa mở)
        List<Vector2Int> validExits = new List<Vector2Int>();

        foreach (Vector2Int exitTile in chunk.exitPoints)
        {
            Vector2Int direction = pathfinder.GetDirectionFromEdgeTile(exitTile);
            Vector2Int neighborCoord = chunk.chunkCoord + direction;

            // Chỉ spawn tại exit chưa connect với chunk đã mở
            if (!visualizedCoords.Contains(neighborCoord))
            {
                validExits.Add(exitTile);
            }
        }

        // Nếu không có exit hợp lệ -> Bỏ qua
        if (validExits.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] Chunk {chunk.chunkCoord} không có exit hợp lệ!");
            return;
        }

        // Chọn ngẫu nhiên 1 exit từ các exit hợp lệ
        Vector2Int selectedExit = validExits[Random.Range(0, validExits.Count)];

        // Tính path từ exit này về Home (0,0)
        List<Vector3> pathToHome = pathfinder.CalculatePathToHome(chunk, selectedExit);

        if (pathToHome == null || pathToHome.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] No path to Home from exit {selectedExit} in chunk {chunk.chunkCoord}");
            return;
        }

        // Spawn Enemy từ Object Pool Manager (Enum-Based)
        Vector3 spawnPosition = pathToHome[0];
        GameObject enemyObj = ObjectPoolManager.Instance.Spawn(enemyType, spawnPosition, Quaternion.identity);

        if (enemyObj == null)
        {
            Debug.LogError($"[EnemySpawner] Failed to spawn enemy! Pool '{enemyType}' might be full or not configured.");
            return;
        }

        // Setup Enemy với path
        EnemyBase enemyAI = enemyObj.GetComponent<EnemyBase>();
        if (enemyAI != null)
        {
            enemyAI.Setup(pathToHome);
        }
        else
        {
            Debug.LogError($"[EnemySpawner] Spawned object không có component EnemyBase! Returning to pool.");
            ObjectPoolManager.Instance.ReturnToPool(enemyObj);
        }
    }

    /// <summary>
    /// Tìm tất cả các "End Chunks" (chunks có exit trỏ ra vùng chưa mở).
    /// Đây là các điểm spawn enemy hợp lệ theo luật Tower Defense.
    /// </summary>
    private List<ChunkData> GetAllEndChunks()
    {
        List<ChunkData> endChunks = new List<ChunkData>();

        foreach (Vector2Int visCoord in visualizedCoords)
        {
            if (!worldChunks.TryGetValue(visCoord, out ChunkData visChunk)) continue;

            bool hasUnconnectedExit = false;

            foreach (Vector2Int exitPoint in visChunk.exitPoints)
            {
                Vector2Int direction = pathfinder.GetDirectionFromEdgeTile(exitPoint);
                Vector2Int neighborCoord = visCoord + direction;

                if (!visualizedCoords.Contains(neighborCoord))
                {
                    hasUnconnectedExit = true;
                    break;
                }
            }

            if (hasUnconnectedExit)
            {
                endChunks.Add(visChunk);
            }
        }

        return endChunks;
    }

    #endregion

    #region Debug

    [TabGroup("Tabs", "Debug"), Button("Force Start Next Wave", ButtonSizes.Large), GUIColor(1f, 0.5f, 0.5f)]
    private void DebugForceStartWave()
    {
        StartNextWave();
    }

    [TabGroup("Tabs", "Debug"), Button("Stop Current Wave", ButtonSizes.Medium)]
    private void DebugStopWave()
    {
        StopSpawning();
    }

    private void OnDestroy()
    {
        // Cleanup khi destroy object
        StopSpawning();
    }

    #endregion
}
