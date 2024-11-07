using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Networking;

public class GM : Singleton<GM> {
	protected GM () {} // singleton only - no constructor

	public GameCTRL game;
	public PlayerCTRL player;
	public AudioCTRL audio;
	public StateLoops stateLoops;
	public StyleCTRL style;
	public FrameRateChecker fpsChecker;

	public GoToTrigger eve_EnterLevel;
    public UnityEvent eve_answerLibraryChanged;
    public UnityEventString eve_subqChanged;

	void Awake()
	{
		// eve_EnterLevel = new GoToTrigger();
		// eve_answerLibraryChanged = new UnityEvent();
		// eve_subqChanged = new UnityEventString();
	}




}
