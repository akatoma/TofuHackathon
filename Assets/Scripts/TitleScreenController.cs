using UnityEngine;
using UnityEngine.SceneManagement;

// タイトル画面用のシーンに置く空オブジェクト(例: "TitleScreenManager")にアタッチする。
public class TitleScreenController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject titlePanel; // 最初に表示するタイトル(タイトルロゴなど)
    public GameObject menuPanel;  // クリック後に表示するメニュー(スタート/設定ボタン)

    [Header("Scene")]
    public string gameSceneName = "main"; // Build Settingsに追加しておくこと

    bool hasAdvancedToMenu = false;

    void Awake()
    {
        // メニュー操作のため、カーソルを表示・アンロックしておく
        // (プレイヤーのPlayerControllerはmainシーン側でロックするので、ここでは触れない)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
        }
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    void Update()
    {
        // タイトル表示中に、画面のどこかをクリックしたらメニューへ
        if (!hasAdvancedToMenu && Input.GetMouseButtonDown(0))
        {
            ShowMenu();
        }
    }

    void ShowMenu()
    {
        hasAdvancedToMenu = true;

        if (titlePanel != null)
        {
            titlePanel.SetActive(false);
        }
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }

    // 「ゲームスタート」ボタンのOnClick()に登録する
    public void OnStartButtonPressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // 「設定」ボタンのOnClick()に登録する(今は仮実装)
    public void OnSettingsButtonPressed()
    {
        Debug.Log("設定画面は未実装です");
        // TODO: 設定パネルを作ったら、ここでtitlePanel/menuPanelと同じように表示切り替えする
    }
}