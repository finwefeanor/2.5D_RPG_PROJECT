using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;


// On the Player root. The ONE place that decides how many summons exist and where new
// ones appear. It does not think for them: each Summon picks its own target.
public class PlayerSummons : MonoBehaviour
{

    [Tooltip("Metres in front of the player where the row of new summons is centred.")]
    public float spawnForward = 1.5f;

    [Tooltip("How far NavMesh.SamplePosition may move a spawn point to reach walkable ground.")]
    public float navMeshSnapDistance = 2f;

    private readonly List<Summon> active = new List<Summon>();   // oldest first

    void OnEnable()  { GameEvents.OnPlayerDied += DespawnAll; }
    void OnDisable() { GameEvents.OnPlayerDied -= DespawnAll; }

    // Called by PlayerAttack on the summon clip's DealAttackDamage event.
    public void Spawn(AbilityDefinition ability, int damage)
    {
        if (ability.summonPrefab == null)
        {
            Debug.LogWarning($"{ability.name}: delivery is Summon but summonPrefab is empty.");
            return;
        }

        active.RemoveAll(s => s == null);   // anything destroyed behind our back

        int count = Mathf.Max(1, ability.summonCount);
        int cap = Mathf.Max(1, ability.maxActiveSummons);

        for (int i = 0; i < count; i++)
        {
            // Cap reached: the oldest one goes first.
            while (active.Count >= cap)
            {
                Summon oldest = active[0];
                active.RemoveAt(0);
                if (oldest != null) oldest.Despawn();
            }

            if (!TryGetSpawnPoint(i, count, ability.summonSpacing, out Vector3 point)) continue;

            Summon summon = Instantiate(ability.summonPrefab, point, transform.rotation);
            summon.Init(this, damage, ability.summonLifetime, FreeSlot());
            active.Add(summon);
        }
    }

        // A row in front of the player: 2 summons = -0.5 / +0.5 spacing, 3 = -1 / 0 / +1.
    // SamplePosition moves each point onto the NavMesh - an agent created off the mesh
    // can never move. This is the only place SamplePosition is needed.
    private bool TryGetSpawnPoint(int index, int count, float spacing, out Vector3 point)
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float side = (index - (count - 1) * 0.5f) * spacing;
        Vector3 wanted = transform.position + forward * spawnForward + right * side;

        if (NavMesh.SamplePosition(wanted, out NavMeshHit hit, navMeshSnapDistance, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        // Wall in front: fall back to the player's own spot, which is always walkable.
        if (NavMesh.SamplePosition(transform.position, out hit, navMeshSnapDistance, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

        // Lowest follow slot no living summon uses, so they don't stand on top of each other.
    private int FreeSlot()
    {
        for (int slot = 0; ; slot++)
        {
            bool taken = false;
            foreach (Summon s in active)
            {
                if (s != null && s.Slot == slot) { taken = true; break; }
            }
            if (!taken) return slot;
        }
    }

        // Called by a Summon when it leaves on its own (lifetime over).
    public void Remove(Summon summon) => active.Remove(summon);

    // Player died (and later: class switch).
    public void DespawnAll()
    {
        Summon[] all = active.ToArray();   // Despawn calls Remove - don't edit the list mid-loop
        active.Clear();
        foreach (Summon s in all)
        {
            if (s != null) s.Despawn();
        }
    }


}
