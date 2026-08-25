using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Slider healthSlider;

    [Header("Effect")]
    public GameObject panel;
    private Coroutine panelRoutine;

    void OnEnable()
    {
        playerController.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerController.currentHealth, playerController.maxHealth);
        
    }
    void OnDisable()
    {
        playerController.OnHealthChanged -= HandleHealthChanged;
    }
    void HandleHealthChanged(int current, int max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

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
