// ============================================================
//  CharacterVisual.cs  —  Assets/Scripts/
//
//  Sits on the ROOT of each class's visual prefab
//  (MagePlayerVisual, BarbarianPlayerVisual, ...).
//
//  Holds references to things INSIDE this visual prefab only —
//  same rule as PlayerRefs / ShopUIRefs. PlayerClass hands these
//  to the Player-root scripts after spawning the visual, so no
//  script on the Player root needs a drag-link into the visual.
//
//  Adding the component (or right-click → Reset) auto-fills the
//  Animator and the KayKit hand sockets (handslot.r / handslot.l).
// ============================================================
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    public Animator animator;

    [Header("Hand sockets (KayKit rigs: handslot.r / handslot.l)")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    [Header("Optional — class-specific equipment visuals")]
    [Tooltip("Body piece recoloured / shown when chest armour is equipped. Leave empty if this model has none.")]
    public GameObject outfitObject;

    [Tooltip("Hat shown when a head item is equipped. Leave empty if this model has none.")]
    public GameObject hatObject;

    void Reset()
    {
        animator        = GetComponent<Animator>();
        rightHandSocket = FindDeep("handslot.r");
        leftHandSocket  = FindDeep("handslot.l");
    }

    private Transform FindDeep(string childName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == childName) return t;
        return null;
    }
}
