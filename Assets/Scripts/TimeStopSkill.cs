using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Playerにアタッチする。
// セーブがある間だけ発動できるスキル。発動中、IFreezableを実装した
// プレイヤー以外のオブジェクトすべての動きを止める。
public class TimeStopSkill : MonoBehaviour
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.E; // 発動キー。要件があれば変更してください

    [Header("Settings")]
    public float duration = 3f; // 効果時間(秒)。0以下にすると、もう一度押すまで止まったままになる

    bool isActive = false;
    float remainingTime = 0f;
    readonly List<IFreezable> frozenTargets = new List<IFreezable>();

    void OnEnable()
    {
        // セーブが削除されたら、発動中でも強制的に解除する
        SnapshotManager.OnSnapshotCleared += HandleSnapshotCleared;
    }

    void OnDisable()
    {
        SnapshotManager.OnSnapshotCleared -= HandleSnapshotCleared;
    }

    void HandleSnapshotCleared()
    {
        if (isActive)
        {
            Deactivate();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(activateKey))
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

    void TryActivate()
    {
        bool hasSave = SnapshotManager.Instance != null && SnapshotManager.Instance.HasSnapshot;
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
            // 自分自身(Player)配下のものは対象外
            if (target is MonoBehaviour mb && mb.transform.root == transform.root)
            {
                continue;
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