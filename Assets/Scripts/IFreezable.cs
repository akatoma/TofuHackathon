// TimeStopSkillの「停止」対象になりたいオブジェクトが実装するインターフェース。
// 敵・弾など、プレイヤー以外で「動きを止めたい」ものすべてに実装させる。
public interface IFreezable
{
    void Freeze();
    void Unfreeze();
}