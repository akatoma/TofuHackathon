using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// シーンに1つだけ空オブジェクト(例: "GameManager")を作ってアタッチする。
// Qキー: その瞬間のシーン内の状態を保存
// Rキー: 直前に保存した状態まで巻き戻す
//
// 敵などISnapshotableを実装したオブジェクトは、
// 保存/復元のたびにシーンから自動的に集められるので、
// 個別の登録処理を書く必要はない。
public class SnapshotManager : MonoBehaviour
{
    public static SnapshotManager Instance { get; private set; }

    // 他のシステム(ゲージなど)がセーブ/ロードのタイミングを知りたいときに購読するイベント
    public static event System.Action OnSnapshotSaved;
    public static event System.Action OnSnapshotLoaded;
    public static event System.Action OnSnapshotCleared;

    public bool HasSnapshot => hasSnapshot;

    [Header("Input")]
    public KeyCode saveKey = KeyCode.Q;
    public KeyCode loadKey = KeyCode.R; // 巻き戻しキー。要件があれば変更してください

    readonly Dictionary<ISnapshotable, object> snapshot = new Dictionary<ISnapshotable, object>();
    bool hasSnapshot = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[SnapshotManager] 既存のInstanceがあるため、こちら({name})は自身を破棄します。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[SnapshotManager] Awake. Instance = {name} (id:{GetInstanceID()})");
    }

    void OnDestroy()
    {
        // シーン再ロード時、破棄される自分自身がInstanceを持ったままにならないようにする
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            // セーブデータが既にあれば削除、なければ新規に保存する(トグル式)
            if (hasSnapshot)
            {
                ClearSnapshot();
            }
            else
            {
                SaveSnapshot();
            }
        }
        else if (Input.GetKeyDown(loadKey))
        {
            LoadSnapshot();
        }
    }

    public void SaveSnapshot()
    {
        snapshot.Clear();

        // シーン内のISnapshotable実装オブジェクトを毎回集め直す
        // (敵の数が増減しても対応できるようにするため)
        foreach (ISnapshotable target in FindObjectsOfType<MonoBehaviour>().OfType<ISnapshotable>())
        {
            snapshot[target] = target.CaptureSnapshot();
        }

        hasSnapshot = true;
        Debug.Log($"[SnapshotManager] Saved. Targets: {snapshot.Count}");

        OnSnapshotSaved?.Invoke();
    }

    public void LoadSnapshot()
    {
        if (!hasSnapshot)
        {
            Debug.Log("[SnapshotManager] まだ保存されていません。");
            return;
        }

        foreach (KeyValuePair<ISnapshotable, object> pair in snapshot)
        {
            // 保存後に破棄されたオブジェクト(倒された敵など)は復元しない。
            // 破棄されたオブジェクトの「復活」まで扱いたい場合は、
            // 生成/破棄をこの仕組みとは別に管理する拡張が必要になる。
            if (pair.Key is MonoBehaviour mb && mb == null)
            {
                continue;
            }

            pair.Key.RestoreSnapshot(pair.Value);
        }

        Debug.Log("[SnapshotManager] Restored.");

        OnSnapshotLoaded?.Invoke();
    }

    public void ClearSnapshot()
    {
        snapshot.Clear();
        hasSnapshot = false;
        Debug.Log("[SnapshotManager] セーブデータを削除しました。");

        OnSnapshotCleared?.Invoke();
    }
}