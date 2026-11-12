using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class PlayerSummons : MonoBehaviour
{

    public float spawnRange = 5.0f;
    private NavMeshAgent navMeshAgent;

    private Transform playerPosition;

    private GameObject spawnLocation;

    public List<Summon> summonLists = new List<Summon>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        
    }


    public bool SpawnPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 spawnDirtoPlayer = playerPosition.transform.position - spawnLocation.transform.position;
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomSpawnPoint = spawnDirtoPlayer + Random.insideUnitSphere * spawnRange;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomSpawnPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    private void DespawnAll()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 point;
        if (SpawnPoint(transform.position, spawnRange, out point))
        {
            Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
        }
        
    }

}
