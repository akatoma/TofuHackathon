using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour, ISnapshotable
{
    [Header("References")]
    public PlayerController playerController;
    public ObjectPicker objectPicker;
    public Slider healthSlider;
    public Slider gaugeSlider;

    [Header("Delusion Gauge")]
    public float maxValue = 100f;
    float currentValue = 0f;
    public float fillRate = 5f; // セーブがある間、1秒あたりに増える量
    public float increaseOnSave = 5f; // セーブ(Q)1回あたりの増加量
    public float increaseOnLoad = 5f; // 巻き戻し(R)1回あたりの増加量

    [Header("Object Pick Gauge Settings")]
    public float increasePerSecondWhileHolding = 2f; // 保持中の1秒あたりの増加量
    public float increaseOnThrow = 5f;               // 投擲(左クリック)時の増加量
    private Coroutine holdGaugeCoroutine;


    [Header("Bullets Hit Effect")]
    public GameObject panel;
    private Coroutine panelRoutine;

    [Header("Wave")]
    public Transform player;        // 波紋の発生位置(Playerをアサイン)
    public GameObject ripplePrefab; // RippleEffectを付けたQuadのプレハブ

    [Header("Darken Overlay")]
    public Image darkenOverlay;     // 画面全体を覆うUI Image(初期アルファ0にしておく)
    public float darkenTargetAlpha = 0.6f;
    public float darkenFadeDuration = 0.5f;
    Coroutine fadeCoroutine;

    bool isGameOver = false;

    class State
    {
        public float gaugeValue;
    }

    void Start()
    {
        gaugeSlider.maxValue = maxValue;
        gaugeSlider.value = currentValue;
    }

    void OnEnable()
    {
        playerController.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerController.currentHealth, playerController.maxHealth);

        EnemyController.OnEnemyDefeated += HandleEnemyDefeated;

        SnapshotManager.OnBeforeSave += HandleBeforeSave;
        SnapshotManager.OnSnapshotSaved += HandleSaved;
        SnapshotManager.OnSnapshotLoaded += HandleLoaded;
        SnapshotManager.OnSnapshotCleared += HandleCleared;

        if (objectPicker != null)
        {
            objectPicker.OnObjectPicked += HandleObjectPicked;
            objectPicker.OnObjectDropped += HandleObjectDropped;
            objectPicker.OnObjectThrown += HandleObjectThrown;
        }

        // 起動時、既にセーブがある状態なら暗転も即座に反映しておく
        bool currentlySaved = SnapshotManager.Instance != null && SnapshotManager.Instance.HasSnapshot;
        SetDarkenImmediate(currentlySaved ? darkenTargetAlpha : 0f);

    }
    void OnDisable()
    {
        playerController.OnHealthChanged -= HandleHealthChanged;

        EnemyController.OnEnemyDefeated -= HandleEnemyDefeated;

        SnapshotManager.OnBeforeSave -= HandleBeforeSave;
        SnapshotManager.OnSnapshotSaved -= HandleSaved;
        SnapshotManager.OnSnapshotLoaded -= HandleLoaded;
        SnapshotManager.OnSnapshotCleared -= HandleCleared;

        if (objectPicker != null)
        {
            objectPicker.OnObjectPicked -= HandleObjectPicked;
            objectPicker.OnObjectDropped -= HandleObjectDropped;
            objectPicker.OnObjectThrown -= HandleObjectThrown;
        }
    }

    //UI
    void HandleHealthChanged(int current, int max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
    void HandleBeforeSave()
    {
        // キャプチャされる前に加算するので、この後に巻き戻してもコストは消えない
        Increase(increaseOnSave);
    }
    void HandleSaved()
    {
        // ゲージ加算はHandleBeforeSave側で既に行っているので、ここでは演出のみ
        SpawnRipple();
        FadeDarken(darkenTargetAlpha);
    }
    void HandleLoaded()
    {
        Increase(increaseOnLoad);
    }
    void HandleCleared()
    {
        FadeDarken(0f);
    }

    void HandleEnemyDefeated(GameObject defeated)
    {
        if (isGameOver) return;

        // 敵を倒すとゲージが全回復する
        currentValue = 0f;
        gaugeSlider.value = currentValue;
        Debug.Log("[GameManager] Enemy defeated - gauge fully recovered.");
    }

    void Update()
    {
        // セーブがある間だけ、一定速度でゲージが増え続ける
        bool hasSave = SnapshotManager.Instance != null && SnapshotManager.Instance.HasSnapshot;
        if (hasSave)
        {
            Increase(fillRate * Time.deltaTime);
        }
    }

    // オブジェクト保持・離す・投擲処理
    void HandleObjectPicked()
    {
        if (holdGaugeCoroutine != null)
        {
            StopCoroutine(holdGaugeCoroutine);
        }
        holdGaugeCoroutine = StartCoroutine(HoldGaugeRoutine());
    }
    void HandleObjectDropped()
    {
        if (holdGaugeCoroutine != null)
        {
            StopCoroutine(holdGaugeCoroutine);
            holdGaugeCoroutine = null;
        }
    }
    void HandleObjectThrown()
    {
        HandleObjectDropped();
        Increase(increaseOnThrow); // 投擲時に5ゲージ増加
    }

    // 1秒ごとに2ずつゲージを増加させるコルーチン
    IEnumerator HoldGaugeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            Increase(increasePerSecondWhileHolding);
        }
    }

    void Increase(float amount)
    {
        if (isGameOver) return;

        currentValue = Mathf.Min(currentValue + amount, maxValue);
        gaugeSlider.value = currentValue;

        if (currentValue >= maxValue)
        {
            TriggerGameOver();
        }
    }

    //GameOver
    void TriggerGameOver()
    {
        isGameOver = true;
        Debug.Log("[RewindGauge] GAME OVER");
        SceneManager.LoadScene("GameoverScene"); 
    }

    //Playerの被ダメEffect
    public void ShowHitPanel(float seconds)
    {
        if (panelRoutine != null)
        {
            StopCoroutine(panelRoutine);
        }
        panelRoutine = StartCoroutine(ShowPanelRoutine(seconds));
    }
    private IEnumerator ShowPanelRoutine(float seconds)
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(seconds);
        panel.SetActive(false);
        panelRoutine = null;
    }

    //波紋
    void SpawnRipple()
    {
        if (ripplePrefab == null || player == null)
        {
            return;
        }

        Instantiate(ripplePrefab, player.position - Vector3.down * 0.4f, Quaternion.identity);
    }

    void FadeDarken(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeDarkenRoutine(targetAlpha));
    }

    IEnumerator FadeDarkenRoutine(float targetAlpha)
    {
        if (darkenOverlay == null)
        {
            yield break;
        }

        float startAlpha = darkenOverlay.color.a;
        float t = 0f;

        while (t < darkenFadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / darkenFadeDuration);
            SetDarkenImmediate(a);
            yield return null;
        }

        SetDarkenImmediate(targetAlpha);
    }

    void SetDarkenImmediate(float alpha)
    {
        if (darkenOverlay == null)
        {
            return;
        }

        Color c = darkenOverlay.color;
        c.a = alpha;
        darkenOverlay.color = c;
    }

    //保存
    public object CaptureSnapshot()
    {
        return new State
        {
            gaugeValue = currentValue
        };
    }

    //復元
    public void RestoreSnapshot(object snapshot)
    {
        if (snapshot is not State state)
        {
            return;
        }

        currentValue = state.gaugeValue;
        gaugeSlider.value = currentValue;
    }
}