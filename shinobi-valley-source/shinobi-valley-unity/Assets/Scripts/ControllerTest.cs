using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

using DoozyUI;
using Invector.CharacterController;
// using MonsterLove.StateMachine;

public class ControllerTest : MonoBehaviour
{

    public enum States
    {
        Init,
        CheckLook,
        CanLook,
        CheckWalk,
        CanWalk,
        Play
    }


    public GameObject dialoguePrefab;

    float prevRot = 0f;
    float lookSum = 0f;
    // StateMachine<States> fsm;


    void Start()
    {


        // fsm = StateMachine<States>.Initialize(this);
        // fsm.ChangeState(States.Init);
    }


    public void TriggerState(string input)
    {
        States output;
        if (System.Enum.TryParse<States>(input, out output))
        {
            // fsm.ChangeState(output);
        }
        else
        {
            Debug.Log(input + " is not a State!");
        }
    }


    void Init_Enter()
    {
        GM.Instance.player.LockMovement(true);
        GM.Instance.player.LockCamera(true);
    }


    void CheckLook_Enter()
    {
        Say(new string[] {
            "This is you! You are a monkey ninja on a journey to your Master.",
            "Use your mouse to look around",
            "STATE:CanLook"
        });
    }

    void CanLook_Enter()
    {
        GM.Instance.player.LockMovement(true);
        GM.Instance.player.LockCamera(false);

        prevRot = Camera.main.transform.rotation.eulerAngles.y;
    }

    void CanLook_Update()
    {
        if (lookSum < 2200f)
        {
            lookSum += Mathf.Abs(Camera.main.transform.rotation.eulerAngles.y - prevRot);
            prevRot = Camera.main.transform.rotation.eulerAngles.y;
        }
        else
        {
            // fsm.ChangeState(States.CheckWalk);
        }
    }

    void CheckWalk_Enter()
    {
        GM.Instance.player.LockMovement(true);
        GM.Instance.player.LockCamera(true);


        Say(new string[] {
            "Great! Now let's try moving.",
            "Use the W, A, S, D keys to walk.",
            "You can hold SHIFT to run - and press SPACE to jump.",
            "STATE:CanWalk"
        });
    }

    void CanWalk_Enter()
    {
        GM.Instance.player.LockMovement(false);
        GM.Instance.player.LockCamera(false);
    }

    void Say(string input)
    {
        UIManager.ShowNotification(
            dialoguePrefab,
            -1,
            true,
            "Test Title",
            input,
            null,
            new string[] { "Continue" },
            new string[] { "Continue" }
            );
    }

    void Say(string[] input)
    {
        if (input.Length > 1)
        {
            string[] output = input.Skip(1).ToArray();
            UIManager.ShowNotification(
                dialoguePrefab,
                -1,
                true,
                "Test Title",
                input[0],
                null,
                new string[] { "Continue" },
                new string[] { "Continue" },
                new UnityAction[] { () => Say(output) }
                );
        }
        else
        {
            if (input[0].StartsWith("DO:"))
            {
                string parsed = input[0].Substring(3);
                Invoke(parsed, 0F);
            }
            else if (input[0].StartsWith("STATE:"))
            {
                string parsed = input[0].Substring(6);
                TriggerState(parsed);
            }
            else
            {
                Say(input[0]);
            }
        }
    }



}
