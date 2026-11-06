using UnityEngine;

public class MerchantMovement : MonoBehaviour
{
    public Transform playerTranform;

    [Tooltip("Degrees per second.")]
    [SerializeField] private float turnSpeed = 240f;

    [Tooltip("Start facing the player when it comes this close (metres).")]
    [SerializeField] private float lookRange = 6f;

    private Quaternion restRotation; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion target = restRotation;
        Vector3 mercDirToPlayer = playerTranform.position - transform.position;
        mercDirToPlayer.y = 0f;

        if (mercDirToPlayer.sqrMagnitude > 0.01f && mercDirToPlayer.sqrMagnitude <= lookRange * lookRange)
        {
            target = Quaternion.LookRotation(mercDirToPlayer);      
            
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        
    }
}
