using UnityEngine;

public class FireProjectile : MonoBehaviour
{
    PlayerAttack playerAttack;
    // Only GameObjects with a Rigidbody can be assigned as the projectile.
    public Rigidbody projectile;
    // Speed of the projectile when fired.
    // This is a public variable so it can be adjusted in the Unity Editor.
    public float speed = 4;
    // Update is called once per frame
    // This method checks for input and fires a projectile if the attack action is pressed.

    private void projectileAttack()
    {
                // Check if the "Attack" action was pressed this frame
        // If it was, instantiate a projectile at the player's position and set its velocity.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Rigidbody p = Instantiate(projectile, transform.position, transform.rotation);
            p.linearVelocity = transform.forward * speed;

            playerAttack.TryAttack();
        }
        
    }

}
