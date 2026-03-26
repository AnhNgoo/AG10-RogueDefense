using UnityEngine;
using System;

/// <summary>
/// Singleton Manager quản lý tiền tệ trong trận (In-match Gold).
/// SOLID: Single Responsibility - Chỉ quản lý logic số liệu, không can thiệp UI.
/// PATTERN: Observer Pattern - Dùng Event để thông báo thay đổi cho UI.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    #region Singleton Pattern (Scene-Only)

    private static CurrencyManager _instance;
    public static CurrencyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CurrencyManager>();

                if (_instance == null)
                {
                    Debug.LogError("[CurrencyManager] Không tìm thấy instance trong scene! Hãy thêm GameObject với CurrencyManager.");
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Configuration

    [Header("Starting Money")]
    [Tooltip("Số vàng ban đầu khi bắt đầu trận đấu")]
    [SerializeField] private int _startingGold = 150;

    #endregion

    #region State

    [SerializeField] private int _currentGold;

    /// <summary>
    /// Property chỉ đọc - Lấy số vàng hiện tại.
    /// </summary>
    public int CurrentGold => _currentGold;

    #endregion

    #region Observer Pattern - Event

    /// <summary>
    /// Event tĩnh được bắn ra mỗi khi số vàng thay đổi.
    /// Parameter: Số vàng MỚI sau khi thay đổi.
    /// OBSERVER PATTERN: UI sẽ subscribe event này để tự động cập nhật.
    /// </summary>
    public static event Action<int> OnGoldChanged;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton Setup (Scene-only, không DontDestroyOnLoad)
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[CurrencyManager] Đã có instance khác, hủy duplicate.");
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
    /// Khởi tạo hệ thống tiền tệ với số vàng ban đầu.
    /// Gọi ở Start() để đảm bảo UI đã subscribe event trước.
    /// </summary>
    public void Initialize()
    {
        _currentGold = _startingGold;

        // Bắn event để UI cập nhật giá trị ban đầu
        OnGoldChanged?.Invoke(_currentGold);

        Debug.Log($"[CurrencyManager] Initialized - Starting Gold: {_startingGold}");
    }

    /// <summary>
    /// Thêm vàng (khi tiêu diệt enemies, hoàn thành wave, v.v.).
    /// </summary>
    /// <param name="amount">Số vàng cộng thêm (phải dương)</param>
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[CurrencyManager] AddGold với số âm/0: {amount}. Bỏ qua.");
            return;
        }

        _currentGold += amount;

        // Play SFX khi nhận vàng
        AudioManager.Instance?.PlaySFX(SoundType.Coin);

        // Bắn event thông báo thay đổi
        OnGoldChanged?.Invoke(_currentGold);

        Debug.Log($"[CurrencyManager] +{amount} Gold → Total: {_currentGold}");
    }

    /// <summary>
    /// Thử chi tiêu vàng (mua tháp, nâng cấp, v.v.).
    /// </summary>
    /// <param name="amount">Số vàng cần chi tiêu</param>
    /// <returns>True nếu đủ tiền và đã trừ thành công, False nếu không đủ</returns>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[CurrencyManager] TrySpendGold với số âm/0: {amount}. Bỏ qua.");
            return false;
        }

        // Kiểm tra đủ tiền
        if (_currentGold < amount)
        {
            Debug.Log($"[CurrencyManager] Không đủ vàng! Cần: {amount}, Hiện có: {_currentGold}");
            return false;
        }

        // Trừ tiền
        _currentGold -= amount;

        // Bắn event thông báo thay đổi
        OnGoldChanged?.Invoke(_currentGold);

        Debug.Log($"[CurrencyManager] -{amount} Gold → Remaining: {_currentGold}");
        return true;
    }

    /// <summary>
    /// Kiểm tra có đủ tiền hay không (không trừ tiền).
    /// Dùng để enable/disable UI button.
    /// </summary>
    public bool CanAfford(int amount)
    {
        return _currentGold >= amount;
    }

    #endregion

    #region Debug Tools (Odin hoặc Default Button)

#if UNITY_EDITOR
    [ContextMenu("Add 100 Gold")]
    private void DebugAdd100Gold()
    {
        AddGold(100);
    }

    [ContextMenu("Spend 50 Gold")]
    private void DebugSpend50Gold()
    {
        TrySpendGold(50);
    }

    [ContextMenu("Reset to Starting Gold")]
    private void DebugReset()
    {
        Initialize();
    }
#endif

    #endregion
}
