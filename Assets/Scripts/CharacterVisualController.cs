// ============================================================
//  CharacterVisualController.cs  -  Assets/Scripts/
//  Attach to the Player GameObject.
//
//  Listens to EquipmentManager.OnEquipmentChanged and
//  updates the 3D outfit child object accordingly.
//  This is the ONLY script that touches the visual -
//  no other script should enable/disable the outfit directly.
// ============================================================
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterVisualController : MonoBehaviour
{
    [Header("Equipment pieces (filled at runtime by PlayerClass from the CharacterVisual)")]
    [FormerlySerializedAs("hatObject")]
    public GameObject headPiece;
    [FormerlySerializedAs("outfitObject")]
    public GameObject chestPiece;

    [Header("Hand sockets (drag handslot.l / handslot.r Transforms here)")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    private EquipmentManager _equipmentManager;
    private Material _chestMaterial;

    private GameObject _currentRightHandInstance;
    private GameObject _currentLeftHandInstance;


    // Called by PlayerClass (execution order -100) BEFORE this Awake runs,
    // so the material caching below already sees the spawned visual's outfit.
    public void BindVisual(CharacterVisual v)
    {
        headPiece       = v.headPiece;
        chestPiece      = v.chestPiece;
        rightHandSocket = v.rightHandSocket;
        leftHandSocket  = v.leftHandSocket;
    }

    void Awake()
    {
        _equipmentManager = GetComponent<EquipmentManager>();
        if (_equipmentManager == null)
        {
            Debug.LogError("CharacterVisualController requires EquipmentManager on the same GameObject.");
            return;
        }

        // Own material copy for the chest piece, so tinting it doesn't recolour the
        // whole shared KayKit 'skeleton' material (every skeleton in the scene uses it).
        if (chestPiece != null)
        {
            var renderer = chestPiece.GetComponent<Renderer>();
            if (renderer != null)
                _chestMaterial = renderer.material = new Material(renderer.sharedMaterial);
        }

        // Pieces are part of the model and visible by default - hide until equipped.
        if (headPiece  != null) headPiece.SetActive(false);
        if (chestPiece != null) chestPiece.SetActive(false);

        // Subscribe to equipment changes
        _equipmentManager.OnEquipmentChanged += RefreshVisuals;
    }

    void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        if (_equipmentManager != null)
            _equipmentManager.OnEquipmentChanged -= RefreshVisuals;
    }

    void RefreshVisuals()
    {
        // No early return on a missing outfit: models without one (Barbarian, Rogue)
        // must still show weapons. Every block below null-checks its own object.
        // Check if any visual slot is filled (Head or Chest)
        var headItem = _equipmentManager.GetEquipped(EquipSlot.Head);
        var chestItem = _equipmentManager.GetEquipped(EquipSlot.Chest);
        var rightHandItem = _equipmentManager.GetEquipped(EquipSlot.RightHand); //added
        var leftHandItem = _equipmentManager.GetEquipped(EquipSlot.LeftHand); //added

        // Chest piece: shown while a Chest item is equipped, tinted by its outfitColor.
        if (chestPiece != null)
        {
            chestPiece.SetActive(chestItem != null);
            if (chestItem != null && _chestMaterial != null)
                _chestMaterial.color = chestItem.outfitColor;
        }

        // Head piece: shown while a Head item is equipped.
        if (headPiece != null)
            headPiece.SetActive(headItem != null);

        // Weapons/shield: spawn prefab at socket
        UpdateHandSlot(rightHandItem, rightHandSocket, ref _currentRightHandInstance);
        UpdateHandSlot(leftHandItem, leftHandSocket, ref _currentLeftHandInstance);
    }

    void UpdateHandSlot(ItemData item, Transform socket, ref GameObject currentInstance)
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        if (item != null && item.equipPrefab != null && socket != null)
        {
            currentInstance = Instantiate(item.equipPrefab, socket);
            currentInstance.transform.localPosition = item.equipPositionOffset;
            currentInstance.transform.localRotation = Quaternion.Euler(item.equipRotationOffset);
            currentInstance.transform.localScale = Vector3.one;
        }
    }




}