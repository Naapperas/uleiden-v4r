using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 using UnityEngine.SceneManagement;

public class FrameRateChecker : MonoBehaviour
{

    public int fpsThreshold = 15;
    public int conseqThreshold = 5;


    const float fpsMeasurePeriod = 1f;
    private int thresholdCount;
    private int m_FpsAccumulator = 0;
    private float m_FpsNextPeriod = 0;
    private int m_CurrentFps;
    private bool checkActive = false;

    WebCaller webcall;


    void Awake()
    {
        GM.Instance.fpsChecker = this;
    }

    void Start()
    {
        webcall = GetComponent<WebCaller>();
    }

    public void InitChecker()
    {
        m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        checkActive = true;
    }

    public void StopChecker()
    {
        checkActive = false;
    }

    void Update()
    {
        if (!checkActive) return;

        m_FpsAccumulator++;
        if (Time.realtimeSinceStartup > m_FpsNextPeriod)
        {
            m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
            m_FpsAccumulator = 0;
            m_FpsNextPeriod += fpsMeasurePeriod;

            if (m_CurrentFps < fpsThreshold)
            {
                thresholdCount += 1;
                LOGGER.Instance.AddToTimeseries("FPSWARNING", m_CurrentFps.ToString());
                Debug.Log("FPS too low!");
            }
            else
            {
                thresholdCount = 0;
            }

            if (thresholdCount >= conseqThreshold)
            {
                GM.Instance.stateLoops.PauseAllGameplay();
                LOGGER.Instance.AddToTimeseries("FPS_SHUTDOWN", m_CurrentFps.ToString());
#if UNITY_WEBGL && !UNITY_EDITOR
                webcall.FpsWarning();
#endif
                checkActive = false;
                Invoke("LoadEmptyScene", 1F);
            }

        }


    }

    void LoadEmptyScene()
    {
        SceneManager.LoadScene("Empty"); 
    }
}
