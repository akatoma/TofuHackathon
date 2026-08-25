using UnityEngine;

// 敵オブジェクトにアタッチする。同じGameObjectにCollider(トリガーではない通常のもの)が必要。
// PlayerAttackのHittable Layersに、このオブジェクトのレイヤーを含めておくこと。
//
// IDamageable   : PlayerAttackから殴られたときにダメージを受ける
// ISnapshotable : SnapshotManagerのQ/Rで状態を保存・復元される
[RequireComponent(typeof(Collider))]
public class EnemySaveAndDamage : MonoBehaviour,  ISnapshotable
{
    [Header("Health")]
    public int maxHealth = 50;

    int currentHealth;
    bool isDead = false;

    // 保存したい情報。AIの状態などが増えたらここに追加していく
    class State
    {
        public Vector3 position;
        public Quaternion rotation;
        public int health;
        public bool isDead;
    }

    void Awake()
    {
        currentHealth = maxHealth;
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