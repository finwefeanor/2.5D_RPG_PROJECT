using UnityEngine;
using UnityEngine.EventSystems;

// On the HUDCanvas JumpButton. Fires on finger DOWN (like ATTACK), only raises
// GameEvents.JumpPressed - the canvas never references the Player (rule 1).
public class JumpButtonRelay : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData) => GameEvents.JumpPressed();
}
