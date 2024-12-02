using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NodeCanvas.StateMachines;

public class GameCTRL : MonoBehaviour
{
    [Header("Settings")]
    public HLP.DebugMode debug;
    public HLP.LogMode logging;
    public bool recordGameplay;
    public bool randomizeParams = true;

    [Header("Randomized Params")]
    public HLP.PlayDirection playDirection;
    public HLP.Style gameStyle = HLP.Style.NINJA;
    public bool patternsActive = true;
    public bool contextActive = true;


    [Header("Parameters")]
    public float positionLogFreq;

    [Header("Misc Assets")]
    public FSMOwner sceneFSM;

    [Header("Direction Assets")]
    public Transform anchorA;
    public Transform anchorB;
    public Transform startScene;
    public Transform endScene;


    Vector3 prevPos;
    Quaternion prevRot;
    float posDelta;
    float rotDelta;
    bool posLogActive;


    RecordGameplay recorder;
    WebCaller webcall;

    internal bool playConditionSet = false;

    // string surveyUrl = "https://leidenuniv.eu.qualtrics.com/jfe/form/SV_6opH3UpK4ANIhkG?gameid=";

    void Awake()
    {
        GM.Instance.game = this;

        // Parameters are overwritten when LOGGING is on!
        if (randomizeParams)
        {
            playDirection = (HLP.PlayDirection)Random.Range(0, 2);
            gameStyle = (HLP.Style)Random.Range(0, 2);
            patternsActive = Random.value < 0.5F;
            contextActive = Random.value < 0.5F;
        }
    }

    void Start()
    {
        recorder = GetComponent<RecordGameplay>();
        webcall = GetComponent<WebCaller>();
    }



    public void LogGameStart()
    {
        LOGGER.Instance.AddToTimeseries("GAMESESSION", "START");

        ResetPrevPosRot();
    }

    public void LogGameEnd()
    {
        LOGGER.Instance.AddToTimeseries("GAMESESSION", "END");
    }


    public void StartVideoRecording()
    {
        if (!recordGameplay) return;

#if UNITY_WEBGL
        recordGameplay = false;
        Debug.LogWarning("Not recording Gameplay footage on WebGL!");
        return;
#endif  
        Debug.Log("Starting Video Recording");
        LOGGER.Instance.AddToTimeseries("VIDEOREC", "START");
        recorder.StartRecording();
    }

    public void StopVideoRecording()
    {
        if (!recordGameplay) return;

        Debug.Log("Stopping Video Recording");
        LOGGER.Instance.AddToTimeseries("VIDEOREC", "STOP");
        recorder.StopRecording();
    }

    public void OpenSurvey()
    {
        /*         string surveyString = LOGGER.Instance.userName + "U" + LOGGER.Instance.userId.ToString();

        #if UNITY_WEBGL && !UNITY_EDITOR
                webcall.OpenSurvey(surveyString);
        #else
                Screen.fullScreen = false;
                Application.OpenURL(surveyUrl + surveyString + "&typeid=LAB");
        #endif */
    }


    #region Position Logging

    public void StartPositionLogging()
    {
        if (posLogActive) return;
        posLogActive = true;

        if (positionLogFreq > 0F)
        {
            Debug.Log("Starting Position Logging");
            ResetPrevPosRot();
            LOGGER.Instance.AddToTimeseries("INFO", "START POSLOG");
            InvokeRepeating("PostPosition", 0F, positionLogFreq);
        }
        else if (logging == HLP.LogMode.LOG)
        {
            Debug.LogWarning("Logging is on but post-frequency is 0");
        }
    }


    public void StopPositionLogging()
    {
        Debug.Log("Stopping Position Logging");
        LOGGER.Instance.AddToTimeseries("INFO", "STOP POSLOG");
        CancelInvoke("PostPosition");

        posLogActive = false;
    }

    void PostPosition()
    {
        posDelta = Vector3.Distance(prevPos, GM.Instance.player.playerObj.position);
        rotDelta = Quaternion.Angle(prevRot, Camera.main.transform.rotation);

        prevRot = Camera.main.transform.rotation;
        prevPos = GM.Instance.player.playerObj.position;

        // Debug.Log(string.Format("PosDelta: {0} -- RotDelta: {1}",posDelta,rotDelta));

        string logLine = GM.Instance.player.playerObj.position.ToString("F3") +
            "_" + Camera.main.transform.rotation.ToString("F5") +
            "_" + Camera.main.transform.rotation.eulerAngles.y.ToString("F3") +
            "," + Camera.main.transform.rotation.eulerAngles.x.ToString("F3") +
            "_POSDELTA:" + posDelta.ToString("F4") +
            "_ROTDELTA:" + rotDelta.ToString("F4") +
            "_PD:" + GM.Instance.stateLoops.pathDistance.ToString("F3") +
            "_JMP:" + (GM.Instance.player.tpCtrl.isJumping ? 1 : 0) +
            "_RUN:" + (GM.Instance.player.tpCtrl.isSprinting ? 1 : 0);

        // playerPos_cameraRotation_cameraRotationEulerY_cameraRotationEulerX_POSDELTA:posDelta_ROTDELTA:rotDelta_PD:pd_JMP:jmp_RUN:run

        LOGGER.Instance.AddToTimeseries("POSLOG", logLine);
    }

    public void ResetPrevPosRot()
    {
        prevPos = GM.Instance.player.playerObj.position;
        prevRot = Camera.main.transform.rotation;
    }

    #endregion



    public void LocationTrigger(TriggerSender.Info input)
    {
        // e.g. => GOAL_ENTER
        sceneFSM.SendEvent(input.type.ToString() + "_" + input.eve.ToString());

        // By default print all trigger events that are not STAY
        if (input.eve != TriggerSender.Event.STAY)
        {
            LOGGER.Instance.AddToTimeseries(
                "TRIGGER_" + input.type.ToString() + "_" + input.eve.ToString(),
                input.description);
        }

    }

    public void InitPlayCondition()
    {
        if (playConditionSet)
        {
            Debug.LogWarning("Play condition already set!");
            return;
        }

        // Set Play Direction
        if (playDirection == HLP.PlayDirection.A2B)
        {
            startScene.transform.parent = anchorA;
            endScene.transform.parent = anchorB;
        }
        else if (playDirection == HLP.PlayDirection.B2A)
        {
            startScene.transform.parent = anchorB;
            endScene.transform.parent = anchorA;
        }

        startScene.localPosition = Vector3.zero;
        startScene.localRotation = Quaternion.identity;
        endScene.localPosition = Vector3.zero;
        endScene.localRotation = Quaternion.identity;

        // Initialize the style in the environment
        GM.Instance.style.Init();

        playConditionSet = true;

        Debug.Log(string.Format("Local conditions assigned -- STYLE: {0}, PATTERNS: {1}, DIR: {2}, TXT: {3}",
            gameStyle.ToString(), patternsActive.ToString(), playDirection.ToString(), contextActive.ToString()));
    }


    public void QuitApplication()
    {
        Application.Quit();
    }


}
