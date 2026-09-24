using Unity.VisualScripting;
using UnityEngine;

public class PauseUIRefs : MonoBehaviour
{

    public static PauseUIRefs Instance {get; private set;}

    [Header("Assign in the PauseMenuCanvas prefab")]
    public GameObject pausePanel;

    void Awake() => Instance = this;
    
}
