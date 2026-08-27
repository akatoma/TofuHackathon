using UnityEngine;
using UnityEngine.UI;

// 敵の頭上に配置するWorld Space Canvasのルートオブジェクトにアタッチする。
// 表示/非表示・数値更新はEnemyController側から呼び出す。初期状態は非表示。
public class EnemyHealthBar : MonoBehaviour
{
    public Slider healthSlider;

    Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
        gameObject.SetActive(false); // 初めて攻撃を受けるまでは非表示
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        // 常にカメラの方を向かせる(ビルボード)
        transform.forward = mainCamera.transform.forward;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetHealth(int current, int max)
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}