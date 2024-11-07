using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupCTRL : MonoBehaviour
{

    public Transform pickupModel;

    // void DelayedDestruction()
    // {
    //     Destroy(gameObject);
    // }

    public void PickupBanana()
    {
        pickupModel.gameObject.SetActive(false);
        // Invoke("DelayedDestruction", 5F);
    }
}
