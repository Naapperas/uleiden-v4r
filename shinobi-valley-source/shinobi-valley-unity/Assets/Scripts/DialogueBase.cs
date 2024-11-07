using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NodeCanvas.DialogueTrees;
using TMPro;
using DoozyUI;

public class DialogueBase : MonoBehaviour
{

    [System.Serializable]
    public class SubtitleDelays
    {
        public float characterDelay = 0.05f;
        public float sentenceDelay = 0.5f;
        public float commaDelay = 0.1f;
        public float finalDelay = 1.2f;
    }

    //Options...
    [Header("Input Options")]
    public bool skipOnInput;
    public bool waitForInput;

    //Group...
    [Header("Subtitles")]
    public RectTransform subtitlesGroup;
    public TextMeshProUGUI actorSpeech;
    public TextMeshProUGUI actorName;
    public RectTransform waitInputIndicator;
    public SubtitleDelays subtitleDelays = new SubtitleDelays();
    public List<AudioClip> typingSounds;
    public AudioClip confirmSound;

    private GameObject actorNameParent;
    private UIElement localUI;

    //Group...
    [Header("Multiple Choice")]
    public RectTransform optionsGroup;
    public Button optionButton;
    private Dictionary<Button, int> cachedButtons;
    private Vector2 originalSubsPosition;
    private bool isWaitingChoice;

    private AudioSource _localSource;
    private AudioSource localSource
    {
        get { return _localSource != null ? _localSource : _localSource = gameObject.AddComponent<AudioSource>(); }
    }

    void OnEnable()
    {
        DialogueTree.OnDialogueStarted += OnDialogueStarted;
        DialogueTree.OnDialoguePaused += OnDialoguePaused;
        DialogueTree.OnDialogueFinished += OnDialogueFinished;
        DialogueTree.OnSubtitlesRequest += OnSubtitlesRequest;
        DialogueTree.OnMultipleChoiceRequest += OnMultipleChoiceRequest;
    }

    void OnDisable()
    {
        DialogueTree.OnDialogueStarted -= OnDialogueStarted;
        DialogueTree.OnDialoguePaused -= OnDialoguePaused;
        DialogueTree.OnDialogueFinished -= OnDialogueFinished;
        DialogueTree.OnSubtitlesRequest -= OnSubtitlesRequest;
        DialogueTree.OnMultipleChoiceRequest -= OnMultipleChoiceRequest;
    }

    void Start()
    {
        // subtitlesGroup.gameObject.SetActive(false);
        localUI = GetComponent<UIElement>();

        optionsGroup.gameObject.SetActive(false);
        optionButton.gameObject.SetActive(false);
        waitInputIndicator.gameObject.SetActive(false);
        originalSubsPosition = subtitlesGroup.transform.position;

        actorNameParent = actorName.transform.parent.gameObject;
    }

    void OnDialogueStarted(DialogueTree dlg)
    {
        //nothing special...
    }

    void OnDialoguePaused(DialogueTree dlg)
    {
        subtitlesGroup.gameObject.SetActive(false);
        optionsGroup.gameObject.SetActive(false);
    }

    void OnDialogueFinished(DialogueTree dlg)
    {
        Debug.Log("FINISHED");
        // subtitlesGroup.gameObject.SetActive(false);
        optionsGroup.gameObject.SetActive(false);
        if (cachedButtons != null)
        {
            foreach (var tempBtn in cachedButtons.Keys)
            {
                if (tempBtn != null)
                {
                    Destroy(tempBtn.gameObject);
                }
            }
            cachedButtons = null;
        }
    }


    void OnSubtitlesRequest(SubtitlesRequestInfo info)
    {
        StartCoroutine(Internal_OnSubtitlesRequestInfo(info));
    }

    IEnumerator Internal_OnSubtitlesRequestInfo(SubtitlesRequestInfo info)
    {

        waitInputIndicator.gameObject.SetActive(false);

        var text = info.statement.text;
        var audio = info.statement.audio;
        var actor = info.actor;

        CancelInvoke("DelayedEnd");

        bool initialActivation = false;
        if (!localUI.isVisible)
        {
            localUI.Show(false);
            initialActivation = true;
        }

        actorSpeech.text = text;

        actorName.text = info.statement.meta;
        actorSpeech.color = actor.dialogueColor;


        actorNameParent.SetActive(actorName.text.Replace(" ", "") != "");

        if (initialActivation)
        {
            yield return new WaitForSecondsRealtime(0.5F);
        }

        if (audio != null)
        {
            var actorSource = actor.transform != null ? actor.transform.GetComponent<AudioSource>() : null;
            var playSource = actorSource != null ? actorSource : localSource;
            playSource.clip = audio;
            playSource.Play();
            actorSpeech.text = text;
            var timer = 0f;
            while (timer < audio.length)
            {
                if (skipOnInput && Input.GetMouseButtonDown(0))
                {
                    playSource.Stop();
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }
        }

        if (audio == null)
        {
            var tempText = "";
            var inputDown = false;
            if (skipOnInput)
            {
                StartCoroutine(CheckInput(() => { inputDown = true; }));
            }

            // Quick hack to make character appear immediately
            actorSpeech.text = text;

            if (confirmSound != null)
            {
                localSource.PlayOneShot(confirmSound, 0.6f);
            }

            yield return new WaitForSeconds(0.3F);

            // yield return null;

            // for (int i = 0; i < text.Length; i++)
            // {

            //     if (skipOnInput && inputDown)
            //     {
            //         actorSpeech.text = text;
            //         yield return null;
            //         break;
            //     }

            //     if (subtitlesGroup.gameObject.activeSelf == false)
            //     {
            //         yield break;
            //     }

            //     char c = text[i];

            //     tempText += c;

            //     yield return StartCoroutine(DelayPrint(subtitleDelays.characterDelay));
            //     if (i % 3 == 0) PlayTypeSound();
            //     if (c == '.' || c == '!' || c == '?')
            //     {
            //         yield return StartCoroutine(DelayPrint(subtitleDelays.sentenceDelay));
            //         PlayTypeSound();
            //     }
            //     if (c == ',')
            //     {
            //         yield return StartCoroutine(DelayPrint(subtitleDelays.commaDelay));
            //         PlayTypeSound();
            //     }

            //     actorSpeech.text = tempText;
            // }

            if (!waitForInput)
            {
                yield return StartCoroutine(DelayPrint(subtitleDelays.finalDelay));
            }
        }

        if (waitForInput)
        {
            waitInputIndicator.gameObject.SetActive(true);
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }
            // waitInputIndicator.gameObject.SetActive(false);
        }



        yield return null;
        // subtitlesGroup.gameObject.SetActive(false);

        info.Continue();
        Invoke("DelayedEnd", 0.1F);


    }

    void DelayedEnd()
    {
        localUI.Hide(false);
    }

    void PlayTypeSound()
    {
        if (typingSounds.Count > 0)
        {
            var sound = typingSounds[Random.Range(0, typingSounds.Count)];
            if (sound != null)
            {
                localSource.PlayOneShot(sound, Random.Range(0.6f, 1f));
            }
        }
    }

    IEnumerator CheckInput(System.Action Do)
    {
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
        Do();
    }

    IEnumerator DelayPrint(float time)
    {
        var timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }




    void OnMultipleChoiceRequest(MultipleChoiceRequestInfo info)
    {

        optionsGroup.gameObject.SetActive(true);
        var buttonHeight = optionButton.GetComponent<RectTransform>().rect.height;
        optionsGroup.sizeDelta = new Vector2(optionsGroup.sizeDelta.x, (info.options.Values.Count * buttonHeight) + 20);

        cachedButtons = new Dictionary<Button, int>();
        int i = 0;

        foreach (KeyValuePair<IStatement, int> pair in info.options)
        {
            var btn = (Button)Instantiate(optionButton);
            btn.gameObject.SetActive(true);
            btn.transform.SetParent(optionsGroup.transform, false);
            btn.transform.localPosition = (Vector2)optionButton.transform.localPosition - new Vector2(0, buttonHeight * i);
            btn.GetComponentInChildren<Text>().text = pair.Key.text;
            cachedButtons.Add(btn, pair.Value);
            btn.onClick.AddListener(() => { Finalize(info, cachedButtons[btn]); });
            i++;
        }

        if (info.showLastStatement)
        {
            subtitlesGroup.gameObject.SetActive(true);
            var newY = optionsGroup.position.y + optionsGroup.sizeDelta.y + 1;
            subtitlesGroup.position = new Vector2(subtitlesGroup.position.x, newY);
        }

        if (info.availableTime > 0)
        {
            StartCoroutine(CountDown(info));
        }
    }

    IEnumerator CountDown(MultipleChoiceRequestInfo info)
    {
        isWaitingChoice = true;
        var timer = 0f;
        while (timer < info.availableTime)
        {
            if (isWaitingChoice == false)
            {
                yield break;
            }
            timer += Time.deltaTime;
            SetMassAlpha(optionsGroup, Mathf.Lerp(1, 0, timer / info.availableTime));
            yield return null;
        }

        if (isWaitingChoice)
        {
            Finalize(info, info.options.Values.Last());
        }
    }

    void Finalize(MultipleChoiceRequestInfo info, int index)
    {
        isWaitingChoice = false;
        SetMassAlpha(optionsGroup, 1f);
        optionsGroup.gameObject.SetActive(false);
        if (info.showLastStatement)
        {
            subtitlesGroup.gameObject.SetActive(false);
            subtitlesGroup.transform.position = originalSubsPosition;
        }
        foreach (var tempBtn in cachedButtons.Keys)
        {
            Destroy(tempBtn.gameObject);
        }
        info.SelectOption(index);
    }

    void SetMassAlpha(RectTransform root, float alpha)
    {
        foreach (var graphic in root.GetComponentsInChildren<CanvasRenderer>())
        {
            graphic.SetAlpha(alpha);
        }
    }
}
