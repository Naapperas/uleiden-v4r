using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

public class CharacterHandler : MonoBehaviour
{
    public Material openEyes;
    public Material glanceEyes;
    public Material closedEyes;

    public Renderer localRender;

    Animator anim;
    Material[] mats;

    bool isMeditating = false;
    bool didFacePlayer = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }


    public void CloseEyes()
    {
        mats[1] = closedEyes;
        localRender.materials = mats;
    }

    public void OpenEyes()
    {
        mats[1] = openEyes;
        localRender.materials = mats;
    }

    public void GlanceEyes()
    {
        mats[1] = glanceEyes;
        localRender.materials = mats;
    }

    public void StartMeditation()
    {
        mats = localRender.materials;

        anim.SetTrigger("StartMeditation");
        CloseEyes();
        isMeditating = true;
    }

    public void EndMeditation()
    {
        
        anim.SetTrigger("StopMeditation");
        OpenEyes();
        isMeditating = false;
    }

    public void FacePlayer()
    {
        if (!isMeditating && !didFacePlayer)
        {
            transform.DOLookAt(GM.Instance.player.playerObj.position, 0.5F, AxisConstraint.Y);
            didFacePlayer = true;
        }
    }

}
