using UnityEngine;

// Plays one clip out of a set (hurt sounds, death sounds). Never the same clip twice in
// a row when there are 2+ clips, plus a small random pitch, so a crowd of skeletons
// doesn't sound like one sample on loop. Used by Enemy and Summon.
public static class RandomSound
{
    // Index of a clip to play, different from lastIndex when there is a choice.
    // -1 = empty set.
    public static int PickIndex(AudioClip[] clips, int lastIndex)
    {
        if (clips == null || clips.Length == 0) return -1;
        if (clips.Length == 1) return 0;
        if (lastIndex < 0 || lastIndex >= clips.Length) return Random.Range(0, clips.Length);

        int i = Random.Range(0, clips.Length - 1);   // one slot fewer...
        if (i >= lastIndex) i++;                     // ...then step over the last one
        return i;
    }

    // Plays one clip from the set through source, at a random pitch. Empty set (or an
    // empty slot) = the source's own clip. Returns the index played: keep it and pass it
    // back as lastIndex next time.
    public static int Play(AudioSource source, AudioClip[] clips, float pitchVariation, int lastIndex)
    {
        if (source == null) return lastIndex;

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        int i = PickIndex(clips, lastIndex);
        if (i >= 0 && clips[i] != null) source.PlayOneShot(clips[i]);
        else source.Play();
        return i;
    }
}
