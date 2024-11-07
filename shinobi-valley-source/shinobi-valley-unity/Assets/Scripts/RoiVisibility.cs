using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoiVisibility : MonoBehaviour
{

    public ROI[] roiList;

    [System.Serializable]
    public class ROI
    {
        public string roiLabel;
        public Transform roiTrans;
    }


    Vector3 pointOnScreen;
    Vector2 notRendered = new Vector2(-1F, -1F);




    public string ReturnVisibilityString()
    {
        string output = "";

        foreach (ROI roi in roiList)
        {

            Vector2 result = notRendered;
            foreach (Transform child in roi.roiTrans)
            {
                result = RenderedOnScreen(child);
                if (result != notRendered) break;
            }

            if (result != notRendered)
            {
                output += roi.roiLabel + result.ToString("F2") + ",";
            }
        }

        if (output.Length > 0) output = output.Substring(0, output.Length - 1);

        return output;
    }

    Vector2 RenderedOnScreen(Transform toCheck)
    {
        pointOnScreen = Camera.main.WorldToViewportPoint(toCheck.position);

        //Is in front
        if (pointOnScreen.z < 0) return notRendered;

        //Is in FOV
        if ((pointOnScreen.x < 0) || (pointOnScreen.x > 1F) ||
                (pointOnScreen.y < 0) || (pointOnScreen.y > 1F))
        {
            return notRendered;
        }

        RaycastHit hit;

        if (Physics.Linecast(Camera.main.transform.position, toCheck.position, out hit))
        {
            Debug.DrawLine(Camera.main.transform.position, toCheck.position, Color.yellow, 0.9F);
            if (hit.transform.name != toCheck.name)
            {
                return notRendered;
            }
        }

        return pointOnScreen;
    }

}
