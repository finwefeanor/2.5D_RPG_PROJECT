using UnityEngine;
using System;

public static class GameEvents
{
    public static event Action<int, int> OnPlayerHealthChanged;
    public static event Action OnInventoryChanged;


    public static event Action OnPlayerDied;

    public static event Action<int> OnGoldChanged;

    public static event Action OnInteractPressed;
    public static event Action OnAttackPressed;
    public static void AttackPressed() => OnAttackPressed?.Invoke();
    public static void InteractPressed() => OnInteractPressed?.Invoke();

    public static void GoldChanged(int gold) => OnGoldChanged?.Invoke(gold);
    public static void PlayerDied() => OnPlayerDied?.Invoke();

    public static void PlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);
    public static void InventoryChanged() => OnInventoryChanged?.Invoke();
}
