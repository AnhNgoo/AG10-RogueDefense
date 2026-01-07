using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Artillery : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRange = 20.0f; // Phạm vi bắn tối đa

    private bool gameOver = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FireProjectile());
    }

    IEnumerator FireProjectile()
    {
        while (true)
        {
            yield return new WaitForSeconds(3.0f);

            if (!gameOver)
            {
                // Tìm enemy gần nhất
                GameObject nearestEnemy = FindNearestEnemy();

                if (nearestEnemy != null)
                {
                    GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                    Projectile projectile = projectileObject.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        Vector3 direction = (nearestEnemy.transform.position - firePoint.position).normalized;
                        projectile.Init(10.0f, direction);
                    }
                }
            }
        }
    }

    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            // Chỉ xét enemy trong phạm vi bắn
            if (distance < minDistance && distance <= fireRange)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra nếu enemy chạm vào Artillery
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GameOver();
        }
    }

    void GameOver()
    {
        if (!gameOver)
        {
            gameOver = true;
            Debug.Log("Game Over! Enemy đã chạm vào Artillery!");

            // Dừng game
            Time.timeScale = 0f;

            // Có thể reload scene hoặc hiển thị UI game over
            // Uncomment dòng dưới nếu muốn reload scene sau 2 giây
            // StartCoroutine(ReloadSceneAfterDelay(2.0f));
        }
    }

    IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
