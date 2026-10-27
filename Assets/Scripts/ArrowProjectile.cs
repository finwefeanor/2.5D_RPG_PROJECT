using UnityEngine;
using UnityEngine.Serialization;

// Spawned by PlayerAttack.FireProjectile on the ranged clip's DealAttackDamage event.
// Flies straight, damages the first Enemy it touches, and is removed on anything solid.
// Damage, speed and range come from the AbilityDefinition - not from this prefab.
[RequireComponent(typeof(Rigidbody))]
public class ArrowProjectile : MonoBehaviour
{
    [Tooltip("Optional effect spawned at the impact point (hit spark comes in the VFX step).")]
    [FormerlySerializedAs("explosion")]
    [SerializeField] private GameObject impactEffect;

    [Tooltip("Rotation that makes the model's tip point along the flight direction. " +
            "KayKit arrow (fbx(unity)) is modelled tip-down (-Y): use (270, 0, 0).")]
    [SerializeField] private Vector3 modelRotation = new Vector3(270f, 0f, 0f);

    private Rigidbody rb;
    private PlayerAttack owner;
    private AbilityDefinition ability;
    private int damage;
    private bool spent;   // ContinuousDynamic can report two contacts in one step

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // Called once, right after Instantiate.
    public void Launch(PlayerAttack owner, AbilityDefinition ability, int damage, Vector3 direction)
    {
        this.owner = owner;
        this.ability = ability;
        this.damage = damage;

        // Face the flight direction, then turn the model so its tip leads.
        // Done here (not in Instantiate) so the prefab's model offset is never lost.
        direction = direction.normalized;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(modelRotation);

        // Never collide with the shooter.
        Collider arrowCollider = GetComponentInChildren<Collider>();
        if (arrowCollider != null)
        {
            foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(arrowCollider, ownerCollider);
        }

        // Velocity uses the direction, not transform.forward: with the model offset,
        // the object's own forward axis no longer points where the arrow flies.
        float speed = Mathf.Max(0.1f, ability.projectileSpeed);
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, ability.projectileRange / speed);
    }

    void OnCollisionEnter(Collision collision)
    {
                
        if (spent) return;
        spent = true;

        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            if (owner != null) owner.OnProjectileHit(ability);
        }

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}