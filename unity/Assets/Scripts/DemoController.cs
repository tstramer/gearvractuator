using System;
using UnityEngine;
using VRStandardAssets.Utils;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

public class DemoController : MonoBehaviour
{
	private readonly float ConventionalFocusDistanceMeters = 1.5f;

	private enum State {
		Connecting=0,
		Intro=1,
		Calibration=2,
		DemoInstructions=3,
		Scenes=4,
		GazeInfo=5
	};

	private readonly string ConnectingText = "Connecting to motor controller...";

	private readonly string CalibrationText = "Calibration in progress...";

	private readonly string DemoInstructionsText = 
		"Great! To switch between scenes, press the joystick left or right. " + 
		"To toggle gaze contingent focus on and off, press the 'A' buton.";

	private readonly string IntroText = 
		"Welcome! This is a demo of gaze contingent focus. " +
		"To start, we need to calibrate the eye-tracker. " +
		"To do this, you will be shown 5 points one-by-one on screen. " +
		"Each calibration point will be accepted automatically when fixating on it for about 1 second.";

	[SerializeField]
	private GazeContingentFocus gazeContigentFocus;

	[SerializeField]
	private GameObject gazeCursor;

	[SerializeField]
	private GameObject controllerImage;

	[SerializeField]
	private EyeCalibrator eyeCalibrator;

	[SerializeField]
	private VRInput vrInput;

	[SerializeField]
	private GameObject[] scenes;

	[SerializeField]
	private DisplayAccomodationManager accomodationManager;

	[SerializeField]
	private Material defaultSkybox;

	[SerializeField]
	private InfoBox infoBox;

	private GameObject currentScene;
	private int currentSceneIdx;
	private State currentState;

	private NamedLogger logger = new NamedLogger("DemoController");

	private void Awake() {
		SMI.SMIEyeTrackingMobile.Instance.enableSMINotificationInVR = false;
	}

	private void Start() {
		currentSceneIdx = 0;
		currentScene = null;
		SetState (State.Connecting);
	}

	private void OnEnable() {
		vrInput.OnClick += HandleClick;
		vrInput.OnSwipe += HandleSwipe;
		accomodationManager.OnDisplayReady += HandleDisplayReady;
		eyeCalibrator.OnCalibrationComplete += HandleCalibrationComplete;
	}

	private void OnDisable() {
		vrInput.OnClick -= HandleClick;
		vrInput.OnSwipe -= HandleSwipe;
		accomodationManager.OnDisplayReady -= HandleDisplayReady;
		eyeCalibrator.OnCalibrationComplete -= HandleCalibrationComplete;
	}

	private void HandleClick() {
		if (currentState == State.Scenes) {
			SetGazeContingentFocusActive (!gazeContigentFocus.enabled);
			SetState (State.GazeInfo);
		}
	}

	private void HandleSwipe(VRInput.SwipeDirection swipeDirection) {
		if (currentState == State.Scenes) {
			switch (swipeDirection) {
			case VRInput.SwipeDirection.NONE:
				break;
			case VRInput.SwipeDirection.UP:
				break;
			case VRInput.SwipeDirection.DOWN:
				break;
			case VRInput.SwipeDirection.LEFT:
				currentSceneIdx = mod (currentSceneIdx - 1, scenes.Length);
				ShowSelectedScene ();
				break;
			case VRInput.SwipeDirection.RIGHT:
				currentSceneIdx = mod (currentSceneIdx + 1, scenes.Length);
				ShowSelectedScene ();
				break;
			}
		}
	}

	private void HandleDisplayReady() {
		StartCoroutine (AccomodateConventionalWithDelay ());
		SetState (State.Intro);
	}

	private IEnumerator AccomodateConventionalWithDelay() {
		yield return new WaitForSeconds (1f);
		accomodationManager.AccommodateVirtualDistance (ConventionalFocusDistanceMeters);
	}

	private void HandleCalibrationComplete() {
		SetState (State.DemoInstructions);
	}

	private int mod(int x, int m) {
		return (x%m + m)%m;
	}

	private void ShowSelectedScene() {
		SetDefaultSceneSettings ();
		DestroyCurrentScene ();
		currentScene = Instantiate(scenes[currentSceneIdx] as UnityEngine.Object, Vector3.zero, Quaternion.identity) as GameObject;
		currentScene.SetActive (true);
		logger.DebugLog ("Enabling scene " + currentScene.name);
	}

	private void SetDefaultSceneSettings() {
		RenderSettings.skybox = defaultSkybox;
		RenderSettings.fog = false;
	}

	private void DestroyCurrentScene() {
		if (currentScene != null) {
			Destroy (currentScene);
		}
	}

	private void SetGazeContingentFocusActive(bool active) {
		gazeContigentFocus.enabled = active;
		gazeCursor.SetActive (false);

		GameObject gazePoint = GameObject.Find ("SMI_GazePoint");
		if (gazePoint != null) gazePoint.SetActive (false);

		if (!active) {
			accomodationManager.AccommodateVirtualDistance (ConventionalFocusDistanceMeters);
		}
	}

	private void SetState(State state) {
		logger.DebugLog ("Setting state to " + state);
		switch (state) {
		case State.Connecting:
			DestroyCurrentScene();
			SetGazeContingentFocusActive (false);
			infoBox.ShowText (ConnectingText);
			break;
		case State.Intro:
			infoBox.ShowTextAndFadeOut(IntroText, 15f, () => {
				SetState(State.Calibration);
			});
			break;
		case State.Calibration:
			infoBox.ShowText (CalibrationText);
			eyeCalibrator.StartCalibration ();
			break;
		case State.DemoInstructions:
			accomodationManager.AccommodateVirtualDistance (ConventionalFocusDistanceMeters);
			infoBox.ShowTextAndFadeOut(DemoInstructionsText, 10f, () => {
				SetGazeContingentFocusActive (true);
				SetState(State.Scenes);
			});
			break;
		case State.Scenes:
			ShowSelectedScene ();
			break;
		case State.GazeInfo:
			DestroyCurrentScene();
			string infoBoxText = "Gaze contingent focus " + (gazeContigentFocus.enabled ? "on" : "off");
			infoBox.ShowTextAndFadeOut(infoBoxText, 1.25f, () => {
				SetState(State.Scenes);
			});
			logger.DebugLog (infoBoxText);
			break;
		}
		controllerImage.SetActive (state == State.DemoInstructions);
		currentState = state;
	}
}