using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 5.0f;
    [SerializeField] private float spawnHeight = 0.5f;

    private GameObject[] groundObjects;

    void Start()
    {
        // Tìm tất cả objects có tag "Ground"
        groundObjects = GameObject.FindGameObjectsWithTag("Ground");

        if (groundObjects.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy object nào có tag 'Ground'. Hãy thêm tag 'Ground' cho Plane");
            return;
        }

        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (groundObjects.Length == 0 || enemyPrefab == null)
            return;

        // Chọn ngẫu nhiên một ground object
        GameObject randomGround = groundObjects[Random.Range(0, groundObjects.Length)];

        // Lấy bounds của ground object
        Renderer groundRenderer = randomGround.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            Bounds bounds = groundRenderer.bounds;

            // Tạo vị trí spawn ngẫu nhiên trong bounds
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 spawnPosition = new Vector3(randomX, bounds.max.y + spawnHeight, randomZ);

            // Spawn enemy
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
