using UnityEngine;

// Playerオブジェクトにアタッチする。
// Attack Origin には Head(カメラ)のTransformをアサインすること。
// (視線の上下(ピッチ)も攻撃方向に反映されるため)
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;   // 拳が届く距離
    public float attackRadius = 0.4f;  // 判定の太さ(SphereCastの半径)
    public float attackCooldown = 0.5f; // 連打制限(秒)

    [Header("References")]
    public Transform attackOrigin;      // Head(カメラ)のTransformをアサイン
    public LayerMask hittableLayers = ~0; // Playerレイヤーは除外しておくこと

    [Header("Debug")]
    public bool debugLog = true; // 敵未実装の間、判定確認用にログを出す

    float lastAttackTime = -999f;

    void Awake()
    {
        if (attackOrigin == null)
        {
            attackOrigin = Camera.main != null ? Camera.main.transform : transform;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        Vector3 origin = attackOrigin.position;
        Vector3 direction = attackOrigin.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin, attackRadius, direction, attackRange, hittableLayers, QueryTriggerInteraction.Ignore);

        int hitCount = 0;

        foreach (RaycastHit hit in hits)
        {
            // 自分自身(Playerの子オブジェクトなど)は除外
            if (hit.collider.transform.root == transform.root)
            {
                continue;
            }

            EnemyController damageable = hit.collider.GetComponentInParent<EnemyController>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                hitCount++;
            }
        }

        if (debugLog)
        {
            Debug.Log($"[PlayerAttack] Attack fired. Hits: {hitCount}");
        }

        // TODO: パンチのアニメーション再生、SE再生などをここに追加
    }

    // Sceneビューで攻撃範囲を確認できるようにする
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
}