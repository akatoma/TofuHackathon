using System.Collections;
using UnityEngine;
using TMPro;

// シーン内の空オブジェクト(例: "MissionManager")にアタッチする。
// ミッション: 「Enemy」タグの付いたオブジェクトを指定数討伐する。
// クリアすると、生存している「Ally」タグの数とクリアタイムをリザルトとして表示する。
public class MissionManager : MonoBehaviour
{
    // ミッションクリアの瞬間に通知される(ゲージなど、他のシステムが購読するだけでよい)
    public static event System.Action OnMissionCleared;

    [Header("Mission Settings")]
    public int targetKillCount = 5;
    public string enemyTag = "Enemy";
    public string allyTag = "Ally"; // 生存者カウント用。味方PrefabにこのTagを付けておく

    [Header("Mission UI (右上に配置)")]
    public GameObject missionPanel;
    public TMP_Text missionText;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TMP_Text resultText; // 生存者数とクリアタイムをまとめて表示

    [Header("Victory Slow Motion")]
    public float slowMotionTimeScale = 0.1f; // クリア演出中のTime.timeScale(プレイヤーも含め全体に効く)
    public float slowMotionDuration = 1.5f;  // 演出を見せる実時間(秒。Time.timeScaleの影響を受けない)

    [Header("Clear Mail")]
    public MailSender mailSender;
    public TMP_InputField emailInputField; // プレイヤーが自分のメールアドレスを入力する欄

    [Header("Novel Sequence")]
    public NovelSequencePlayer novelSequencePlayer; // クリア後、結果を見せる前に再生する演出(未設定なら即座に結果表示)

    int defeatedCount = 0;
    float startTime;
    bool isCleared = false;

    // ノベル演出の再生中も、演出終了後まで結果情報を保持しておく
    int pendingSurvivorCount;
    float pendingClearTime;

    void Start()
    {
        startTime = Time.time;
        defeatedCount = 0;
        isCleared = false;

        if (missionPanel != null)
        {
            missionPanel.SetActive(true);
        }
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        UpdateMissionText();
    }

    void OnEnable()
    {
        EnemyController.OnEnemyDefeated += HandleEnemyDefeated;
    }

    void OnDisable()
    {
        EnemyController.OnEnemyDefeated -= HandleEnemyDefeated;
    }

    void HandleEnemyDefeated(GameObject defeated)
    {
        if (isCleared)
        {
            return;
        }

        if (defeated == null || !defeated.CompareTag(enemyTag))
        {
            return; // Enemyタグ以外(将来別種の敵など)はカウントしない
        }

        defeatedCount++;
        UpdateMissionText();

        if (defeatedCount >= targetKillCount)
        {
            ClearMission();
        }
    }

    void UpdateMissionText()
    {
        if (missionText != null)
        {
            missionText.text = $"敵を倒せ: {defeatedCount} / {targetKillCount}";
        }
    }

    void ClearMission()
    {
        isCleared = true;
        OnMissionCleared?.Invoke();

        // 常時ロック/非表示にしているカーソルを解放する
        // (これをしないと、この後のInputField/ボタンが一切操作できない)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        // 演出が始まる前(=Time.timeScaleが下がる前)に記録を取っておく。
        // そうしないと、スロー演出中の実時間がクリアタイムに歪んで含まれてしまう
        int survivorCount = GameObject.FindGameObjectsWithTag(allyTag).Length;
        float clearTime = Time.time - startTime;

        StartCoroutine(VictorySlowMotionRoutine(survivorCount, clearTime));
        OnMissionCleared?.Invoke();
    }

    IEnumerator VictorySlowMotionRoutine(int survivorCount, float clearTime)
    {
        // 主人公を含む世界全体をスローにする
        Time.timeScale = slowMotionTimeScale;

        // Time.timeScaleの影響を受けない実時間で待つ
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        Time.timeScale = 1f;

        pendingSurvivorCount = survivorCount;
        pendingClearTime = clearTime;

        if (novelSequencePlayer != null)
        {
            // 再生後、NovelSequencePlayerのOn Sequence Complete()から
            // OnNovelSequenceComplete()が呼ばれて結果表示に進む(Inspectorで接続)
            novelSequencePlayer.Play();
        }
        else
        {
            ShowResult(pendingSurvivorCount, pendingClearTime);
        }
    }

    // NovelSequencePlayerの On Sequence Complete() に登録する
    public void OnNovelSequenceComplete()
    {
        ShowResult(pendingSurvivorCount, pendingClearTime);
    }

    void ShowResult(int survivorCount, float clearTime)
    {
        if (resultText != null)
        {
            resultText.text = $"生存者: {survivorCount}人\nクリアタイム: {FormatTime(clearTime)}";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        Debug.Log($"[MissionManager] Mission Cleared. Survivors: {survivorCount}, Time: {FormatTime(clearTime)}");
    }

    string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    // リザルト画面の「送信」ボタンのOnClick()に登録する
    public void OnSendMailButtonPressed()
    {
        if (mailSender == null || emailInputField == null)
        {
            return;
        }

        mailSender.SendClearMail(emailInputField.text);
    }
}