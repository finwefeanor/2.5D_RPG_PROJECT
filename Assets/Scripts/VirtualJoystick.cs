using UnityEngine;
using UnityEngine.EventSystems;

// On-screen joystick. Attach to the background circle (UI Image, Raycast Target on);
// its child Image is the handle knob (anchored to the centre of the background).
// PlayerController reads Horizontal / Vertical when the keyboard axes are idle.
//
// Input is measured from the CENTRE of the background rect, whatever its pivot is.
// (The old code measured from the pivot: with a bottom-left pivot, "down" and "left"
// only registered when the finger left the circle - impossible at the screen edge.)
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Tooltip("How far the knob travels, as a fraction of the background radius.")]
    [SerializeField] private float handleRange = 1f;

    [Tooltip("Input below this (0-1) counts as zero, so a resting thumb doesn't creep.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float deadZone = 0.1f;

    private Vector2 input = Vector2.zero;

    public float Horizontal => input.x;
    public float Vertical => input.y;

    public static VirtualJoystick Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 radius = background.rect.size * 0.5f;
        Vector2 offset = local - background.rect.center;   // from the circle's centre

        input = new Vector2(offset.x / radius.x, offset.y / radius.y);
        input = Vector2.ClampMagnitude(input, 1f);
        if (input.magnitude < deadZone) input = Vector2.zero;

        handle.anchoredPosition = new Vector2(input.x * radius.x, input.y * radius.y) * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
