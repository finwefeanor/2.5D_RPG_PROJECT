// ============================================================
//  CharacterClassDefinition.cs  —  Assets/Scripts/
//
//  ScriptableObject describing ONE playable class: what it
//  looks like, its level-1 stats, and its unarmed attack.
//  Mage / Barbarian / Rogue are just different assets of this
//  type — no "if (class == Mage)" anywhere in code.
//
//  Create via: Right-click in Project → Create → RPG → Character Class
//
//  Later (progression phase): ability list with requiredLevel,
//  resource type (mana / rage / energy), per-level stat growth.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewClass", menuName = "RPG/Character Class")]
public class CharacterClassDefinition : ScriptableObject
{
    [Header("Identity")]
    public string className = "New Class";
    [TextArea(2, 4)]
    public string description = "";
    public Sprite icon;   // for a future class-select screen

    [Header("Visual")]
    [Tooltip("Visual prefab spawned as the Player's child. Its root needs CharacterVisual, " +
             "Animator, CharacterMotor and PlayerAnimationEventRelay. " +
             "Null = keep whatever visual is already inside the Player prefab.")]
    public CharacterVisual visualPrefab;

    [Header("Base stats (level 1)")]
    public int maxHealth = 30;
    public int baseAttackDamage = 10;

    [Tooltip("Multiplies every ability's animationSpeed. 1 = normal.")]
    [Range(0.25f, 3f)]
    public float attackSpeed = 1f;

    public float moveSpeed = 5f;

    [Header("Combat")]
    [Tooltip("Attack used when the right hand is empty.")]
    public AbilityDefinition unarmedAttack;
}
