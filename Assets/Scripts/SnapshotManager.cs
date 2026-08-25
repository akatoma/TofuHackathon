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

    [Header("Input")]
    public KeyCode saveKey = KeyCode.Q;
    public KeyCode loadKey = KeyCode.R; // 巻き戻しキー。要件があれば変更してください

    readonly Dictionary<ISnapshotable, object> snapshot = new Dictionary<ISnapshotable, object>();
    bool hasSnapshot = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            SaveSnapshot();
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
    }
}