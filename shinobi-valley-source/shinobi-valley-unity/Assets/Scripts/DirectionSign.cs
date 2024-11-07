using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionSign : MonoBehaviour
{

    public Transform sign;

    public void SetDirection(HLP.PlayDirection input)
    {
        if (input == HLP.PlayDirection.A2B ) sign.localEulerAngles = Vector3.zero;
        else sign.localEulerAngles = new Vector3(0F, 180F, 0F);
    }

}
