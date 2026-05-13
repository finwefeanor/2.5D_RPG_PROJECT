using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource generalMusic;

    void Start()
    {
        generalMusic.Play();
    }

    public void StopGeneralMusic()
    {
        generalMusic.Stop();
    }
}