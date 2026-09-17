// ============================================================
//  TopDownCameraController.cs  –  place in Assets/Scripts/
//  Attach to Main Camera. Target resolves at runtime from
//  GameManager.Instance.Player — no Inspector wiring needed.
// ============================================================
using UnityEngine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("Position")]
    //public Vector3 offset = new Vector3(0, 22, -14);
    public Vector3 offset = new Vector3(0, 18, -18);
    public float smoothSpeed = 8f;

    [Header("Rotation")]
    public float pitchAngle = 45f; // degrees down from horizontal
    public bool lockRotation = true;

    private Transform target;

    void Start()
    {
        ResolvePlayer();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired,
                                          smoothSpeed * Time.deltaTime);

        if (lockRotation)
            transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
    }

    private void ResolvePlayer()
    {
        PlayerRefs refs = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (refs == null)
        {
            Debug.LogWarning($"{name}: no Player registered — camera will not follow.", this);
            return;
        }

        target = refs.transform;

        // Snap to position immediately so the camera doesn't glide in from
        // wherever it was left in the scene on the first few frames.
        transform.position = target.position + offset;
        if (lockRotation)
            transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
    }
}