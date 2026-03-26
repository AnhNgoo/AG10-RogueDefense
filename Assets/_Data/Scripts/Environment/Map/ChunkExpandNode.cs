using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

/// <summary>
/// Chunk Expand Node - Node UI World Space cho phép player click để mở rộng chunk.
/// Thiết kế:
/// - Billboard effect (luôn hướng về Camera).
/// - Lưu tham chiếu đến ChunkData target.
/// - Click handler (World Space Canvas Button hoặc BoxCollider Raycast).
/// - Tích hợp với WorldMapManager để expand chunk.
/// - Lắng nghe Wave Events: Ẩn Canvas khi đang combat (OnWaveStarted), hiện lại khi combat xong (OnWaveCompleted).
/// </summary>
public class ChunkExpandNode : MonoBehaviour
{
    #region Inspector Configuration

    [Title("Node Configuration")]
    [BoxGroup("References")]
    [Required]
    [Tooltip("Canvas World Space chứa Button Expand")]
    [SerializeField] private Canvas _canvas;

    [BoxGroup("References")]
    [Tooltip("BoxCollider để fallback raycast (optional, nếu không dùng Canvas Button)")]
    [SerializeField] private BoxCollider _collider;

    [BoxGroup("Settings")]
    [Tooltip("Bật Billboard effect (Canvas luôn hướng về Camera)")]
    [SerializeField] private bool _enableBillboard = true;

    [BoxGroup("Settings")]
    [Tooltip("Màu sắc highlight khi hover (optional)")]
    [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0f, 0.5f);

    [BoxGroup("Runtime Info"), ReadOnly, ShowInInspector]
    [Tooltip("Chunk mà node này đại diện")]
    private ChunkData _targetChunk;

    [BoxGroup("Runtime Info"), ReadOnly, ShowInInspector]
    [Tooltip("Tọa độ chunk")]
    private Vector2Int _chunkCoord;

    #endregion

    #region Private Fields

    private Material _originalMaterial;
    private Renderer _renderer;
    private bool _isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Cache Renderer nếu có (để highlight)
        _renderer = GetComponentInChildren<Renderer>();

        // Ẩn Canvas mặc định (chỉ hiện khi Initialize)
        // Disable Canvas COMPONENT, KHÔNG disable GameObject (tránh trigger OnDisable)
        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
    }

    private void OnEnable()
    {
        // Đăng ký lắng nghe Wave Events
        EnemySpawner.OnWaveStarted += HideNodeDuringCombat;
        EnemySpawner.OnWaveCompleted += ShowNodeAfterCombat;
    }

    private void OnDisable()
    {
        // Hủy đăng ký để tránh memory leak
        EnemySpawner.OnWaveStarted -= HideNodeDuringCombat;
        EnemySpawner.OnWaveCompleted -= ShowNodeAfterCombat;
    }

    private void LateUpdate()
    {
        // Billboard Effect: Canvas luôn hướng về Camera
        if (_enableBillboard && _canvas != null && _canvas.enabled)
        {
            if (Camera.main != null)
            {
                _canvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Khởi tạo node với ChunkData target.
    /// Gọi bởi WorldMapManager khi spawn node.
    /// </summary>
    public void Initialize(ChunkData chunk, Vector2Int coord)
    {
        if (chunk == null)
        {
            Debug.LogError("[ChunkExpandNode] Chunk data null! Không thể khởi tạo node.");
            return;
        }

        _targetChunk = chunk;
        _chunkCoord = coord;
        _isInitialized = true;

        // Hiển thị Canvas (check EnemySpawner.IsWaveActive để đảm bảo đúng trạng thái)
        if (_canvas != null)
        {
            // Nếu đang có wave active thì ẩn Canvas, ngược lại thì hiện
            // CRITICAL: Disable Canvas COMPONENT, KHÔNG disable GameObject (tránh trigger OnDisable cascade)
            if (EnemySpawner.IsWaveActive)
            {
                _canvas.enabled = false;
                Debug.Log("[ChunkExpandNode] Initialize: Wave đang active, ẩn Canvas.");
            }
            else
            {
                _canvas.enabled = true;
                Debug.Log("[ChunkExpandNode] Initialize: Wave không active, hiện Canvas.");
            }
        }

        // Cache original material
        if (_renderer != null)
        {
            _originalMaterial = _renderer.sharedMaterial;
        }
    }

    /// <summary>
    /// Callback khi player bấm nút Expand (World Space Canvas Button).
    /// Hook vào Button OnClick event trong Inspector.
    /// </summary>
    public void OnExpandButtonClicked()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[ChunkExpandNode] Node chưa được khởi tạo!");
            return;
        }

        if (_targetChunk == null)
        {
            Debug.LogError("[ChunkExpandNode] Target chunk null!");
            return;
        }

        // Play SFX khi bấm nút expand
        AudioManager.Instance?.PlaySFX(SoundType.ButtonClick);

        // Gọi WorldMapManager để expand chunk
        if (WorldMapManager.Instance != null)
        {
            WorldMapManager.Instance.ExpandChunk(_targetChunk);
        }
        else
        {
            Debug.LogError("[ChunkExpandNode] WorldMapManager.Instance null!");
        }
    }

    /// <summary>
    /// Cleanup node (gọi khi xóa node).
    /// </summary>
    public void Cleanup()
    {
        _targetChunk = null;
        _isInitialized = false;

        if (_canvas != null)
        {
            _canvas.enabled = false;
        }
    }

    #endregion

    #region Wave Event Handlers

    /// <summary>
    /// Ẩn Canvas khi Wave bắt đầu (ngăn player mở map khi đang combat).
    /// CRITICAL FIX: Disable Canvas COMPONENT thay vì GameObject để tránh trigger OnDisable cascade.
    /// </summary>
    private void HideNodeDuringCombat()
    {
        Debug.Log($"[ChunkExpandNode] HideNodeDuringCombat called. Canvas null? {_canvas == null}, Initialized? {_isInitialized}");

        if (_canvas != null && _isInitialized)
        {
            _canvas.enabled = false; // FIXED: Disable component, NOT GameObject!
            Debug.Log($"[ChunkExpandNode] Canvas ẨN (Wave bắt đầu). Coord: {_chunkCoord}");
        }
    }

    /// <summary>
    /// Hiện Canvas trở lại khi Wave kết thúc.
    /// CRITICAL FIX: Enable Canvas COMPONENT thay vì GameObject.
    /// </summary>
    private void ShowNodeAfterCombat()
    {
        Debug.Log($"[ChunkExpandNode] ShowNodeAfterCombat called. Canvas null? {_canvas == null}, Initialized? {_isInitialized}");

        if (_canvas != null && _isInitialized)
        {
            _canvas.enabled = true; // FIXED: Enable component, NOT GameObject!
            Debug.Log($"[ChunkExpandNode] Canvas HIỆN (Wave kết thúc). Coord: {_chunkCoord}");
        }
    }

    #endregion

    #region Mouse Interaction (Fallback - Nếu không dùng Canvas Button)

    /// <summary>
    /// Fallback: Click detection qua BoxCollider (nếu không dùng World Space Canvas Button).
    /// Chỉ hoạt động nếu có BoxCollider và EventSystem không block.
    /// </summary>
    private void OnMouseDown()
    {
        // Kiểm tra xem có click vào UI không (EventSystem check)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return; // Click vào UI, không xử lý
        }

        // Gọi expand
        OnExpandButtonClicked();
    }

    /// <summary>
    /// Highlight khi hover (optional visual feedback).
    /// </summary>
    private void OnMouseEnter()
    {
        if (_renderer != null && _originalMaterial != null)
        {
            // Tạo material instance tạm để highlight
            Material tempMaterial = new Material(_originalMaterial);
            tempMaterial.color = _highlightColor;
            _renderer.material = tempMaterial;
        }
    }

    /// <summary>
    /// Xóa highlight khi mouse ra.
    /// </summary>
    private void OnMouseExit()
    {
        if (_renderer != null && _originalMaterial != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
        }
    }

    #endregion

    #region Debug Helpers

#if UNITY_EDITOR
    [BoxGroup("Debug"), Button("Test Expand"), GUIColor(0.4f, 1f, 0.4f)]
    private void DebugExpand()
    {
        OnExpandButtonClicked();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_isInitialized) return;

        // Vẽ chunk boundary
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

        // Vẽ label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Expand Node\nChunk: {_chunkCoord}");
    }
#endif

    #endregion
}
