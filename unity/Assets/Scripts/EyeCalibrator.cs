using System;
using UnityEngine;
using System.Collections;

/**
 * 
 */
public class EyeCalibrator : MonoBehaviour {

	public Action OnCalibrationComplete;

	[SerializeField]
	private float[] calibrationDistancesMeters = { 1.5f, .35f, .25f };

	[SerializeField]
	private EyeCalibrationUpdater eyeCalibrationUpdater;

	[SerializeField]
	private DisplayAccomodationManager displayAccomodationManager;

	private int currentCalibrationIdx = 0;
	private bool readyToCalibrateNext;
	private bool doneCalibrating;

	private NamedLogger logger = new NamedLogger ("EyeCalibrator");

	private void Start() {
		readyToCalibrateNext = false;
		doneCalibrating = false;
		currentCalibrationIdx = 0;
		SMI.SMIEyeTrackingMobile.Instance.SetCalibrationReturnCallback(OnCalibrationResult);
	}

	public void StartCalibration() {
		readyToCalibrateNext = true;
	}

	private void Update() {
		if (readyToCalibrateNext) {
			readyToCalibrateNext = false;
			StartCoroutine (CalibrateCurrentWithDelay ());
		} else if (doneCalibrating) {
			doneCalibrating = false;
			if (OnCalibrationComplete != null) {
				OnCalibrationComplete ();
			}
		}
	}

	public IEnumerator CalibrateCurrentWithDelay() {
		yield return new WaitForSeconds (.5f);
		CalibrateCurrent ();
	}

	public void CalibrateCurrent() {
		logger.DebugLog ("Starting calibration for distance (meters): " + calibrationDistancesMeters [currentCalibrationIdx]);
		displayAccomodationManager.AccommodateVirtualDistance (calibrationDistancesMeters [currentCalibrationIdx]);

		#if UNITY_EDITOR
			OnCalibrationResult(0);
		#else
			SMI.SMIEyeTrackingMobile.Instance.smi_StartFivePointCalibration ();
		#endif
	}

	private void OnCalibrationResult(int returnCode) {
		float virtualDistance = calibrationDistancesMeters [currentCalibrationIdx];
		string calibrationName = GetCalibrationName (currentCalibrationIdx);

		logger.DebugLog("Calibration result for distance " + virtualDistance + ": " + SMI.SMIEyeTrackingMobile.ErrorIDContainer.getErrorMessage(returnCode));
		logger.DebugLog ("Calibration saved as: " + calibrationName);

		#if !UNITY_EDITOR
			SMI.SMIEyeTrackingMobile.Instance.smi_SaveCalibration(calibrationName);
		#endif

		eyeCalibrationUpdater.AddCalibration (virtualDistance, calibrationName);

		currentCalibrationIdx++;

		if (HasMoreToCalibrate ()) {
			readyToCalibrateNext = true;
		} else {
			doneCalibrating = true;
		}
	}


	public bool HasMoreToCalibrate() {
		return currentCalibrationIdx < calibrationDistancesMeters.Length;
	}

	private string GetCalibrationName(int idx) {
		return "calibration_" + idx;
	}
}