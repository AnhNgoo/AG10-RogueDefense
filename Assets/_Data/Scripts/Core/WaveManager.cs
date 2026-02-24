using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// Cấu hình cho từng loại quái (Type, Unlock Wave, Weight, HP Multiplier).
/// Dùng để spawn nhiều loại quái khác nhau trong cùng 1 wave.
/// </summary>
[Serializable]
public struct EnemySpawnConfig
{
    [Tooltip("Loại quái sẽ spawn")]
    [LabelText("Enemy Type")]
    public PoolType enemyType;

    [Tooltip("Từ Wave thứ mấy thì loại quái này bắt đầu xuất hiện?")]
    [LabelText("Unlock at Wave")]
    [Range(1, 50)]
    public int startWave;

    [Tooltip("Tỉ lệ xuất hiện (Trọng số). VD: Basic=10, Fast=3 -> Cứ 13 con thì có 3 con Fast")]
    [LabelText("Spawn Weight")]
    [Range(1, 20)]
    public int weight;

    [Tooltip("Hệ số máu riêng cho loại này (VD: Tank=2.0 -> Máu gấp đôi Basic)")]
    [LabelText("HP Multiplier")]
    [Range(0.5f, 5f)]
    public float hpMultiplier;
}

/// <summary>
/// Wave Manager - "Bộ Não" của hệ thống Wave.
/// SOLID: Single Responsibility - Chỉ tính toán logic wave (số lượng, stats), không spawn.
/// PATTERN: Singleton + Strategy Pattern (công thức scaling có thể thay đổi).
/// KIẾN TRÚC: Tách biệt "Tư duy" (WaveManager) và "Hành động" (EnemySpawner).
/// </summary>
public class WaveManager : MonoBehaviour
{
    #region Singleton Pattern (Scene-Only)

    private static WaveManager _instance;
    public static WaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WaveManager>();

                if (_instance == null)
                {
                    Debug.LogError("[WaveManager] Không tìm thấy instance trong scene! Hãy thêm GameObject với WaveManager.");
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Inspector Configuration

    [Title("Wave Calculation Settings", "Công thức tính toán Wave", TitleAlignment = TitleAlignments.Centered)]

    [BoxGroup("Enemy Count Formula")]
    [Tooltip("Số quái cơ bản cho Wave 1")]
    [LabelText("Base Count")]
    [Range(1, 10)]
    public int baseEnemyCount = 3;

    [BoxGroup("Enemy Count Formula")]
    [Tooltip("Hệ số nhân số lượng quái mỗi wave (Formula: base + wave * multiplier)")]
    [LabelText("Count Multiplier")]
    [Range(0.5f, 3f)]
    public float enemyCountMultiplier = 1.2f;

    [BoxGroup("Enemy Stats Formula")]
    [Tooltip("Máu cơ bản của quái ở Wave 1")]
    [LabelText("Base HP")]
    [Range(10f, 500f)]
    public float baseEnemyHP = 100f;

    [BoxGroup("Enemy Stats Formula")]
    [Tooltip("Hệ số nhân máu mỗi wave (Formula: baseHP * multiplier^(wave-1))")]
    [LabelText("HP Multiplier")]
    [Range(1.0f, 2.0f)]
    public float enemyHPMultiplier = 1.15f;

    [Title("Enemy Type Configuration", "Cấu hình các loại quái", TitleAlignment = TitleAlignments.Centered)]
    [BoxGroup("Enemy Types")]
    [Tooltip("Danh sách các loại quái có thể spawn (Unlock theo wave, Weight, HP Multiplier)")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "enemyType")]
    public List<EnemySpawnConfig> enemyConfigs = new List<EnemySpawnConfig>
    {
        new EnemySpawnConfig { enemyType = PoolType.EnemyBasic, startWave = 1, weight = 10, hpMultiplier = 1.0f },
        new EnemySpawnConfig { enemyType = PoolType.EnemyFast, startWave = 3, weight = 3, hpMultiplier = 0.7f },
        new EnemySpawnConfig { enemyType = PoolType.EnemySlow, startWave = 5, weight = 2, hpMultiplier = 1.5f },
        // new EnemySpawnConfig { enemyType = PoolType.EnemyBoss, startWave = 10, weight = 1, hpMultiplier = 3.0f },
    };

    #endregion

    #region Runtime State

    [Title("Runtime Info", TitleAlignment = TitleAlignments.Centered)]
    [BoxGroup("State")]
    [ShowInInspector, ReadOnly]
    [LabelText("Current Wave")]
    public int CurrentWave { get; private set; }

    [BoxGroup("State")]
    [ShowInInspector, ReadOnly]
    [LabelText("Max Waves")]
    public int MaxWaves { get; private set; }

    [BoxGroup("State")]
    [ShowInInspector, ReadOnly]
    [ProgressBar(0, "MaxWaves")]
    [LabelText("Progress")]
    private int WaveProgress => CurrentWave;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton Setup (Scene-only, không DontDestroyOnLoad)
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[WaveManager] Đã có instance khác, hủy duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Khởi tạo hệ thống Wave dựa trên số lượng Hidden Chunks.
    /// DYNAMIC SCALING: Số wave = Số chunk ẩn (mỗi lần expand = 1 wave).
    /// Gọi bởi WorldMapManager sau khi generate map xong.
    /// </summary>
    /// <param name="totalHiddenChunks">Tổng số chunk ẩn trong map</param>
    public void Initialize(int totalHiddenChunks)
    {
        MaxWaves = totalHiddenChunks;
        CurrentWave = 0;

        Debug.Log($"[WaveManager] Initialized - Max Waves: {MaxWaves} (Dynamic based on map size)");
    }

    /// <summary>
    /// Bắt đầu Wave mới - TÍNH TOÁN và GỌI EnemySpawner.
    /// SOLID: Tách biệt "Suy nghĩ" (tính toán) và "Hành động" (spawn).
    /// NÂNG CẤP: Hỗ trợ nhiều loại quái với Weight-Based Spawning.
    /// </summary>
    /// <param name="endChunks">Danh sách các chunk endpoint để spawn quái</param>
    /// <param name="enemySpawner">Reference đến EnemySpawner (từ WorldMapManager)</param>
    public void StartNextWave(List<ChunkData> endChunks, EnemySpawner enemySpawner)
    {
        // Tăng wave index
        CurrentWave++;

        // Kiểm tra điều kiện thắng trận (vượt quá max waves)
        if (CurrentWave > MaxWaves)
        {
            Debug.Log($"[WaveManager] === YOU WIN! === Đã hoàn thành {MaxWaves} waves!");
            // TODO: Gọi GameManager.OnVictory() để hiển thị Victory Screen
            return;
        }

        // Tính số lượng quái (CÔNG THỨC SCALING)
        int enemyCount = CalculateEnemyCount();

        // Tính BASE HP cho wave này (chưa nhân với hpMultiplier của từng loại)
        float baseWaveHP = CalculateEnemyHP();

        // LỌC CÁC LOẠI QUÁI HỢP LỆ (startWave <= CurrentWave)
        List<EnemySpawnConfig> validConfigs = GetValidEnemyConfigs();

        if (validConfigs.Count == 0)
        {
            Debug.LogError($"[WaveManager] Wave {CurrentWave}: Không có loại quái nào hợp lệ! Kiểm tra enemyConfigs.");
            return;
        }

        Debug.Log($"[WaveManager] === WAVE {CurrentWave}/{MaxWaves} ===");
        Debug.Log($"[WaveManager] Enemy Count: {enemyCount}, Base HP: {baseWaveHP:F0}, Valid Types: {validConfigs.Count}");

        // GỌI EnemySpawner để thực thi spawn (COMMAND PATTERN)
        if (enemySpawner != null)
        {
            enemySpawner.ExecuteSpawnWave(endChunks, enemyCount, baseWaveHP, validConfigs);
        }
        else
        {
            Debug.LogError("[WaveManager] EnemySpawner null! Không thể spawn quái.");
        }
    }

    /// <summary>
    /// Lọc ra các loại quái có thể spawn ở wave hiện tại (startWave <= CurrentWave).
    /// </summary>
    private List<EnemySpawnConfig> GetValidEnemyConfigs()
    {
        List<EnemySpawnConfig> validConfigs = new List<EnemySpawnConfig>();

        foreach (var config in enemyConfigs)
        {
            if (config.startWave <= CurrentWave)
            {
                validConfigs.Add(config);
            }
        }

        return validConfigs;
    }

    #endregion

    #region Wave Calculation Logic

    /// <summary>
    /// Tính số lượng quái cho wave hiện tại.
    /// CÔNG THỨC: EnemyCount = BaseCount + (CurrentWave * Multiplier)
    /// VÍ DỤ: Base=3, Multiplier=1.2
    ///   - Wave 1: 3 + (1 * 1.2) = 4.2 → 4 quái
    ///   - Wave 5: 3 + (5 * 1.2) = 9 quái
    ///   - Wave 10: 3 + (10 * 1.2) = 15 quái
    /// </summary>
    private int CalculateEnemyCount()
    {
        float rawCount = baseEnemyCount + (CurrentWave * enemyCountMultiplier);
        int finalCount = Mathf.RoundToInt(rawCount);

        // Đảm bảo tối thiểu 1 quái
        return Mathf.Max(finalCount, 1);
    }

    /// <summary>
    /// Tính máu quái cho wave hiện tại.
    /// CÔNG THỨC: EnemyHP = BaseHP * (Multiplier ^ (CurrentWave - 1))
    /// VÍ DỤ: BaseHP=100, Multiplier=1.15
    ///   - Wave 1: 100 * (1.15^0) = 100 HP
    ///   - Wave 5: 100 * (1.15^4) = 174.9 HP
    ///   - Wave 10: 100 * (1.15^9) = 355.8 HP
    /// SCALING: Exponential growth để tăng độ khó nhanh hơn Linear.
    /// </summary>
    private float CalculateEnemyHP()
    {
        // Công thức mũ: HP tăng theo cấp số nhân
        float hp = baseEnemyHP * Mathf.Pow(enemyHPMultiplier, CurrentWave - 1);

        // Làm tròn đến 1 chữ số thập phân
        return Mathf.Round(hp * 10f) / 10f;
    }

    #endregion

    #region Debug Tools

#if UNITY_EDITOR
    [Title("Debug Tools")]
    [Button(ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
    private void TestCalculateWave1()
    {
        CurrentWave = 1;
        Debug.Log($"Wave 1: Count={CalculateEnemyCount()}, HP={CalculateEnemyHP():F1}");
    }

    [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
    private void TestCalculateWave5()
    {
        CurrentWave = 5;
        Debug.Log($"Wave 5: Count={CalculateEnemyCount()}, HP={CalculateEnemyHP():F1}");
    }

    [Button(ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.4f)]
    private void TestCalculateWave10()
    {
        CurrentWave = 10;
        Debug.Log($"Wave 10: Count={CalculateEnemyCount()}, HP={CalculateEnemyHP():F1}");
    }

    [Button(ButtonSizes.Large), GUIColor(1f, 0.5f, 0.5f)]
    private void ResetToWave1()
    {
        CurrentWave = 0;
        Debug.Log("[WaveManager] Reset về Wave 0");
    }
#endif

    #endregion
}
