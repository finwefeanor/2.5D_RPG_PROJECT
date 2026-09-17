using UnityEngine;

public class AttackButtonRelay : MonoBehaviour
{
    public void OnAttackButtonPressed() => GameEvents.AttackPressed();
    
}
