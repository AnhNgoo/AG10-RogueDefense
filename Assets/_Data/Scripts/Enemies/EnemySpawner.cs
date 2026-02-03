using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Quản lý việc spawn quái theo wave (đợt).
/// Hỗ trợ Object Pooling và cơ chế Staggered Spawning (Spawn rải rác).
/// Refactored with proper Odin Inspector grouping and strict Object Pooling.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Dependencies

    // Dependency Injection variables
    private MapPathfinder pathfinder;
    private Dictionary<Vector2Int, ChunkData> worldChunks;
    private List<Vector2Int> visualizedCoords;

    #endregion

    #region Wave Configuration

    [Title("Wave Configuration", "Settings for spawning waves", TitleAlignment = TitleAlignments.Centered)]

    [BoxGroup("Current State")]
    [Tooltip("Wave hiện tại (bắt đầu từ 1)")]
    [LabelText("Current Wave")]
    public int currentWaveIndex = 1;

    [BoxGroup("Spawn Settings")]
    [HorizontalGroup("Spawn Settings/Main")]
    [Range(0.1f, 3f)]
    [SuffixLabel("seconds")]
    [Tooltip("Thời gian delay giữa các lần spawn (giây) - Staggering")]
    public float spawnInterval = 0.5f;

    [HorizontalGroup("Spawn Settings/Main")]
    [Tooltip("Loại Enemy spawn cho Wave này")]
    public PoolType enemyType = PoolType.EnemyBasic;

    [BoxGroup("Balancing")]
    [HorizontalGroup("Balancing/Params")]
    [Tooltip("Hệ số nhân cho số lượng quái (Wave scaling từ Wave 2+)")]
    [Range(1f, 3f)]
    [LabelText("Scaling Multiplier")]
    public float waveScalingMultiplier = 1.5f;

    [HorizontalGroup("Balancing/Params")]
    [Tooltip("Số quái cố định cho Wave 1 (Tutorial)")]
    [Range(1, 5)]
    [LabelText("Wave 1 Count")]
    public int wave1EnemyCount = 1;

    #endregion

    #region Runtime Info

    [Title("Runtime Statistics")]
    [BoxGroup("Stats", CenterLabel = true)]
    [ReadOnly, ShowInInspector]
    [LabelText("Is Spawning")]
    private bool isSpawning = false;

    [BoxGroup("Stats")]
    [HorizontalGroup("Stats/Detail")]
    [ReadOnly, ShowInInspector, LabelWidth(150)]
    private int lastWaveEnemyCount = 0;

    [BoxGroup("Stats")]
    [HorizontalGroup("Stats/Detail")]
    [ReadOnly, ShowInInspector, LabelWidth(150)]
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
    /// </summary>
    [Button(ButtonSizes.Large), PropertyOrder(1)]
    [BoxGroup("Actions", CenterLabel = true)]
    [GUIColor(0.4f, 1f, 0.4f)]
    [DisableInEditorMode]
    public void StartNextWave()
    {
        if (isSpawning)
        {
            Debug.LogWarning("[EnemySpawner] Wave đang chạy! Bỏ qua lệnh spawn mới.");
            return;
        }

        // Validate
        if (GameObject.Find("WorldMapManager")?.GetComponent<ChunkData>() is ChunkData result)
        {
            // Fallback logic if needed, maintained from previous version
        }

        // Tìm điểm spawn
        List<ChunkData> endChunks = GetAllEndChunks();

        if (endChunks.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Không tìm thấy điểm spawn (EndChunk)!");
            return;
        }

        // Tính số lượng quái
        int enemyCount = CalculateEnemyCountForWave(endChunks.Count);

        // Lưu thống kê
        lastWaveEnemyCount = enemyCount;
        lastWaveEndChunks = endChunks.Count;

        Debug.Log($"[EnemySpawner] === WAVE {currentWaveIndex} BẮT ĐẦU === ({enemyCount} quái, {endChunks.Count} cổng spawn)");

        // Bắt đầu Coroutine spawn
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(endChunks, enemyCount));
    }

    /// <summary>
    /// Dừng spawn ngay lập tức.
    /// </summary>
    [Button(ButtonSizes.Medium), PropertyOrder(2)]
    [BoxGroup("Actions")]
    [GUIColor(1f, 0.5f, 0.5f)]
    [DisableInEditorMode]
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
    /// Giữ nguyên công thức tính số lượng quái.
    /// </summary>
    private int CalculateEnemyCountForWave(int activeEndChunksCount)
    {
        // Wave 1: Cố định (Tutorial)
        if (currentWaveIndex == 1)
        {
            return wave1EnemyCount;
        }

        // Wave 2+: Công thức mới (base + wave * scaling)
        float rawCount = 1.5f + (currentWaveIndex * 1.2f);
        int totalCount = Mathf.RoundToInt(rawCount);

        return Mathf.Max(totalCount, 1); // Tối thiểu 1 quái
    }

    /// <summary>
    /// Coroutine spawn quái từ từ (Round Robin Interleave).
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(List<ChunkData> endChunks, int enemyCount)
    {
        isSpawning = true;

        // Tính phân phối công bằng cho từng chunk
        int baseCount = enemyCount / endChunks.Count;
        int remainder = enemyCount % endChunks.Count;

        // Tạo danh sách số lượng cho từng chunk
        List<int> countsPerChunk = new List<int>();
        for (int i = 0; i < endChunks.Count; i++)
        {
            // Chunk đầu tiên nhận thêm từ remainder
            int count = baseCount + (i < remainder ? 1 : 0);
            countsPerChunk.Add(count);
            // Debug.Log($"[EnemySpawner] Chunk {endChunks[i].chunkCoord} will spawn {count} enemies.");
        }

        // Tạo spawn queue với Interleave (Xen Kẽ)
        List<ChunkData> spawnQueue = new List<ChunkData>();
        int maxCount = baseCount + (remainder > 0 ? 1 : 0); // Số round tối đa

        for (int round = 0; round < maxCount; round++)
        {
            for (int i = 0; i < endChunks.Count; i++)
            {
                // Nếu chunk này còn quái ở round hiện tại
                if (round < countsPerChunk[i])
                {
                    spawnQueue.Add(endChunks[i]);
                }
            }
        }

        // Debug.Log($"[EnemySpawner] Spawn Queue (Interleaved): {string.Join(", ", spawnQueue.ConvertAll(c => c.chunkCoord.ToString()))}");

        // Spawn từng quái theo thứ tự trong queue
        int spawnedCount = 0;
        foreach (ChunkData spawnChunk in spawnQueue)
        {
            SpawnEnemyAtChunk(spawnChunk);
            spawnedCount++;

            // Delay trước khi spawn con tiếp theo
            yield return new WaitForSeconds(spawnInterval);
        }

        // Wave hoàn thành ->
        isSpawning = false;
        currentWaveIndex++; // Tăng wave cho đợt sau
        Debug.Log($"[EnemySpawner] === WAVE {currentWaveIndex - 1} HOÀN THÀNH ===");
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
            return;
        }

        // Spawn Enemy từ Object Pool Manager (Use specific method for Queue based pool if needed, but standard Spawn() is fine)
        Vector3 spawnPosition = pathToHome[0];
        GameObject enemyObj = ObjectPoolManager.Instance.Spawn(enemyType, spawnPosition, Quaternion.identity);

        if (enemyObj != null)
        {
            EnemyBase enemyScript = enemyObj.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                enemyScript.Setup(pathToHome);
            }
        }
        else
        {
            Debug.LogError($"[EnemySpawner] Failed to spawn enemy!");
        }
    }

    /// <summary>
    /// Tìm tất cả các "End Chunks" (chunks có exit trỏ ra vùng chưa mở).
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

    #region Helper

    private Color GetWaveProgressColor(float value)
    {
        return Color.Lerp(Color.green, Color.red, Mathf.Clamp01(currentWaveIndex / 10f));
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    #endregion
}
