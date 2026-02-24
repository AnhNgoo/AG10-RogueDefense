using UnityEngine;
using TMPro;

/// <summary>
/// Component UI hiển thị Wave hiện tại (WAVE: X/Y).
/// SOLID: Single Responsibility - Chỉ quan tâm Hiển thị, không logic tính toán.
/// PATTERN: Observer - Subscribe vào EnemySpawner.OnWaveIndexChanged để tự động cập nhật.
/// </summary>
public class WaveUI : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("Text hiển thị Wave (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI _waveText;

    [Header("Formatting")]
    [Tooltip("Format text hiển thị (ví dụ: 'WAVE: {0}/{1}' hoặc 'Wave {0} / {1}')")]
    [SerializeField] private string _textFormat = "WAVE: {0}/{1}";

    [Tooltip("Màu text khi ở wave đầu (tutorial)")]
    [SerializeField] private Color _earlyWaveColor = Color.white;

    [Tooltip("Màu text khi ở wave cuối (khó)")]
    [SerializeField] private Color _lateWaveColor = new Color(1f, 0.3f, 0.3f, 1f);

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        // Subscribe vào event của EnemySpawner
        EnemySpawner.OnWaveIndexChanged += UpdateWaveUI;
    }

    private void OnDisable()
    {
        // Unsubscribe để tránh memory leak
        EnemySpawner.OnWaveIndexChanged -= UpdateWaveUI;
    }

    private void Start()
    {
        // Validation
        if (_waveText == null)
        {
            Debug.LogError("[WaveUI] Chưa assign _waveText! Hãy kéo TextMeshProUGUI vào Inspector.");
        }

        // Cập nhật UI ban đầu (hiển thị "WAVE: 1/10")
        UpdateWaveUI(1, 10);
    }

    #endregion

    #region Event Handler (Observer Pattern)

    /// <summary>
    /// Callback được gọi khi EnemySpawner bắn event OnWaveIndexChanged.
    /// </summary>
    /// <param name="currentWave">Wave hiện tại</param>
    /// <param name="maxWaves">Tổng số wave</param>
    private void UpdateWaveUI(int currentWave, int maxWaves)
    {
        if (_waveText == null) return;

        // Cập nhật text theo format
        _waveText.text = string.Format(_textFormat, currentWave, maxWaves);

        // Đổi màu text dựa trên tiến độ wave (tùy chọn)
        UpdateTextColor(currentWave, maxWaves);

        Debug.Log($"[WaveUI] Cập nhật UI: Wave {currentWave}/{maxWaves}");
    }

    #endregion

    #region Visual Feedback

    /// <summary>
    /// Đổi màu text dựa trên tiến độ wave (wave càng cao càng đỏ).
    /// </summary>
    private void UpdateTextColor(int currentWave, int maxWaves)
    {
        if (_waveText == null) return;

        // Tính tỉ lệ tiến độ (0.0 -> 1.0)
        float progress = (float)currentWave / maxWaves;

        // Lerp màu từ trắng (đầu) sang đỏ (cuối)
        _waveText.color = Color.Lerp(_earlyWaveColor, _lateWaveColor, progress);
    }

    #endregion

    #region Context Menu (Debug)

#if UNITY_EDITOR
    [ContextMenu("Test Wave 1/10")]
    private void TestWave1()
    {
        UpdateWaveUI(1, 10);
    }

    [ContextMenu("Test Wave 5/10")]
    private void TestWave5()
    {
        UpdateWaveUI(5, 10);
    }

    [ContextMenu("Test Wave 10/10")]
    private void TestWave10()
    {
        UpdateWaveUI(10, 10);
    }
#endif

    #endregion
}
