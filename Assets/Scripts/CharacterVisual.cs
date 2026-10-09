// ============================================================
//  CharacterVisual.cs  -  Assets/Scripts/
//
//  Sits on the ROOT of each class's visual prefab
//  (MagePlayerVisual, BarbarianPlayerVisual, ...).
//
//  Holds references to things INSIDE this visual prefab only -
//  same rule as PlayerRefs / ShopUIRefs. PlayerClass hands these
//  to the Player-root scripts after spawning the visual, so no
//  script on the Player root needs a drag-link into the visual.
//
//  Adding the component (or right-click → Reset) auto-fills the
//  Animator and the KayKit hand sockets (handslot.r / handslot.l).
// ============================================================
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterVisual : MonoBehaviour
{
    public Animator animator;

    [Header("Hand sockets (KayKit rigs: handslot.r / handslot.l)")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    [Header("Equipment pieces - built into THIS model, hidden until that slot is filled")]
    [Tooltip("Shown while a Head item is equipped. Mage: Skeleton_Mage_Hat, " +
             "Barbarian: Skeleton_Warrior_Helmet, Rogue: Skeleton_Rogue_Hood. Empty = no head piece.")]
    [FormerlySerializedAs("hatObject")]
    public GameObject headPiece;

    [Tooltip("Shown while a Chest item is equipped, tinted by the item's Outfit Color (white = original). " +
             "Barbarian: Skeleton_Warrior_Cloak, Rogue: Skeleton_Rogue_Cape. Empty = no chest piece.")]
    [FormerlySerializedAs("outfitObject")]
    public GameObject chestPiece;

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