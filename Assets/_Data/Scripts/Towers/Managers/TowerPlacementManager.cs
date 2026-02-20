using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

/// <summary>
/// Tower Placement Manager - Quản lý logic đặt tháp trên lưới.
/// KIẾN TRÚC:
/// - Singleton Pattern để truy cập toàn cục.
/// - Tích hợp WorldMapManager (kiểm tra TileType + tâm tile).
/// - Tích hợp ObjectPoolManager (spawn tháp từ pool).
/// - Raycast vào Plane toán học (Y=1) để tính vị trí chính xác.
/// UX (Chuẩn Mobile - 2-Step Confirmation):
/// - Ghost theo con trỏ (Hover mode, không cần hold).
/// - Click/Touch -> Ghost nằm im tại vị trí cuối.
/// - Hiện World Space Confirm UI -> Player xác nhận trước khi đặt.
/// - Camera bị LOCK khi đang placement (tránh vô tình vuốt).
/// VALIDATION:
/// - Chỉ đặt tháp trên TileType.Ground.
/// - Cấm đặt trên Path, Home, Start/EndPoint.
/// - Cấm đặt chồng tháp (Data-based check).
/// GRID SNAPPING:
/// - Lấy tọa độ tâm tile từ WorldMapManager.GetTileCenterWorldPosition().
/// - Vị trí đặt tháp = TileCenter + Vector3.up * _placementYOffset.
/// </summary>
public class TowerPlacementManager : SerializedMonoBehaviour
{
    #region Singleton Pattern

    private static TowerPlacementManager _instance;

    public static TowerPlacementManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TowerPlacementManager>();

                if (_instance == null)
                {
                    Debug.LogError("[TowerPlacementManager] Không tìm thấy instance trong Scene! Thêm TowerPlacementManager vào Scene.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[TowerPlacementManager] Phát hiện duplicate instance. Đang hủy object này.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    #endregion

    #region Inspector Configuration

    [Title("Placement Configuration")]
    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Tower Prefabs")]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
    [Tooltip("Danh sách ánh xạ giữa PoolType và Ghost Prefab")]
    public List<TowerPlacementData> towerConfigs = new List<TowerPlacementData>();

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/References")]
    [Required]
    [Tooltip("Reference đến WorldMapManager (để kiểm tra TileType)")]
    [SerializeField] private WorldMapManager _worldMapManager;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/References")]
    [Required]
    [Tooltip("Reference đến MapGenerationSettings (để lấy tileSize)")]
    [SerializeField] private MapGenerationSettings _mapSettings;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/References")]
    [Required]
    [Tooltip("Reference đến CameraController (để lock camera khi đang xây)")]
    [SerializeField] private CameraController _cameraController;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Placement Rules")]
    [Tooltip("LayerMask cho Ground Plane (để Raycast)")]
    [SerializeField] private LayerMask _groundLayer;

    [TabGroup("Tabs", "Settings"), BoxGroup("Tabs/Settings/Placement Rules")]
    [Tooltip("Offset Y khi đặt tháp (để tháp nằm nổi trên mặt đất)")]
    [SerializeField] private float _placementYOffset = 1f;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private PoolType _selectedTowerType = PoolType.None;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private bool _isPlacementMode = false;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private bool _isWaitingForFirstRelease = false;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private bool _isWaitingForConfirmation = false;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private Vector3 _lastValidPosition = Vector3.zero;

    [TabGroup("Tabs", "Runtime"), BoxGroup("Tabs/Runtime/Debug Info"), ReadOnly, ShowInInspector]
    private Vector2Int _lastValidTileCoord = Vector2Int.zero;

    #endregion

    #region Private Fields

    private TowerGhost _currentGhost;
    private Camera _mainCamera;
    private Plane _groundPlane;
    private Dictionary<PoolType, TowerPlacementData> _configCache = new Dictionary<PoolType, TowerPlacementData>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_mainCamera == null)
        {
            Debug.LogError("[TowerPlacementManager] Không tìm thấy Main Camera!");
        }

        // Khởi tạo Ground Plane (Y=1) để khớp với cao độ Ground tiles
        _groundPlane = new Plane(Vector3.up, new Vector3(0, 1f, 0));

        // Cache Tower Configs vào Dictionary
        foreach (var config in towerConfigs)
        {
            if (!_configCache.ContainsKey(config.poolType))
            {
                _configCache.Add(config.poolType, config);
            }
        }

        // Validate references
        if (_worldMapManager == null)
        {
            _worldMapManager = FindObjectOfType<WorldMapManager>();
            if (_worldMapManager == null)
            {
                Debug.LogError("[TowerPlacementManager] Không tìm thấy WorldMapManager!");
            }
        }

        if (_mapSettings == null)
        {
            if (_worldMapManager != null && _worldMapManager.settings != null)
            {
                _mapSettings = _worldMapManager.settings;
            }
            else
            {
                Debug.LogError("[TowerPlacementManager] MapGenerationSettings chưa được gán!");
            }
        }
    }

    private void Update()
    {
        if (!_isPlacementMode) return;

        HandlePlacementInput();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Chọn loại tháp để bắt đầu placement mode.
    /// Gọi khi player bấm nút UI chọn tháp.
    /// </summary>
    public void SelectTower(PoolType towerType)
    {
        if (towerType == PoolType.None)
        {
            Debug.LogWarning("[TowerPlacementManager] Không thể chọn PoolType.None!");
            return;
        }

        if (!_configCache.ContainsKey(towerType))
        {
            Debug.LogError($"[TowerPlacementManager] Tower {towerType} chưa được cấu hình!");
            return;
        }

        // Hủy placement mode cũ (nếu có)
        if (_isPlacementMode)
        {
            CancelPlacement();
        }

        // Bắt đầu placement mode mới
        _selectedTowerType = towerType;
        _isPlacementMode = true;
        _isWaitingForConfirmation = false;
        _isWaitingForFirstRelease = true;

        // CAMERA LOCK: Khóa camera để tránh vô tình vuốt khi đặt tháp
        if (_cameraController != null)
        {
            _cameraController.SetEnabled(false);
        }

        SpawnGhost(towerType);
    }

    /// <summary>
    /// Hủy placement mode (không đặt tháp).
    /// Mở khóa Camera và ẩn World Space Confirm UI.
    /// </summary>
    public void CancelPlacement()
    {
        if (_currentGhost != null)
        {
            _currentGhost.SetConfirmUIVisibility(false);
            _currentGhost.Hide();
            Destroy(_currentGhost.gameObject);
            _currentGhost = null;
        }

        _isPlacementMode = false;
        _isWaitingForConfirmation = false;
        _selectedTowerType = PoolType.None;
        _lastValidPosition = Vector3.zero;

        // CAMERA UNLOCK: Mở khóa camera
        if (_cameraController != null)
        {
            _cameraController.SetEnabled(true);
        }
    }

    /// <summary>
    /// Xác nhận đặt tháp tại vị trí đã chọn (2-Step Placement).
    /// Gọi bởi TowerGhost.OnConfirmClicked() khi player bấm nút Confirm (✓).
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!_isPlacementMode || !_isWaitingForConfirmation)
        {
            Debug.LogWarning("[TowerPlacementManager] Không thể confirm - không ở trạng thái chờ xác nhận!");
            return;
        }

        if (_lastValidPosition == Vector3.zero)
        {
            Debug.LogWarning("[TowerPlacementManager] Vị trí không hợp lệ! Hủy placement.");
            CancelPlacement();
            return;
        }

        PlaceTower(_lastValidPosition, _lastValidTileCoord);
        CancelPlacement();
    }

    #endregion

    #region Private Methods - Input Handling

    /// <summary>
    /// Xử lý input khi đang ở placement mode.
    /// UX (2-Step): Ghost theo con trỏ -> Click để dừng -> Hiện Confirm Panel.
    /// </summary>
    private void HandlePlacementInput()
    {
        // BƯỚC 1: Đợi người chơi nhả chuột đầu tiên (từ UI button)
        if (_isWaitingForFirstRelease)
        {
            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                _isWaitingForFirstRelease = false;
            }

            UpdateGhostPosition();
            return;
        }

        // BƯỚC 2: Nếu đang chờ xác nhận -> KHÔNG cập nhật Ghost (đóng băng)
        if (_isWaitingForConfirmation)
        {
            return;
        }

        // BƯỚC 3: Cập nhật Ghost position
        UpdateGhostPosition();

        // BƯỚC 4: Kiểm tra Click XUỐNG để chọn vị trí
        bool clickedDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (clickedDown && !isOverUI)
        {
            if (_lastValidPosition != Vector3.zero)
            {
                _isWaitingForConfirmation = true;

                if (_currentGhost != null)
                {
                    _currentGhost.SetConfirmUIVisibility(true);
                }
            }
            else
            {
                Debug.LogWarning("[TowerPlacementManager] Vị trí không hợp lệ! Không thể đặt tháp.");
            }
        }
    }

    /// <summary>
    /// Cập nhật vị trí Ghost theo con trỏ chuột.
    /// Ưu tiên Physics.Raycast vào Mesh, Fallback về Plane (Y=1).
    /// </summary>
    private void UpdateGhostPosition()
    {
        Vector3 inputPosition = GetInputPosition();

        if (inputPosition == Vector3.zero)
        {
            if (_currentGhost != null)
            {
                _currentGhost.SetState(false);
            }
            return;
        }

        // Raycast thông minh: Physics Mesh -> Plane (Y=1)
        Ray ray = _mainCamera.ScreenPointToRay(inputPosition);
        Vector3 rawHitPoint = Vector3.zero;
        bool hasHit = false;

        // Thử Physics Raycast vào Mesh
        if (_groundLayer != 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
            {
                rawHitPoint = hit.point;
                hasHit = true;
            }
        }

        // Fallback: Plane toán học (Y=1)
        if (!hasHit)
        {
            float enter;
            if (_groundPlane.Raycast(ray, out enter))
            {
                rawHitPoint = ray.GetPoint(enter);
                hasHit = true;
            }
        }

        if (!hasHit)
        {
            if (_currentGhost != null)
            {
                _currentGhost.SetState(false);
            }
            return;
        }

        // Convert world position sang tile coordinate
        Vector2Int tileCoord;
        TileType tileType;
        bool tileExists = IsGroundTile(rawHitPoint, out tileCoord, out tileType);

        if (!tileExists)
        {
            if (_currentGhost != null)
            {
                _currentGhost.SetState(false);
            }
            _lastValidPosition = Vector3.zero;
            return;
        }

        // Lấy tâm tile từ WorldMapManager
        Vector3 exactTileCenter = Vector3.zero;

        if (_worldMapManager != null)
        {
            exactTileCenter = _worldMapManager.GetTileCenterWorldPosition(tileCoord);
        }
        else
        {
            Debug.LogError("[TowerPlacementManager] WorldMapManager null!");
            return;
        }

        // Tính vị trí đặt tháp = TileCenter + Y offset
        Vector3 finalTowerPosition = exactTileCenter + Vector3.up * _placementYOffset;

        // Validate vị trí
        bool isValidTileType = (tileType == TileType.Ground);
        bool isNotOccupied = (_worldMapManager == null || !_worldMapManager.IsTileOccupied(tileCoord));
        bool isValid = isValidTileType && isNotOccupied;

        // Cập nhật Ghost
        if (_currentGhost != null)
        {
            _currentGhost.UpdatePosition(finalTowerPosition);
            _currentGhost.SetState(isValid);
        }

        // Lưu last valid position
        if (isValid)
        {
            _lastValidPosition = finalTowerPosition;
            _lastValidTileCoord = tileCoord;
        }
        else
        {
            _lastValidPosition = Vector3.zero;
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

    #region Private Methods - Placement Logic

    /// <summary>
    /// Spawn Ghost Prefab.
    /// </summary>
    private void SpawnGhost(PoolType towerType)
    {
        if (!_configCache.TryGetValue(towerType, out TowerPlacementData config))
        {
            Debug.LogError($"[TowerPlacementManager] Không tìm thấy config cho {towerType}!");
            return;
        }

        if (config.ghostPrefab == null)
        {
            Debug.LogError($"[TowerPlacementManager] Ghost Prefab cho {towerType} chưa được gán!");
            return;
        }

        GameObject ghostObj = Instantiate(config.ghostPrefab, Vector3.zero, Quaternion.identity);
        _currentGhost = ghostObj.GetComponent<TowerGhost>();

        if (_currentGhost == null)
        {
            Debug.LogError($"[TowerPlacementManager] Ghost Prefab không có component TowerGhost!");
            Destroy(ghostObj);
            return;
        }

        _currentGhost.Show(Vector3.zero);
    }

    /// <summary>
    /// Kiểm tra vị trí có phải Ground tile không.
    /// Output: tileCoord (tọa độ tile), tileType (loại tile).
    /// </summary>
    private bool IsGroundTile(Vector3 worldPosition, out Vector2Int tileCoord, out TileType tileType)
    {
        tileCoord = Vector2Int.zero;
        tileType = TileType.Ground;

        if (_worldMapManager == null || _mapSettings == null) return false;

        // Convert World Position -> Global Tile Coordinate (với CenterOffset)
        float tileSize = _mapSettings.tileSize;
        float centerOffset = _mapSettings.CenterOffset;

        float shiftedX = worldPosition.x + centerOffset;
        float shiftedZ = worldPosition.z + centerOffset;

        int globalTileX = Mathf.RoundToInt(shiftedX / tileSize);
        int globalTileZ = Mathf.RoundToInt(shiftedZ / tileSize);

        tileCoord = new Vector2Int(globalTileX, globalTileZ);

        tileType = _worldMapManager.GetTileType(tileCoord);

        if (tileType == TileType.EndPoint)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Đặt tháp tại vị trí hợp lệ.
    /// </summary>
    private void PlaceTower(Vector3 worldPosition, Vector2Int tileCoord)
    {
        PoolType towerType = _selectedTowerType;

        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("[TowerPlacementManager] ObjectPoolManager không tồn tại!");
            return;
        }

        GameObject towerObj = ObjectPoolManager.Instance.Spawn(towerType, worldPosition, Quaternion.identity);

        if (towerObj == null)
        {
            Debug.LogError($"[TowerPlacementManager] Không thể spawn tháp {towerType}!");
            return;
        }

        TowerBase tower = towerObj.GetComponent<TowerBase>();
        if (tower != null)
        {
            tower.Initialize(worldPosition);
        }

        if (_worldMapManager != null)
        {
            _worldMapManager.MarkTileOccupied(tileCoord);
        }
    }

    #endregion

    #region Debug Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_isPlacementMode) return;

        if (_lastValidPosition != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_lastValidPosition, 0.5f);

            Vector3 tileGroundPos = new Vector3(_lastValidPosition.x, 0f, _lastValidPosition.z);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(tileGroundPos, Vector3.one * 0.3f);
        }
    }
#endif

    #endregion
}

/// <summary>
/// Cấu hình cho mỗi loại tháp (PoolType -> Ghost Prefab).
/// </summary>
[Serializable]
public struct TowerPlacementData
{
    [HorizontalGroup("Row")]
    [LabelWidth(80)]
    [Tooltip("Loại tháp (PoolType)")]
    public PoolType poolType;

    [HorizontalGroup("Row")]
    [Required]
    [PreviewField(50, ObjectFieldAlignment.Left)]
    [Tooltip("Prefab Ghost tương ứng")]
    public GameObject ghostPrefab;
}
