using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Component quản lý nút mua tháp trong Shop UI.
/// SOLID: Single Responsibility - Chỉ xử lý hiển thị, validation, và tooltip của 1 nút.
/// PATTERN: Observer - Subscribe vào CurrencyManager.OnGoldChanged để tự động khóa/mở nút.
/// CROSS-PLATFORM: Hỗ trợ cả PC (Hover) và Mobile (Hold Touch).
/// </summary>
public class TowerShopButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    #region Inspector Fields

    [Header("Tower Configuration")]
    [Tooltip("Prefab của tháp (để lấy thông tin Name, Description, BuildCost)")]
    [SerializeField] private TowerBase _towerPrefab;

    [Header("UI References")]
    [Tooltip("Component Button của nút này")]
    [SerializeField] private Button _button;

    [Header("Tooltip Configuration")]
    [Tooltip("Panel chứa thông tin tooltip (DÙNG CHUNG cho tất cả các nút)")]
    [SerializeField] private GameObject _tooltipPanel;

    [Tooltip("Text hiển thị tên tháp trong tooltip")]
    [SerializeField] private TextMeshProUGUI _nameText;

    [Tooltip("Text hiển thị mô tả tháp trong tooltip")]
    [SerializeField] private TextMeshProUGUI _descText;

    [Tooltip("Text hiển thị giá tiền trong tooltip")]
    [SerializeField] private TextMeshProUGUI _costText;

    [Header("Tooltip Visual Settings")]
    [Tooltip("Khoảng cách đẩy Tooltip lên trên nút bấm (Y > 0 = lên trên)")]
    [SerializeField] private Vector3 _tooltipOffset = new Vector3(0, 150f, 0);

    [Header("Affordability Visual")]
    [Tooltip("Màu text giá khi đủ tiền")]
    [SerializeField] private Color _affordableColor = Color.white;

    [Tooltip("Màu text giá khi KHÔNG đủ tiền")]
    [SerializeField] private Color _unaffordableColor = new Color(1f, 0.3f, 0.3f, 1f);

    #endregion

    #region Private State

    // Cờ tracking để phân biệt PC (Hover) vs Mobile (Hold)
    private bool _isPointerDown = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Validation
        if (_towerPrefab == null)
        {
            Debug.LogError("[TowerShopButton] Chưa assign _towerPrefab! Nút này sẽ không hoạt động.", gameObject);
        }

        if (_button == null)
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogError("[TowerShopButton] Không tìm thấy Button component!", gameObject);
            }
        }

        // Ẩn tooltip mặc định
        if (_tooltipPanel != null)
        {
            _tooltipPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Subscribe vào CurrencyManager event (Observer Pattern)
        CurrencyManager.OnGoldChanged += UpdateInteractable;

        // Cập nhật trạng thái ban đầu
        if (CurrencyManager.Instance != null)
        {
            UpdateInteractable(CurrencyManager.Instance.CurrentGold);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe để tránh memory leak
        CurrencyManager.OnGoldChanged -= UpdateInteractable;

        // Ẩn tooltip nếu đang hiển thị
        HideTooltip();
    }

    #endregion

    #region Observer Pattern - Event Handler

    /// <summary>
    /// Callback được gọi khi số vàng thay đổi.
    /// LOGIC: Tự động khóa/mở nút dựa trên affordability.
    /// </summary>
    /// <param name="currentGold">Số vàng hiện tại của người chơi</param>
    private void UpdateInteractable(int currentGold)
    {
        if (_towerPrefab == null || _button == null) return;

        // Lấy giá tháp
        int buildCost = _towerPrefab.BuildCost;

        // Kiểm tra đủ tiền không
        bool canAfford = currentGold >= buildCost;

        // Khóa/Mở nút
        _button.interactable = canAfford;

        // Đổi màu text trong tooltip (nếu tooltip đang mở)
        if (_tooltipPanel != null && _tooltipPanel.activeInHierarchy)
        {
            UpdateCostTextColor(canAfford);
        }
    }

    /// <summary>
    /// Cập nhật màu text giá tiền dựa trên affordability.
    /// </summary>
    private void UpdateCostTextColor(bool canAfford)
    {
        if (_costText == null) return;

        _costText.color = canAfford ? _affordableColor : _unaffordableColor;
    }

    #endregion

    #region Tooltip System

    /// <summary>
    /// Hiển thị tooltip ngay trên đầu nút bấm.
    /// ĐỘNG: Tooltip panel di chuyển đến vị trí của nút hiện tại.
    /// </summary>
    private void ShowTooltip()
    {
        if (_tooltipPanel == null || _towerPrefab == null) return;

        // 1. GÁN THÔNG TIN từ TowerPrefab vào Tooltip
        if (_nameText != null)
        {
            _nameText.text = _towerPrefab.TowerName;
        }

        if (_descText != null)
        {
            _descText.text = _towerPrefab.Description;
        }

        if (_costText != null)
        {
            _costText.text = $"{_towerPrefab.BuildCost} Gold";

            // Đổi màu dựa trên affordability
            if (CurrencyManager.Instance != null)
            {
                bool canAfford = CurrencyManager.Instance.CanAfford(_towerPrefab.BuildCost);
                UpdateCostTextColor(canAfford);
            }
        }

        // 2. CẬP NHẬT VỊ TRÍ Tooltip (Dynamic Positioning)
        // QUAN TRỌNG: transform.position là Screen Space position của nút bấm
        // Cộng thêm offset để đẩy tooltip lên trên
        _tooltipPanel.transform.position = transform.position + _tooltipOffset;

        // 3. BẬT Tooltip
        _tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// Ẩn tooltip.
    /// </summary>
    private void HideTooltip()
    {
        if (_tooltipPanel != null)
        {
            _tooltipPanel.SetActive(false);
        }
    }

    #endregion

    #region Event System Handlers (Cross-Platform)

    /// <summary>
    /// PC: Chuột di chuyển VÀO nút (Hover).
    /// MOBILE: Không trigger trên mobile touch.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Chỉ show tooltip nếu KHÔNG phải đang hold touch (mobile)
        // Vì OnPointerEnter có thể fire sau OnPointerDown trên một số thiết bị
        if (!_isPointerDown)
        {
            ShowTooltip();
        }
    }

    /// <summary>
    /// PC: Chuột di chuyển RA KHỎI nút.
    /// MOBILE: Không trigger trên mobile touch.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Chỉ hide nếu không đang hold touch (tránh hide sớm trên mobile)
        if (!_isPointerDown)
        {
            HideTooltip();
        }
    }

    /// <summary>
    /// MOBILE: Chạm và GIỮ tay trên nút (Touch Down).
    /// PC: Click chuột xuống (Mouse Down).
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;

        // Hiển thị tooltip khi hold touch (Mobile UX)
        ShowTooltip();
    }

    /// <summary>
    /// MOBILE: Nhả tay ra khỏi nút (Touch Up).
    /// PC: Thả chuột (Mouse Up).
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;

        // Ẩn tooltip khi nhả tay/chuột
        HideTooltip();
    }

    #endregion

    #region Public API (Optional - For External Control)

    /// <summary>
    /// Lấy TowerBase prefab của nút này.
    /// Dùng để TowerPlacementManager biết spawn tháp gì khi click nút.
    /// </summary>
    public TowerBase TowerPrefab => _towerPrefab;

    /// <summary>
    /// Force hiển thị tooltip (debug purposes).
    /// </summary>
    public void ForceShowTooltip()
    {
        ShowTooltip();
    }

    /// <summary>
    /// Force ẩn tooltip (debug purposes).
    /// </summary>
    public void ForceHideTooltip()
    {
        HideTooltip();
    }

    #endregion

    #region Debug Tools

#if UNITY_EDITOR
    [ContextMenu("Test Show Tooltip")]
    private void TestShowTooltip()
    {
        ShowTooltip();
    }

    [ContextMenu("Test Hide Tooltip")]
    private void TestHideTooltip()
    {
        HideTooltip();
    }

    [ContextMenu("Test Update Affordability (Affordable)")]
    private void TestAffordable()
    {
        UpdateInteractable(999999);
    }

    [ContextMenu("Test Update Affordability (Unaffordable)")]
    private void TestUnaffordable()
    {
        UpdateInteractable(0);
    }
#endif

    #endregion
}
