using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Quản lý việc spawn quái theo wave (đợt).
/// Hỗ trợ Object Pooling và cơ chế Staggered Spawning (Spawn rải rác).
/// EVENTS: OnWaveStarted, OnWaveCompleted để đồng bộ UI/Gameplay (ví dụ: ẩn Expand Nodes khi đang combat).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Wave Events

    /// <summary>
    /// Event phát khi Wave bắt đầu (quái bắt đầu spawn).
    /// Sử dụng: ChunkExpandNode ẩn Canvas để ngăn player mở map khi đang combat.
    /// </summary>
    public static event Action OnWaveStarted;

    /// <summary>
    /// Event phát khi Wave kết thúc (tất cả quái đã chết hết).
    /// Sử dụng: ChunkExpandNode hiện Canvas trở lại để cho phép mở rộng map.
    /// </summary>
    public static event Action OnWaveCompleted;

    /// <summary>
    /// Event phát khi Wave index thay đổi (cho UI cập nhật hiển thị Wave).
    /// Parameters: currentWave, maxWaves.
    /// OBSERVER PATTERN: WaveUI subscribe vào event này để tự động cập nhật.
    /// </summary>
    public static event Action<int, int> OnWaveIndexChanged;

    /// <summary>
    /// Số lượng enemy đang sống (tăng khi spawn, giảm khi chết).
    /// Dùng để track khi nào tất cả quái chết hết thì bắn OnWaveCompleted.
    /// </summary>
    public static int ActiveEnemies = 0;

    /// <summary>
    /// Trạng thái Wave đang active hay không (true = đang combat, false = đã xong wave).
    /// Dùng để đồng bộ UI Expand Nodes được spawn ra giữa wave.
    /// </summary>
    public static bool IsWaveActive { get; private set; } = false;

    #endregion
    #region Dependencies

    // Dependency Injection variables
    private MapPathfinder pathfinder;
    private Dictionary<Vector2Int, ChunkData> worldChunks;
    private List<Vector2Int> visualizedCoords;

    #endregion

    #region Spawn Configuration

    [Title("Spawn Execution Settings", "EnemySpawner chỉ thực thi spawn, không tính toán", TitleAlignment = TitleAlignments.Centered)]

    [BoxGroup("Spawn Settings")]
    [Range(0.1f, 3f)]
    [SuffixLabel("seconds")]
    [Tooltip("Thời gian delay giữa các lần spawn (giây) - Staggering")]
    public float spawnInterval = 0.5f;

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

    // Flag đánh dấu wave hiện tại đã spawn xong chưa
    private bool hasFinishedSpawning = false;
    private bool hasTriggeredWaveCompleted = false; // Tránh bắn nhiều lần

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

    /// <summary>
    /// Start: Bắn OnWaveCompleted ngay khi game bắt đầu để UI Expand Nodes hiển thị.
    /// Khắc phục vấn đề: Node được sinh ra trước khi có wave nào kết thúc sẽ không hiện Canvas.
    /// </summary>
    private void Start()
    {
        // Bắn event OnWaveCompleted để đánh thức tất cả UI Expand Nodes
        OnWaveCompleted?.Invoke();
    }

    /// <summary>
    /// Kiểm tra khi nào Wave thực sự kết thúc (spawn xong VÀ tất cả enemy chết hết).
    /// </summary>
    private void Update()
    {
        // Chỉ check khi đã spawn xong và chưa bắn OnWaveCompleted
        if (hasFinishedSpawning && !hasTriggeredWaveCompleted)
        {
            // Nếu tất cả enemy đã chết -> Bắn event OnWaveCompleted
            if (ActiveEnemies <= 0)
            {
                int completedWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
                Debug.Log($"[EnemySpawner] === WAVE {completedWave} HOÀN THÀNH === (Tất cả enemy đã chết)");

                // Phát event Wave kết thúc
                OnWaveCompleted?.Invoke();

                // Cập nhật trạng thái Wave: Không còn active
                IsWaveActive = false;

                // Đánh dấu đã bắn event
                hasTriggeredWaveCompleted = true;
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Thực thi spawn wave (KHÔNG tính toán - nhận lệnh từ WaveManager).
    /// SOLID: EnemySpawner chỉ làm "Tay Chân", WaveManager là "Bộ Não".
    /// NÂNG CẤP: Hỗ trợ spawn nhiều loại quái khác nhau trong cùng 1 wave.
    /// </summary>
    /// <param name="endChunks">Danh sách chunk endpoints để spawn</param>
    /// <param name="enemyCount">Số lượng quái (đã tính bởi WaveManager)</param>
    /// <param name="baseWaveHP">Base HP của wave (chưa nhân hpMultiplier)</param>
    /// <param name="validConfigs">Các loại quái có thể spawn (với weight + hpMultiplier)</param>
    public void ExecuteSpawnWave(List<ChunkData> endChunks, int enemyCount, float baseWaveHP, List<EnemySpawnConfig> validConfigs)
    {
        if (isSpawning)
        {
            Debug.LogWarning("[EnemySpawner] Wave đang chạy! Bỏ qua lệnh spawn mới.");
            return;
        }

        if (endChunks == null || endChunks.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Không có endChunks để spawn!");
            return;
        }

        if (validConfigs == null || validConfigs.Count == 0)
        {
            Debug.LogError("[EnemySpawner] Không có loại quái nào hợp lệ để spawn!");
            return;
        }

        // Reset flags cho wave mới
        hasFinishedSpawning = false;
        hasTriggeredWaveCompleted = false;

        // CRITICAL FIX: Reset số lượng enemy đang active về 0
        ActiveEnemies = 0;

        // Lưu thống kê
        lastWaveEnemyCount = enemyCount;
        lastWaveEndChunks = endChunks.Count;

        Debug.Log($"[EnemySpawner] Executing spawn: {enemyCount} enemies, BaseHP={baseWaveHP:F0}, Types={validConfigs.Count}, Gates={endChunks.Count}");

        // BẮN EVENT: Cập nhật Wave Index cho UI (OBSERVER PATTERN)
        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
        int maxWaves = WaveManager.Instance != null ? WaveManager.Instance.MaxWaves : 10;
        OnWaveIndexChanged?.Invoke(currentWave, maxWaves);

        // Bắt đầu Coroutine spawn
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(endChunks, enemyCount, baseWaveHP, validConfigs));
    }

    /// <summary>
    /// Lấy danh sách tất cả End Chunks (để WaveManager có thể gọi).
    /// REFACTORED: Public để WorldMapManager có thể truy cập.
    /// </summary>
    public List<ChunkData> GetAllEndChunks()
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

    #region Wave Execution Logic

    /// <summary>
    /// Coroutine spawn quái từ từ (Round Robin Interleave).
    /// NÂNG CẤP: Chọn loại quái ngẫu nhiên theo weight trước mỗi lần spawn.
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(List<ChunkData> endChunks, int enemyCount, float baseWaveHP, List<EnemySpawnConfig> validConfigs)
    {
        isSpawning = true;

        // Phát event Wave bắt đầu
        OnWaveStarted?.Invoke();

        // Cập nhật trạng thái Wave: Đang active
        IsWaveActive = true;

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
            // CHỌN LOẠI QUÁI NGẪU NHIÊN DỰA TRÊN WEIGHT (Weighted Random Selection)
            EnemySpawnConfig selectedConfig = SelectRandomEnemyByWeight(validConfigs);

            // TÍNH HP THỰC TẾ (Base HP * HP Multiplier của loại quái)
            float finalHP = baseWaveHP * selectedConfig.hpMultiplier;

            // Spawn quái với loại và HP đã chọn
            SpawnEnemyAtChunk(spawnChunk, selectedConfig.enemyType, finalHP);
            spawnedCount++;

            // Delay trước khi spawn con tiếp theo
            yield return new WaitForSeconds(spawnInterval);
        }

        // Wave hoàn thành spawn
        isSpawning = false;

        // Đánh dấu đã spawn xong (Update() sẽ check ActiveEnemies để bắn OnWaveCompleted)
        hasFinishedSpawning = true;

        Debug.Log($"[EnemySpawner] Spawned {spawnedCount}/{enemyCount} enemies. Đang chờ chúng chết hết...");
    }

    #endregion

    #region Spawn Methods

    /// <summary>
    /// Chọn ngẫu nhiên 1 loại quái dựa trên trọng số (Weight-Based Random Selection).
    /// VÍ DỤ: Basic(weight=10) + Fast(weight=3) -> Tổng=13 -> 10/13 cơ hội Basic, 3/13 cơ hội Fast.
    /// </summary>
    private EnemySpawnConfig SelectRandomEnemyByWeight(List<EnemySpawnConfig> configs)
    {
        // Tính tổng weight
        int totalWeight = 0;
        foreach (var config in configs)
        {
            totalWeight += config.weight;
        }

        // Random 1 số từ 0 đến totalWeight
        int randomValue = UnityEngine.Random.Range(0, totalWeight);

        // Tìm config tương ứng
        int cumulativeWeight = 0;
        foreach (var config in configs)
        {
            cumulativeWeight += config.weight;
            if (randomValue < cumulativeWeight)
            {
                return config;
            }
        }

        // Fallback (không bao giờ xảy ra nếu logic đúng)
        return configs[0];
    }

    /// <summary>
    /// Spawn 1 Enemy tại MỘT ExitPoint ngẫu nhiên của chunk.
    /// NÂNG CẤP: Nhận loại quái và HP cụ thể cho từng con.
    /// </summary>
    private void SpawnEnemyAtChunk(ChunkData chunk, PoolType enemyType, float finalHP)
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
        Vector2Int selectedExit = validExits[UnityEngine.Random.Range(0, validExits.Count)];

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
                // ĐẶT MÁU CUSTOM (từ WaveManager) TRƯỚC KHI Setup
                enemyScript.SetCustomHealth(finalHP);

                // Setup path
                enemyScript.Setup(pathToHome);

                // Tăng số lượng enemy đang active
                ActiveEnemies++;

                Debug.Log($"[EnemySpawner] Spawned {enemyType} at {spawnPosition}. HP={finalHP:F0}, ActiveEnemies: {ActiveEnemies}");
            }
        }
        else
        {
            Debug.LogError($"[EnemySpawner] Failed to spawn enemy!");
        }
    }

    #endregion

    #region Helper

    private Color GetWaveProgressColor(float value)
    {
        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
        int maxWaves = WaveManager.Instance != null ? WaveManager.Instance.MaxWaves : 10;
        return Color.Lerp(Color.green, Color.red, Mathf.Clamp01((float)currentWave / maxWaves));
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    #endregion
}
