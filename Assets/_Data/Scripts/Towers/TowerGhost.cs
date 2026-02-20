using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Ghost - Hiển thị preview tháp khi đặt trên lưới.
/// Thiết kế:
/// - Giữ nguyên màu model tháp.
/// - Sử dụng Indicator Box (Cube transparent) để hiển thị trạng thái Valid/Invalid.
/// - Tích hợp World Space Confirm UI (Canvas Billboard) cho 2-Step Placement.
/// </summary>
public class TowerGhost : MonoBehaviour
{
    #region Inspector Configuration

    [Title("Ghost Configuration")]
    [BoxGroup("Indicator Box")]
    [Tooltip("MeshRenderer của Box hiển thị trạng thái (tự động tạo nếu chưa có)")]
    [SerializeField, ReadOnly] private MeshRenderer _indicatorRenderer;

    [BoxGroup("Indicator Box")]
    [Required]
    [Tooltip("Material Xanh Lục (transparent) - Vị trí hợp lệ")]
    [SerializeField] private Material _validMaterial;

    [BoxGroup("Indicator Box")]
    [Required]
    [Tooltip("Material Đỏ (transparent) - Vị trí không hợp lệ")]
    [SerializeField] private Material _invalidMaterial;

    [BoxGroup("Indicator Box")]
    [Tooltip("Kích thước Box (TileSize). Mặc định: 2x2 units")]
    [SerializeField] private Vector3 _indicatorSize = new Vector3(2f, 0.1f, 2f);

    [BoxGroup("Indicator Box")]
    [Tooltip("Offset Y của Box so với transform gốc (đặt dưới chân tháp)")]
    [SerializeField] private float _indicatorYOffset = 0f;

    [Title("World Space Confirm UI")]
    [BoxGroup("Confirm UI")]
    [Required]
    [Tooltip("Canvas World Space chứa nút Xác nhận/Hủy (Đính dưới chân Ghost)")]
    [SerializeField] private GameObject _confirmCanvas;

    [BoxGroup("Confirm UI")]
    [Tooltip("Canvas luôn hướng về Camera (Billboard effect)")]
    [SerializeField] private bool _enableBillboard = true;

    [BoxGroup("Settings")]
    [Tooltip("Offset Y so với vị trí đặt thật (để ghost nổi lên một chút)")]
    [SerializeField] private float _heightOffset = 0.2f;

    [BoxGroup("Settings")]
    [Tooltip("Tốc độ Lerp khi di chuyển ghost (để smooth animation)")]
    [SerializeField] private float _moveSpeed = 15f;

    [BoxGroup("Runtime Info"), ReadOnly, ShowInInspector]
    [Tooltip("Trạng thái hiện tại của Ghost")]
    private string CurrentState => _isValidPlacement ? "✓ Valid (Green)" : "✗ Invalid (Red)";

    #endregion

    #region Private Fields

    private bool _isValidPlacement = true;
    private Vector3 _targetPosition;
    private GameObject _indicatorBox;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeIndicatorBox();

        if (_confirmCanvas != null)
        {
            _confirmCanvas.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gameObject.activeInHierarchy)
        {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _moveSpeed);
        }
    }

    private void LateUpdate()
    {
        if (_enableBillboard && _confirmCanvas != null && _confirmCanvas.activeInHierarchy)
        {
            if (Camera.main != null)
            {
                _confirmCanvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Khởi tạo Indicator Box (tự động tạo nếu chưa có).
    /// </summary>
    private void InitializeIndicatorBox()
    {
        Transform existingBox = transform.Find("IndicatorBox");

        if (existingBox != null)
        {
            _indicatorBox = existingBox.gameObject;
            _indicatorRenderer = _indicatorBox.GetComponent<MeshRenderer>();
        }

        if (_indicatorBox == null)
        {
            CreateIndicatorBox();
        }

        if (_indicatorRenderer == null)
        {
            Debug.LogError("[TowerGhost] Không tìm thấy MeshRenderer trên IndicatorBox!", gameObject);
        }
        else
        {
            _indicatorRenderer.sharedMaterial = _validMaterial;
        }
    }

    /// <summary>
    /// Tạo Indicator Box từ đầu (GameObject Cube).
    /// </summary>
    private void CreateIndicatorBox()
    {
        _indicatorBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _indicatorBox.name = "IndicatorBox";
        _indicatorBox.transform.SetParent(transform);
        _indicatorBox.transform.localPosition = new Vector3(0f, _indicatorYOffset, 0f);
        _indicatorBox.transform.localRotation = Quaternion.identity;
        _indicatorBox.transform.localScale = _indicatorSize;

        Collider boxCollider = _indicatorBox.GetComponent<Collider>();
        if (boxCollider != null)
        {
            Destroy(boxCollider);
        }

        _indicatorRenderer = _indicatorBox.GetComponent<MeshRenderer>();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Hiển thị ghost tại vị trí chỉ định.
    /// </summary>
    public void Show(Vector3 position)
    {
        gameObject.SetActive(true);
        _targetPosition = position + Vector3.up * _heightOffset;
        transform.position = _targetPosition;
    }

    /// <summary>
    /// Ẩn ghost.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Cập nhật vị trí ghost (gọi mỗi frame).
    /// </summary>
    public void UpdatePosition(Vector3 position)
    {
        _targetPosition = position + Vector3.up * _heightOffset;
    }

    /// <summary>
    /// Đổi trạng thái Valid/Invalid bằng cách thay đổi Material của Indicator Box.
    /// </summary>
    public void SetState(bool isValid)
    {
        if (_isValidPlacement == isValid) return;

        _isValidPlacement = isValid;

        if (_indicatorRenderer == null) return;

        _indicatorRenderer.sharedMaterial = isValid ? _validMaterial : _invalidMaterial;
    }

    /// <summary>
    /// Lấy trạng thái hiện tại của ghost.
    /// </summary>
    public bool IsValidPlacement() => _isValidPlacement;

    /// <summary>
    /// Hiển thị/Ẩn World Space Confirm UI (gọi khi Ghost đóng băng chờ xác nhận).
    /// </summary>
    public void SetConfirmUIVisibility(bool isVisible)
    {
        if (_confirmCanvas != null)
        {
            _confirmCanvas.SetActive(isVisible);
        }
        else
        {
            Debug.LogWarning("[TowerGhost] Confirm Canvas chưa được gán!", gameObject);
        }
    }

    /// <summary>
    /// Callback khi player bấm nút Xác nhận (✓).
    /// </summary>
    public void OnConfirmClicked()
    {
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.ConfirmPlacement();
        }
        else
        {
            Debug.LogError("[TowerGhost] TowerPlacementManager.Instance null!");
        }
    }

    /// <summary>
    /// Callback khi player bấm nút Hủy (X).
    /// </summary>
    public void OnCancelClicked()
    {
        if (TowerPlacementManager.Instance != null)
        {
            TowerPlacementManager.Instance.CancelPlacement();
        }
        else
        {
            Debug.LogError("[TowerGhost] TowerPlacementManager.Instance null!");
        }
    }

    #endregion

    #region Debug Helpers

#if UNITY_EDITOR
    [BoxGroup("Debug"), Button("Test Valid State (Green)"), GUIColor(0.4f, 1f, 0.4f)]
    private void DebugSetValid()
    {
        SetState(true);
    }

    [BoxGroup("Debug"), Button("Test Invalid State (Red)"), GUIColor(1f, 0.4f, 0.4f)]
    private void DebugSetInvalid()
    {
        SetState(false);
    }

    [BoxGroup("Debug"), Button("Toggle Ghost Visibility"), GUIColor(0.4f, 0.8f, 1f)]
    private void DebugToggleVisibility()
    {
        if (gameObject.activeInHierarchy)
            Hide();
        else
            Show(transform.position);
    }

    [BoxGroup("Debug"), Button("Recreate Indicator Box"), GUIColor(1f, 1f, 0.4f)]
    private void DebugRecreateBox()
    {
        if (_indicatorBox != null)
        {
            DestroyImmediate(_indicatorBox);
        }

        CreateIndicatorBox();
    }
#endif

    #endregion
}
