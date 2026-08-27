using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//敵のメイン挙動
//敵オブジェクトにアタッチ
//弾丸と当たり判定は後々お引越し☆

public class EnemyController : MonoBehaviour, ISnapshotable, IFreezable
{
    [Header("Health")]
    public int maxHealth = 50;
    int currentHealth;
    bool isDead = false;

    [Header("Health Bar")]
    public EnemyHealthBar healthBar; // 頭上に配置したWorld Space Canvasをアサイン
    bool hasBeenHit = false;

    [Header("Damage Flash")]
    public float flashDuration = 0.1f; // 点滅する時間(秒)
    public Color flashColor = Color.red; // 点滅時の色
    private List<Material> enemyMaterials = new List<Material>();
    private List<Color> originalColors = new List<Color>();
    private Coroutine flashCoroutine;

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

    [Header("Freeze Transition")]
    public float freezeDuration = 0.4f;   // 停止までの減速時間(秒)
    public float unfreezeDuration = 0.4f; // 再開までの加速時間(秒)

    // TimeStopSkillによる速度倍率。1=通常速度、0=完全停止。瞬時ではなく徐々に変化する
    float speedScale = 1f;
    Coroutine speedTransitionCoroutine;

    class State
    {
        public Vector3 position;
        public Quaternion rotation;
        public int health;
        public bool isDead;
        public bool hasBeenHit;
    }

    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, maxHealth);
            healthBar.SetVisible(false);
        }

        // 自身および子オブジェクトのRendererからマテリアルと元の色を取得して保持
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    enemyMaterials.Add(mat);
                    originalColors.Add(mat.color);
                }
            }
        }
    }


    void FixedUpdate()
    {
        if (speedScale <= 0f)
        {
            return; // 完全に停止しきっている
        }

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
            enemyRb.MovePosition(enemyRb.position + retreatDirection * retreatSpeed * speedScale * Time.fixedDeltaTime);
        }
        else if (distance >= moveDistance)
        {
            // 接近
            Vector3 direction = toTarget / distance;
            enemyRb.MovePosition(enemyRb.position + direction * moveSpeed * speedScale * Time.fixedDeltaTime);
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
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage. Remaining: {currentHealth}");

        // ダメージ時の赤色点滅処理を呼び出し
        FlashRed();

        if (!hasBeenHit)
        {
            hasBeenHit = true;
            healthBar?.SetVisible(true);
        }
        healthBar?.SetHealth(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void FlashRed()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // マテリアルの色を赤に変更
        for (int i = 0; i < enemyMaterials.Count; i++)
        {
            if (enemyMaterials[i] != null)
            {
                enemyMaterials[i].color = flashColor;
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // 元の色に戻す
        ResetColor();
    }

    void ResetColor()
    {
        for (int i = 0; i < enemyMaterials.Count; i++)
        {
            if (enemyMaterials[i] != null)
            {
                enemyMaterials[i].color = originalColors[i];
            }
        }
    }

    void Die()
    {
        isDead = true;
        currentHealth = 0;

        // 死亡時に点滅を停止し元の色に戻す
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        ResetColor();

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

        EnemyBullet enemyBullet = BulletPool.Instance.Spawn(bulletPrefab);
        enemyBullet.Fire(
            transform.position + direction * 0.8f,
            Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f),
            direction * bulletSpeed,
            attackDamage,
            bulletLifetime
        );
    }

    // TimeStopSkillから呼ばれる。瞬時ではなく、短時間かけて減速して止まる
    public void Freeze()
    {
        StartSpeedTransition(0f, freezeDuration);
    }

    public void Unfreeze()
    {
        StartSpeedTransition(1f, unfreezeDuration);
    }

    void StartSpeedTransition(float targetScale, float duration)
    {
        if (speedTransitionCoroutine != null)
        {
            StopCoroutine(speedTransitionCoroutine);
        }
        speedTransitionCoroutine = StartCoroutine(SpeedTransitionRoutine(targetScale, duration));
    }

    IEnumerator SpeedTransitionRoutine(float targetScale, float duration)
    {
        float start = speedScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = duration > 0f ? t / duration : 1f;
            speedScale = Mathf.Lerp(start, targetScale, p);
            yield return null;
        }

        speedScale = targetScale;
    }

    //保存
    public object CaptureSnapshot()
    {
        return new State
        {
            position = transform.position,
            rotation = transform.rotation,
            health = currentHealth,
            isDead = isDead,
            hasBeenHit = hasBeenHit
        };
    }

    //復元
    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not State state)
        {
            return;
        }

        // スナップショット復元時にも色をリセット
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        ResetColor();

        transform.position = state.position;
        transform.rotation = state.rotation;
        currentHealth = state.health;
        isDead = state.isDead;
        hasBeenHit = state.hasBeenHit;

        // セーブ時点で生きていたなら再アクティブ化、死んでいたなら非アクティブのまま
        gameObject.SetActive(!isDead);

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, maxHealth);
            healthBar.SetVisible(hasBeenHit);
        }
    }
}