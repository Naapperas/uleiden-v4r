using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepTrigger : MonoBehaviour
{

    AudioCTRL audioCtrl;

    int contactPoints;

    private void Start()
    {
        audioCtrl = GM.Instance.audio;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (InIgnoreList(other)) return;

        if (contactPoints <= 0)
        {
            audioCtrl.PlayFootstep();
            contactPoints = 0;
        }

        contactPoints++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (InIgnoreList(other)) return;
        contactPoints--;
    }

    bool InIgnoreList(Collider other)
    {
        if (other.CompareTag("Player")) return true;
        if (other.name.Equals("OutOfBoundsTrigger")) return true;
        if (other.name.Equals("StartZoneTrigger")) return true;
        if (other.name.Equals("GoalTrigger")) return true;
        if (other.name.Equals("ROI_Trigger")) return true;
        if (other.name.Equals("ChunkTrigger")) return true;

        return false;
    }
}
