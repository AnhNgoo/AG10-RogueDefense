using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Camera Controller chuyên nghiệp cho Tower Defense Top-Down/Isometric.
/// Chức năng: Pan (kéo thả), Zoom (pinch/scroll), Rotation (orbit), Inertia, Bounds.
/// Tối ưu cho Mobile với Odin Inspector configuration.
/// </summary>
public class CameraController : MonoBehaviour
{
    #region Inspector Configuration

    [Title("Camera Settings")]
    [BoxGroup("Camera")]
    [SerializeField] private Camera _camera;

    [BoxGroup("Camera")]
    [SerializeField] private bool _isPerspective = true;

    [BoxGroup("Camera")]
    [Tooltip("Enable to use this controller")]
    [SerializeField] private bool _enableController = true;

    // === REFERENCES ===
    [Title("References (Optional Auto-Config)")]
    [BoxGroup("References")]
    [Tooltip("Auto-configure bounds from Map Settings (Optional)")]
    [SerializeField] private MapGenerationSettings _mapSettings;

    [BoxGroup("References")]
    [Tooltip("WorldMapManager for auto-align camera (Optional)")]
    [SerializeField] private WorldMapManager _mapManager;

    [BoxGroup("References")]
    [SerializeField, Range(1f, 3f)]
    [Tooltip("Multiplier for auto bounds size calculation (1 = exact map size, 2 = 2x map size)")]
    private float _boundsAutoSizeMultiplier = 1.5f;

    // === PAN SETTINGS ===
    [Title("Pan (Movement) Settings")]
    [BoxGroup("Pan")]
    [SerializeField, Range(0.1f, 5f)]
    [Tooltip("Pan speed multiplier")]
    private float _panSpeed = 1f;

    [BoxGroup("Pan")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("0 = Instant, 1 = Very Smooth")]
    private float _panSmoothing = 0.15f;

    [BoxGroup("Pan/Inertia")]
    [SerializeField]
    [Tooltip("Enable momentum/inertia when releasing drag")]
    private bool _enableInertia = true;

    [BoxGroup("Pan/Inertia")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("How much momentum is preserved (0 = None, 1 = Full)")]
    private float _inertiaStrength = 0.5f;

    [BoxGroup("Pan/Inertia")]
    [SerializeField, Range(0f, 10f)]
    [Tooltip("How fast momentum decays")]
    private float _inertiaDamping = 5f;

    // === ZOOM SETTINGS ===
    [Title("Zoom Settings")]
    [BoxGroup("Zoom")]
    [SerializeField, Range(0.1f, 10f)]
    [Tooltip("Zoom speed for scroll wheel / pinch")]
    private float _zoomSpeed = 2f;

    [BoxGroup("Zoom")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("0 = Instant, 1 = Very Smooth")]
    private float _zoomSmoothing = 0.2f;

    [BoxGroup("Zoom/Perspective")]
    [SerializeField, ShowIf("_isPerspective")]
    [Tooltip("Min Field of View (Perspective)")]
    private float _minFOV = 20f;

    [BoxGroup("Zoom/Perspective")]
    [SerializeField, ShowIf("_isPerspective")]
    [Tooltip("Max Field of View (Perspective)")]
    private float _maxFOV = 80f;

    [BoxGroup("Zoom/Orthographic")]
    [SerializeField, HideIf("_isPerspective")]
    [Tooltip("Min Orthographic Size")]
    private float _minOrthoSize = 2f;

    [BoxGroup("Zoom/Orthographic")]
    [SerializeField, HideIf("_isPerspective")]
    [Tooltip("Max Orthographic Size")]
    private float _maxOrthoSize = 20f;

    // === BOUNDS SETTINGS ===
    [Title("Map Bounds")]
    [BoxGroup("Bounds")]
    [SerializeField]
    [Tooltip("Enable map boundary limits")]
    private bool _enableBounds = true;

    [BoxGroup("Bounds")]
    [SerializeField, ShowIf("_enableBounds")]
    [Tooltip("Center of the allowed map area")]
    private Vector3 _boundsCenter = Vector3.zero;

    [BoxGroup("Bounds")]
    [SerializeField, ShowIf("_enableBounds")]
    [Tooltip("Width (X) and Depth (Z) of the allowed map area")]
    private Vector2 _boundsSize = new Vector2(50f, 50f);

    // === ROTATION SETTINGS ===
    [Title("Rotation (Orbit) Settings")]
    [BoxGroup("Rotation")]
    [SerializeField]
    [Tooltip("Enable orbit rotation feature")]
    private bool _enableRotation = true;

    [BoxGroup("Rotation")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("0 = Instant, 1 = Very Smooth")]
    private float _rotationSmoothing = 0.2f;

    [BoxGroup("Rotation")]
    [SerializeField]
    [Tooltip("Rotation angle per step (degrees)")]
    private float _rotationStep = 90f;

    #endregion

    #region Runtime State (Debug)

    [Title("Runtime State (Debug)")]
    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private Vector3 _targetPosition;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private float _targetZoom;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private Vector3 _currentVelocity;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private Vector3 _momentum;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private bool _isDragging;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private float _targetYaw;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private Quaternion _targetRotation;

    #endregion

    #region Private Fields

    // Ground Plane for Raycast (Y = 0)
    private Plane _groundPlane;

    // Pan Variables
    private Vector3 _dragStartPoint;      // World position when drag starts
    private Vector3 _currentHitPoint;     // Current world position under finger/mouse
    private Vector3 _lastHitPoint;        // Previous frame hit point (for velocity calculation)
    private Vector3 _dragVelocity;        // Current drag velocity

    // Touch/Mouse Input
    private int _primaryTouchId = -1;     // Track which touch is controlling camera
    private Vector2 _lastMousePosition;

    // Zoom Variables
    private float _zoomVelocity;
    private float _initialPinchDistance;
    private float _pinchStartZoom;

    // Rotation Variables
    private Vector3 _orbitPivot;          // Point to orbit around (center of screen)

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Get Camera component if not assigned
        if (_camera == null)
            _camera = GetComponent<Camera>();

        // Initialize ground plane (Y = 0, Normal pointing up)
        _groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Initialize target values to current state
        _targetPosition = transform.position;
        _targetZoom = _isPerspective ? _camera.fieldOfView : _camera.orthographicSize;
        _targetRotation = transform.rotation;
        _targetYaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        // Auto-configure bounds from MapGenerationSettings if assigned
        if (_mapSettings != null && _enableBounds)
        {
            AutoConfigureBoundsFromMapSettings();
        }

        // Auto-align camera to map (wait 1 frame for map to generate)
        // if (_mapManager != null && _enableRotation)
        // {
        //     Invoke(nameof(AlignCameraToMap), 0.1f);
        // }
    }

    private void Update()
    {
        if (!_enableController) return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            RotateRight();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            RotateLeft();
        }

        HandleInput();
    }

    private void LateUpdate()
    {
        if (!_enableController) return;

        ApplyInertia();
        ApplyBounds();
        ApplySmoothing();
        ApplyRotationSmoothing();
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Xử lý input chính - Touch (Mobile) và Mouse (Editor).
    /// </summary>
    private void HandleInput()
    {
        // Touch Input (Mobile)
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // Mouse Input (Editor/PC Debug)
        else if (Application.isEditor || Input.mousePresent)
        {
            HandleMouseInput();
        }
        else
        {
            // No input - allow inertia to decay
            _isDragging = false;
        }
    }

    /// <summary>
    /// Xử lý Touch Input (Mobile).
    /// </summary>
    private void HandleTouchInput()
    {
        // Single Touch = Pan
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDrag(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Moved:
                    UpdateDrag(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDrag();
                    break;
            }
        }
        // Two Touches = Pinch Zoom
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            HandlePinchZoom(touch0, touch1);

            // Stop panning during pinch
            _isDragging = false;
            _momentum = Vector3.zero;
        }
    }

    /// <summary>
    /// Xử lý Mouse Input (Editor/PC Debug).
    /// </summary>
    private void HandleMouseInput()
    {
        // Mouse Button 0 (Left Click) = Pan
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag(Input.mousePosition, -1);
        }
        else if (Input.GetMouseButton(0) && _isDragging)
        {
            UpdateDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }

        // Mouse Scroll Wheel = Zoom
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            HandleScrollZoom(scrollDelta);
        }
    }

    #endregion

    #region Pan Logic (Raycast to Ground Plane)

    /// <summary>
    /// Bắt đầu kéo - Raycast xuống mặt phẳng Y=0 để lấy anchor point.
    /// </summary>
    private void StartDrag(Vector2 screenPosition, int touchId)
    {
        Ray ray = _camera.ScreenPointToRay(screenPosition);

        // Raycast to ground plane (Y = 0)
        if (_groundPlane.Raycast(ray, out float enter))
        {
            _dragStartPoint = ray.GetPoint(enter);
            _currentHitPoint = _dragStartPoint;
            _lastHitPoint = _dragStartPoint;
            _isDragging = true;
            _primaryTouchId = touchId;
            _momentum = Vector3.zero;        // Reset momentum when starting new drag
            _dragVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Cập nhật kéo - Tính delta và cập nhật vị trí target.
    /// </summary>
    private void UpdateDrag(Vector2 screenPosition)
    {
        if (!_isDragging) return;

        Ray ray = _camera.ScreenPointToRay(screenPosition);

        if (_groundPlane.Raycast(ray, out float enter))
        {
            _lastHitPoint = _currentHitPoint;
            _currentHitPoint = ray.GetPoint(enter);

            // Calculate movement delta (inverted for natural drag feel)
            Vector3 delta = (_dragStartPoint - _currentHitPoint) * _panSpeed;

            // Update target position
            _targetPosition += delta;

            // Calculate velocity for inertia (world units per frame)
            _dragVelocity = (_currentHitPoint - _lastHitPoint) * -_panSpeed;

            // Update drag start point for continuous dragging
            _dragStartPoint = _currentHitPoint;
        }
    }

    /// <summary>
    /// Kết thúc kéo - Apply momentum nếu bật inertia.
    /// </summary>
    private void EndDrag()
    {
        if (_isDragging && _enableInertia)
        {
            // Capture momentum from final drag velocity
            _momentum = _dragVelocity * _inertiaStrength * 10f; // Scale up for noticeable effect
        }

        _isDragging = false;
        _primaryTouchId = -1;
    }

    #endregion

    #region Zoom Logic

    /// <summary>
    /// Xử lý Pinch Zoom (2 ngón tay).
    /// </summary>
    private void HandlePinchZoom(Touch touch0, Touch touch1)
    {
        // Both touches just began - initialize pinch
        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            _initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
            _pinchStartZoom = _targetZoom;
            return;
        }

        // Both touches moving - calculate pinch zoom
        if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);
            float deltaPinch = _initialPinchDistance - currentDistance;

            // Scale delta by screen size for consistent zoom speed across devices
            float normalizedDelta = deltaPinch / Screen.height;

            ApplyZoom(normalizedDelta * _zoomSpeed * 50f);
        }
    }

    /// <summary>
    /// Xử lý Scroll Wheel Zoom (Chuột).
    /// </summary>
    private void HandleScrollZoom(float scrollDelta)
    {
        ApplyZoom(-scrollDelta * _zoomSpeed * 5f); // Negative for natural scroll direction
    }

    /// <summary>
    /// Apply zoom change vào target zoom (xem xét Perspective/Orthographic).
    /// </summary>
    private void ApplyZoom(float zoomDelta)
    {
        _targetZoom += zoomDelta;

        // Clamp based on camera projection
        if (_isPerspective)
        {
            _targetZoom = Mathf.Clamp(_targetZoom, _minFOV, _maxFOV);
        }
        else
        {
            _targetZoom = Mathf.Clamp(_targetZoom, _minOrthoSize, _maxOrthoSize);
        }
    }

    #endregion

    #region Movement & Smoothing

    /// <summary>
    /// Apply inertia/momentum khi không kéo.
    /// </summary>
    private void ApplyInertia()
    {
        if (_isDragging || !_enableInertia) return;

        if (_momentum.sqrMagnitude > 0.001f)
        {
            // Add momentum to target position
            _targetPosition += _momentum * Time.deltaTime;

            // Decay momentum exponentially
            _momentum = Vector3.Lerp(_momentum, Vector3.zero, _inertiaDamping * Time.deltaTime);
        }
        else
        {
            _momentum = Vector3.zero;
        }
    }

    /// <summary>
    /// Clamp target position trong map bounds.
    /// </summary>
    private void ApplyBounds()
    {
        if (!_enableBounds) return;

        // Calculate bounds rect
        float halfWidth = _boundsSize.x * 0.5f;
        float halfDepth = _boundsSize.y * 0.5f;

        float minX = _boundsCenter.x - halfWidth;
        float maxX = _boundsCenter.x + halfWidth;
        float minZ = _boundsCenter.z - halfDepth;
        float maxZ = _boundsCenter.z + halfDepth;

        // Clamp XZ position
        _targetPosition.x = Mathf.Clamp(_targetPosition.x, minX, maxX);
        _targetPosition.z = Mathf.Clamp(_targetPosition.z, minZ, maxZ);

        // If clamped, kill momentum in that direction
        if (_targetPosition.x <= minX || _targetPosition.x >= maxX)
            _momentum.x = 0f;

        if (_targetPosition.z <= minZ || _targetPosition.z >= maxZ)
            _momentum.z = 0f;
    }

    /// <summary>
    /// Smoothly interpolate camera position và zoom.
    /// </summary>
    private void ApplySmoothing()
    {
        // Smooth Position Movement
        if (_panSmoothing > 0.01f)
        {
            float smoothTime = _panSmoothing * 0.3f; // Convert to smooth time
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _targetPosition,
                ref _currentVelocity,
                smoothTime
            );
        }
        else
        {
            transform.position = _targetPosition;
        }

        // Smooth Zoom
        if (_isPerspective)
        {
            _camera.fieldOfView = Mathf.Lerp(
                _camera.fieldOfView,
                _targetZoom,
                1f - _zoomSmoothing
            );
        }
        else
        {
            _camera.orthographicSize = Mathf.Lerp(
                _camera.orthographicSize,
                _targetZoom,
                1f - _zoomSmoothing
            );
        }
    }

    #endregion

    #region Gizmos

    /// <summary>
    /// Vẽ map bounds trong Scene View.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!_enableBounds) return;

        Gizmos.color = Color.yellow;

        Vector3 center = _boundsCenter;
        Vector3 size = new Vector3(_boundsSize.x, 0.1f, _boundsSize.y);

        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawCube(center, size);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Đặt vị trí camera ngay lập tức (không smooth).
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        _targetPosition = position;
        transform.position = position;
        _momentum = Vector3.zero;
    }

    /// <summary>
    /// Đặt zoom level ngay lập tức (không smooth).
    /// </summary>
    public void SetZoom(float zoom)
    {
        _targetZoom = zoom;
        if (_isPerspective)
        {
            _targetZoom = Mathf.Clamp(_targetZoom, _minFOV, _maxFOV);
            _camera.fieldOfView = _targetZoom;
        }
        else
        {
            _targetZoom = Mathf.Clamp(_targetZoom, _minOrthoSize, _maxOrthoSize);
            _camera.orthographicSize = _targetZoom;
        }
    }

    /// <summary>
    /// Di chuyển camera đến vị trí target (smooth).
    /// </summary>
    public void MoveTo(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
        _momentum = Vector3.zero;
    }

    /// <summary>
    /// Bật/tắt controller.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _enableController = enabled;
        if (!enabled)
        {
            _isDragging = false;
            _momentum = Vector3.zero;
        }
    }

    /// <summary>
    /// Xoay camera sang trái (orbit quanh tâm màn hình).
    /// </summary>
    public void RotateLeft()
    {
        if (!_enableRotation) return;
        RotateCamera(_rotationStep);
    }

    /// <summary>
    /// Xoay camera sang phải (orbit quanh tâm màn hình).
    /// </summary>
    public void RotateRight()
    {
        if (!_enableRotation) return;
        RotateCamera(-_rotationStep);
    }

    /// <summary>
    /// Đặt camera yaw angle trực tiếp (0-360).
    /// </summary>
    public void SetYaw(float yaw)
    {
        _targetYaw = yaw;
        _targetRotation = Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Tự động cấu hình camera bounds từ MapGenerationSettings.
    /// </summary>
    private void AutoConfigureBoundsFromMapSettings()
    {
        if (_mapSettings == null)
        {
            Debug.LogWarning("[CameraController] MapGenerationSettings reference null. Không thể auto-configure bounds.");
            return;
        }

        // Tính world size của map
        int totalChunksX = _mapSettings.maxCoord - _mapSettings.minCoord + 1;
        int totalChunksZ = _mapSettings.maxCoord - _mapSettings.minCoord + 1;

        float worldWidth = totalChunksX * _mapSettings.ChunkWorldSize;
        float worldDepth = totalChunksZ * _mapSettings.ChunkWorldSize;

        // Apply padding multiplier
        _boundsSize = new Vector2(
            worldWidth * _boundsAutoSizeMultiplier,
            worldDepth * _boundsAutoSizeMultiplier
        );

        // Tính bounds center (map center)
        float centerChunkX = (_mapSettings.minCoord + _mapSettings.maxCoord) / 2f;
        float centerChunkZ = (_mapSettings.minCoord + _mapSettings.maxCoord) / 2f;

        _boundsCenter = new Vector3(
            centerChunkX * _mapSettings.ChunkWorldSize,
            0f,
            centerChunkZ * _mapSettings.ChunkWorldSize
        );
    }

    /// <summary>
    /// Xoay camera quanh pivot point (screen center).
    /// </summary>
    private void RotateCamera(float angleDelta)
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (_groundPlane.Raycast(ray, out float enter))
        {
            _orbitPivot = ray.GetPoint(enter);
        }
        else
        {
            _orbitPivot = new Vector3(_targetPosition.x, 0f, _targetPosition.z);
        }

        Vector3 offset = _targetPosition - _orbitPivot;
        Quaternion rotation = Quaternion.Euler(0f, angleDelta, 0f);
        Vector3 rotatedOffset = rotation * offset;

        _targetPosition = _orbitPivot + rotatedOffset;

        _targetYaw += angleDelta;
        _targetYaw = Mathf.Repeat(_targetYaw, 360f);

        _targetRotation = Quaternion.Euler(transform.eulerAngles.x, _targetYaw, transform.eulerAngles.z);
    }

    /// <summary>
    /// Apply smooth rotation interpolation.
    /// </summary>
    private void ApplyRotationSmoothing()
    {
        if (!_enableRotation) return;

        if (_rotationSmoothing > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _targetRotation,
                1f - _rotationSmoothing
            );
        }
        else
        {
            transform.rotation = _targetRotation;
        }
    }

    /// <summary>
    /// Tự động align camera theo hướng tối ưu dựa trên Base exit points.
    /// </summary>
    public void AlignCameraToMap()
    {
        if (_mapManager == null)
        {
            Debug.LogWarning("[CameraController] WorldMapManager reference null. Không thể auto-align.");
            return;
        }

        ChunkData baseChunk = _mapManager.GetChunk(Vector2Int.zero);
        if (baseChunk == null || baseChunk.exitPoints == null || baseChunk.exitPoints.Count == 0)
        {
            Debug.LogWarning("[CameraController] Base Chunk (0,0) không có exit points. Không thể auto-align.");
            return;
        }

        // Tính average exit direction
        Vector2 avgExitDirection = Vector2.zero;
        int centerTile = _mapSettings.chunkSize / 2;

        foreach (var exitTile in baseChunk.exitPoints)
        {
            Vector2 direction = new Vector2(exitTile.x - centerTile, exitTile.y - centerTile);
            avgExitDirection += direction.normalized;
        }
        avgExitDirection /= baseChunk.exitPoints.Count;

        // Xác định optimal camera yaw dựa trên exit direction
        float optimalYaw = 0f;

        if (Mathf.Abs(avgExitDirection.x) > Mathf.Abs(avgExitDirection.y))
        {
            optimalYaw = avgExitDirection.x > 0 ? 180f : 0f;
        }
        else
        {
            optimalYaw = avgExitDirection.y > 0 ? 270f : 90f;
        }

        // Orbit camera quanh map center (Vector3.zero)
        float angleDelta = Mathf.DeltaAngle(_targetYaw, optimalYaw);
        _orbitPivot = Vector3.zero;
        Vector3 offset = _targetPosition - Vector3.zero;
        Quaternion rotation = Quaternion.Euler(0f, angleDelta, 0f);
        Vector3 rotatedOffset = rotation * offset;

        _targetPosition = Vector3.zero + rotatedOffset;
        _targetYaw = optimalYaw;
        _targetRotation = Quaternion.Euler(transform.eulerAngles.x, _targetYaw, transform.eulerAngles.z);

        transform.position = _targetPosition;
        transform.rotation = _targetRotation;
    }

    #endregion
}
