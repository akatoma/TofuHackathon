using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//敵のメイン挙動
//敵オブジェクトにアタッチ
//弾丸と当たり判定は後々お引越し☆

public class EnemyController : MonoBehaviour, ISnapshotable, IFreezable
{
    // 敵が倒された瞬間に通知される。GameManagerなどはこれを購読するだけでよい
    public static event System.Action OnEnemyDefeated;

    [Header("Health")]
    public int maxHealth = 50;
    int currentHealth;
    bool isDead = false;

    [Header("Health Bar")]
    public EnemyHealthBar healthBar; // 頭上に配置したWorld Space Canvasをアサイン
    bool hasBeenHit = false;

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
    float attackTimer = 0f; // 次の攻撃までの残り時間。speedScaleの影響を受けて進む

    Rigidbody enemyRb;

    [Header("Save-State Highlight")]
    public Color highlightColor = Color.red; // セーブがある間、この色でEmission発光させる
    public float highlightIntensity = 2f;

    Renderer[] renderers;
    MaterialPropertyBlock propBlock;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Freeze Transition")]
    public float freezeDuration = 0.4f;   // スローになるまでの減速時間(秒)
    public float unfreezeDuration = 1.5f; // 通常速度に戻るまでの加速時間(秒)。長めに設定

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

        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, maxHealth);
            healthBar.SetVisible(false);
        }
    }

    void OnEnable()
    {
        SnapshotManager.OnSnapshotSaved += HandleSnapshotSaved;
        SnapshotManager.OnSnapshotCleared += HandleSnapshotCleared;

        // 有効化された時点で既にセーブがある状態なら、最初からハイライトしておく
        bool currentlySaved = SnapshotManager.Instance != null && SnapshotManager.Instance.HasSnapshot;
        SetHighlighted(currentlySaved);
    }

    void OnDisable()
    {
        SnapshotManager.OnSnapshotSaved -= HandleSnapshotSaved;
        SnapshotManager.OnSnapshotCleared -= HandleSnapshotCleared;
    }

    void HandleSnapshotSaved()
    {
        SetHighlighted(true);
    }

    void HandleSnapshotCleared()
    {
        SetHighlighted(false);
    }

    void SetHighlighted(bool on)
    {
        if (renderers == null)
        {
            return;
        }

        Color emission = on ? highlightColor * highlightIntensity : Color.black;

        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorId, emission);
            r.SetPropertyBlock(propBlock);
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
            //攻撃: クールダウンの経過もspeedScaleに合わせて遅くする
            attackTimer -= Time.fixedDeltaTime * speedScale;
            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                Attack();
            }
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
    void Die()
    {
        isDead = true;
        currentHealth = 0;

        // Destroyではなく非アクティブ化することで、
        // 巻き戻し(Rキー)でセーブ時点が「生存中」なら復活できるようにする
        gameObject.SetActive(false);
        Debug.Log($"{name} defeated.");

        OnEnemyDefeated?.Invoke();
    }

    void Attack()
    {
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
        bulletRb.velocity = direction * bulletSpeed; // 常に「本来の速度」でまず初期化する

        // 自分(敵)が現在スロー中なら、弾も生まれた瞬間からスローで始める
        if (speedScale < 1f)
        {
            FreezableRigidbody freezable = bullet.GetComponent<FreezableRigidbody>();
            if (freezable != null)
            {
                freezable.InitializeFrozen(speedScale);
            }
        }

        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet == null)
        {
            enemyBullet = bullet.AddComponent<EnemyBullet>();
        }

        enemyBullet.damage = attackDamage;
        enemyBullet.lifetime = bulletLifetime;
    }

    // TimeStopSkillから呼ばれる。瞬時ではなく、短時間かけて指定の速度倍率(slowFactor)まで減速する
    public void Freeze(float slowFactor)
    {
        StartSpeedTransition(Mathf.Clamp01(slowFactor), freezeDuration);
    }

    public void Unfreeze()
    {
        StartSpeedTransition(1f, unfreezeDuration);
    }

    void StartSpeedTransition(float targetScale, float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            // 非アクティブ(倒されて非表示中など)はコルーチンを開始できないため、
            // アニメーションなしで値だけ即座に反映する
            speedScale = targetScale;
            return;
        }

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
            GameManager gameManager = FindObjectOfType<GameManager>();
            gameManager.ShowHitPanel(0.3f);
            Destroy(gameObject);
            return;
        }
        Destroy(gameObject);
    }
    
}