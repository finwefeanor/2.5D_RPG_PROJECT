using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    private Slider slider;
    private Enemy enemy;
    private Transform cam;

    void Start()
    {
        slider = GetComponentInChildren<Slider>();
        enemy = GetComponentInParent<Enemy>();
        cam = Camera.main.transform;

        if (enemy != null)
            enemy.OnHealthChanged += UpdateBar;
    }

    void UpdateBar(int current, int max)
    {
        slider.maxValue = max;
        slider.value = current;
    }

    void LateUpdate()
    {
        // Billboard — always face the camera
        //transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
        Vector3 direction = transform.position - cam.position;
        //direction.y = 0f; // strip vertical component — bar stays upright regardless of camera pitch
        direction.x = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnDestroy()
    {
        if (enemy != null)
            enemy.OnHealthChanged -= UpdateBar;
    }
}
