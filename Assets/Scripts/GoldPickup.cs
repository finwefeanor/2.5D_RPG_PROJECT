using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    public int amount = 5;
    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InventoryManager inv = other.GetComponent<InventoryManager>();
            if (inv != null)
            {
                inv.AddGold(amount);
                Debug.Log($"Picked up {amount} gold.");
            }
            Destroy(gameObject);
        }
    }
}
