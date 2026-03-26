using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI Manager cho màn hình Thắng/Thua (Game End).
/// SOLID: Single Responsibility - Chỉ quản lý UI Game End, không can thiệp logic game.
/// PATTERN: Observer Pattern - Subscribe vào WaveManager.OnVictory và BaseHealthManager.OnDefeat.
/// DESIGN: Dùng CHUNG 1 Panel cho cả Win/Lose, chỉ thay đổi nội dung.
/// INTEGRATION: Kế thừa MenuBase để UIManager quản lý thống nhất.
/// </summary>
public class GameEndUI : MenuBase
{
    #region MenuBase Override

    /// <summary>
    /// Override Type từ MenuBase - Định danh loại menu này.
    /// </summary>
    public override MenuType Type => MenuType.GameEndMenu;

    #endregion

    #region Inspector Configuration

    [Header("UI References")]
    [Tooltip("Panel chứa toàn bộ UI Game End")]
    [SerializeField] private GameObject _panel;

    [Tooltip("Text hiển thị tiêu đề (VICTORY / DEFEAT)")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [Tooltip("Text hiển thị thống kê wave (Wave X / Y)")]
    [SerializeField] private TextMeshProUGUI _waveStatsText;

    [Header("Visual Settings")]
    [Tooltip("Màu chữ khi thắng")]
    [SerializeField] private Color _winColor = Color.green;

    [Tooltip("Màu chữ khi thua")]
    [SerializeField] private Color _loseColor = Color.red;

    [Header("Scene Management")]
    [Tooltip("Tên Scene Main Menu để quay về")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("Animation (Optional)")]
    [Tooltip("DOTween Animation component để làm hiệu ứng popup")]
    [SerializeField] private DOTweenAnimation _panelAnim;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Awake - Base initialization only.
    /// QUAN TRỌNG: Không subscribe event ở đây nữa!
    /// UIManager sẽ lo việc subscribe (vì UIManager luôn active, GameEndUI có thể inactive).
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[GameEndUI] Awake - GameObject: {gameObject.name}, Active: {gameObject.activeSelf}");
    }

    #endregion

    #region Public API - Called by UIManager

    /// <summary>
    /// PUBLIC: Hiển thị màn hình THUA.
    /// Được gọi bởi UIManager.HandleDefeat() (không phải event trực tiếp).
    /// MEDIATOR PATTERN: UIManager làm trung gian giữa BaseHealthManager và GameEndUI.
    /// </summary>
    public void ShowDefeat()
    {
        Debug.Log("[GameEndUI] ShowDefeat được gọi!");

        // Play SFX thua cuộc
        AudioManager.Instance?.PlaySFX(SoundType.Lose);

        ShowUI(isWin: false);
    }

    /// <summary>
    /// PUBLIC: Hiển thị màn hình THẮNG.
    /// Được gọi bởi UIManager.HandleVictory() (không phải event trực tiếp).
    /// </summary>
    public void ShowVictory()
    {
        Debug.Log("[GameEndUI] ShowVictory được gọi!");

        // Play SFX chiến thắng
        AudioManager.Instance?.PlaySFX(SoundType.Victory);

        ShowUI(isWin: true);
    }

    #endregion

    #region UI Logic

    /// <summary>
    /// Hiển thị UI Game End (CHUNG cho cả Win và Lose).
    /// SOLID: Single Method - Dùng parameter để phân biệt Win/Lose thay vì 2 hàm riêng.
    /// UIManager Integration: Gọi UIManager để mở menu thay vì tự SetActive.
    /// </summary>
    /// <param name="isWin">true = Victory, false = Defeat</param>
    private void ShowUI(bool isWin)
    {
        // Dừng game (Freeze thời gian)
        Time.timeScale = 0f;

        // Gọi UIManager để mở menu này (UIManager sẽ lo việc SetActive)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenMenu(MenuType.GameEndMenu);
        }
        else
        {
            Debug.LogError("[GameEndUI] UIManager null! Không thể mở GameEndMenu.");
            return;
        }

        // Bật Panel con (nếu có cấu trúc nested)
        if (_panel != null)
        {
            _panel.SetActive(true);
        }

        // Cập nhật Title và Màu dựa trên kết quả
        if (_titleText != null)
        {
            if (isWin)
            {
                _titleText.text = "VICTORY!";
                _titleText.color = _winColor;
            }
            else
            {
                _titleText.text = "DEFEAT!";
                _titleText.color = _loseColor;
            }
        }

        // Cập nhật thống kê Wave
        if (_waveStatsText != null)
        {
            UpdateWaveStats(isWin);
        }

        // Chạy Animation Popup (nếu có)
        if (_panelAnim != null)
        {
            _panelAnim.DORestart();
        }

        Debug.Log($"[GameEndUI] Hiển thị màn hình {(isWin ? "VICTORY" : "DEFEAT")}");
    }

    /// <summary>
    /// Cập nhật text thống kê wave.
    /// LOGIC:
    ///   - Nếu THUA: Hiển thị wave hiện tại đang chơi (VD: thua ở Wave 3 → "Wave 3/134")
    ///   - Nếu THẮNG: Hiển thị tổng số waves hoàn thành
    /// </summary>
    private void UpdateWaveStats(bool isWin)
    {
        if (WaveManager.Instance == null)
        {
            _waveStatsText.text = "Wave: N/A";
            return;
        }

        int currentWave = WaveManager.Instance.CurrentWave;
        int maxWaves = WaveManager.Instance.MaxWaves;

        if (isWin)
        {
            // Thắng: Đã hoàn thành tất cả waves
            _waveStatsText.text = $"Waves Completed: {maxWaves}/{maxWaves}";
        }
        else
        {
            // Thua: Hiển thị wave hiện tại (wave đang chơi khi thua)
            // VD: Thua ở Wave 3 → "Wave 3/134"
            _waveStatsText.text = $"Wave {currentWave}/{maxWaves}";
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Nút "Restart Level" - Tải lại scene hiện tại.
    /// PUBLIC: Để gọi từ Button.onClick trong Inspector.
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log("[GameEndUI] Restart Level");

        // CRITICAL: Xóa tất cả enemies và towers trước khi reload scene (fix ghost objects)
        EnemyBase.ClearAllActiveEnemies();
        TowerBase.ClearAllTowers();

        // Dọn dẹp tất cả UI trước khi load scene mới
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseAllMenusAndPopups();
        }

        // Mở khóa thời gian trước khi load scene
        Time.timeScale = 1f;

        // Tải lại scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Nút "Main Menu" - Quay về màn hình chính.
    /// PUBLIC: Để gọi từ Button.onClick trong Inspector.
    /// </summary>
    public void GoToMainMenu()
    {
        Debug.Log($"[GameEndUI] Go to Main Menu: {_mainMenuSceneName}");

        // CRITICAL: Xóa tất cả enemies và towers trước khi load scene mới (fix ghost objects)
        EnemyBase.ClearAllActiveEnemies();
        TowerBase.ClearAllTowers();

        // Dọn dẹp tất cả UI trước khi load scene mới
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseAllMenusAndPopups();
        }

        // Mở khóa thời gian trước khi load scene
        Time.timeScale = 1f;

        // Tải scene Main Menu
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    #endregion

    #region Debug Tools

#if UNITY_EDITOR
    [ContextMenu("Test Show Victory")]
    private void TestShowVictory()
    {
        ShowVictory();
    }

    [ContextMenu("Test Show Defeat")]
    private void TestShowDefeat()
    {
        ShowDefeat();
    }

    [ContextMenu("Reset Time Scale")]
    private void ResetTimeScale()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameEndUI] Reset Time.timeScale = 1");
    }
#endif

    #endregion
}
