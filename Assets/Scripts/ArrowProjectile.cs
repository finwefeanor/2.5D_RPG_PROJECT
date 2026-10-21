using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject explosion;
    void Start()
    {
         // Deletes the projectile after 10 seconds, regardless
         // of whether it collided with anything. This prevents
         // instances from staying in the scene indefinitely.
         Destroy(gameObject,10);
    }
    void OnCollisionEnter()
    {
        // When the projectile hits something, create an explosion
        // and remove the projectile.
        Instantiate(explosion,transform.position,transform.rotation);
        Destroy(gameObject);
    }
}
