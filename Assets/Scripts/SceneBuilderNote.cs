// ============================================================
//  SceneBuilderNote.cs  –  place in Assets/Scripts/
//  A lightweight component that stores a developer note
//  on auto-generated GameObjects.  Visible in the Inspector.
// ============================================================
using UnityEngine;

public class SceneBuilderNote : MonoBehaviour
{
    [TextArea(2, 6)]
    public string Note = "";
}
