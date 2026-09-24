using UnityEngine;

public class PlayerUIRefs : MonoBehaviour
{

    public static PlayerUIRefs Instance { get; private set; }

    [Header("Assign inside the PlayerHealthBarCanvas prefab")]
    public GameObject deathScreenPanel;

    void Awake() => Instance = this;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
