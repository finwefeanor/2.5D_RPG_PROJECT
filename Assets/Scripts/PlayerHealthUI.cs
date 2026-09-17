using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerHealthUI : MonoBehaviour
{
    public Slider slider;

    void OnEnable()  => GameEvents.OnPlayerHealthChanged += UpdateBar;
    void OnDisable() => GameEvents.OnPlayerHealthChanged -= UpdateBar;

    void Start()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>();

        var health = GameManager.Instance?.Player?.Health;
        if (health != null) UpdateBar(health.health, health.maxHealth);
    }

    void UpdateBar(int current, int max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
    }
}
