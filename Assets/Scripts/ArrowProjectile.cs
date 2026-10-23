using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private GameObject explosion;
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifetime = 10f;

    private Rigidbody rb;
    private int damage;

    public void Initialize(int damage, GameObject owner)
    {
        this.damage = damage;

        // Don't collide with the player who fired it
        Collider arrowCollider = GetComponent<Collider>();
        foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(arrowCollider, ownerCollider);
        }
    }

    private void Awake()
    {
        this.rb = GetComponent<Rigidbody>();
        this.rb.useGravity = false;
        this.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // fast objects won't pass through
    }

    private void Start()
    {
        this.rb.linearVelocity = transform.forward * speed;   // Unity 6; use rb.velocity on 2022 or older
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        if (explosion != null)
        {
            Instantiate(explosion, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }
}