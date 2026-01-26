using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// In-Game HUD - UI hiển thị khi đang chơi game.
/// Chứa nút Pause để mở Settings Menu.
/// </summary>
public class InGameHUD : MenuBase
{
    #region Override Properties

    public override MenuType Type => MenuType.InGameHUD;

    #endregion

    #region Inspector Fields

    [Title("UI References")]
    [Required]
    [SerializeField] private Button _pauseButton;

    // Có thể thêm các UI khác như: Wave Counter, Gold, Health, etc.

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Bind Pause Button
        if (_pauseButton != null)
        {
            _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
    }

    private void Start()
    {
        // QUAN TRỌNG: Phát nhạc gameplay khi vào game
        // (Gọi ở Start để đảm bảo AudioManager đã khởi tạo xong)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(SoundType.GameplayMusic);
        }

        // Đảm bảo Time.timeScale = 1 khi vào game
        Time.timeScale = 1f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void OnDestroy()
    {
        // Unbind Events
        if (_pauseButton != null)
        {
            _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Xử lý nút Pause - Mở Settings Popup
    /// </summary>
    private void OnPauseButtonClicked()
    {
        // Play SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        Debug.Log("[InGameHUD] Pause Button Clicked");

        // Mở Settings Popup
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPopup(MenuType.Settings);
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
