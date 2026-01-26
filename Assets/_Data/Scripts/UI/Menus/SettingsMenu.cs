using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Settings Menu - Menu cài đặt (Popup).
/// Logic phức tạp nhất: Quản lý Volume, Icon Mute/Unmute, và Smooth Resume.
/// BẮT BUỘC sử dụng Coroutine cho Smooth Resume (Time.timeScale).
/// </summary>
public class SettingsMenu : MenuBase
{
    #region Override Properties

    public override MenuType Type => MenuType.Settings;

    #endregion

    #region Inspector Fields - UI Components

    [Title("Audio Controls - Music")]
    [Required]
    [SerializeField] private Slider _musicSlider;

    [Required]
    [SerializeField] private Image _musicIcon;

    [SerializeField] private TextMeshProUGUI _musicVolumeText;

    [Space(5)]
    [Title("Audio Controls - SFX")]
    [Required]
    [SerializeField] private Slider _sfxSlider;

    [Required]
    [SerializeField] private Image _sfxIcon;

    [SerializeField] private TextMeshProUGUI _sfxVolumeText;

    [Space(10)]
    [Title("Icon Sprites")]
    [InfoBox("Tách riêng icon cho Music và SFX", InfoMessageType.Info)]

    [HorizontalGroup("Music Icons")]
    [PreviewField(50)]
    [LabelText("Music On")]
    [SerializeField] private Sprite _musicOnSprite;

    [HorizontalGroup("Music Icons")]
    [PreviewField(50)]
    [LabelText("Music Off")]
    [SerializeField] private Sprite _musicOffSprite;

    [HorizontalGroup("SFX Icons")]
    [PreviewField(50)]
    [LabelText("SFX On")]
    [SerializeField] private Sprite _sfxOnSprite;

    [HorizontalGroup("SFX Icons")]
    [PreviewField(50)]
    [LabelText("SFX Off")]
    [SerializeField] private Sprite _sfxOffSprite;

    [Space(10)]
    [Title("Buttons")]
    [Required]
    [SerializeField] private Button _closeButton;

    [Space(10)]
    [Title("Smooth Resume Settings")]
    [InfoBox("Logic Coroutine: Tăng dần Time.timeScale từ 0 -> 1 trong duration", InfoMessageType.Info)]
    [Range(0.1f, 2f)]
    [SerializeField] private float _resumeDuration = 0.25f;

    #endregion

    #region Private Fields

    // Cooldown cho SFX Slider (tránh spam âm thanh)
    private float _lastSFXPlayTime = 0f;
    private const float SFX_COOLDOWN = 0.15f; // 150ms

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Bind Slider Events
        if (_musicSlider != null)
        {
            _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // Bind Close Button
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Load current volume và update UI
        LoadCurrentSettings();

        // Nếu đang ở Game Scene -> Pause game cứng
        if (SceneManager.GetActiveScene().name == "Game")
        {
            Time.timeScale = 0f;
            Debug.Log("[SettingsMenu] Game Paused (Time.timeScale = 0)");
        }
    }

    private void OnDestroy()
    {
        // Unbind Events
        if (_musicSlider != null)
        {
            _musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }

    #endregion

    #region Settings Management

    /// <summary>
    /// Load settings hiện tại từ AudioManager
    /// </summary>
    private void LoadCurrentSettings()
    {
        if (AudioManager.Instance == null) return;

        // Music Volume
        float musicVolume = AudioManager.Instance.GetMusicVolume();
        if (_musicSlider != null)
        {
            _musicSlider.SetValueWithoutNotify(musicVolume); // Không trigger event
        }
        UpdateMusicUI(musicVolume);

        // SFX Volume
        float sfxVolume = AudioManager.Instance.GetSFXVolume();
        if (_sfxSlider != null)
        {
            _sfxSlider.SetValueWithoutNotify(sfxVolume); // Không trigger event
        }
        UpdateSFXUI(sfxVolume);
    }

    #endregion

    #region Slider Event Handlers

    /// <summary>
    /// Xử lý khi Music Slider thay đổi
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }

        // Update UI (Icon + Text)
        UpdateMusicUI(value);
    }

    /// <summary>
    /// Xử lý khi SFX Slider thay đổi
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        // Update UI (Icon + Text)
        UpdateSFXUI(value);

        // Play SFX test với COOLDOWN (tránh spam âm thanh khi kéo slider)
        if (AudioManager.Instance != null && value > 0)
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - _lastSFXPlayTime >= SFX_COOLDOWN)
            {
                AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
                _lastSFXPlayTime = currentTime;
            }
        }
    }

    #endregion

    #region UI Update

    /// <summary>
    /// Update Music Icon (Mute/Unmute) và Text
    /// </summary>
    private void UpdateMusicUI(float volume)
    {
        // Update Icon - Dùng sprite riêng cho Music
        if (_musicIcon != null)
        {
            _musicIcon.sprite = volume > 0 ? _musicOnSprite : _musicOffSprite;
        }

        // Update Text (%)
        if (_musicVolumeText != null)
        {
            _musicVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }

    /// <summary>
    /// Update SFX Icon (Mute/Unmute) và Text
    /// </summary>
    private void UpdateSFXUI(float volume)
    {
        // Update Icon - Dùng sprite riêng cho SFX
        if (_sfxIcon != null)
        {
            _sfxIcon.sprite = volume > 0 ? _sfxOnSprite : _sfxOffSprite;
        }

        // Update Text (%)
        if (_sfxVolumeText != null)
        {
            _sfxVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }

    #endregion

    #region Close Logic

    /// <summary>
    /// Override Close để xử lý Resume logic
    /// QUAN TRỌNG: Hàm này GỌI UIManager, KHÔNG tự đóng để tránh stack overflow
    /// </summary>
    public override void Close()
    {
        // Nếu đang ở Game Scene -> Ủy quyền Resume cho UIManager
        // (UIManager luôn sống, Coroutine không bị kill khi menu đóng)
        if (SceneManager.GetActiveScene().name == "Game" && UIManager.Instance != null)
        {
            UIManager.Instance.ResumeGameSmoothly(_resumeDuration);
        }

        // Gọi base.Close() sẽ trigger UIManager.CloseMenu(this)
        // UIManager sẽ gọi CloseInternal() để thực sự đóng menu
        base.Close();
    }

    /// <summary>
    /// Xử lý nút Close
    /// </summary>
    private void OnCloseButtonClicked()
    {
        // Play SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        Debug.Log("[SettingsMenu] Close Button Clicked");

        // Gọi Close() sẽ trigger logic Resume và đóng menu
        Close();
    }

    #endregion

    #region Debug (Odin Inspector)

#if UNITY_EDITOR
    [Title("Debug Tools")]

    [Button(ButtonSizes.Medium)]
    [GUIColor(1f, 0.8f, 0.3f)]
    private void DebugResetTimeScale()
    {
        Time.timeScale = 1f;
        Debug.Log("[SettingsMenu] Time.timeScale đã reset về 1");
    }
#endif

    #endregion
}
