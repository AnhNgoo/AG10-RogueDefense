using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float speed = 3.0f;

    private Transform target;

    void Start()
    {
        // Tìm Artillery trong scene
        GameObject artillery = GameObject.FindGameObjectWithTag("Artillery");
        if (artillery != null)
        {
            target = artillery.transform;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Artillery. Hãy đảm bảo Artillery có tag 'Artillery'");
        }
    }

    void Update()
    {
        if (target != null)
        {
            // Di chuyển về phía Artillery
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Xoay về phía Artillery
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Nếu va chạm với projectile thì chết ngay
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Destroy(gameObject);
            Destroy(collision.gameObject); // Hủy luôn projectile
        }
    }
}
