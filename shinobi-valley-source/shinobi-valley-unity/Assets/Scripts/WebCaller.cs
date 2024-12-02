using UnityEngine;
using System.Runtime.InteropServices;

public class WebCaller : MonoBehaviour
{

    [DllImport("__Internal")]
    private static extern void CallOpenSurvey(string str);

    [DllImport("__Internal")]
    private static extern void CallFpsWarning();

    public void OpenSurvey(string input)
    {
        // Participants are meant to start answering the survey before playing
        // CallOpenSurvey(input);
    }

    public void FpsWarning()
    {
        // DO nothing, since on low spec hardware this can make the web-based game crash
        // CallFpsWarning();
    }
}