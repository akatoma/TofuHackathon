using UnityEngine;
using UnityEngine.SceneManagement;

// シーン内の空オブジェクト(例: "SceneLoader")にアタッチする。
// UnityEventのStatic Parameterからシーン名を渡して呼び出せる、汎用のシーン遷移スクリプト。
public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}