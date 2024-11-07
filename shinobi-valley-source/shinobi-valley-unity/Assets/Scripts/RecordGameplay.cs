/* 
*   NatCorder
*   Copyright (c) 2019 Yusuf Olokoba
*/

// namespace NatCorder.Examples {




using UnityEngine;
using UnityEngine.UI;
using NatCorder.Clocks;
using NatCorder.Inputs;
using NatCorder;

using System.Collections;

public class RecordGameplay : MonoBehaviour
{

    [Header("Recording")]
    public int videoWidth = 1280;
    public int videoHeight = 720;
    public AudioListener audioListener;


    private MP4Recorder videoRecorder;
    private IClock recordingClock;
    private CameraInput cameraInput;
    private AudioInput audioInput;

    public void StartRecording()
    {
        // Start recording
        recordingClock = new RealtimeClock();
        videoRecorder = new MP4Recorder(
            videoWidth,
            videoHeight,
            30,
            AudioSettings.outputSampleRate,
            (int)AudioSettings.speakerMode,
            OnRecordingStopped,
            (int)(1280 * 720 * 11.4F), // video bitrate
            2, // keyframe interval
            LOGGER.Instance.userName + "U" + LOGGER.Instance.userId.ToString()
        );
        // Create recording inputs
        cameraInput = new CameraInput(videoRecorder, recordingClock, Camera.main);
        audioInput = new AudioInput(videoRecorder, recordingClock, audioListener);

    }



    public void StopRecording()
    {
        // Stop the recording inputs
        audioInput.Dispose();
        cameraInput.Dispose();

        // Stop recording
        videoRecorder.Dispose();
    }

    private void OnRecordingStopped(string path)
    {
        Debug.Log("Saved recording to: " + path);
        // Playback the video
    }
}
