using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Interaction Manager - Quản lý tương tác với tháp đã xây trên bản đồ.
/// KIẾN TRÚC:
/// - Singleton Pattern để truy cập toàn cục.
/// - Tách biệt với TowerPlacementManager (Single Responsibility Principle).
/// - Raycast để phát hiện click vào tháp.
/// TÍNH NĂNG:
/// - Click vào tháp -> Hiện Edit UI (Upgrade/Sell buttons) và Range Indicator.
/// - Click ra ngoài -> Tắt Edit UI của tháp đang chọn.
/// - Xử lý Upgrade và Sell logic.
/// </summary>
public class TowerInteractionManager : SerializedMonoBehaviour
{
    #region Singleton Pattern

    private static TowerInteractionManager _instance;

    public static TowerInteractionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TowerInteractionManager>();

                if (_instance == null)
                {
                    Debug.LogError("[TowerInteractionManager] Không tìm thấy instance trong Scene! Thêm TowerInteractionManager vào Scene.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[TowerInteractionManager] Phát hiện duplicate instance. Đang hủy object này.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    #endregion

    #region Inspector Configuration

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Raycast")]
    [Tooltip("LayerMask cho Tower Layer (để Raycast phát hiện tháp)")]
    [SerializeField] private LayerMask _towerLayer;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private TowerBase _selectedTower = null;

    #endregion

    #region Private Fields

    private Camera _mainCamera;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_mainCamera == null)
        {
            Debug.LogError("[TowerInteractionManager] Không tìm thấy Main Camera!");
        }
    }

    private void Update()
    {
        // Chỉ xử lý khi KHÔNG ở chế độ placement
        if (TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.IsPlacementMode)
        {
            return;
        }

        HandleTowerSelection();
    }

    #endregion

    #region Tower Selection Logic

    /// <summary>
    /// Kiểm tra xem pointer có đang hover trên UI không.
    /// Xử lý ĐÚNG cho cả PC (Mouse) và Mobile (Touch với fingerId).
    /// FIX: IsPointerOverGameObject() không nhận fingerId trên Mobile gây bug click xuyên World Space Canvas.
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // MOBILE: Check Touch với fingerId
        if (Input.touchCount > 0)
        {
            // Lấy fingerId của touch đầu tiên
            int fingerId = Input.GetTouch(0).fingerId;
            return EventSystem.current.IsPointerOverGameObject(fingerId);
        }

        // PC/EDITOR: Check Mouse (không cần fingerId)
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Xử lý việc click vào tháp hoặc click ra ngoài.
    /// </summary>
    private void HandleTowerSelection()
    {
        // Kiểm tra click chuột trái hoặc touch
        bool clickedDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (!clickedDown) return;

        // Bỏ qua nếu click vào UI (World Space Canvas hoặc Screen Space Canvas)
        if (IsPointerOverUI())
        {
            return;
        }

        // Raycast từ camera
        Vector3 inputPosition = GetInputPosition();
        Ray ray = _mainCamera.ScreenPointToRay(inputPosition);
        RaycastHit hit;

        // Thử raycast vào Tower Layer
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _towerLayer))
        {
            // Click trúng một tháp
            TowerBase clickedTower = hit.collider.GetComponent<TowerBase>();

            if (clickedTower == null)
            {
                // Thử tìm trong parent (trường hợp collider nằm ở child)
                clickedTower = hit.collider.GetComponentInParent<TowerBase>();
            }

            if (clickedTower != null)
            {
                SelectTower(clickedTower);
                return;
            }
        }

        // Click ra ngoài -> Deselect tháp hiện tại
        DeselectTower();
    }

    /// <summary>
    /// Chọn một tháp (hiện Edit UI).
    /// </summary>
    private void SelectTower(TowerBase tower)
    {
        if (tower == _selectedTower) return; // Đã chọn rồi

        // Deselect tháp cũ (nếu có)
        if (_selectedTower != null)
        {
            _selectedTower.ToggleEditMode(false);
        }

        // Select tháp mới
        _selectedTower = tower;
        _selectedTower.ToggleEditMode(true);
    }

    /// <summary>
    /// Bỏ chọn tháp hiện tại (ẩn Edit UI).
    /// </summary>
    private void DeselectTower()
    {
        if (_selectedTower != null)
        {
            _selectedTower.ToggleEditMode(false);
            _selectedTower = null;
        }
    }

    /// <summary>
    /// Lấy vị trí input (chuột hoặc touch).
    /// </summary>
    private Vector3 GetInputPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Property để TowerPlacementManager kiểm tra xem có đang chọn tháp không.
    /// </summary>
    public bool HasSelectedTower => _selectedTower != null;

    /// <summary>
    /// Force deselect tháp (gọi từ bên ngoài nếu cần).
    /// </summary>
    public void ForceDeselectTower()
    {
        DeselectTower();
    }

    #endregion
}
