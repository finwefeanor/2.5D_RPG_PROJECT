using UnityEngine;
using System;

public static class GameEvents
{
    public static event Action<int, int> OnPlayerHealthChanged;
    public static event Action OnInventoryChanged;


    public static event Action OnPlayerDied;

    public static event Action<int> OnGoldChanged;

    public static event Action<CharacterClassDefinition> OnPlayerClassChanged;
    public static void PlayerClassChanged(CharacterClassDefinition c) => OnPlayerClassChanged?.Invoke(c);

    public static event Action OnInteractPressed;
    public static event Action OnAttackPressed;
    public static void AttackPressed() => OnAttackPressed?.Invoke();
    public static event Action OnJumpPressed;                       // JUMP button (HUD)
    public static void JumpPressed() => OnJumpPressed?.Invoke();
    public static event Action OnSkillPressed;                      // SKILL button (HUD)
    public static void SkillPressed() => OnSkillPressed?.Invoke();
    public static event Action<float> OnSkillUsed;                  // cooldown seconds, for the HUD overlay
    public static void SkillUsed(float cooldown) => OnSkillUsed?.Invoke(cooldown);
    public static void InteractPressed() => OnInteractPressed?.Invoke();

    public static void GoldChanged(int gold) => OnGoldChanged?.Invoke(gold);
    public static void PlayerDied() => OnPlayerDied?.Invoke();

    public static void PlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);
    public static void InventoryChanged() => OnInventoryChanged?.Invoke();
}
