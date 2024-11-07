using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Events;

public class TriggerSender : MonoBehaviour
{
    public enum Type { ROI, GOAL, STARTZONE }
    public enum Event { ENTER, STAY, EXIT }

    public Type type;
    public bool triggerEnter, triggerStay, triggerExit;
    public string description;

    public bool onlyFirstTrigger;
    public bool onlyFirstEvent;
    bool hasTriggeredEnter;
    bool hasTriggeredStay;
    bool hasTriggeredExit;

    bool hasTriggeredEnterEvent;

    public UnityEvent enterEvent;

    Info triggerInfo;
    float lastTime;

    public struct Info
    {
        public Event eve;
        public Type type;
        public string description;
    }

    void Start()
    {
        triggerInfo = new Info();
        triggerInfo.type = this.type;
        triggerInfo.description = this.description;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!GM.Instance.game.playConditionSet) return;
        if (!triggerEnter) return;
        if (onlyFirstTrigger && hasTriggeredEnter) return;

        if (other.CompareTag("Player"))
        {
            triggerInfo.eve = Event.ENTER;
            GM.Instance.game.LocationTrigger(triggerInfo);

            if (!hasTriggeredEnterEvent)
            {
                enterEvent.Invoke();
                if (onlyFirstEvent) hasTriggeredEnterEvent = true;
            }

            hasTriggeredEnter = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!GM.Instance.game.playConditionSet) return;
        if (!triggerStay) return;

        if (other.CompareTag("Player"))
        {
            // Log stay every second
            if (lastTime < Time.time - 1F)
            {
                triggerInfo.eve = Event.STAY;
                GM.Instance.game.LocationTrigger(triggerInfo);
                lastTime = Time.time;
                hasTriggeredStay = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!GM.Instance.game.playConditionSet) return;
        if (!triggerExit) return;
        if (onlyFirstTrigger && hasTriggeredExit) return;

        if (other.CompareTag("Player"))
        {
            triggerInfo.eve = Event.EXIT;
            GM.Instance.game.LocationTrigger(triggerInfo);
            hasTriggeredExit = true;
        }
    }
}
