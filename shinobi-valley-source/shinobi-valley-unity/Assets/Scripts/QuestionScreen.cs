using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class QuestionScreen : MonoBehaviour
{
    public TMP_InputField questionInput;
    public GameObject sliderHandle;
    public Button submitButton;

    public Scrollbar scrollBar;

    public GameObject[] panels;

    int questionSlide;
    bool screenFinished;

    private void OnEnable()
    {
        questionInput.text = "";
        sliderHandle.SetActive(false);
        submitButton.interactable = false;
        screenFinished = false;
        scrollBar.value = 0.5F;
        questionSlide = 0;

        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(questionSlide == i);
        }
    }



    public void SliderClicked(BaseEventData eventData)
    {


        PointerEventData pointerData = eventData as PointerEventData;

        Vector2 result;
        Vector2 clickPosition = pointerData.position;
        RectTransform thisRect = scrollBar.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, clickPosition, null, out result);
        result += thisRect.sizeDelta / 2;

        float output = result.x / thisRect.rect.width;

        scrollBar.value = output;
        sliderHandle.SetActive(true);

    }

    void Update()
    {
        submitButton.interactable = false;

        if (screenFinished) return;

        switch (questionSlide)
        {
            case 0:
                if (questionInput.text != "") submitButton.interactable = true;
                break;
            case 1:
                if (sliderHandle.activeSelf) submitButton.interactable = true;
                break;
        }

    }

    public void Continue()
    {
        if (screenFinished) return;

        if (questionSlide == 0)
        {
            questionSlide = 1;
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(questionSlide == i);
            }
        }
        else
        {
            // If slider not active, then do not permit continue
            if (!sliderHandle.activeSelf) return;

            screenFinished = true;
            string output = questionInput.text + "_" + scrollBar.value.ToString("F2");
            Debug.Log(output);
            LOGGER.Instance.AddToTimeseries("FEEDBACK", output);
            GM.Instance.stateLoops.QuestionScreenDismissed();
        }
    }


}
