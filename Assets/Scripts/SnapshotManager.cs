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

    // セーブ/ロードのタイミングを知りたいときに購読するイベント
    public static event System.Action OnSnapshotSaved;
    public static event System.Action OnSnapshotLoaded;
    public static event System.Action OnSnapshotCleared;

    public bool HasSnapshot => hasSnapshot;

    [Header("Input")]
    public KeyCode saveKey = KeyCode.Q; //セーブ
    public KeyCode loadKey = KeyCode.R; // 巻き戻し
    public KeyCode stopKey = KeyCode.E; //時間停止

    [Header("Time Stop")]
    public float duration = 3f; // 効果時間(秒)。0以下にすると、もう一度押すまで止まったままになる
    bool isActive = false;
    float remainingTime = 0f;
    readonly List<IFreezable> frozenTargets = new List<IFreezable>();
    public Transform playerTransform;

    [Header("Snapshot")]
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

        if (Input.GetKeyDown(stopKey))
        {
            if (isActive)
            {
                Deactivate(); // 発動中にもう一度押したら早期終了
            }
            else
            {
                TryActivate();
            }
        }

        if (isActive && duration > 0f)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                Deactivate();
            }
        }
    }

    void OnEnable()
    {
        // セーブが削除されたら、発動中でも強制的に解除する
        OnSnapshotCleared += HandleSnapshotCleared;
    }

    void OnDisable()
    {
        OnSnapshotCleared -= HandleSnapshotCleared;
    }

    //保存・やり直し
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

    //時間停止
    void HandleSnapshotCleared()
    {
        if (isActive)
        {
            Deactivate();
        }
    }

    void TryActivate()
    {
        bool hasSave = Instance != null && Instance.HasSnapshot;
        if (!hasSave)
        {
            Debug.Log("[TimeStopSkill] セーブがないため使用できません。");
            return;
        }

        Activate();
    }
    void Activate()
    {
        isActive = true;
        remainingTime = duration;

        frozenTargets.Clear();

        // シーン内のIFreezable実装オブジェクトを毎回集め直す
        foreach (IFreezable target in FindObjectsOfType<MonoBehaviour>().OfType<IFreezable>())
        {
            // // 自分自身(Player)配下のものは対象外
            // if (target is MonoBehaviour mb && mb.transform.root == transform.root)
            // {
            //     continue;
            // }

            if (target is MonoBehaviour mb && mb.transform.root == playerTransform.root)
            {
                continue; // ループ内なら continue;
            }

            target.Freeze();
            frozenTargets.Add(target);
        }

        Debug.Log($"[TimeStopSkill] Activated. Frozen: {frozenTargets.Count}");
    }
    public void Deactivate()
    {
        if (!isActive)
        {
            return;
        }

        foreach (IFreezable target in frozenTargets)
        {
            // 効果中に破棄されたオブジェクト(倒された敵など)はスキップ
            if (target is MonoBehaviour mb && mb == null)
            {
                continue;
            }

            target.Unfreeze();
        }

        frozenTargets.Clear();
        isActive = false;

        Debug.Log("[TimeStopSkill] Deactivated.");
    }
}