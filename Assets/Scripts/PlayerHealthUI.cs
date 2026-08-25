using UnityEngine;
using UnityEngine.UI;

// シーン内の空オブジェクト(例: "PlayerHealthUI")にアタッチする。
// PlayerControllerのOnHealthChangedイベントを購読し、Sliderに反映するだけ。
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Slider healthSlider;

    void OnEnable()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerController != null)
        {
            playerController.OnHealthChanged += HandleHealthChanged;
            // アタッチ直後に現在の体力を反映しておく
            HandleHealthChanged(playerController.currentHealth, playerController.maxHealth);
        }
    }

    void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnHealthChanged -= HandleHealthChanged;
        }
    }

    void HandleHealthChanged(int current, int max)
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}