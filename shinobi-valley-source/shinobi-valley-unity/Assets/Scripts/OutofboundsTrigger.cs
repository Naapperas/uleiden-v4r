using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

using DoozyUI;
using BansheeGz.BGSpline.Components;

public class OutofboundsTrigger : MonoBehaviour
{
    public BGCcMath respawnPerimeter;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine("FallSequence");
        }

    }

    IEnumerator FallSequence()
    {
        GM.Instance.audio.stepsActive = false;
        GM.Instance.audio.PlayFalling();
        HLP.FadeOut();
        yield return new WaitForSeconds(0.5F);

        LOGGER.Instance.AddToTimeseries("OUTOFBOUNDS", GM.Instance.player.playerObj.position.ToString("F3"));

        GM.Instance.player.playerObj.position = respawnPerimeter.CalcPositionByClosestPoint(GM.Instance.player.playerObj.position);
        GM.Instance.game.ResetPrevPosRot();

        HLP.FadeIn();
        yield return new WaitForSeconds(1F);
        GM.Instance.audio.stepsActive = true;
    }
}
