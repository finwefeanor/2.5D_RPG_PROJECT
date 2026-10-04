// ============================================================
//  AbilityDefinition.cs  —  Assets/Scripts/
//
//  ScriptableObject describing ONE attack: which animation
//  plays, how hard it hits, how far it reaches, what it sounds
//  like. Weapons point at one; EquipmentManager falls back to
//  an unarmed one when the right hand is empty.
//
//  Create via: Right-click in Project → Create → RPG → Ability
//
//  Later (progression phase): cooldown, resource cost,
//  requiredLevel, summon prefab.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "RPG/Ability")]
public class AbilityDefinition : ScriptableObject
{
    [Header("Animation")]
    [Tooltip("Written to the Animator's AttackIndex parameter. Must match an Entry " +
             "transition inside Attacks_SubState. 0-9 = 1H, 10-19 = unarmed.")]
    public int animatorIndex = 0;

    [Tooltip("Playback speed of THIS attack's clip. 1 = as authored, 1.5 = 50% faster. " +
             "Animation Events are normalized, so contact frames scale with it.")]
    [Range(0.25f, 3f)]
    public float animationSpeed = 1f;

    [Header("Hit")]
    [Tooltip("Multiplies PlayerAttack.baseAttackDamage. Unarmed < 1, heavy weapons > 1.")]
    public float damageMultiplier = 1f;

    [Tooltip("Radius of the hit sphere around PlayerAttack.attackPoint.")]
    public float range = 1.5f;

    [Header("Audio")]
    [Tooltip("Optional. Null = play the attack AudioSource's own clip.")]
    public AudioClip sound;
}
