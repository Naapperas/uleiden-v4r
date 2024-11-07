using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCTRL : MonoBehaviour
{
    [Header("ROI Cue")]
    public AudioSource roiCueSource;
    public AudioClip[] roiTriggerCue;

    [Header("Footsteps")]
    public bool stepsActive;
    public AudioSource stepSource;
    public AudioClip[] stepSounds;

    [Header("Jump")]
    public AudioSource jumpSource;

    [Header("Falling")]
    public AudioSource fallSource;

    [Header("EndSounds")]
    public AudioSource chirpSource;

    [Header("Others")]
    public AudioSource introMusic;


    float stepVolume;
    int roiCuePointer;

    void Awake()
    {
        GM.Instance.audio = this;
    }

    void Start()
    {
        stepVolume = stepSource.volume;
    }

    public void PlayRoiTrigger()
    {
        if (roiTriggerCue.Length == 0) return;

        if (roiCuePointer >= roiTriggerCue.Length) roiCuePointer = 0;
        roiCueSource.PlayOneShot(roiTriggerCue[roiCuePointer]);
        roiCuePointer += 1;
    }

    public void PlayFootstep()
    {
        if (stepSounds.Length == 0 || !stepsActive) return;

        stepSource.pitch = Random.Range(0.8F, 1.2F);
        stepSource.volume = stepVolume * Random.Range(0.8F, 1.2F);
        stepSource.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Length)]);
    }

    public void PlayFalling()
    {
        fallSource.Play();
    }
}
