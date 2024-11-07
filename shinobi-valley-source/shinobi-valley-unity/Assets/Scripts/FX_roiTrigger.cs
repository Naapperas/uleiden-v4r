using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

public class FX_roiTrigger : MonoBehaviour
{

    Animation anim;
    Renderer rend;

    ParticleSystem particles;

    private void Start()
    {
        particles = GetComponent<ParticleSystem>();
        anim = GetComponent<Animation>();
        rend = GetComponent<Renderer>();

        rend.materials[0].DOFade(0F, "_TintColor", 0F);
    }


    public void TriggerFX()
    {
        if (particles == null) return;

        particles.Play();
        anim.Play();
        GM.Instance.audio.PlayRoiTrigger();
        rend.materials[0].DOFade(0.7F, "_TintColor", 1F);
    }

    public void AnimEnd()
    {
        Destroy(particles);
    }
}

