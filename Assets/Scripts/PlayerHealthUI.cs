using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    private Slider slider;
    private PlayerHealth playerHealth;

    void Start()
    {
        slider = GetComponent<Slider>();
        playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateBar;
    }

    void UpdateBar(int current, int max)
    {
        slider.maxValue = max;
        slider.value = current;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateBar;
    }
}

