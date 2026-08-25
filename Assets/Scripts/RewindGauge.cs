using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// シーン内の空オブジェクト(例: "RewindGauge")にアタッチする。
// SnapshotManagerのセーブ/ロードイベントを購読し、ゲージを増加させる。
// マックスに達したらゲームオーバー処理(onGameOver)を呼ぶ。
public class RewindGauge : MonoBehaviour
{
    [Header("Gauge Settings")]
    public float maxValue = 100f;
    public float increaseOnSave = 10f; // セーブ(Q)1回あたりの増加量
    public float increaseOnLoad = 10f; // 巻き戻し(R)1回あたりの増加量

    [Header("UI")]
    public Slider gaugeSlider; // 画面左下に配置したSliderをアサイン(Interactableはオフ推奨)

    [Header("Game Over")]
    public UnityEvent onGameOver; // ゲームオーバー時の処理をInspectorで割り当てる
                                   // (例: GameOverパネルの表示、シーン遷移など)

    float currentValue = 0f;
    bool isGameOver = false;

    void OnEnable()
    {
        SnapshotManager.OnSnapshotSaved += HandleSaved;
        SnapshotManager.OnSnapshotLoaded += HandleLoaded;
    }

    void OnDisable()
    {
        SnapshotManager.OnSnapshotSaved -= HandleSaved;
        SnapshotManager.OnSnapshotLoaded -= HandleLoaded;
    }

    void Start()
    {
        UpdateUI();
    }

    void HandleSaved()
    {
        Increase(increaseOnSave);
    }

    void HandleLoaded()
    {
        Increase(increaseOnLoad);
    }

    void Increase(float amount)
    {
        if (isGameOver)
        {
            return;
        }

        currentValue = Mathf.Min(currentValue + amount, maxValue);
        UpdateUI();

        if (currentValue >= maxValue)
        {
            TriggerGameOver();
        }
    }

    void UpdateUI()
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.maxValue = maxValue;
            gaugeSlider.value = currentValue;
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;
        Debug.Log("[RewindGauge] GAME OVER");
        onGameOver?.Invoke();
    }
}