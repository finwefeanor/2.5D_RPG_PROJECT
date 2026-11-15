using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public interface ICombatTarget
{
    Transform Root { get; } // what to chase / face
    float Radius { get; }   // for edge distance

    bool IsAlive { get; }

    void TakeDamage (int damage );

}
