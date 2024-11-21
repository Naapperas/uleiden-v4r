using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class QuestionScreen : MonoBehaviour
{
    public GameObject sliderHandle;
    public Button submitButton;

    public Scrollbar scrollBar;

    public GameObject panel;

    bool screenFinished;

    private void OnEnable()
    {
        sliderHandle.SetActive(false);
        submitButton.interactable = false;
        screenFinished = false;
        scrollBar.value = 0.5F;

        panel.SetActive(true);
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

        if (sliderHandle.activeSelf) submitButton.interactable = true;
    }

    public void Continue()
    {
        if (screenFinished) return;

        // If slider not active, then do not permit continue
        if (!sliderHandle.activeSelf) return;

        screenFinished = true;
        string output = scrollBar.value.ToString("F2");
        Debug.Log(output);
        LOGGER.Instance.AddToTimeseries("FEEDBACK", output);
        GM.Instance.stateLoops.QuestionScreenDismissed();
    }
}
