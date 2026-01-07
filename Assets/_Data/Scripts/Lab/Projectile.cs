using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10.0f;
    [SerializeField] private float lifeTime = 5.0f;
    [SerializeField] Rigidbody rb;
    [SerializeField] private float rotationSpeed = 200.0f;

    Vector3 direction;
    private Transform target;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(float speed, Vector3 direction)
    {
        this.speed = speed;
        this.direction = direction.normalized;
        FindNearestEnemy();
        StartCoroutine(DestroyAfterTime());
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float minDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                target = enemy.transform;
            }
        }
    }

    IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    void Update()
    {
        // Nếu có target và target còn tồn tại, tự động đuổi theo
        if (target != null)
        {
            Vector3 targetDirection = (target.position - transform.position).normalized;
            direction = Vector3.RotateTowards(direction, targetDirection, rotationSpeed * Time.deltaTime * Mathf.Deg2Rad, 0.0f);
        }

        rb.velocity = direction * speed;
        transform.forward = direction;
    }
}
