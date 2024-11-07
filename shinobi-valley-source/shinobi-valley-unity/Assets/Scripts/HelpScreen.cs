using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpScreen : MonoBehaviour
{

    public Toggle invertToggle;
    public Scrollbar camSensitivity;

    private void OnEnable()
    {
        if (GM.Instance.player == null) return;
        if (GM.Instance.player.tpInput == null) return;

        GM.Instance.player.blockSettingChange = true;
        invertToggle.isOn = GM.Instance.player.tpInput.invertCamera;
        float input = GM.Instance.player.tpInput.sensitivityMultiplier;

        if (input == 1F) camSensitivity.value = 0.5F;
        else if (input > 1F) camSensitivity.value = HLP.Map(input, 1F, 10F, 0.5F, 1F);
        else camSensitivity.value = HLP.Map(input, 0.1F, 1F, 0F, 0.5F);
   

        GM.Instance.player.blockSettingChange = false;
    }

}
