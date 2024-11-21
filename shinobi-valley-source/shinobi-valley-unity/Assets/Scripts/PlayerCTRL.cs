using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Invector.CharacterController;

using BansheeGz.BGSpline.Components;



public class PlayerCTRL : MonoBehaviour
{
    public enum Perspective { THIRDPERSON, FIRSTPERSON }

    public BGCcMath respawnPerimeter;

    // HACK: make public so we can edit it in the editor
    public Perspective persp;

    // TODO: add slider for randomization threshold

    internal Transform playerObj;
    internal vThirdPersonCamera tpCam;
    internal vThirdPersonController tpCtrl;
    internal vThirdPersonInput tpInput;
    float camDistance;
    GameObject playerModel;
    float fallTimer;

    float respawnTimer;
    int permafallCount;

    [HideInInspector] public bool blockSettingChange;

    void Awake()
    {
        GM.Instance.player = this;
    }

    void Start()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player").transform;

        tpCam = Camera.main.GetComponent<vThirdPersonCamera>();
        tpInput = playerObj.GetComponent<vThirdPersonInput>();
        tpCtrl = playerObj.GetComponent<vThirdPersonController>();
        camDistance = tpCam.defaultDistance;

        playerModel = playerObj.Find("PlayerModel").gameObject;

        if (Random.Range(1, 10) >= 5)
        {
            SetPerspectiveTo(Perspective.THIRDPERSON);
        }
        else
        {
            SetPerspectiveTo(Perspective.FIRSTPERSON);
        }
    }

    void LateUpdate()
    {
        MonitorPermaFalling();
    }

    void MonitorPermaFalling()
    {
        if (tpCtrl.isGrounded)
        {
            fallTimer = Time.time;
            return;
        }

        // Ground player if falling persists for 3 seconds
        if (fallTimer + 3.0f < Time.time)
        {
            tpCtrl.isGrounded = true;
            permafallCount++;

            if (permafallCount == 1) respawnTimer = Time.time;

            Debug.LogWarning("Permafall Fix");
        }

        // Respawn if permaFall does not fix
        if (permafallCount > 10)
        {
            Debug.LogWarning("Count above 10");


            if (respawnTimer + 2F > Time.time)
            {
                Debug.LogWarning("RESPAWN");
                tpCtrl.isGrounded = true;


                // Respawn if the count happened within the last second
                LOGGER.Instance.AddToTimeseries("PERMAFALLRESPAWN", GM.Instance.player.playerObj.position.ToString("F3"));
                playerObj.position = respawnPerimeter.CalcPositionByClosestPoint(GM.Instance.player.playerObj.position);
                GM.Instance.game.ResetPrevPosRot();

            }

            permafallCount = 0;
        }

    }


    public void SetPerspectiveTo(Perspective input)
    {
        tpCam.defaultDistance =
            input == Perspective.THIRDPERSON ? camDistance : 0f;

        playerModel.SetActive(input == Perspective.THIRDPERSON);
        persp = input;
    }

    public void LockMovement(bool input)
    {
        tpCtrl.lockMovement = input;
    }

    public void LockCamera(bool input)
    {
        tpCam.lockCamera = input;
        tpInput.ControlsActive(!input);
    }

    public void InvertCamera(bool input)
    {
        if (blockSettingChange) return;
        tpInput.invertCamera = input;
    }

    public void SetCameraSensitivity(float input)
    {
        if (blockSettingChange) return;

        if (input == 0.5F) tpInput.sensitivityMultiplier = 1F;
        else if (input > 0.5F) tpInput.sensitivityMultiplier = HLP.Map(input, 0.5F, 1F, 1F, 10F);
        else tpInput.sensitivityMultiplier = HLP.Map(input, 0F, 0.5F, 0.1F, 1F);
    }


    public Quaternion GetCamRotation()
    {
        return tpCam.transform.rotation;
    }
}
