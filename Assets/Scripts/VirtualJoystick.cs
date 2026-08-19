using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Attach this to a UI Image (the joystick "background" circle).
// Add a child UI Image (the joystick "handle" knob) and assign it in the Inspector.
// Both Images need Image > Raycast Target enabled.
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 1f; // max distance handle can move, in local units

    private Vector2 input = Vector2.zero;

    public float Horizontal => input.x;
    public float Vertical => input.y;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("pointer down");
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out position))
        {
            position.x = position.x / background.sizeDelta.x;
            position.y = position.y / background.sizeDelta.y;

            input = new Vector2(position.x * 2, position.y * 2);
            input = (input.magnitude > 1f) ? input.normalized : input;

            handle.anchoredPosition = new Vector2(
                input.x * (background.sizeDelta.x / 2) * handleRange,
                input.y * (background.sizeDelta.y / 2) * handleRange
            );
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
