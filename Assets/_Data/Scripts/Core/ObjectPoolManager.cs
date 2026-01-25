using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// OBJECT POOL MANAGER: Quản lý tất cả Pool trong game (Enum-Based).
/// SINGLETON PATTERN: Chỉ có 1 instance duy nhất.
/// 
/// ARCHITECTURE:
/// - Dùng Enum thay vì String tag để type-safe.
/// - Config qua List PoolData (kéo thả trong Inspector hoặc tự tìm bằng AssetList).
/// - Dictionary<PoolType, Queue<GameObject>> để quản lý pool.
/// 
/// PERFORMANCE:
/// - Giảm 90% GC Allocation so với Instantiate/Destroy.
/// - Warmup Pool tại Start để tránh lag spike lúc đầu game.
/// </summary>
public class ObjectPoolManager : SerializedMonoBehaviour
{
    #region Singleton Pattern

    private static ObjectPoolManager _instance;

    public static ObjectPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectPoolManager>();

                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("[ObjectPoolManager]");
                    _instance = managerObj.AddComponent<ObjectPoolManager>();
                    DontDestroyOnLoad(managerObj);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[ObjectPoolManager] Duplicate instance detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Inspector Configuration

    [TabGroup("Tabs", "Config"), BoxGroup("Tabs/Config/Pool Configuration")]
    [Required]
    [InlineEditor(InlineEditorModes.GUIAndHeader)]
    [Tooltip("File cấu hình Pool (PoolConfig ScriptableObject)")]
    public PoolConfig poolConfig;

    [TabGroup("Tabs", "Runtime Info"), ReadOnly, ShowInInspector]
    public int TotalPools => poolDictionary?.Count ?? 0;

    [TabGroup("Tabs", "Runtime Info"), ReadOnly, ShowInInspector]
    public int TotalActiveObjects
    {
        get
        {
            int total = 0;
            if (activeCount != null)
            {
                foreach (var count in activeCount.Values)
                {
                    total += count;
                }
            }
            return total;
        }
    }

    #endregion

    #region Internal Data Structures

    [System.Serializable]
    private class PoolInfo
    {
        public PoolType poolType;
        public GameObject prefab;
        public int initialSize;
        public int maxSize;
        public Transform parent;
    }

    // Dictionary: PoolType -> Queue<GameObject>
    private Dictionary<PoolType, Queue<GameObject>> poolDictionary;

    // Dictionary: PoolType -> PoolInfo (lưu config)
    private Dictionary<PoolType, PoolInfo> poolSettings;

    // Dictionary: PoolType -> Active Count (số object đang active)
    private Dictionary<PoolType, int> activeCount;

    #endregion

    #region Initialization

    private void Start()
    {
        InitializePools();
    }

    /// <summary>
    /// Khởi tạo tất cả Pools từ PoolConfig.
    /// Tạo sẵn initialSize objects cho mỗi pool (Warmup).
    /// </summary>
    private void InitializePools()
    {
        poolDictionary = new Dictionary<PoolType, Queue<GameObject>>();
        poolSettings = new Dictionary<PoolType, PoolInfo>();
        activeCount = new Dictionary<PoolType, int>();

        // Validation: Kiểm tra PoolConfig có được gán không
        if (poolConfig == null)
        {
            Debug.LogError("[ObjectPoolManager] PoolConfig is null! Assign PoolConfig ScriptableObject in Inspector.");
            return;
        }

        if (poolConfig.pools == null || poolConfig.pools.Count == 0)
        {
            Debug.LogWarning("[ObjectPoolManager] PoolConfig has no pools configured! Add pools in PoolConfig asset.");
            return;
        }

        // Duyệt qua tất cả PoolDataEntry trong PoolConfig
        foreach (PoolDataEntry entry in poolConfig.pools)
        {
            // Validation: Kiểm tra tính hợp lệ của entry
            if (!entry.IsValid(out string errorMessage))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{entry.poolType}' invalid: {errorMessage}. Skipping.");
                continue;
            }

            // Kiểm tra duplicate PoolType
            if (poolDictionary.ContainsKey(entry.poolType))
            {
                Debug.LogWarning($"[ObjectPoolManager] Duplicate PoolType '{entry.poolType}'! Skipping.");
                continue;
            }

            // Tạo Pool Info
            PoolInfo info = new PoolInfo
            {
                poolType = entry.poolType,
                prefab = entry.prefab,
                initialSize = entry.initialSize,
                maxSize = entry.maxSize
            };

            // Tạo GameObject cha cho pool này (dùng parentName từ config)
            GameObject parentObj = new GameObject(entry.parentName);
            parentObj.transform.SetParent(transform);
            info.parent = parentObj.transform;

            // Tạo Queue và Warmup
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int i = 0; i < entry.initialSize; i++)
            {
                GameObject obj = CreateNewObject(info);
                objectQueue.Enqueue(obj);
            }

            // Lưu vào Dictionaries
            poolDictionary[entry.poolType] = objectQueue;
            poolSettings[entry.poolType] = info;
            activeCount[entry.poolType] = 0;

            Debug.Log($"[ObjectPoolManager] ✓ Pool '{entry.poolType}' created with {entry.initialSize} objects (Max: {entry.maxSize}).");
        }

        Debug.Log($"[ObjectPoolManager] ✓ Initialized {poolDictionary.Count} pools successfully.");
    }

    /// <summary>
    /// Tạo GameObject mới từ Prefab (internal use).
    /// </summary>
    private GameObject CreateNewObject(PoolInfo info)
    {
        GameObject obj = Instantiate(info.prefab, info.parent);
        obj.name = $"{info.poolType}"; // Đặt tên gọn
        obj.SetActive(false);
        return obj;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Spawn GameObject từ Pool.
    /// Nếu Pool rỗng -> Tạo thêm (nếu chưa đạt maxSize).
    /// </summary>
    /// <param name="poolType">Loại Pool</param>
    /// <param name="position">Vị trí spawn</param>
    /// <param name="rotation">Góc quay</param>
    /// <returns>GameObject đã spawn (hoặc null nếu pool đầy)</returns>
    public GameObject Spawn(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        // Kiểm tra Pool có tồn tại không
        if (!poolDictionary.ContainsKey(poolType))
        {
            Debug.LogError($"[ObjectPoolManager] Pool '{poolType}' doesn't exist! Did you create PoolData for it?");
            return null;
        }

        GameObject obj = null;

        // Lấy object từ Pool nếu còn
        if (poolDictionary[poolType].Count > 0)
        {
            obj = poolDictionary[poolType].Dequeue();
        }
        else
        {
            // Pool rỗng -> Kiểm tra có thể expand không
            if (activeCount[poolType] < poolSettings[poolType].maxSize)
            {
                obj = CreateNewObject(poolSettings[poolType]);
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolType}' expanded! Active: {activeCount[poolType] + 1}/{poolSettings[poolType].maxSize}");
            }
            else
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{poolType}' reached maxSize ({poolSettings[poolType].maxSize})! Cannot spawn more.");
                return null;
            }
        }

        // Setup GameObject
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Tăng active count
        activeCount[poolType]++;

        // Gọi OnSpawnFromPool
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawnFromPool();
        }

        return obj;
    }

    /// <summary>
    /// Trả GameObject về Pool.
    /// GỌI HÀM NÀY THAY VÌ Destroy(gameObject).
    /// </summary>
    /// <param name="obj">GameObject cần trả về</param>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[ObjectPoolManager] Attempted to return null object!");
            return;
        }

        // Lấy PoolType từ IPoolable
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable == null)
        {
            Debug.LogError($"[ObjectPoolManager] Object '{obj.name}' doesn't implement IPoolable! Destroying instead.");
            Destroy(obj);
            return;
        }

        PoolType poolType = poolable.PoolType;

        // Kiểm tra Pool có tồn tại không
        if (!poolDictionary.ContainsKey(poolType))
        {
            Debug.LogError($"[ObjectPoolManager] Pool '{poolType}' doesn't exist! Destroying object.");
            Destroy(obj);
            return;
        }

        // Gọi OnReturnToPool
        poolable.OnReturnToPool();

        // Ẩn object và đưa về parent
        obj.SetActive(false);
        obj.transform.SetParent(poolSettings[poolType].parent);

        // Đưa vào Queue
        poolDictionary[poolType].Enqueue(obj);

        // Giảm active count
        activeCount[poolType]--;
    }

    /// <summary>
    /// Xóa toàn bộ Pools (dùng khi reset game hoặc chuyển Scene).
    /// </summary>
    public void ClearAllPools()
    {
        if (poolDictionary == null) return;

        foreach (var pool in poolDictionary.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        poolDictionary.Clear();
        poolSettings.Clear();
        activeCount.Clear();

        Debug.Log("[ObjectPoolManager] ✓ All pools cleared.");
    }

    #endregion

    #region Debug Utilities

    [TabGroup("Tabs", "Debug"), Button("Log Pool Status", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
    private void LogPoolStatus()
    {
        if (poolDictionary == null || poolDictionary.Count == 0)
        {
            Debug.Log("[ObjectPoolManager] No pools initialized yet.");
            return;
        }

        Debug.Log("=== OBJECT POOL STATUS ===");

        foreach (var kvp in poolDictionary)
        {
            PoolType type = kvp.Key;
            int available = kvp.Value.Count;
            int active = activeCount[type];
            int maxSize = poolSettings[type].maxSize;

            Debug.Log($"Pool '{type}': Active={active}, Available={available}, Max={maxSize}");
        }

        Debug.Log($"Total Active Objects: {TotalActiveObjects}");
    }

    /// <summary>
    /// Lấy thông tin Pool dạng string (dùng cho UI debug).
    /// </summary>
    public string GetPoolInfo(PoolType poolType)
    {
        if (!poolDictionary.ContainsKey(poolType))
            return $"{poolType}: Not found";

        int available = poolDictionary[poolType].Count;
        int active = activeCount[poolType];
        int total = available + active;

        return $"{poolType}: Total={total}, Active={active}, Available={available}";
    }

    #endregion
}
