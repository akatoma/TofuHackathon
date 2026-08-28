using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// タイトル画面用のシーンに置く空オブジェクト(例: "TitleScreenManager")にアタッチする。
public class TitleScreenController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject titlePanel; // 最初に表示するタイトル(タイトルロゴなど)
    public GameObject menuPanel;  // クリック後に表示するメニュー(スタート/設定ボタン)

    [Header("Scene")]
    public string gameSceneName = "main"; // Build Settingsに追加しておくこと

    [Header("Name Entry")]
    public TMP_InputField surnameField;   // 苗字
    public TMP_InputField givenNameField; // 名前
    public TMP_Text registeredNamesText;  // 登録済み名前一覧の表示。不要なら未設定でOK

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

        RefreshNameListDisplay();
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

    // 名前追加Panel内の「追加」ボタンのOnClick()に登録する
    public void OnAddNameButtonPressed()
    {
        string surname = surnameField != null ? surnameField.text.Trim() : "";
        string given = givenNameField != null ? givenNameField.text.Trim() : "";

        string fullName = $"{surname} {given}".Trim();

        if (string.IsNullOrEmpty(fullName))
        {
            return;
        }

        MobNameRegistry.AddName(fullName);

        if (surnameField != null)
        {
            surnameField.text = "";
        }
        if (givenNameField != null)
        {
            givenNameField.text = "";
        }

        RefreshNameListDisplay();
    }

    void RefreshNameListDisplay()
    {
        if (registeredNamesText == null)
        {
            return;
        }

        registeredNamesText.text = string.Join("\n", MobNameRegistry.Names);
    }
}