using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Slider healthSlider;
    public Slider gaugeSlider;

    [Header("Delusion Gauge")]
    public float maxValue = 100f;
    float currentValue = 0f;
    public float fillRate = 5f; // セーブがある間、1秒あたりに増える量
    public float increaseOnSave = 5f; // Qを押すたびに追加で増える量
    public float increaseOnLoad = 5f; // Rを押すたびに追加で増える量

    [Header("Game Over")]
    public UnityEvent onGameOver; // ゲームオーバー時の処理をInspectorで割り当てる
                                  // (例: GameOverパネルの表示、シーン遷移など)
    bool isGameOver = false;

    [Header("Effect")]
    public GameObject panel;
    private Coroutine panelRoutine;


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

        SnapshotManager.OnSnapshotSaved += HandleSaved;
        SnapshotManager.OnSnapshotLoaded += HandleLoaded;
    }
    void OnDisable()
    {
        playerController.OnHealthChanged -= HandleHealthChanged;

        EnemyController.OnEnemyDefeated -= HandleEnemyDefeated;

        SnapshotManager.OnSnapshotSaved -= HandleSaved;
        SnapshotManager.OnSnapshotLoaded -= HandleLoaded;
    }

    void HandleSaved()
    {
        Increase(increaseOnSave);
    }

    void HandleLoaded()
    {
        Increase(increaseOnLoad);
    }

    void HandleEnemyDefeated()
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

    //UI
    void HandleHealthChanged(int current, int max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
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
        onGameOver?.Invoke();
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
}