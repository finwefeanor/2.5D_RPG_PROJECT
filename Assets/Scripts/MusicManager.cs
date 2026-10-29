// ============================================================
//  MusicManager.cs  -  Assets/Scripts/
//  Lives on the GameManager prefab. Plays one looping 2D
//  background track for the whole scene.
//
//  The AudioSource is created and configured here, so the
//  prefab only stores data (which clip, how loud).
//  Import music clips with Load Type = Streaming.
// ============================================================
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Tooltip("Looping background track (Load Type = Streaming in its import settings).")]
    [SerializeField] private AudioClip music;

    [Tooltip("Music volume. Keep it under the SFX so hits stay readable.")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.35f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();

        source.clip = music;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 2D: same volume wherever the camera is
        source.volume = volume;
        source.priority = 0;        // highest priority: SFX voices never steal it
    }

    void Start()
    {
        if (music != null) source.Play();
    }

    public void StopGeneralMusic()
    {
        if (source != null) source.Stop();
    }
}
