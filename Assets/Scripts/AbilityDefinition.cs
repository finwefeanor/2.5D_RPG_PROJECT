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

    [Header("Combo")]
    [Tooltip("Attack that follows if the player presses again during this swing. " +
             "Null = no combo. Must use a DIFFERENT clip than this one.")]
    public AbilityDefinition nextInCombo;

    [Tooltip("Normalized time (0-1) from which a press during this swing is remembered. " +
             "Earlier presses are ignored, so mashing from the first frame doesn't chain.")]
    [Range(0f, 1f)]
    public float comboInputOpens = 0.2f;

    [Tooltip("Normalized time (0-1) at which a remembered press starts the next attack. " +
             "Keep it AFTER this clip's contact frame, or this swing's hit is skipped.")]
    [Range(0f, 1f)]
    public float comboChainAt = 0.5f;

    [Header("Audio")]
    [Tooltip("Played on every swing, hit or miss. One picked at random. Empty = silent swing.")]
    public AudioClip[] swingSounds;

    [Tooltip("Played only when the swing hits something. One picked at random. " +
             "Empty = the attack AudioSource's own clip (currently sword-slice).")]
    public AudioClip[] hitSounds;

    // Random clip from a set, or null if the set is empty.
    public static AudioClip Pick(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

}
