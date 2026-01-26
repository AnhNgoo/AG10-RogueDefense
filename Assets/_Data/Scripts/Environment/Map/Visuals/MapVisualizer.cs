using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Class chịu trách nhiệm hiển thị (Visualization) map data thành GameObjects trong Scene.
/// Logic Exclusive Tiles + Mesh Combining: Gộp mesh theo Material để giảm Draw Call.
/// Path -> PathOnly, Ground -> Dirt+Grass, Home -> DirtOnly (tất cả được combine).
/// HomeBase vẫn Instantiate riêng vì có logic gameplay.
/// </summary>
public class MapVisualizer
{
    private MapGenerationSettings settings;
    private Transform visualContainer;
    private Transform parentTransform;

    public MapVisualizer(MapGenerationSettings settings, Transform parentTransform)
    {
        this.settings = settings;
        this.parentTransform = parentTransform;
    }

    /// <summary>
    /// Hiển thị một Chunk bằng Mesh Combining (gộp mesh theo Material).
    /// Logic: Thu thập CombineInstance cho từng loại tile, sau đó gộp thành 1 GameObject.
    /// </summary>
    public void VisualizeChunk(ChunkData chunk)
    {
        // Tạo container nếu chưa có
        if (visualContainer == null)
        {
            GameObject containerObj = new GameObject("MapVisuals");
            visualContainer = containerObj.transform;
            visualContainer.SetParent(parentTransform);
        }

        // Tạo sub-container cho chunk này
        GameObject chunkContainer = new GameObject($"Chunk_{chunk.chunkCoord.x}_{chunk.chunkCoord.y}");
        chunkContainer.transform.SetParent(visualContainer);

        // Tính toạ độ World của Chunk
        Vector3 chunkWorldPos = new Vector3(
            chunk.chunkCoord.x * settings.ChunkWorldSize,
            0,
            chunk.chunkCoord.y * settings.ChunkWorldSize
        );

        // Kiểm tra xem có phải Base Chunk không
        bool isBaseChunk = (chunk.chunkCoord == Vector2Int.zero);

        // ========================================
        // BƯỚC 1: THU THẬP COMBINE INSTANCES
        // ========================================
        List<CombineInstance> dirtCombines = new List<CombineInstance>();
        List<CombineInstance> pathCombines = new List<CombineInstance>();
        List<CombineInstance> grassCombines = new List<CombineInstance>();

        // Lấy mesh và material từ prefabs (cache để tối ưu)
        Mesh dirtMesh = GetMeshFromPrefab(settings.dirtPrefab);
        Mesh pathMesh = GetMeshFromPrefab(settings.pathPrefab);
        Mesh grassMesh = GetMeshFromPrefab(settings.grassPrefab);

        Material dirtMaterial = GetMaterialFromPrefab(settings.dirtPrefab);
        Material pathMaterial = GetMaterialFromPrefab(settings.pathPrefab);
        Material grassMaterial = GetMaterialFromPrefab(settings.grassPrefab);

        // Duyệt qua 81 tiles để thu thập dữ liệu
        for (int x = 0; x < settings.chunkSize; x++)
        {
            for (int z = 0; z < settings.chunkSize; z++)
            {
                TileType tileType = chunk.tiles[x, z];

                // Tính toạ độ local của tile (relative to chunk)
                Vector3 tileLocalPos = new Vector3(
                    (x * settings.tileSize) - settings.CenterOffset,
                    0,
                    (z * settings.tileSize) - settings.CenterOffset
                );

                // Xử lý theo TileType (LOẠI TRỪ - không đè lên nhau)
                switch (tileType)
                {
                    case TileType.Path:
                    case TileType.StartPoint:
                    case TileType.EndPoint:
                        // Đường đi -> CHỈ PathPrefab (Y = 0)
                        if (pathMesh != null)
                        {
                            CombineInstance pathInstance = new CombineInstance();
                            pathInstance.mesh = pathMesh;
                            pathInstance.transform = Matrix4x4.TRS(tileLocalPos, Quaternion.identity, Vector3.one);
                            pathCombines.Add(pathInstance);
                        }
                        break;

                    case TileType.Ground:
                        // Đất thường -> Dirt (Y = 0) + Grass (Y = 1)
                        if (dirtMesh != null)
                        {
                            CombineInstance dirtInstance = new CombineInstance();
                            dirtInstance.mesh = dirtMesh;
                            dirtInstance.transform = Matrix4x4.TRS(tileLocalPos, Quaternion.identity, Vector3.one);
                            dirtCombines.Add(dirtInstance);
                        }
                        if (grassMesh != null)
                        {
                            Vector3 grassLocalPos = tileLocalPos + Vector3.up * 1f;
                            CombineInstance grassInstance = new CombineInstance();
                            grassInstance.mesh = grassMesh;
                            grassInstance.transform = Matrix4x4.TRS(grassLocalPos, Quaternion.identity, Vector3.one);
                            grassCombines.Add(grassInstance);
                        }
                        break;

                    case TileType.Home:
                        // Khu vực Home (3x3) -> CHỈ Dirt (Y = 0) làm nền sân nhà
                        if (dirtMesh != null)
                        {
                            CombineInstance dirtInstance = new CombineInstance();
                            dirtInstance.mesh = dirtMesh;
                            dirtInstance.transform = Matrix4x4.TRS(tileLocalPos, Quaternion.identity, Vector3.one);
                            dirtCombines.Add(dirtInstance);
                        }
                        break;
                }
            }
        }

        // ========================================
        // BƯỚC 2: TẠO COMBINED MESHES
        // ========================================

        // Dirt Combined
        if (dirtCombines.Count > 0 && dirtMaterial != null)
        {
            GameObject dirtCombined = new GameObject("Combined_Dirt");
            dirtCombined.transform.SetParent(chunkContainer.transform);
            dirtCombined.transform.localPosition = chunkWorldPos;

            MeshFilter meshFilter = dirtCombined.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = dirtCombined.AddComponent<MeshRenderer>();

            Mesh combinedDirtMesh = new Mesh();
            combinedDirtMesh.CombineMeshes(dirtCombines.ToArray(), true, true);
            meshFilter.mesh = combinedDirtMesh;
            meshRenderer.material = dirtMaterial;

            // Optional: Add collider for ground interaction
            MeshCollider collider = dirtCombined.AddComponent<MeshCollider>();
            collider.sharedMesh = combinedDirtMesh;
        }

        // Path Combined
        if (pathCombines.Count > 0 && pathMaterial != null)
        {
            GameObject pathCombined = new GameObject("Combined_Path");
            pathCombined.transform.SetParent(chunkContainer.transform);
            pathCombined.transform.localPosition = chunkWorldPos;

            MeshFilter meshFilter = pathCombined.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = pathCombined.AddComponent<MeshRenderer>();

            Mesh combinedPathMesh = new Mesh();
            combinedPathMesh.CombineMeshes(pathCombines.ToArray(), true, true);
            meshFilter.mesh = combinedPathMesh;
            meshRenderer.material = pathMaterial;

            // Optional: Add collider for path interaction
            MeshCollider collider = pathCombined.AddComponent<MeshCollider>();
            collider.sharedMesh = combinedPathMesh;
        }

        // Grass Combined
        if (grassCombines.Count > 0 && grassMaterial != null)
        {
            GameObject grassCombined = new GameObject("Combined_Grass");
            grassCombined.transform.SetParent(chunkContainer.transform);
            grassCombined.transform.localPosition = chunkWorldPos;

            MeshFilter meshFilter = grassCombined.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = grassCombined.AddComponent<MeshRenderer>();

            Mesh combinedGrassMesh = new Mesh();
            combinedGrassMesh.CombineMeshes(grassCombines.ToArray(), true, true);
            meshFilter.mesh = combinedGrassMesh;
            meshRenderer.material = grassMaterial;

            // Grass thường không cần collider (chỉ visual)
        }

        // ========================================
        // BƯỚC 3: INSTANTIATE HOME BASE (CHỈ BASE CHUNK - KHÔNG COMBINE)
        // ========================================
        if (isBaseChunk && settings.homeBasePrefab != null)
        {
            // Tính vị trí tâm chunk (tile 4,4)
            Vector3 homeCenterPos = chunkWorldPos + Vector3.up; // Tâm chunk = (0, 0, 0) vì Base Chunk ở (0,0)

            // Tính rotation dựa trên hướng exit
            Quaternion homeRotation = CalculateHomeRotation(chunk);

            // Instantiate HomeBase tại tâm (KHÔNG gộp vì có logic gameplay)
            GameObject homeObj = Object.Instantiate(settings.homeBasePrefab, homeCenterPos, homeRotation, chunkContainer.transform);
            homeObj.name = "HomeBase";
        }
    }

    /// <summary>
    /// Xóa toàn bộ visual cũ trong scene.
    /// </summary>
    public void ClearVisuals()
    {
        if (visualContainer != null)
        {
            Object.Destroy(visualContainer.gameObject);
            visualContainer = null;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Lấy Mesh từ Prefab (MeshFilter ở root hoặc child đầu tiên).
    /// </summary>
    private Mesh GetMeshFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        // Kiểm tra root
        MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh;
        }

        // Kiểm tra child đầu tiên
        meshFilter = prefab.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh;
        }

        Debug.LogWarning($"[MapVisualizer] Không tìm thấy MeshFilter trong Prefab: {prefab.name}");
        return null;
    }

    /// <summary>
    /// Lấy Material từ Prefab (MeshRenderer ở root hoặc child đầu tiên).
    /// </summary>
    private Material GetMaterialFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        // Kiểm tra root
        MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            return meshRenderer.sharedMaterial;
        }

        // Kiểm tra child đầu tiên
        meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            return meshRenderer.sharedMaterial;
        }

        Debug.LogWarning($"[MapVisualizer] Không tìm thấy MeshRenderer trong Prefab: {prefab.name}");
        return null;
    }

    /// <summary>
    /// Tính rotation của Home Base dựa trên hướng đi ra (exitPoint đầu tiên).
    /// </summary>
    private Quaternion CalculateHomeRotation(ChunkData chunk)
    {
        if (chunk.exitPoints.Count == 0)
        {
            return Quaternion.identity; // Không có exit, giữ nguyên hướng
        }

        // Lấy exit point đầu tiên (Base Chunk chỉ có 1 exit)
        Vector2Int exit = chunk.exitPoints[0];
        Vector2Int center = new Vector2Int(4, 4);

        // Xác định hướng dựa trên exit so với center
        if (exit.y > center.y) // Exit ở phía Bắc (trên)
        {
            return Quaternion.Euler(0, 0, 0); // Quay về Bắc (0°)
        }
        else if (exit.y < center.y) // Exit ở phía Nam (dưới)
        {
            return Quaternion.Euler(0, 180, 0); // Quay về Nam (180°)
        }
        else if (exit.x > center.x) // Exit ở phía Đông (phải)
        {
            return Quaternion.Euler(0, 90, 0); // Quay về Đông (90°)
        }
        else if (exit.x < center.x) // Exit ở phía Tây (trái)
        {
            return Quaternion.Euler(0, -90, 0); // Quay về Tây (-90° hoặc 270°)
        }

        return Quaternion.identity; // Fallback
    }

    #endregion
}
