// ============================================================
//  CharacterVisualController.cs  —  Assets/Scripts/
//  Attach to the Player GameObject.
//
//  Listens to EquipmentManager.OnEquipmentChanged and
//  updates the 3D outfit child object accordingly.
//  This is the ONLY script that touches the visual —
//  no other script should enable/disable the outfit directly.
// ============================================================
using UnityEngine;

public class CharacterVisualController : MonoBehaviour
{
    [Header("Outfit / Hat (built-in rig objects — just toggled)")]
    public GameObject outfitObject;
    public GameObject hatObject; // e.g. Skeleton_Mage_Hat, drag it in as-is

    [Header("Hand sockets (drag handslot.l / handslot.r Transforms here)")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    private EquipmentManager _equipmentManager;
    private Material _outfitMaterial;

    private GameObject _currentRightHandInstance;
    private GameObject _currentLeftHandInstance;


    void Awake()
    {
        _equipmentManager = GetComponent<EquipmentManager>();
        if (_equipmentManager == null)
        {
            Debug.LogError("CharacterVisualController requires EquipmentManager on the same GameObject.");
            return;
        }

        // Cache a material instance so we don't modify the shared asset
        if (outfitObject != null)
        {
            var renderer = outfitObject.GetComponent<Renderer>();
            if (renderer != null)
                _outfitMaterial = renderer.material = new Material(renderer.sharedMaterial);
        }

        if (hatObject != null)
            hatObject.SetActive(false); // hidden by default, same as outfit

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
        if (outfitObject == null) return;
        // Check if any visual slot is filled (Head or Chest)
        var headItem = _equipmentManager.GetEquipped(EquipSlot.Head);
        var chestItem = _equipmentManager.GetEquipped(EquipSlot.Chest);
        var rightHandItem = _equipmentManager.GetEquipped(EquipSlot.RightHand); //added
        var leftHandItem = _equipmentManager.GetEquipped(EquipSlot.LeftHand); //added

        // Outfit color swap — driven ONLY by chest slot
        if (outfitObject != null)
        {
            if (chestItem != null)
            {
                outfitObject.SetActive(true);
                if (_outfitMaterial != null)
                    _outfitMaterial.color = chestItem.outfitColor;
            }
            else
            {
                outfitObject.SetActive(false);
            }
        }
        // Hat: simple on/off
        if (hatObject != null)
            hatObject.SetActive(headItem != null);
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
