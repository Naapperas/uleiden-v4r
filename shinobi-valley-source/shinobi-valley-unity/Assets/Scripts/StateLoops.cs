using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BansheeGz.BGSpline.Components;


using UnityEngine.Audio;
using DG.Tweening;
using DoozyUI;

using TMPro;

public class StateLoops : MonoBehaviour
{

    const float QTIMERBASE = 60F;
    readonly int[] QINTERVALS = { 1, 3, 5, 8, 12, 17, 23, 30, 38, 47, 57 };


    public AudioMixerGroup musicMix;
    public CanvasGroup helperMenuIndicator;
    public CanvasGroup informationPanel;
    public Transform playerMeditationPoint;
    public Transform endShotCamPoint;

    public TextMeshProUGUI idText;

    public BGCcMath pathCurve;

    [Header("WEB / LAB End Slide")]
    public GameObject webRoot;
    public GameObject labRoot;
    public TextMeshProUGUI idOutput;

    [Header("Connection Feedback")]
    public TextMeshProUGUI connectionFeedback;



    [HideInInspector] public float pretimer = 0f;
    [HideInInspector] public float timer = 0f;
    [HideInInspector] public float questionTimer = 0f;


    [HideInInspector] public float lookSum = 0f;
    [HideInInspector] public bool meditationOver = false;
    [HideInInspector] public string timeReply;
    [HideInInspector] public bool routineActive = false;
    [HideInInspector] public bool permitHelperMenu = false;
    [HideInInspector] public bool reachedEnd = false;

    [HideInInspector] public bool timerPaused = false;

    [HideInInspector] public float pathDistance = 0F;


    CharacterHandler sensei;


    float prevRot = 0f;
    bool showingHelperMenu = false;
    bool uiVisible;

    int questionTimePointer = 0;

    bool lastQuestionTriggered = false;

    void Awake()
    {
        GM.Instance.stateLoops = this;
    }

    void Start()
    {
        GM.Instance.audio.stepsActive = false;
        musicMix.audioMixer.SetFloat("MusicVolume", -80F);
        helperMenuIndicator.alpha = 0F;

        if (GM.Instance.game.debug == HLP.DebugMode.DEBUG)
        {
            Debug.LogWarning("DEBUG MODE - Bypassing regular game flow");
            UIManager.HideUiElement("Intro", "MainMenu");
            musicMix.audioMixer.SetFloat("MusicVolume", 0F);
            GM.Instance.audio.stepsActive = true;

            GM.Instance.game.InitPlayCondition();
        }

        LOGGER.Instance.Init();
    }



    void Update()
    {

        uiVisible = UIManager.GetVisibleUIElements().Count > 0;
        if (uiVisible && helperMenuIndicator.alpha == 1F) DisplayHelperMenuIndicator(false);
        else if (!uiVisible && helperMenuIndicator.alpha == 0F && permitHelperMenu) DisplayHelperMenuIndicator(true);

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            if (permitHelperMenu && GM.Instance.player.tpCtrl.isGrounded) ToggleHelperMenu();
        }

        pathDistance = Vector3.Distance(
                 pathCurve.CalcPositionByClosestPoint(GM.Instance.player.playerObj.position),
                 GM.Instance.player.playerObj.position
             );


        if (
                GM.Instance.player.tpCtrl.isGrounded && // grounded?
                !uiVisible && !reachedEnd && !routineActive && // no other UI, end not triggered, end not over
                questionTimer / QTIMERBASE > QINTERVALS[questionTimePointer] && // question is over the last counter
                !lastQuestionTriggered // last question not yet triggered
            )
        {

            TriggerQuestion();
        }

    }

    void TriggerQuestion()
    {
        if (questionTimePointer + 1 < QINTERVALS.Length)
        {
            questionTimePointer += 1;
        }
        else
        {
            lastQuestionTriggered = true;
        }
        timerPaused = true;

        // Close Help menu if open
        if (showingHelperMenu) ToggleHelperMenu();

        LockCharacter(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        GM.Instance.game.StopPositionLogging();
        UIManager.ShowUiElementAndHideAllTheOthers("QuestionScreen", "Utility", false);

    }

    public void QuestionScreenDismissed()
    {
        LockCharacter(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        UIManager.HideUiElement("QuestionScreen", "Utility");
        GM.Instance.game.StartPositionLogging();

        timerPaused = false;
    }


    public void InitLogger()
    {
        LOGGER.Instance.InitSession();
    }

    public bool LoggerConnected()
    {
        return LOGGER.Instance.loggerReady;
    }

    public int GetUserId()
    {
        return LOGGER.Instance.userId;
    }

    public void Init()
    {
        GM.Instance.game.InitPlayCondition();
        GM.Instance.fpsChecker.InitChecker();

        // Fade in music
        musicMix.audioMixer.DOSetFloat("MusicVolume", 0F, 1.5F);
        GM.Instance.audio.introMusic.Play();
        GM.Instance.audio.stepsActive = true;
        LockCharacter(true);

        sensei = GameObject.FindGameObjectWithTag("Sensei").GetComponent<CharacterHandler>();
        sensei.StartMeditation();

        foreach (GameObject sign in GameObject.FindGameObjectsWithTag("DirectionSign"))
        {
            sign.GetComponent<DirectionSign>().SetDirection(GM.Instance.game.playDirection);
            sign.SetActive(GM.Instance.game.contextActive);
        }

    }

    #region General Functionality


    public void LockCharacter(bool input)
    {
        GM.Instance.player.LockCamera(input);
        GM.Instance.player.LockMovement(input);
    }

    public void DisplayHelperMenuIndicator(bool input)
    {
        if (input) helperMenuIndicator.DOFade(1F, 0.3F);
        else helperMenuIndicator.DOFade(0F, 0.3F);
    }

    public void ToggleHelperMenu()
    {
        Debug.Log("Toggling Helper Menu");

        if (uiVisible && !showingHelperMenu) return;

        showingHelperMenu = !showingHelperMenu;
        if (showingHelperMenu) UIManager.ShowUiElementAndHideAllTheOthers("HelpScreen", "Utility", false);
        else UIManager.HideUiElement("HelpScreen", "Utility");

        GM.Instance.player.LockCamera(false);
        GM.Instance.player.LockMovement(showingHelperMenu);

        Cursor.visible = showingHelperMenu;
        Cursor.lockState = showingHelperMenu ? CursorLockMode.Confined : CursorLockMode.Locked;

    }

    #endregion

    public void StartPathPreview(Transform input)
    {
        GM.Instance.player.tpCam.enabled = false;
        StartCoroutine(PathPreviewRoutine(input));
    }

    IEnumerator PathPreviewRoutine(Transform input)
    {
        float _fadeTime = 0.6F;
        float _prevTime = 4.6F;

        if (GM.Instance.game.debug == HLP.DebugMode.FASTFORWARD)
        {
            _fadeTime = 0.1F;
            _prevTime = 0.5F;
        }

        routineActive = true;
        yield return new WaitForSeconds(_fadeTime);
        HLP.FadeOut();
        yield return new WaitForSeconds(_fadeTime);

        for (int i = 0; i < input.childCount; i++)
        {
            Camera.main.transform.parent = input.Find("View_" + i.ToString());
            Camera.main.transform.parent.DOLocalRotate(new Vector3(0F, 2F, 0F), 5F, RotateMode.LocalAxisAdd).SetEase(Ease.Linear);
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
            HLP.FadeIn();
            yield return new WaitForSeconds(_prevTime);
            HLP.FadeOut();
            yield return new WaitForSeconds(_fadeTime);
        }

        Camera.main.transform.parent = null;
        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;

        GM.Instance.player.tpCam.enabled = true;

        HLP.FadeIn();
        yield return new WaitForSeconds(_fadeTime);
        routineActive = false;
    }

    #region Look Tutorial

    public void InvertCamMenu_Show()
    {
        GM.Instance.player.LockCamera(false);
        GM.Instance.player.LockMovement(true);

        UIManager.ShowUiElementAndHideAllTheOthers("InvertCam", "Utility", false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

    }

    public void InvertCamConfirmMenu_Show()
    {
        UIManager.ShowUiElementAndHideAllTheOthers("InvertCam_Confirm", "Utility", false);
    }


    public void LookTutorial_Enter()
    {
        GM.Instance.player.LockCamera(false);
        lookSum = 0F;

        // HACK: can't seem to add actions on the scene manager, add code here to do that
        informationPanel.alpha = 1F;
        idText.text = $"Player ID: {LOGGER.Instance.userName}";
    }

    public void LookTutorial_Update()
    {
        lookSum += Mathf.Abs(GM.Instance.player.GetCamRotation().eulerAngles.y - prevRot);
        prevRot = GM.Instance.player.GetCamRotation().eulerAngles.y;
    }

    public void LookTutorial_Exit()
    {
        GM.Instance.player.LockCamera(true);
    }

    #endregion

    public void Gameflow_Update()
    {
        if (!timerPaused) pretimer += Time.deltaTime;
        if (!timerPaused) questionTimer += Time.deltaTime;

        // If pretimer over 10 minutes - reduce rest time by half
        if (pretimer >= 60F * 10F)
        {
            timer = 60F * 2.5F;
            pretimer = 0F;
        }

        UpdateTimeReply();
    }

    public void ReachedMasterMessage()
    {
        LOGGER.Instance.AddToTimeseries("REACHED_MASTER", GM.Instance.player.playerObj.position.ToString("F3"));
    }

    public void TimedGameflow_Update()
    {
        if (!timerPaused) timer += Time.deltaTime;
        if (!timerPaused) questionTimer += Time.deltaTime;

        UpdateTimeReply();

        if (timer >= 60F * 5F)
        {
            if (!meditationOver) sensei.EndMeditation();
            meditationOver = true;
        }
    }

    void UpdateTimeReply()
    {
        if (timer < 60F * 1F) timeReply = "5 minutes or so";
        else if (timer < 60F * 2F) timeReply = "3-4 minutes or so";
        else if (timer < 60F * 4F) timeReply = "2-3 minutes or so";
        else timeReply = "in a minute or so";
    }

    public void StartEndShot()
    {
        GM.Instance.player.tpCam.enabled = false;
        GM.Instance.fpsChecker.StopChecker();

        StartCoroutine(EndShotRoutine());

    }


    IEnumerator EndShotRoutine()
    {
        routineActive = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        HLP.FadeOut();
        yield return new WaitForSeconds(0.6F);

        sensei.transform.localRotation = Quaternion.identity;

        GM.Instance.player.playerObj.transform.parent = playerMeditationPoint;
        GM.Instance.player.playerObj.transform.localRotation = Quaternion.identity;
        GM.Instance.player.playerObj.transform.localPosition = Vector3.zero;
        GM.Instance.game.ResetPrevPosRot();

        sensei.StartMeditation();
        GM.Instance.player.playerObj.GetComponent<CharacterHandler>().StartMeditation();

        yield return new WaitForSeconds(1F);

        musicMix.audioMixer.DOSetFloat("MusicVolume", 2F, 2F);
        HLP.FadeIn();

        Camera.main.transform.parent = endShotCamPoint;
        Camera.main.transform.localPosition = Vector3.zero;
        Camera.main.transform.localRotation = Quaternion.identity;

        endShotCamPoint.parent.DOLocalRotate(new Vector3(0F, 50F, 0F), 22F, RotateMode.LocalAxisAdd).SetEase(Ease.Linear);
        yield return new WaitForSeconds(5F);
        GM.Instance.audio.chirpSource.Play();

        yield return new WaitForSeconds(1F);
        GM.Instance.player.playerObj.GetComponent<CharacterHandler>().GlanceEyes();
        yield return new WaitForSeconds(1F);
        GM.Instance.player.playerObj.GetComponent<CharacterHandler>().CloseEyes();
        yield return new WaitForSeconds(7F);
        HLP.FadeOut();
        yield return new WaitForSeconds(0.6F);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;



        string surveyId = LOGGER.Instance.userName + "U" + LOGGER.Instance.userId.ToString();
        idOutput.text = "Your unique identifier: <b>" + surveyId + "</b>";


#if UNITY_WEBGL
        webRoot.SetActive(true);
        labRoot.SetActive(false);
#else
        webRoot.SetActive(false);
        labRoot.SetActive(true);
#endif

        routineActive = false;
        reachedEnd = true;

        LOGGER.Instance.EndSession();
        musicMix.audioMixer.DOSetFloat("MusicVolume", -80F, 1.5F);

        yield return new WaitForSeconds(2F);

#if UNITY_WEBGL
        GM.Instance.game.OpenSurvey();
#endif

    }


    public void PauseAllGameplay()
    {
        Debug.LogWarning("Paused all Gameplay");

        HLP.FadeOut();
        musicMix.audioMixer.DOSetFloat("MusicVolume", -80F, 1.5F);
        timerPaused = true;

        // Close Help menu if open
        if (showingHelperMenu) ToggleHelperMenu();

        LockCharacter(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        GM.Instance.game.StopPositionLogging();

    }

    public void ConnectionFailed()
    {
        connectionFeedback.text = "Failed to load. Refresh browser!";
    }

}
