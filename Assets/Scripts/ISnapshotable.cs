// 巻き戻し(セーブ/ロード)の対象になりたいオブジェクトが実装するインターフェース。
// Player自身やこの後実装する敵など、状態を保存・復元したい対象すべてに実装させる。
public interface ISnapshotable
{
    // その瞬間の状態を、任意のデータにまとめて返す
    object CaptureSnapshot();

    // CaptureSnapshotで返したデータを受け取り、その状態に戻す
    void RestoreSnapshot(object snapshot);
}