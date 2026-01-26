using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

/// <summary>
/// Main Menu - Menu chính khi vào game.
/// Sử dụng UniTask để load Scene async.
/// </summary>
public class MainMenu : MenuBase
{
    #region Override Properties

    public override MenuType Type => MenuType.MainMenu;

    #endregion

    #region Inspector Fields

    [Title("UI References")]
    [Required]
    [SerializeField] private Button _playButton;

    [Required]
    [SerializeField] private Button _optionsButton;

    [Required]
    [SerializeField] private Button _quitButton;

    [Space(10)]
    [Title("Scene Settings")]
    [SceneObjectsOnly]
    [SerializeField] private string _gameSceneName = "Game";

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Bind Button Events
        if (_playButton != null)
        {
            _playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        if (_optionsButton != null)
        {
            _optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Phát nhạc menu khi mở
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(SoundType.MenuMusic);
        }
    }

    private void OnDestroy()
    {
        // Unbind Events
        if (_playButton != null)
        {
            _playButton.onClick.RemoveListener(OnPlayButtonClicked);
        }

        if (_optionsButton != null)
        {
            _optionsButton.onClick.RemoveListener(OnOptionsButtonClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Xử lý nút Play - Load Scene Game bằng UniTask
    /// </summary>
    private void OnPlayButtonClicked()
    {
        // Play SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        Debug.Log("[MainMenu] Play Button Clicked");

        // Load Scene async bằng UniTask
        LoadGameSceneAsync().Forget();
    }

    /// <summary>
    /// Xử lý nút Options - Mở Settings Popup
    /// </summary>
    private void OnOptionsButtonClicked()
    {
        // Play SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        Debug.Log("[MainMenu] Options Button Clicked");

        // Mở Settings Popup
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPopup(MenuType.Settings);
        }
    }

    /// <summary>
    /// Xử lý nút Quit - Thoát game
    /// </summary>
    private void OnQuitButtonClicked()
    {
        // Play SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ButtonClick);
        }

        Debug.Log("[MainMenu] Quit Button Clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Async Scene Loading (UniTask)

    /// <summary>
    /// Load Scene Game bằng UniTask (async/await)
    /// </summary>
    private async UniTaskVoid LoadGameSceneAsync()
    {
        // Disable nút Play để tránh spam click
        if (_playButton != null)
        {
            _playButton.interactable = false;
        }

        Debug.Log($"[MainMenu] Bắt đầu load scene: {_gameSceneName}");

        // QUAN TRỌNG: Fade Out nhạc menu trước khi chuyển scene
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeOutAndStop();
        }

        try
        {
            // Load Scene async
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_gameSceneName);

            // Chờ cho đến khi load xong
            // UniTask hỗ trợ await AsyncOperation trực tiếp
            await asyncLoad;

            Debug.Log($"[MainMenu] Đã load xong scene: {_gameSceneName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MainMenu] Lỗi khi load scene: {ex.Message}");

            // Enable lại nút Play nếu lỗi
            if (_playButton != null)
            {
                _playButton.interactable = true;
            }
        }
    }

    #endregion

    #region Debug (Odin Inspector)

#if UNITY_EDITOR
    [Title("Debug Tools")]

    [Button(ButtonSizes.Medium)]
    [GUIColor(0.3f, 0.8f, 1f)]
    private void DebugPlayButton()
    {
        OnPlayButtonClicked();
    }

    [Button(ButtonSizes.Medium)]
    [GUIColor(0.3f, 1f, 0.3f)]
    private void DebugOptionsButton()
    {
        OnOptionsButtonClicked();
    }
#endif

    #endregion
}
