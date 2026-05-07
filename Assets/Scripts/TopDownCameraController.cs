// ============================================================
//  TopDownCameraController.cs  –  place in Assets/Scripts/
//  Attach to Main Camera.  Assign the Player transform in the
//  Inspector, or it will auto-find the "Player" GameObject.
// ============================================================
using UnityEngine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // drag the Player here

    [Header("Position")]
    //public Vector3  offset      = new Vector3(0, 22, -14);
    public Vector3 offset = new Vector3(0, 18, -18);
    public float    smoothSpeed = 8f;

    [Header("Rotation")]
    public float pitchAngle     = 45f; // degrees down from horizontal
    public bool  lockRotation   = true;

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
            else return;
        }

        // Desired position
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired,
                                          smoothSpeed * Time.deltaTime);

        if (lockRotation)
            transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
    }
}
