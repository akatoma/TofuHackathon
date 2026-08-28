using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//敵のメイン挙動
//敵オブジェクトにアタッチ

public class EnemyController : MonoBehaviour, ISnapshotable, IFreezable
{
    // 敵が倒された瞬間に通知される。倒されたGameObject自身を渡すので、
    // 購読側でタグなどを見て判定できる
    public static event System.Action<GameObject> OnEnemyDefeated;

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

    [Header("Pickable Impact Damage & Knockback Settings")]
    public ObjectPicker objectPicker; // プレイヤーのObjectPickerをアサイン
    public string pickableTag = "Pickable";
    public float minImpactVelocity = 3f;           // ダメージ・ノックバックを発生させる最低衝突速度
    public float damageMultiplier = 2f;            // 速度に対するダメージ倍率
    public float knockbackMultiplier = 1.0f;       // 通常のノックバック力倍率
    public float handHeldKnockbackMultiplier = 2.0f; // 手持ち時の追加ノックバック倍率（例: 2.0なら手持ちで吹き飛ばし2倍）
    public float upwardForceMultiplier = 0.05f;    // 上方向へ浮かす力の倍率

    [Header("Tracking")]
    public float moveSpeed = 3f;
    public float moveDistance = 6f;
    public float retreatDistance = 2f;
    public float retreatSpeed = 1f;
    public Transform target;
    bool isRetreating = false;

    [Header("Attack")]
    public int attackDamage = 40;
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

    [Header("Audio")]
    public AudioSource audioBullet;
    public AudioSource audioHand;

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

        propBlock = new MaterialPropertyBlock();

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
            // 攻撃: クールダウンの経過もspeedScaleに合わせて遅くする
            attackTimer -= Time.fixedDeltaTime * speedScale;
            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                Attack();
            }
        }
        
        enemyRb.MoveRotation(Quaternion.LookRotation(toTarget.normalized, Vector3.up));
    }

    // Pickableオブジェクトとの衝突判定処理（ダメージ＋後ろ方向ノックバック）
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag(pickableTag))
        {
            // 物理衝突時の相手の相対速度
            float impactSpeed = collision.relativeVelocity.magnitude;
            bool isHeldByPlayer = false;

            // 手持ち中であれば振り回し速度を加味する
            if (objectPicker != null)
            {
                // 手から離れていないかの判定
                isHeldByPlayer = objectPicker.IsHoldingObject;

                if (isHeldByPlayer && objectPicker.HeldObjectVelocity.magnitude > minImpactVelocity)
                {
                    float holdSpeed = objectPicker.HeldObjectVelocity.magnitude;
                    if (holdSpeed > impactSpeed)
                    {
                        impactSpeed = holdSpeed;
                    }
                }
            }

            // 一定以上のスピードでぶつかった場合のみダメージ＆ノックバック
            if (impactSpeed >= minImpactVelocity)
            {
                // 1. ダメージ処理
                int calculatedDamage = Mathf.RoundToInt(impactSpeed * damageMultiplier);
                TakeDamage(calculatedDamage);

                // 2. ノックバック処理（手持ち状態なら倍率を掛ける）
                float effectiveKnockbackMultiplier = knockbackMultiplier;
                if (isHeldByPlayer)
                {
                    effectiveKnockbackMultiplier *= handHeldKnockbackMultiplier;
                }

                ApplyKnockback(impactSpeed, effectiveKnockbackMultiplier);

                Debug.Log($"Hit by {collision.gameObject.name}! Speed: {impactSpeed:F1}, Damage: {calculatedDamage}, Held: {isHeldByPlayer}");
            }
        }
    }

    void ApplyKnockback(float speed, float currentKnockbackMultiplier)
    {
        if (enemyRb == null) return;

        // 敵の向き基準で「後ろ方向（-transform.forward）」を算出
        Vector3 backwardDirection = -transform.forward;
        backwardDirection.y = 0f;
        backwardDirection.Normalize();

        // ノックバックベクトルの計算（真後ろ + 少し上方への力）
        Vector3 knockbackForce = (backwardDirection + Vector3.up * upwardForceMultiplier) * (speed * currentKnockbackMultiplier);

        // 瞬発的な力として加える
        enemyRb.AddForce(knockbackForce, ForceMode.Impulse);
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage. Remaining: {currentHealth}");

        audioHand.Play();

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

        OnEnemyDefeated?.Invoke(gameObject);
    }

    void Attack()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.1f)
        {
            return;
        }

        audioBullet.Play();
        direction.Normalize();

        EnemyBullet enemyBullet = BulletPool.Instance.Spawn(bulletPrefab);
        enemyBullet.Fire(
            transform.position + direction * 0.8f + Vector3.up * 0.5f, // 少し前方かつ上方に発射位置を調整
            Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f),
            direction * bulletSpeed,
            attackDamage,
            bulletLifetime
        );
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