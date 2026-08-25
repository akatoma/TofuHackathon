// ダメージを受けられるオブジェクトが実装するインターフェース。
// 後で敵(EnemyController等)を作るときは、これを実装するだけで
// PlayerAttack側のコードを一切変更せずに攻撃対象にできる。
public interface IDamageable
{
    void TakeDamage(int amount);
}