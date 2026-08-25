using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour, ISnapshotable
{
    [Header("Movement")]
    public float moveSpeed = 7f;

    [Header("Mouse Look (Yaw)")]
    public float mouseSensitivity = 220f;
    public bool lockCursor = true;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;   // 拳が届く距離
    public float attackRadius = 0.4f;  // 判定の太さ(SphereCastの半径)
    public float attackCooldown = 0.5f; // 連打制限(秒)
    float nextAttackTime;


    [Header("References")]
    public Transform attackOrigin;      // Head(カメラ)のTransformをアサイン
    public LayerMask hittableLayers = ~0; // Playerレイヤーは除外しておくこと
    public CameraController cameraController;


    // セーブ/巻き戻し(PlayerSnapshot)からこの値を読み書きできるようにする。
    // これをRigidbodyの回転と一緒に復元しないと、次のFixedUpdateで
    // 巻き戻し前のyawに上書きされてしまう
    public float Yaw
    {
        get => yaw;
        set => yaw = value;
    }

    Rigidbody rb;
    Vector3 inputDirection = Vector3.zero;
    float yaw;

    class State
    {
        public Vector3 position;
        public float yaw;
        public float pitch;
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // 衝突などで物理的に転倒しないようにする

        yaw = transform.eulerAngles.y;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(rotation);

        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 move = forward * inputDirection.z + right * inputDirection.x;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }

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
            if (hit.collider.transform.root == transform.root)continue;

            EnemyController damageable = hit.collider.GetComponentInParent<EnemyController>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                hitCount++;
            }
            Debug.Log($"[PlayerAttack] Attack fired. Hits: {hitCount}");
        }

        // TODO: パンチのアニメーション再生、SE再生などをここに追加
    }

    // Sceneビューで攻撃範囲を確認できるようにする
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = attackOrigin.position;
        Vector3 end = origin + attackOrigin.forward * attackRange;
        Gizmos.DrawWireSphere(origin, attackRadius);
        Gizmos.DrawWireSphere(end, attackRadius);
        Gizmos.DrawLine(origin, end);
    }

    public object CaptureSnapshot()
    {
        return new State
        {
            position = rb.position,
            yaw = Yaw,
            pitch = cameraController.Pitch,
            velocity = rb.velocity,       
            angularVelocity = rb.angularVelocity
        };
    }

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
    }
}