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

    [Tooltip("Optional TrailRenderer on a CHILD object of the bolt. Detached on despawn so it " +
             "fades out instead of vanishing together with the bolt.")]
    [SerializeField] private TrailRenderer trail;

    private float despawnTime;   // out-of-range time, set in Launch

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

    void Update()
    {
        if (!spent && Time.time >= despawnTime) Despawn();   // flew projectileRange metres
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

        // Not Destroy(gameObject, t): a timed Destroy would skip Despawn and kill the trail.
        despawnTime = Time.time + ability.projectileRange / speed;
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
        {
            // Spawn at the real contact point, facing out of the surface that was hit.
            Vector3 point = transform.position;
            Quaternion facing = Quaternion.identity;
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                point = contact.point;
                facing = Quaternion.LookRotation(contact.normal);
            }
            Instantiate(impactEffect, point, facing);
        }

        Despawn();
    }


    // The one exit for both endings (hit / out of range). The trail is detached so it
    // fades out on its own; autodestruct removes it once its last point has faded.
    private void Despawn()
    {
        spent = true;

        if (trail != null)
        {
            trail.transform.SetParent(null, true);
            trail.emitting = false;
            trail.autodestruct = true;
        }

        Destroy(gameObject);
    }




}