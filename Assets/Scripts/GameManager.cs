using UnityEngine;

// GameManager.cs — one Player property, never grows
// GameManager.cs
public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<GameManager>();
            return instance;
        }
    }

    public PlayerRefs Player { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public void RegisterPlayer(PlayerRefs refs) => Player = refs;
}