using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Quản lý Object Pooling cho toàn bộ game sử dụng Enum key.
/// Singleton Pattern.
/// Tối ưu hóa với Queue<GameObject> (O(1)) thay vì List (O(n)).
/// UI được đánh bóng bằng Odin Inspector.
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
            Debug.LogWarning("[ObjectPoolManager] Phát hiện duplicate instance. Đang hủy object này.");
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Đảm bảo GameObject này là Root Object (tách khỏi parent nếu có)
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Inspector Configuration

    [Title("Pool Configuration", "Settings for all object pools", TitleAlignment = TitleAlignments.Centered)]
    [BoxGroup("Config")]
    [Required]
    [InlineEditor(InlineEditorModes.GUIAndHeader)]
    [Tooltip("File cấu hình Pool (PoolConfig ScriptableObject)")]
    public PoolConfig poolConfig;

    [BoxGroup("Runtime Info", CenterLabel = true)]
    [HorizontalGroup("Runtime Info/Stats")]
    [VerticalGroup("Runtime Info/Stats/Left")]
    [ReadOnly, ShowInInspector, LabelWidth(100), LabelText("Initial Pools")]
    public int TotalPools => poolDictionary?.Count ?? 0;

    [VerticalGroup("Runtime Info/Stats/Right")]
    [ReadOnly, ShowInInspector, LabelWidth(120), LabelText("Total Active Objects")]
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

    [BoxGroup("Runtime Info")]
    [ShowInInspector, ReadOnly]
    [DictionaryDrawerSettings(KeyLabel = "Pool Type", ValueLabel = "Queue Status")]
    [LabelText("Active Objects per Pool")]
    public Dictionary<PoolType, int> ActiveCountsView => activeCount;

    #endregion

    #region Internal Data Structures

    [System.Serializable]
    private class PoolInfo
    {
        [ReadOnly]
        public PoolType poolType;
        [ReadOnly]
        public Transform parent;
        public GameObject prefab;
        public int initialSize;
        public int maxSize;
    }

    // Dictionary: PoolType -> Queue<GameObject> (O(1) access)
    private Dictionary<PoolType, Queue<GameObject>> poolDictionary;

    // Dictionary: PoolType -> PoolInfo (config storage)
    private Dictionary<PoolType, PoolInfo> poolSettings;

    // Dictionary: PoolType -> Active Count
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
    [Button("Re-Initialize Pools", ButtonSizes.Medium), PropertyOrder(10)]
    [BoxGroup("Actions", CenterLabel = true)]
    [DisableInEditorMode]
    private void InitializePools()
    {
        poolDictionary = new Dictionary<PoolType, Queue<GameObject>>();
        poolSettings = new Dictionary<PoolType, PoolInfo>();
        activeCount = new Dictionary<PoolType, int>();

        // Validation
        if (poolConfig == null)
        {
            Debug.LogError("[ObjectPoolManager] PoolConfig is null! Assign PoolConfig ScriptableObject in Inspector.");
            return;
        }

        if (poolConfig.pools == null || poolConfig.pools.Count == 0)
        {
            Debug.LogWarning("[ObjectPoolManager] PoolConfig chưa được cấu hình pool nào.");
            return;
        }

        // Duyệt qua tất cả PoolDataEntry trong PoolConfig
        foreach (PoolDataEntry entry in poolConfig.pools)
        {
            if (!entry.IsValid(out string errorMessage))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{entry.poolType}' invalid: {errorMessage}. Skipping.");
                continue;
            }

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

            // Tạo GameObject cha
            GameObject parentObj = new GameObject(entry.parentName);
            parentObj.transform.SetParent(transform);
            info.parent = parentObj.transform;

            // Tạo Queue và Warmup (Sử dụng Queue<GameObject> theo yêu cầu nghiêm ngặt)
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
        }

        Debug.Log($"[ObjectPoolManager] ✓ Initialized {poolDictionary.Count} pools successfully using Queue<GameObject>.");
    }

    /// <summary>
    /// Tạo GameObject mới từ Prefab.
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
    /// Spawn một GameObject từ pool.
    /// Sử dụng Queue.Dequeue() (O(1)).
    /// </summary>
    public GameObject Spawn(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolType))
        {
            Debug.LogError($"[ObjectPoolManager] Pool '{poolType}' doesn't exist!");
            return null;
        }

        GameObject obj = null;
        Queue<GameObject> queue = poolDictionary[poolType];

        // Lấy object từ Pool nếu còn
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            // Pool rỗng -> Expand nếu chưa đạt maxSize
            if (activeCount[poolType] < poolSettings[poolType].maxSize)
            {
                obj = CreateNewObject(poolSettings[poolType]);
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolType}' expanded. Active: {activeCount[poolType] + 1}/{poolSettings[poolType].maxSize}");
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
    /// Trả GameObject về lại pool của nó.
    /// Sử dụng Queue.Enqueue() (O(1)).
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable == null)
        {
            Debug.LogError($"[ObjectPoolManager] Object '{obj.name}' doesn't implement IPoolable! Destroying.");
            Destroy(obj);
            return;
        }

        PoolType poolType = poolable.PoolType;

        if (!poolDictionary.ContainsKey(poolType))
        {
            Debug.LogError($"[ObjectPoolManager] Pool '{poolType}' doesn't exist! Destroying.");
            Destroy(obj);
            return;
        }

        // Gọi OnReturnToPool
        poolable.OnReturnToPool();

        // Đưa về parent & deactivate
        obj.SetActive(false);
        obj.transform.SetParent(poolSettings[poolType].parent);

        // Đưa vào Queue (O(1))
        poolDictionary[poolType].Enqueue(obj);

        // Giảm active count
        activeCount[poolType]--;
    }

    /// <summary>
    /// Xóa toàn bộ pool.
    /// </summary>
    [Button("Clear All Pools", ButtonSizes.Medium), PropertyOrder(20)]
    [BoxGroup("Actions")]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void ClearAllPools()
    {
        if (poolDictionary == null) return;

        foreach (var pool in poolDictionary.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }

        poolDictionary.Clear();
        poolSettings.Clear();
        activeCount.Clear();

        Debug.Log("[ObjectPoolManager] ✓ All pools cleared.");
    }

    #endregion

    #region Debug Utilities

    [BoxGroup("Actions")]
    [ResponsiveButtonGroup("DebugActions")]
    [Button(ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
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

    public string GetPoolInfo(PoolType poolType)
    {
        if (poolDictionary == null || !poolDictionary.ContainsKey(poolType))
            return $"{poolType}: Not found";

        int available = poolDictionary[poolType].Count;
        int active = activeCount[poolType];
        int total = available + active;

        return $"{poolType}: Total={total}, Active={active}, Available={available}";
    }

    #endregion
}
