using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using DoozyUI;


[System.Serializable]
public class GoToTrigger : UnityEvent<bool, string> { }

[System.Serializable]
public class UnityEventString : UnityEvent<string> { }

[System.Serializable]
public class UnityEventInt : UnityEvent<int> { }

[System.Serializable]
public class UnityEventFloat : UnityEvent<float> { }


public class HLP : MonoBehaviour
{

    public enum DebugMode { DEPLOY, FASTFORWARD, DEBUG }
    public enum LogMode { LOG, NOLOG }
    public enum PlayDirection { A2B, B2A }
    public enum Style { NINJA, SPACE }

    public static System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    public static int IntParseFast(string value)
    {
        value = value.Replace(" ", "");

        int result = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char letter = value[i];
            result = 10 * result + (letter - 48);
        }
        return result;
    }

    public static void SyncToTransform(Transform syncThis, Transform toThat)
    {
        syncThis.position = toThat.position;
        syncThis.rotation = toThat.rotation;
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    public static float Map(float x, float in_min, float in_max, float out_min, float out_max, bool clamp = true)
    {
        if (clamp) x = Mathf.Max(in_min, Mathf.Min(x, in_max));
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    public static void FadeIn()
    {
        UIManager.HideUiElement("FadeBlack", "Utility");
    }

    public static void FadeOut()
    {
        UIManager.ShowUiElementAndHideAllTheOthers("FadeBlack", "Utility", false);
    }

}
