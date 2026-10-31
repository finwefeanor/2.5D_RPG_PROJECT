using UnityEngine;
using UnityEngine.EventSystems;

// On the HUDCanvas AttackButton. Fires the attack the moment the finger touches the
// button (pointer DOWN), not when it lifts (Button.onClick fires on release, which feels
// laggy for combat and loses taps when the thumb slides off). The Button's OnClick list is
// therefore empty - wiring it again would attack twice per tap.
// The canvas never references the Player (rule 1): it only raises GameEvents.AttackPressed.
public class AttackButtonRelay : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData) => GameEvents.AttackPressed();

    // Kept for any old OnClick wiring / testing from code.
    public void OnAttackButtonPressed() => GameEvents.AttackPressed();
}
