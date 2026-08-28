using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.PostProcessing;

// シーンに1つだけ空オブジェクト(例: "GameManager")を作ってアタッチする。
// Qキー: その瞬間のシーン内の状態を保存
// Rキー: 直前に保存した状態まで巻き戻す
// Eキー: セーブがある間だけ発動できるスロー
//
// 敵などISnapshotableを実装したオブジェクトは、
// 保存/復元のたびにシーンから自動的に集められるので、
// 個別の登録処理を書く必要はない。
public class SnapshotManager : MonoBehaviour
{
    public static SnapshotManager Instance { get; private set; }

    // セーブ/ロードのタイミングを知りたいときに購読するイベント
    // OnBeforeSaveはキャプチャの「直前」に発火する。ここで状態を変更すれば、
    // その変更もセーブデータに含められる(=巻き戻しても消えない)
    public static event System.Action OnBeforeSave;
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
    [Range(0f, 1f)]
    public float slowFactor = 0.15f; // 0=完全停止, 1=通常速度。スローの度合い
    bool isActive = false;
    float remainingTime = 0f;
    readonly List<IFreezable> frozenTargets = new List<IFreezable>();
    public Transform playerTransform;

    [Header("Time Stop Effect UI")]
    public PostProcessVolume postProcessVolume; // Vignetteを含んだPost-process Volumeをアサイン
    public TMP_Text remainingTimeText; // 残り時間表示用(任意)
    [Range(0f, 1f)]
    public float maxVignetteIntensity = 1f; // Intensityがどこまで上がるか(0=常に出ない, 1=最大まで出る)
    public float vignetteRampInDuration = 0.4f;  // 発動時、Vignetteが最大になるまでの時間
    public float vignetteRampOutDuration = 1.5f; // 解除時、Vignetteが消えるまでの時間
    public float textFadeOutDuration = 1f;       // 残り時間が0になった時、テキストが消えるまでの時間
    Vignette vignette;
    Coroutine vignetteCoroutine;
    Coroutine textFadeCoroutine;

    [Header("Snapshot")]
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

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out vignette);

            // Intensityの「有効化(overrideState)」チェックがオフだと、
            // .valueに値を入れても一切反映されないため、ここで強制的にオンにしておく
            if (vignette != null)
            {
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0f;
            }
        }
    }

    void OnDestroy()
    {
        // シーン再ロード時、破棄される自分自身がInstanceを持ったままにならないようにする
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        // ゲーム開始地点を最初のセーブポイントにしておく
        SaveSnapshot();

        ResetEffectUI();
    }

    void Update()
    {
        if (MissionManager.isPaused)
        {
            return;
        }
        
        if (Input.GetKeyDown(saveKey))
        {
            // 常に上書き保存する(トグル/削除はしない)
            SaveSnapshot();
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

        if (isActive)
        {
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

    //保存・やり直し
    public void SaveSnapshot()
    {
        OnBeforeSave?.Invoke(); // キャプチャの前に呼ぶことで、ここでの変更もセーブに含まれる

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

        // セーブが削除されたら、時間停止が発動中でも強制的に解除する
        if (isActive)
        {
            Deactivate();
        }

        OnSnapshotCleared?.Invoke();
    }

    //時間停止
    void TryActivate()
    {
        if (!hasSnapshot)
        {
            Debug.Log("[SnapshotManager] セーブがないため時間停止は使用できません。");
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
            // プレイヤー配下のものは対象外
            if (playerTransform != null && target is MonoBehaviour mb && mb.transform.root == playerTransform.root)
            {
                continue;
            }

            target.Freeze(slowFactor);
            frozenTargets.Add(target);
        }

        RampVignette((1f - slowFactor) * maxVignetteIntensity, vignetteRampInDuration);

        Debug.Log($"[SnapshotManager] TimeStop Activated. Frozen: {frozenTargets.Count}");
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

        RampVignette(0f, vignetteRampOutDuration);
        FadeOutText(textFadeOutDuration);

        Debug.Log("[SnapshotManager] TimeStop Deactivated.");
    }

    void RampVignette(float targetIntensity, float duration)
    {
        if (vignette == null)
        {
            return;
        }

        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        vignetteCoroutine = StartCoroutine(VignetteRampRoutine(targetIntensity, duration));
    }

    IEnumerator VignetteRampRoutine(float targetIntensity, float duration)
    {
        float start = vignette.intensity.value;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = duration > 0f ? t / duration : 1f;
            vignette.intensity.value = Mathf.Lerp(start, targetIntensity, p);
            yield return null;
        }

        vignette.intensity.value = targetIntensity;
    }

    void FadeOutText(float duration)
    {
        if (remainingTimeText == null)
        {
            return;
        }

        if (textFadeCoroutine != null)
        {
            StopCoroutine(textFadeCoroutine);
        }
        textFadeCoroutine = StartCoroutine(TextFadeOutRoutine(duration));
    }

    IEnumerator TextFadeOutRoutine(float duration)
    {
        Color startColor = remainingTimeText.color;
        float startAlpha = startColor.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = duration > 0f ? t / duration : 1f;
            Color c = remainingTimeText.color;
            c.a = Mathf.Lerp(startAlpha, 0f, p);
            remainingTimeText.color = c;
            yield return null;
        }

        // 完全に消えたところでテキスト自体もクリアし、
        // 次回の表示に備えてアルファだけ元に戻しておく
        remainingTimeText.text = "";
        Color resetColor = remainingTimeText.color;
        resetColor.a = startAlpha;
        remainingTimeText.color = resetColor;
    }

    void UpdateEffectUI()
    {
        // Vignetteの強さはActivate/Deactivate時にRampVignette()でコルーチン制御しているので、
        // ここでは毎フレーム上書きしない
        if (remainingTimeText != null)
        {
            remainingTimeText.text = duration > 0f
                ? Mathf.Max(remainingTime, 0f).ToString("F1")
                : "";
        }
    }

    void ResetEffectUI()
    {
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }

        if (remainingTimeText != null)
        {
            remainingTimeText.text = "";
        }
    }
}