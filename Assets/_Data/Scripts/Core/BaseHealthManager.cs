using UnityEngine;
using System;

/// <summary>
/// Singleton Manager quản lý máu của Nhà Chính (Base Health).
/// SOLID: Single Responsibility - Chỉ quản lý logic máu, không can thiệp UI.
/// PATTERN: Observer Pattern - Dùng Event để thông báo thay đổi cho UI.
/// </summary>
public class BaseHealthManager : MonoBehaviour
{
    #region Singleton Pattern (Scene-Only)

    private static BaseHealthManager _instance;
    public static BaseHealthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BaseHealthManager>();

                if (_instance == null)
                {
                    Debug.LogError("[BaseHealthManager] Không tìm thấy instance trong scene! Hãy thêm GameObject với BaseHealthManager.");
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Configuration

    [Header("Base Health Settings")]
    [Tooltip("Máu tối đa của Nhà Chính")]
    [SerializeField] private int _maxHealth = 5;

    #endregion

    #region State

    private int _currentHealth;

    /// <summary>
    /// Property chỉ đọc - Lấy máu hiện tại.
    /// </summary>
    public int CurrentHealth => _currentHealth;

    /// <summary>
    /// Property chỉ đọc - Lấy máu tối đa.
    /// </summary>
    public int MaxHealth => _maxHealth;

    #endregion

    #region Observer Pattern - Event

    /// <summary>
    /// Event tĩnh được bắn ra mỗi khi máu Nhà Chính thay đổi.
    /// Parameter: Số máu HIỆN TẠI sau khi thay đổi.
    /// OBSERVER PATTERN: UI sẽ subscribe event này để tự động cập nhật.
    /// </summary>
    public static event Action<int> OnBaseHealthChanged;

    /// <summary>
    /// Event tĩnh được bắn ra khi Nhà Chính hết máu (Game Over - Defeat).
    /// OBSERVER PATTERN: GameEndUI sẽ subscribe event này để hiển thị màn hình thua.
    /// </summary>
    public static event Action OnDefeat;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton Setup (Scene-only, không DontDestroyOnLoad)
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[BaseHealthManager] Đã có instance khác, hủy duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Khởi tạo hệ thống máu với giá trị ban đầu.
    /// Gọi ở Start() để đảm bảo UI đã subscribe event trước.
    /// </summary>
    public void Initialize()
    {
        _currentHealth = _maxHealth;

        // Bắn event để UI cập nhật giá trị ban đầu
        OnBaseHealthChanged?.Invoke(_currentHealth);

        Debug.Log($"[BaseHealthManager] Initialized - Base Health: {_currentHealth}/{_maxHealth}");
    }

    /// <summary>
    /// Nhà Chính nhận sát thương (khi Enemy lọt vào).
    /// </summary>
    /// <param name="damage">Lượng sát thương (mặc định: 1)</param>
    public void TakeDamage(int damage = 1)
    {
        if (damage <= 0)
        {
            Debug.LogWarning($"[BaseHealthManager] TakeDamage với số âm/0: {damage}. Bỏ qua.");
            return;
        }

        // Trừ máu
        _currentHealth -= damage;

        // Giới hạn min = 0
        _currentHealth = Mathf.Max(_currentHealth, 0);

        // Bắn event thông báo thay đổi
        OnBaseHealthChanged?.Invoke(_currentHealth);

        Debug.Log($"[BaseHealthManager] Base nhận {damage} damage → HP còn lại: {_currentHealth}/{_maxHealth}");

        // Kiểm tra Game Over
        if (_currentHealth <= 0)
        {
            HandleGameOver();
        }
    }

    /// <summary>
    /// Hồi máu cho Nhà Chính (tùy chọn - có thể dùng cho Power-up).
    /// </summary>
    /// <param name="amount">Lượng máu hồi</param>
    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[BaseHealthManager] Heal với số âm/0: {amount}. Bỏ qua.");
            return;
        }

        // Cộng máu
        _currentHealth += amount;

        // Giới hạn max
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);

        // Bắn event thông báo thay đổi
        OnBaseHealthChanged?.Invoke(_currentHealth);

        Debug.Log($"[BaseHealthManager] Base hồi {amount} HP → HP hiện tại: {_currentHealth}/{_maxHealth}");
    }

    #endregion

    #region Game Over Logic

    /// <summary>
    /// Xử lý khi Nhà Chính hết máu (Game Over).
    /// </summary>
    private void HandleGameOver()
    {
        Debug.Log("[BaseHealthManager] === GAME OVER === Nhà Chính đã bị phá hủy!");

        // Bắn event để GameEndUI hiển thị màn hình Defeat
        if (OnDefeat != null)
        {
            Debug.Log($"[BaseHealthManager] OnDefeat có {OnDefeat.GetInvocationList().Length} subscribers");
            OnDefeat.Invoke();
        }
        else
        {
            Debug.LogError("[BaseHealthManager] OnDefeat == NULL! Không có ai subscribe event này!");
        }

        // TODO: Dừng spawn wave
        // EnemySpawner.Instance.StopSpawning();
    }

    #endregion

    #region Debug Tools

#if UNITY_EDITOR
    [ContextMenu("Take 1 Damage")]
    private void DebugTakeDamage1()
    {
        TakeDamage(1);
    }

    [ContextMenu("Take 2 Damage")]
    private void DebugTakeDamage2()
    {
        TakeDamage(2);
    }

    [ContextMenu("Heal 1 HP")]
    private void DebugHeal1()
    {
        Heal(1);
    }

    [ContextMenu("Reset to Max Health")]
    private void DebugResetHealth()
    {
        Initialize();
    }

    [ContextMenu("Test Game Over")]
    private void DebugGameOver()
    {
        _currentHealth = 1;
        TakeDamage(1);
    }
#endif

    #endregion
}
