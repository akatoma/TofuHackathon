using UnityEngine;
using UnityEngine.SceneManagement;

// GameOverシーンに置く空オブジェクト(例: "GameOverManager")にアタッチする。
public class GameOverController : MonoBehaviour
{
    [Header("Scene Names")]
    public string titleSceneName = "Title"; // Build Settingsに追加しておくこと
    public string gameSceneName = "main";   // Build Settingsに追加しておくこと

    void Awake()
    {
        // ボタン操作のため、カーソルを表示・アンロックしておく
        // (mainシーンのPlayerControllerがロックした状態のまま引き継がれるため)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 「タイトルへ戻る」ボタンのOnClick()に登録する
    public void OnBackToTitlePressed()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    // 「リスタート」ボタンのOnClick()に登録する
    public void OnRestartPressed()
    {
        // mainシーンをロードし直すだけで、Player/敵/セーブデータ/ゲージなど
        // シーン内のすべての状態が初期状態に戻る(すべてシーンスコープのため)
        SceneManager.LoadScene(gameSceneName);
    }
}