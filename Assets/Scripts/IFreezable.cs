// TimeStopSkillの「スロー」対象になりたいオブジェクトが実装するインターフェース。
// 敵・弾など、プレイヤー以外で「動きを遅くしたい」ものすべてに実装させる。
public interface IFreezable
{
    // slowFactor: 0 = 完全停止, 1 = 通常速度。この間の値でスローの度合いを指定する
    void Freeze(float slowFactor);
    void Unfreeze();
}