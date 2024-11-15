﻿using UnityEngine;
using System.Runtime.InteropServices;

public class WebCaller : MonoBehaviour
{

    [DllImport("__Internal")]
    private static extern void CallOpenSurvey(string str);

    [DllImport("__Internal")]
    private static extern void CallFpsWarning();

    public void OpenSurvey(string input)
    {
        CallOpenSurvey(input);
    }

    public void FpsWarning()
    {
        CallFpsWarning();
    }
}