using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// CHANGED FROM 2D:
//   OnTriggerEnter2D  → OnTriggerEnter
//   OnTriggerExit2D   → OnTriggerExit  (not used here but good habit)
//   SpriteRenderer clothesRenderer → PlayerInventory reference
//     In 3D, "has armor" is tracked by PlayerInventory.HasClothesEquipped()
//     instead of checking if a SpriteRenderer is active

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;
    public int armorReduction = 5;
    public AudioSource playerHitSound;
    public AudioSource playerDieSound;

    private PlayerInventory playerInventory;

    void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    // 3D trigger — same logic, just no "2D" suffix
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
            TakeDamage(20);
    }

    public void TakeDamage(int damage)
    {
        // Armor check via PlayerInventory instead of SpriteRenderer
        if (playerInventory != null && playerInventory.HasClothesEquipped())
            damage -= armorReduction;

        health -= damage;
        Debug.Log("Player health: " + health);

        if (playerHitSound != null) playerHitSound.Play();

        if (health <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Player has died");
        StartCoroutine(HandleDeath());
    }

    IEnumerator HandleDeath()
    {
        if (playerDieSound != null)
            playerDieSound.Play();

        yield return new WaitForSeconds(
            playerDieSound != null ? playerDieSound.clip.length : 3.0f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
