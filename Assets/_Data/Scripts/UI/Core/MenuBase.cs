using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Base Class cho tất cả Menu trong game.
/// Kế thừa SerializedMonoBehaviour của Odin Inspector.
/// Chỉ dùng SetActive đơn giản, KHÔNG dùng animation phức tạp để đảm bảo nút luôn hoạt động.
/// </summary>
public abstract class MenuBase : SerializedMonoBehaviour
{
    #region Abstract Properties

    /// <summary>
    /// Loại Menu - override trong class con
    /// </summary>
    public abstract MenuType Type { get; }

    #endregion

    #region Inspector Fields

    [Title("Menu Base Settings")]
    [InfoBox("Menu này sẽ tự động được UIManager quản lý", InfoMessageType.Info)]
    [ShowInInspector, ReadOnly]
    private MenuType _menuType => Type;

    [Space(10)]
    [Tooltip("Menu có đóng khi nhấn nút Back (Android)?")]
    [SerializeField] protected bool _canCloseWithBackButton = true;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // Override trong class con nếu cần
    }

    protected virtual void OnEnable()
    {
        // Override trong class con nếu cần
    }

    protected virtual void OnDisable()
    {
        // Override trong class con nếu cần
    }

    #endregion

    #region Public API

    /// <summary>
    /// Mở Menu - SetActive(true)
    /// </summary>
    public virtual void Open()
    {
        gameObject.SetActive(true);

        // Play SFX nếu có AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.MenuOpen);
        }

        Debug.Log($"[MenuBase] Opened: {Type}");
    }

    /// <summary>
    /// Đóng Menu - GỌI UIManager để xử lý (tránh stack overflow)
    /// Hàm này CHỈ là Public API, KHÔNG tự tắt gameObject
    /// </summary>
    public virtual void Close()
    {
        // Gọi UIManager để đóng menu
        // UIManager sẽ gọi CloseInternal() sau khi xử lý logic
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseMenu(this);
        }
        else
        {
            // Fallback nếu không có UIManager (không nên xảy ra)
            CloseInternal();
        }
    }

    /// <summary>
    /// INTERNAL: Thực sự đóng menu (chỉ UIManager gọi)
    /// QUAN TRỌNG: Hàm này được gọi bởi UIManager, KHÔNG gọi trực tiếp!
    /// </summary>
    public void CloseInternal()
    {
        gameObject.SetActive(false);

        // Play SFX nếu có AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.MenuClose);
        }

        Debug.Log($"[MenuBase] Closed (Internal): {Type}");
    }

    /// <summary>
    /// Check xem Menu có đang mở không
    /// </summary>
    public bool IsOpen => gameObject.activeSelf;

    #endregion
}
