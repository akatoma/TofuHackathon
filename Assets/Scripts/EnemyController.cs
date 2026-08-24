using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("仮置き")]
    public GameObject panel;

    [Header("Tracking")]
    public float moveSpeed = 3f;
    public float moveDistance = 15f;
    public float retreatDistance = 4f;
    public float retreatSpeed = 2f;
    public Transform target;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackCooldown = 1f;
    public float bulletSpeed = 15f;
    public float bulletLifetime = 3f;
    public GameObject bulletPrefab;

    Rigidbody enemyRb;
    float nextAttackTime;
    bool isRetreating = false;

    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
    }


    void FixedUpdate()
    {
        Vector3 toTarget = target.position - enemyRb.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        Debug.Log(distance);

        if (distance <= retreatDistance)
        {
            isRetreating = true;
        }
        else if (distance >= retreatDistance + 4)
        {
            isRetreating = false;
        }

        if (isRetreating)
        {
            // 後退
            Vector3 retreatDirection = -toTarget.normalized;
            enemyRb.MovePosition(enemyRb.position + retreatDirection * retreatSpeed * Time.fixedDeltaTime);
        }
        else if (distance >= moveDistance)
        {
            // 接近
            Vector3 direction = toTarget / distance;
            enemyRb.MovePosition(enemyRb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        else 
        {
            //攻撃
            Attack();
        }
        
        enemyRb.MoveRotation(Quaternion.LookRotation(toTarget.normalized, Vector3.up));
    }

    void Attack()
    {
        Debug.Log("Attack");
        if (Time.time < nextAttackTime)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.1f)
        {
            return;
        }

        direction.Normalize();
        GameObject bullet = Instantiate(
            bulletPrefab,
            transform.position + direction * 0.8f,
            Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f));

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.velocity = direction * bulletSpeed;

        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet == null)
        {
            enemyBullet = bullet.AddComponent<EnemyBullet>();
        }

        enemyBullet.damage = attackDamage;
        enemyBullet.lifetime = bulletLifetime;
        enemyBullet.panel = panel; 

        nextAttackTime = Time.time + attackCooldown;
    }
}

class EnemyBullet : MonoBehaviour
{
    public int damage;
    public float lifetime;
    public GameObject panel;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.gameObject.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // player.TakeDamage(damage);
            Collider bulletCollider = GetComponent<Collider>();
            if (bulletCollider != null)
            {
                bulletCollider.enabled = false;
            }

            StartCoroutine(PanelRoutine(0.3f)); // 仮ダメージ処理
            return;
        }

        Destroy(gameObject);
    }

    //仮ダメージ処理
    private IEnumerator PanelRoutine(float seconds)
    {
        if (panel == null)
        {
            Destroy(gameObject);
            yield break;
        }

        panel.SetActive(true);
        yield return new WaitForSeconds(seconds); 
        panel.SetActive(false);
        Destroy(gameObject);
    }
}
