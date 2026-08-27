using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour, ISnapshotable
{
    [Header("Movement")]
    public float moveSpeed = 7f;

    [Header("Look (Yaw)")]
    public float mouseSensitivity = 220f;
    public bool lockCursor = true;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;   // 拳が届く距離
    public float attackRadius = 0.4f;  // 判定の太さ(SphereCastの半径)
    public float attackCooldown = 0.5f; // 連打制限(秒)
    float nextAttackTime;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Bullet")]
    public string bulletTag = "Bullet"; // BulletPrefab側にこのタグを付けておく
    public int bulletDamage = 10;

    [Header("Death")]
    public UnityEvent onGameOver; // セーブがない状態で死亡した時の処理をInspectorで割り当てる

    // 体力が変化するたびに(current, max)を通知する。UI側はこれを購読するだけでよい
    public event System.Action<int, int> OnHealthChanged;

    [Header("References")]
    public Transform attackOrigin;      // Head(カメラ)のTransformをアサイン
    public LayerMask hittableLayers = ~0; // Playerレイヤーは除外しておくこと
    public CameraController cameraController;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float groundCheckDistance = 1.1f; // カプセルの高さに合わせて調整してください
    public LayerMask groundLayer = ~0;       // 実際は「Ground」など地面用レイヤーに絞るのを推奨

    [Header("Slide")]
    public KeyCode slideKey = KeyCode.LeftShift;
    public float slideForce = 15f;          // 発動時にかける衝撃の強さ
    public float slideDuration = 0.5f;      // この間、通常移動を止めて慣性に任せる
    public float slideHeadHeight = 0.5f;    // スライディング中のHeadのローカルY座標
    public float headHeightTransition = 0.15f; // 頭の高さが切り替わる時間

    bool isSliding = false;
    float slideEndTime = 0f;
    float standingHeadHeight;
    Coroutine headHeightCoroutine;

    // セーブ/巻き戻しからこの値を読み書きできるようにする。
    // これをRigidbodyの回転と一緒に復元しないと、次のFixedUpdateで
    // 巻き戻し前のyawに上書きされてしまう
    public float Yaw { get; set; }

    Rigidbody rb;
    Vector3 inputDirection = Vector3.zero;

    bool hasSaveAvailable = false; // Qで一度でもセーブされていればtrue

    class State
    {
        public Vector3 position;
        public float yaw; //体の左右回転
        public float pitch;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public int health;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // FixedUpdateではなくここで一度だけ取得する

        Yaw = transform.eulerAngles.y;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (cameraController != null)
        {
            standingHeadHeight = cameraController.transform.localPosition.y;
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnEnable()
    {
        SnapshotManager.OnSnapshotSaved += HandleSnapshotSaved;
        SnapshotManager.OnSnapshotCleared += HandleSnapshotCleared;
    }

    void OnDisable()
    {
        SnapshotManager.OnSnapshotSaved -= HandleSnapshotSaved;
        SnapshotManager.OnSnapshotCleared -= HandleSnapshotCleared;
    }

    void HandleSnapshotSaved()
    {
        hasSaveAvailable = true;
    }

    void HandleSnapshotCleared()
    {
        hasSaveAvailable = false;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector3(h, 0f, v);

        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }
        // 一人称視点なので、体の左右回転(Yaw)はプレイヤー自身がマウスXから直接受け取る
        Yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }

        if (Input.GetKeyDown(slideKey) && !isSliding)
        {
            StartSlide();
        }

        if (isSliding && Time.time >= slideEndTime)
        {
            EndSlide();
        }
    }

    void FixedUpdate()
    {
        // 視点の左右回転はスライディング中も継続する
        Quaternion rotation = Quaternion.Euler(0f, Yaw, 0f);
        rb.MoveRotation(rotation);

        if (isSliding)
        {
            // スライディング中は、毎FixedUpdateで位置を上書きする通常移動を止めて
            // Rigidbodyの慣性(AddForceで与えた衝撃)にそのまま任せる
            return;
        }

        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 move = forward * inputDirection.z + right * inputDirection.x;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }

    void TryJump()
    {
        if (!IsGrounded())
        {
            return;
        }

        // 前フレームまでの落下速度を打ち消してから加える(ジャンプ高さを安定させるため)
        Vector3 velocity = rb.velocity;
        velocity.y = 0f;
        rb.velocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    void StartSlide()
    {
        isSliding = true;
        slideEndTime = Time.time + slideDuration;

        // 発動した瞬間に向いている方向へ強い衝撃を与える
        Vector3 direction = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        rb.AddForce(direction * slideForce, ForceMode.Impulse);

        SetHeadHeight(slideHeadHeight);
    }

    void EndSlide()
    {
        isSliding = false;
        SetHeadHeight(standingHeadHeight);
    }

    void SetHeadHeight(float targetY)
    {
        if (cameraController == null)
        {
            return;
        }

        if (headHeightCoroutine != null)
        {
            StopCoroutine(headHeightCoroutine);
        }
        headHeightCoroutine = StartCoroutine(HeadHeightRoutine(targetY));
    }

    IEnumerator HeadHeightRoutine(float targetY)
    {
        Transform head = cameraController.transform;
        float startY = head.localPosition.y;
        float t = 0f;

        while (t < headHeightTransition)
        {
            t += Time.deltaTime;
            float p = headHeightTransition > 0f ? t / headHeightTransition : 1f;
            Vector3 pos = head.localPosition;
            pos.y = Mathf.Lerp(startY, targetY, p);
            head.localPosition = pos;
            yield return null;
        }

        Vector3 finalPos = head.localPosition;
        finalPos.y = targetY;
        head.localPosition = finalPos;
    }

    //攻撃
    void Attack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }
        nextAttackTime = Time.time + attackCooldown;

        Vector3 origin = attackOrigin.position;
        Vector3 direction = attackOrigin.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin, attackRadius, direction, attackRange, hittableLayers, QueryTriggerInteraction.Ignore);

        int hitCount = 0;

        foreach (RaycastHit hit in hits)
        {
            // 自分自身(Playerの子オブジェクトなど)は除外
            if (hit.collider.transform.root == transform.root) continue;

            EnemyController damageable = hit.collider.GetComponentInParent<EnemyController>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                hitCount++;
            }
        }
    }
    void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag(bulletTag))
        {
            return;
        }

        if (isSliding)
        {
            return; // スライディング中は無敵(ヒットパネルも出さない)
        }

        TakeDamage(bulletDamage);
        GameManager gameManager = FindObjectOfType<GameManager>();
        gameManager.ShowHitPanel(0.3f);
    }
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        if (isSliding)
        {
            Debug.Log("[PlayerController] Sliding - damage ignored.");
            return; // 念のためこちらでも無敵を保証しておく
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"[PlayerController] Bullet hit. HP: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            HandleDeath();
        }
    }

    //Player死亡時の遷移
    void HandleDeath()
    {
        if (hasSaveAvailable && SnapshotManager.Instance != null)
        {
            Debug.Log("[PlayerController] セーブ地点まで強制的に巻き戻します。");
            SnapshotManager.Instance.LoadSnapshot();
        }
        else
        {
            Debug.Log("[PlayerController] セーブがないためゲームオーバー。");
            onGameOver?.Invoke();
        }
    }

    // Sceneビューで攻撃範囲を確認できる
    void OnDrawGizmosSelected()
    {
        if (attackOrigin == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Vector3 origin = attackOrigin.position;
        Vector3 end = origin + attackOrigin.forward * attackRange;
        Gizmos.DrawWireSphere(origin, attackRadius);
        Gizmos.DrawWireSphere(end, attackRadius);
        Gizmos.DrawLine(origin, end);
    }

    //保存
    public object CaptureSnapshot()
    {
        return new State
        {
            position = rb.position,
            yaw = Yaw,
            pitch = cameraController.Pitch,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
            health = currentHealth
        };
    }

    //復元
    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not State state)
        {
            return;
        }

        rb.position = state.position;
        rb.rotation = Quaternion.Euler(0f, state.yaw, 0f);
        rb.velocity = state.velocity;
        rb.angularVelocity = state.angularVelocity;

        // Rigidbodyの回転だけでなく、PlayerController/CameraControllerが
        // 内部で持っているyaw/pitchも一緒に書き戻す(これをしないと
        // 次のフレームで元の向きに上書きされてしまう)
        Yaw = state.yaw;
        cameraController.Pitch = state.pitch;

        // HPもセーブ時点の値に戻す(HPスライダーにも通知する)
        currentHealth = state.health;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}