using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Playerにアタッチする。
// セーブがある間だけ発動できるスキル。発動中、IFreezableを実装した
// プレイヤー以外のオブジェクトすべての動きを止める。
public class TimeStopSkill : MonoBehaviour
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.E; // 発動キー。要件があれば変更してください

    [Header("Settings")]
    public float duration = 3f; // 効果時間(秒)。0以下にすると、もう一度押すまで止まったままになる
    [Range(0f, 1f)]
    public float slowFactor = 0.15f; // 0=完全停止, 1=通常速度。スローの度合い

    [Header("Effect UI")]
    public Image vignetteImage; // UIVignetteシェーダーのMaterialを設定したUI Image
    public Text remainingTimeText; // 残り時間表示用(任意、TextMeshProを使う場合は型を変更してください)

    bool isActive = false;
    float remainingTime = 0f;
    readonly List<IFreezable> frozenTargets = new List<IFreezable>();

    void OnEnable()
    {
        // セーブが削除されたら、発動中でも強制的に解除する
        SnapshotManager.OnSnapshotCleared += HandleSnapshotCleared;

        ResetEffectUI();
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

        if (isActive)
        {
            // 発動中に新しく生まれたオブジェクト(敵が撃った新しい弾など)も
            // 生まれた瞬間からスロー対象に加える
            CaptureNewTargets();

            if (duration > 0f)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime <= 0f)
                {
                    Deactivate();
                }
            }

            UpdateEffectUI();
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
        CaptureNewTargets();

        Debug.Log($"[TimeStopSkill] Activated. Frozen: {frozenTargets.Count}");
    }

    void CaptureNewTargets()
    {
        // シーン内のIFreezable実装オブジェクトを毎回集め直し、
        // まだ対象になっていないものだけ新たにスローにする
        foreach (IFreezable target in FindObjectsOfType<MonoBehaviour>().OfType<IFreezable>())
        {
            // 自分自身(Player)配下のものは対象外
            if (target is MonoBehaviour mb && mb.transform.root == transform.root)
            {
                continue;
            }

            if (frozenTargets.Contains(target))
            {
                continue; // 既にスロー対象になっている
            }

            target.Freeze(slowFactor);
            frozenTargets.Add(target);
        }
    }

    void UpdateEffectUI()
    {
        if (vignetteImage != null && vignetteImage.material != null)
        {
            // スローが強い(slowFactorが小さい)ほどVignetteを強くする
            float strength = 1f - slowFactor;
            vignetteImage.material.SetFloat("_Intensity", strength);
        }

        if (remainingTimeText != null)
        {
            remainingTimeText.text = duration > 0f
                ? Mathf.CeilToInt(remainingTime).ToString()
                : ""; // 手動解除モード(duration<=0)の場合は数字を出さない
        }
    }

    void ResetEffectUI()
    {
        if (vignetteImage != null && vignetteImage.material != null)
        {
            vignetteImage.material.SetFloat("_Intensity", 0f);
        }

        if (remainingTimeText != null)
        {
            remainingTimeText.text = "";
        }
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

        ResetEffectUI();

        Debug.Log("[TimeStopSkill] Deactivated.");
    }
}