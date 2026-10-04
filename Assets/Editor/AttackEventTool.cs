using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools → RPG → Attack Events
// Pick an FBX + clip, type the contact frame, one click writes
// PlayAttackSound + DealAttackDamage at exactly that frame.
public class AttackEventTool : EditorWindow
{
    private GameObject fbx;
    private int clipIndex;
    private int contactFrame;

    [MenuItem("Tools/RPG/Attack Events")]
    private static void Open() => GetWindow<AttackEventTool>("Attack Events");

    private void OnGUI()
    {
        fbx = (GameObject)EditorGUILayout.ObjectField("FBX", fbx, typeof(GameObject), false);
        var importer = fbx != null
            ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(fbx)) as ModelImporter
            : null;
        if (importer == null)
        {
            EditorGUILayout.HelpBox("Drag an FBX from the Project window.", MessageType.Info);
            return;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;

        string[] names = clips.Select(c => c.name).ToArray();
        clipIndex = Mathf.Clamp(EditorGUILayout.Popup("Clip", clipIndex, names), 0, names.Length - 1);
        ModelImporterClipAnimation clip = clips[clipIndex];
        int length = Mathf.Max(1, Mathf.RoundToInt(clip.lastFrame - clip.firstFrame));

        contactFrame = EditorGUILayout.IntSlider("Contact frame", contactFrame, 0, length);
        EditorGUILayout.LabelField("Normalized time", (contactFrame / (float)length).ToString("F3"));

        string current = string.Join(", ",
            clip.events.Select(e => $"{e.functionName} @ {e.time * length:F1}"));
        EditorGUILayout.LabelField("Current events", string.IsNullOrEmpty(current) ? "none" : current);

        if (GUILayout.Button("Set PlayAttackSound + DealAttackDamage"))
        {
            float t = contactFrame / (float)length;

            // Replace any existing attack events on this clip, keep everything else.
            clip.events = clip.events
                .Where(e => e.functionName != "PlayAttackSound" && e.functionName != "DealAttackDamage")
                .Append(new AnimationEvent { functionName = "PlayAttackSound", time = t })
                .Append(new AnimationEvent { functionName = "DealAttackDamage", time = t })
                .OrderBy(e => e.time)
                .ToArray();

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }
}