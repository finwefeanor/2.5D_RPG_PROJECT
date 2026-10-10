// ============================================================
//  PlayerClass.cs  -  Assets/Scripts/
//  Attach to the Player root (next to PlayerRefs).
//
//  Decides WHICH class this player is:
//    1. spawns the class's visual as the Player's child
//       (replacing the one baked into the Player prefab),
//    2. hands that visual's Animator / sockets / pieces to the
//       Player-root scripts,
//    3. applies the class's level-1 stats.
//
//  Runs before every other Player script's Awake (execution
//  order -100), so they all start with the right class.
//  SetClass() repeats the same steps at runtime (class-select
//  menu). One Player prefab for every class.
// ============================================================
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerClass : MonoBehaviour
{
    [SerializeField] private CharacterClassDefinition classDefinition;
 
    public CharacterClassDefinition Definition => classDefinition;
    public CharacterVisual Visual { get; private set; }
 
    void Awake()
    {
        Visual = SpawnVisual(classDefinition);
        if (Visual == null)
        {
            Debug.LogError($"{name}: PlayerClass found no CharacterVisual. " +
                           "Add the component to the root of the visual prefab.", this);
            return;
        }
 
        BindVisual(Visual);
 
        if (classDefinition != null) ApplyStats(classDefinition);
        else Debug.LogWarning($"{name}: PlayerClass has no class assigned - using Player prefab values.", this);
    }
 
    void Start()
    {
        // UI subscribes in OnEnable; by Start everyone is listening.
        GameEvents.PlayerClassChanged(classDefinition);
    }
 
    // Switches class at runtime: new visual, new stats, full health.
    // Items the new class can't use are unequipped (they stay owned).
    // Meant for the class-select screen / testing, not mid-combat.
    public void SetClass(CharacterClassDefinition newClass)
    {
        if (newClass == null || newClass == classDefinition) return;
        classDefinition = newClass;
 
        // A swing on the old Animator never gets its OnStateExit - clear it.
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.ResetAttackState();
 
        Visual = SpawnVisual(classDefinition);
        if (Visual == null) return;
 
        BindVisual(Visual);
        ApplyStats(classDefinition);
 
        EquipmentManager equipment = GetComponent<EquipmentManager>();
        if (equipment != null) equipment.UnequipUnusable();
 
        // Re-show weapons / head / chest pieces on the NEW model.
        CharacterVisualController visuals = GetComponent<CharacterVisualController>();
        if (visuals != null) visuals.Refresh();
 
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) GameEvents.PlayerHealthChanged(health.health, health.maxHealth);
 
        GameEvents.PlayerClassChanged(classDefinition);
    }
 
    // Replaces the current visual child with the class's own one.
    private CharacterVisual SpawnVisual(CharacterClassDefinition c)
    {
        CharacterVisual existing = GetComponentInChildren<CharacterVisual>();
        if (c == null || c.visualPrefab == null) return existing;
 
        Vector3 localPos = Vector3.zero;
        Quaternion localRot = Quaternion.identity;
        Vector3 localScale = Vector3.one;
 
        if (existing != null)
        {
            localPos   = existing.transform.localPosition;
            localRot   = existing.transform.localRotation;
            localScale = existing.transform.localScale;
 
            // Deactivate first: Destroy() only happens at the end of the frame, and
            // GetComponentInChildren<Animator>() elsewhere this frame must not find it.
            existing.gameObject.SetActive(false);
            Destroy(existing.gameObject);
        }
 
        CharacterVisual spawned = Instantiate(c.visualPrefab, transform);
        spawned.name = c.visualPrefab.name;
        spawned.transform.localPosition = localPos;
        spawned.transform.localRotation = localRot;
        spawned.transform.localScale    = localScale;
        return spawned;
    }
 
    // The ONE place that connects the visual to the Player-root scripts.
    private void BindVisual(CharacterVisual v)
    {
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.animator = v.animator;
 
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.SetAnimator(v.animator);
 
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.SetAnimator(v.animator);
 
        PlayerRefs refs = GetComponent<PlayerRefs>();
        if (refs != null) refs.SetAnimator(v.animator);
 
        CharacterVisualController visuals = GetComponent<CharacterVisualController>();
        if (visuals != null) visuals.BindVisual(v);
    }
 
    private void ApplyStats(CharacterClassDefinition c)
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.maxHealth = c.maxHealth;
            health.health    = c.maxHealth;   // start at full
        }
 
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null)
        {
            attack.baseAttackDamage = c.baseAttackDamage;
            attack.attackSpeed      = c.attackSpeed;
        }
 
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.moveSpeed = c.moveSpeed;
 
        EquipmentManager equipment = GetComponent<EquipmentManager>();
        if (equipment != null && c.unarmedAttack != null) equipment.SetUnarmedAttack(c.unarmedAttack);
    }
}
