using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// InGame HUD - Quản lý UI chức năng trong game.
/// Chức năng:
/// - Toggle Build Menu: Mở/đóng menu chọn tháp (Mobile-friendly UX).
/// - Delegation: Gọi TowerPlacementManager để xử lý logic đặt tháp.
/// - Audio/UI Integration: Kết nối với AudioManager và UIManager.
/// </summary>
public class InGameHUD : MenuBase
{
    #region Override Properties

    public override MenuType Type => MenuType.InGameHUD;

    #endregion

    #region Inspector Fields

    [Title("Core UI References")]
    [BoxGroup("Core UI")]
    [Required]
    [SerializeField] private Button _pauseButton;

    [Title("Build System UI")]
    [BoxGroup("Build Menu")]
    [Required]
    [Tooltip("Nút chính để mở/đóng Build Menu (Hình cây búa)")]
    [SerializeField] private Button _mainBuildButton;

    [BoxGroup("Build Menu")]
    [Required]
    [Tooltip("Panel chứa 4 nút tháp + nút Cancel (Mặc định ẨN)")]
    [SerializeField] private GameObject _buildMenuPanel;

    [BoxGroup("Build Menu")]
    [Required]
    [Tooltip("Nút Cancel trong Build Menu (Đóng menu mà không chọn tháp)")]
    [SerializeField] private Button _buildCancelButton;

    [Title("Tower Selection Buttons")]
    [BoxGroup("Tower Buttons")]
    [Required]
    [Tooltip("Nút chọn Tower Fire (Nấm Đỏ)")]
    [SerializeField] private Button _towerFireButton;

    [BoxGroup("Tower Buttons")]
    [Required]
    [Tooltip("Nút chọn Tower Water (Thông Máy)")]
    [SerializeField] private Button _towerWaterButton;

    [BoxGroup("Tower Buttons")]
    [Required]
    [Tooltip("Nút chọn Tower Earth (Pháo Đài)")]
    [SerializeField] private Button _towerEarthButton;

    [BoxGroup("Tower Buttons")]
    [Required]
    [Tooltip("Nút chọn Tower Wind (Cối Tròn)")]
    [SerializeField] private Button _towerWindButton;

    // Có thể thêm các UI khác như: Wave Counter, Gold, Health, etc.

    #endregion

    #region Private State

    private bool _isBuildMenuOpen = false;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Bind Core UI
        if (_pauseButton != null)
        {
            _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        // Bind Build Menu System
        if (_mainBuildButton != null)
        {
            _mainBuildButton.onClick.AddListener(OnMainBuildButtonClicked);
        }

        if (_buildCancelButton != null)
        {
            _buildCancelButton.onClick.AddListener(OnBuildCancelButtonClicked);
        }

        // Bind Tower Selection Buttons
        if (_towerFireButton != null)
        {
            _towerFireButton.onClick.AddListener(() => OnTowerButtonClicked(PoolType.TowerFire));
        }

        if (_towerWaterButton != null)
        {
            _towerWaterButton.onClick.AddListener(() => OnTowerButtonClicked(PoolType.TowerWater));
        }

        if (_towerEarthButton != null)
        {
            _towerEarthButton.onClick.AddListener(() => OnTowerButtonClicked(PoolType.TowerEarth));
        }

        if (_towerWindButton != null)
        {
            _towerWindButton.onClick.AddListener(() => OnTowerButtonClicked(PoolType.TowerWind));
        }

        // Khởi tạo UI State (mặc định ẩn Build Menu Panel)
        if (_buildMenuPanel != null)
        {
            _buildMenuPanel.SetActive(false);
            _isBuildMenuOpen = false;
        }
    }

    private void Start()
    {
        // Phát nhạc gameplay khi vào game (gọi ở Start để đảm bảo AudioManager đã khởi tạo)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(SoundType.GameplayMusic);
        }

        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // Unbind Core UI
        if (_pauseButton != null)
        {
            _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        }

        // Unbind Build Menu
        if (_mainBuildButton != null)
        {
            _mainBuildButton.onClick.RemoveAllListeners();
        }

        if (_buildCancelButton != null)
        {
            _buildCancelButton.onClick.RemoveAllListeners();
        }

        // Unbind Tower Buttons
        if (_towerFireButton != null)
        {
            _towerFireButton.onClick.RemoveAllListeners();
        }

        if (_towerWaterButton != null)
        {
            _towerWaterButton.onClick.RemoveAllListeners();
        }

        if (_towerEarthButton != null)
        {
            _towerEarthButton.onClick.RemoveAllListeners();
        }

        if (_towerWindButton != null)
        {
            _towerWindButton.onClick.RemoveAllListeners();
        }
    }

    #endregion
    #region Button Handlers

    /// <summary>
    /// Xử lý nút Pause - Mở Settings menu.
    /// </summary>
    private void OnPauseButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPopup(MenuType.Settings);
        }
    }

    /// <summary>
    /// Xử lý nút Build chính - Toggle Build Menu (mở/đóng).
    /// </summary>
    private void OnMainBuildButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        _isBuildMenuOpen = !_isBuildMenuOpen;

        if (_buildMenuPanel != null)
        {
            _buildMenuPanel.SetActive(_isBuildMenuOpen);
        }
    }

    /// <summary>
    /// Xử lý nút Cancel - Hủy chế độ xây tháp và đóng Build Menu.
    /// </summary>
    private void OnBuildCancelButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        // Hủy placement mode (xóa Ghost nếu đang active)
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.CancelPlacement();
        }

        // Đóng Build Menu
        _isBuildMenuOpen = false;

        if (_buildMenuPanel != null)
        {
            _buildMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Xử lý khi bấm nút chọn tháp - Bắt đầu placement mode.
    /// UX V3: KHÔNG đóng Build Menu khi chọn tháp (giữ menu mở để chọn tháp khác nhanh).
    /// </summary>
    private void OnTowerButtonClicked(PoolType towerType)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        // Gọi TowerPlacementManager để bắt đầu placement mode
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.SelectTower(towerType);
        }
        else
        {
            Debug.LogError("[InGameHUD] TowerPlacementManager không tồn tại!");
        }
    }

    #endregion

    #region Debug (Odin Inspector)

#if UNITY_EDITOR
    [Title("Debug Tools")]

    [Button(ButtonSizes.Medium)]
    [GUIColor(0.3f, 0.8f, 1f)]
    private void DebugPauseButton()
    {
        OnPauseButtonClicked();
    }
#endif

    #endregion
}
