using UnityEngine;

public class InteractButtonRelay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void OnInteractButtonPressed() => GameEvents.InteractPressed();

    
}
