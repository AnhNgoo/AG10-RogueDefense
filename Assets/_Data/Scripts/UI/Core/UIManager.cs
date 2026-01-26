using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

/// <summary>
/// Singleton Manager quản lý tất cả UI Menu trong game.
/// Tự động tìm và register tất cả MenuBase khi Start.
/// Hỗ trợ cả Normal Menu (đóng menu cũ) và Popup (stack lên trên).
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton Pattern

    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("[UIManager]");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Inspector Fields

    [Title("UI Manager", "Quản lý tất cả Menu trong game", TitleAlignment = TitleAlignments.Centered)]

    [ShowInInspector, ReadOnly]
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    private Dictionary<MenuType, MenuBase> _menuRegistry = new Dictionary<MenuType, MenuBase>();

    [ShowInInspector, ReadOnly]
    [ListDrawerSettings(ShowFoldout = true)]
    private List<MenuType> _popupStack = new List<MenuType>();

    #endregion

    #region Private Fields

    private MenuType _currentMenu = MenuType.None;
    private Coroutine _resumeCoroutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton Setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Đảm bảo GameObject này là Root Object (tách khỏi parent nếu có)
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện khi Scene mới được load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Hủy đăng ký sự kiện khi object bị disable/destroy
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RegisterAllMenus();
    }

    private void Update()
    {
        // Xử lý Android Back Button
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }

    #endregion

    #region Scene Event Handler

    /// <summary>
    /// Callback khi Scene mới được load xong.
    /// Xóa tất cả tham chiếu cũ và đăng ký lại các Menu trong Scene mới.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[UIManager] Scene '{scene.name}' đã load. Đang refresh menu registry...");

        // Xóa tất cả tham chiếu cũ (tránh MissingReferenceException)
        _menuRegistry.Clear();
        _popupStack.Clear();
        _currentMenu = MenuType.None;

        // Đăng ký lại các Menu trong Scene mới
        RegisterAllMenus();

        // Tự động mở InGameHUD nếu vào Game scene
        if (scene.name == "Game")
        {
            if (_menuRegistry.TryGetValue(MenuType.InGameHUD, out MenuBase hud))
            {
                hud.Open();
                _currentMenu = MenuType.InGameHUD;
                Debug.Log("[UIManager] Đã tự động mở InGameHUD trong Game scene.");
            }
            else
            {
                Debug.LogWarning("[UIManager] Không tìm thấy InGameHUD trong Game scene!");
            }
        }
    }

    #endregion

    #region Menu Registration

    /// <summary>
    /// Tự động tìm và đăng ký tất cả MenuBase trong scene
    /// </summary>
    [Button(ButtonSizes.Large), PropertyOrder(-1)]
    [GUIColor(0.3f, 0.8f, 0.3f)]
    private void RegisterAllMenus()
    {
        _menuRegistry.Clear();

        // Tìm tất cả MenuBase trong scene (bao gồm cả inactive)
        MenuBase[] allMenus = FindObjectsOfType<MenuBase>(true);

        foreach (MenuBase menu in allMenus)
        {
            if (menu.Type != MenuType.None)
            {
                _menuRegistry[menu.Type] = menu;

                // Đóng tất cả menu ban đầu (OnSceneLoaded sẽ xử lý việc mở menu phù hợp)
                menu.gameObject.SetActive(false);
            }
        }

        // Mở menu mặc định cho scene hiện tại
        if (_menuRegistry.ContainsKey(MenuType.MainMenu))
        {
            // Nếu có MainMenu -> Mở nó (Scene MainMenu)
            _menuRegistry[MenuType.MainMenu].Open();
            _currentMenu = MenuType.MainMenu;
        }
        else if (_menuRegistry.ContainsKey(MenuType.InGameHUD))
        {
            // Nếu không có MainMenu nhưng có InGameHUD -> Mở nó (Scene Game)
            _menuRegistry[MenuType.InGameHUD].Open();
            _currentMenu = MenuType.InGameHUD;
        }

        Debug.Log($"[UIManager] Registered {_menuRegistry.Count} menus: {string.Join(", ", _menuRegistry.Keys)}");
    }

    #endregion

    #region Public API - Normal Menu

    /// <summary>
    /// Mở Menu mới (đóng menu cũ nếu có)
    /// Dùng cho các menu chính như MainMenu, InGameHUD
    /// </summary>
    public void OpenMenu(MenuType menuType)
    {
        if (menuType == MenuType.None)
        {
            Debug.LogWarning("[UIManager] Không thể mở MenuType.None");
            return;
        }

        if (!_menuRegistry.TryGetValue(menuType, out MenuBase menu))
        {
            Debug.LogError($"[UIManager] Menu {menuType} chưa được register!");
            return;
        }

        // Đóng menu hiện tại
        if (_currentMenu != MenuType.None && _menuRegistry.TryGetValue(_currentMenu, out MenuBase currentMenu))
        {
            currentMenu.CloseInternal(); // GỌI INTERNAL để tránh vòng lặp
        }

        // Đóng tất cả popup
        CloseAllPopups();

        // Mở menu mới
        menu.Open();
        _currentMenu = menuType;

        Debug.Log($"[UIManager] OpenMenu: {menuType}");
    }

    /// <summary>
    /// Đóng Menu (được gọi từ MenuBase.Close)
    /// </summary>
    public void CloseMenu(MenuBase menu)
    {
        if (menu == null) return;

        // Check xem menu này có phải popup không
        if (_popupStack.Contains(menu.Type))
        {
            ClosePopup(menu.Type);
        }
        else
        {
            // Đóng menu thường
            menu.CloseInternal();

            if (_currentMenu == menu.Type)
            {
                _currentMenu = MenuType.None;
            }
        }
    }

    #endregion

    #region Public API - Popup

    /// <summary>
    /// Mở Popup (chồng lên menu hiện tại, KHÔNG đóng menu cũ)
    /// Dùng cho Settings, Pause Menu
    /// </summary>
    public void OpenPopup(MenuType menuType)
    {
        if (menuType == MenuType.None)
        {
            Debug.LogWarning("[UIManager] Không thể mở Popup MenuType.None");
            return;
        }

        if (!_menuRegistry.TryGetValue(menuType, out MenuBase menu))
        {
            Debug.LogError($"[UIManager] Popup {menuType} chưa được register!");
            return;
        }

        // Nếu popup đã mở rồi, không mở lại
        if (_popupStack.Contains(menuType))
        {
            Debug.LogWarning($"[UIManager] Popup {menuType} đã mở rồi!");
            return;
        }

        menu.Open();
        _popupStack.Add(menuType);

        Debug.Log($"[UIManager] OpenPopup: {menuType} (Stack: {_popupStack.Count})");
    }

    /// <summary>
    /// Đóng Popup cụ thể
    /// </summary>
    public void ClosePopup(MenuType menuType)
    {
        if (!_popupStack.Contains(menuType))
        {
            Debug.LogWarning($"[UIManager] Popup {menuType} không có trong stack!");
            return;
        }

        if (_menuRegistry.TryGetValue(menuType, out MenuBase menu))
        {
            menu.CloseInternal(); // GỌI INTERNAL để tránh vòng lặp
        }

        _popupStack.Remove(menuType);

        Debug.Log($"[UIManager] ClosePopup: {menuType} (Stack: {_popupStack.Count})");
    }

    /// <summary>
    /// Đóng popup cuối cùng trong stack (dùng cho Back Button)
    /// </summary>
    public void CloseTopPopup()
    {
        if (_popupStack.Count == 0)
        {
            return;
        }

        MenuType topPopup = _popupStack[_popupStack.Count - 1];
        ClosePopup(topPopup);
    }

    /// <summary>
    /// Đóng tất cả Popup
    /// </summary>
    public void CloseAllPopups()
    {
        for (int i = _popupStack.Count - 1; i >= 0; i--)
        {
            MenuType menuType = _popupStack[i];

            if (_menuRegistry.TryGetValue(menuType, out MenuBase menu))
            {
                menu.CloseInternal(); // GỌI INTERNAL để tránh vòng lặp
            }
        }

        _popupStack.Clear();

        Debug.Log("[UIManager] Đã đóng tất cả popup.");
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Xử lý nút Back (Android)
    /// </summary>
    private void HandleBackButton()
    {
        // Ưu tiên đóng Popup trước
        if (_popupStack.Count > 0)
        {
            CloseTopPopup();
            return;
        }

        // Nếu không có popup, check menu hiện tại
        if (_currentMenu != MenuType.None && _menuRegistry.TryGetValue(_currentMenu, out MenuBase menu))
        {
            // Nếu menu cho phép đóng bằng Back Button
            // (Để tránh đóng MainMenu, có thể check thêm điều kiện)
            Debug.Log($"[UIManager] Back Button pressed on {_currentMenu}");
        }
    }

    /// <summary>
    /// Lấy Menu theo MenuType
    /// </summary>
    public MenuBase GetMenu(MenuType menuType)
    {
        if (_menuRegistry.TryGetValue(menuType, out MenuBase menu))
        {
            return menu;
        }

        return null;
    }

    /// <summary>
    /// Check xem Menu có đang mở không
    /// </summary>
    public bool IsMenuOpen(MenuType menuType)
    {
        if (_menuRegistry.TryGetValue(menuType, out MenuBase menu))
        {
            return menu.IsOpen;
        }

        return false;
    }

    #endregion

    #region Game Resume Management

    /// <summary>
    /// Bắt đầu chuỗi Resume Game (Smooth Transition)
    /// Gọi từ SettingsMenu khi đóng popup.
    /// UIManager chạy Coroutine vì luôn sống (DontDestroyOnLoad).
    /// </summary>
    public void ResumeGameSmoothly(float duration)
    {
        // Nếu đang có Coroutine resume thì stop
        if (_resumeCoroutine != null)
        {
            StopCoroutine(_resumeCoroutine);
        }

        // Start Coroutine mới
        _resumeCoroutine = StartCoroutine(SmoothResumeCoroutine(duration));
    }

    /// <summary>
    /// Coroutine: Smooth Resume (Slow Motion Effect)
    /// Tăng dần Time.timeScale từ 0 -> 1 trong duration
    /// </summary>
    private IEnumerator SmoothResumeCoroutine(float duration)
    {
        Debug.Log("[UIManager] Bắt đầu Smooth Resume...");

        float timer = 0f;

        // Vòng lặp tăng dần Time.timeScale từ 0 -> 1
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Sử dụng unscaledDeltaTime vì timeScale = 0

            // Lerp TUYẾN TÍNH từ 0 -> 1
            float t = Mathf.Clamp01(timer / duration);
            Time.timeScale = Mathf.Lerp(0f, 1f, t);

            yield return null; // Chờ frame tiếp theo
        }

        // Đảm bảo set cứng = 1 khi kết thúc (QUAN TRỌNG)
        Time.timeScale = 1f;

        Debug.Log("[UIManager] Smooth Resume hoàn tất (Time.timeScale = 1)");

        _resumeCoroutine = null;
    }

    #endregion
}
