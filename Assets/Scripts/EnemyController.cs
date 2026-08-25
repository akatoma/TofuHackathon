using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//敵のメイン挙動
//敵オブジェクトにアタッチ
//弾丸と当たり判定は後々お引越し☆

public class EnemyController : MonoBehaviour,  ISnapshotable
{
    [Header("Health")]
    public int maxHealth = 50;
    int currentHealth;
    bool isDead = false;

    [Header("Tracking")]
    public float moveSpeed = 3f;
    public float moveDistance = 15f;
    public float retreatDistance = 2f;
    public float retreatSpeed = 2f;
    public Transform target;
    bool isRetreating = false;


    [Header("Attack")]
    public int attackDamage = 10;
    public float attackCooldown = 1f;
    public float bulletSpeed = 15f;
    public float bulletLifetime = 3f;
    public GameObject bulletPrefab;
    float nextAttackTime;

    Rigidbody enemyRb;

    class State
    {
        public Vector3 position;
        public Quaternion rotation;
        public int health;
        public bool isDead;
    }

    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
    }


    void FixedUpdate()
    {
        Vector3 toTarget = target.position - enemyRb.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        //Debug.Log(distance);

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

    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage. Remaining: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        isDead = true;
        currentHealth = 0;

        // Destroyではなく非アクティブ化することで、
        // 巻き戻し(Rキー)でセーブ時点が「生存中」なら復活できるようにする
        gameObject.SetActive(false);
        Debug.Log($"{name} defeated.");
    }

    void Attack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }
        nextAttackTime = Time.time + attackCooldown;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.1f)
        {
            return;
        }
        
        GetComponent<AudioSource>().Play();
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
    }

    //保存
    public object CaptureSnapshot()
    {
        return new State
        {
            position = transform.position,
            rotation = transform.rotation,
            health = currentHealth,
            isDead = isDead
        };
    }

    //復元
    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not State state)
        {
            return;
        }

        transform.position = state.position;
        transform.rotation = state.rotation;
        currentHealth = state.health;
        isDead = state.isDead;

        // セーブ時点で生きていたなら再アクティブ化、死んでいたなら非アクティブのまま
        gameObject.SetActive(!isDead);
    }
}


//弾丸の挙動☆
class EnemyBullet : MonoBehaviour
{
    public int damage;
    public float lifetime;

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

            GameManager panelController =
                gameObject.GetComponentInParent<GameManager
                >();

            if (panelController != null)
            {
                panelController.ShowHitPanel(0.3f);
            }
            Destroy(gameObject);
            return;
        }
        Destroy(gameObject);
    }
    
}
