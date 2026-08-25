using UnityEngine;

// 敵システム実装前の、動作確認用の仮ターゲット。
// シーン内の適当なCube等にColliderと一緒にこのスクリプトを付けておけば、
// 攻撃の判定範囲やダメージ処理が正しく動くか確認できる。
// 本実装の敵ができたら、このスクリプトの代わりにIDamageableを実装すればよい。
public class TestDummy : MonoBehaviour, IDamageable
{
    public int health = 100;

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"{name} took {amount} damage. Remaining: {health}");

        if (health <= 0)
        {
            Debug.Log($"{name} destroyed.");
            Destroy(gameObject);
        }
    }
}