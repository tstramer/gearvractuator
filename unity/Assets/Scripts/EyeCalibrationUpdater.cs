using System;
using UnityEngine;
using System.Collections.Generic;

/**
 * Stores eye calibrations for different screen distances.
 * Calibrations are created in {EyeCalibrator}.
 * Subscribes for changes to the current screen distance and updates the 
 * calibration to be the one that minimizes the current screen distance
 * and calibration screen distance. 
 */
public class EyeCalibrationUpdater : MonoBehaviour {

	private class Calibration {
		public float distance;
		public string name;

		public Calibration(
			float distance,
			string name
		) {
			this.distance = distance;
			this.name = name;
		}
	}

	[SerializeField]
	private DisplayAccomodationManager accomodationManager;

	private List<Calibration> calibrations = new List<Calibration>();
	private Calibration currentCalibration = null;

	private NamedLogger logger = new NamedLogger("EyeCalibrationUpdater");

	public void OnEnable() {
		accomodationManager.OnDisplayAccomodationUpdate += SetEyeCalibration;
	}

	public void OnDisable() {
		accomodationManager.OnDisplayAccomodationUpdate -= SetEyeCalibration;
	}

	public void SetEyeCalibration(float virtualDistanceMeters) {
		if (calibrations.Count != 0) {
			Calibration closestCalibration = GetClosestCalibration (virtualDistanceMeters);
			if (currentCalibration == null || currentCalibration.name != closestCalibration.name) {
				currentCalibration = closestCalibration;
				logger.DebugLog ("Loading eye calibration: scene distance=" + virtualDistanceMeters + ", calibration distance=" + closestCalibration.distance);
				#if !UNITY_EDITOR
					SMI.SMIEyeTrackingMobile.Instance.smi_LoadCalibration (closestCalibration.name);
				#endif
			}
		}
	}

	public void AddCalibration(float virtualDistanceMeters, string calibrationName) {
		calibrations.Add (new Calibration (virtualDistanceMeters, calibrationName));
	}

	private Calibration GetClosestCalibration(float distance) {
		Calibration bestCalibration = null;
		foreach (Calibration calibration in calibrations) {
			if (bestCalibration == null) {
				bestCalibration = calibration;
			} else {
				float stepsApartCurrent = StepsApart (distance, calibration.distance);
				float stepsApartClosest = StepsApart (distance, bestCalibration.distance);
				if (stepsApartCurrent < stepsApartClosest) {
					bestCalibration = calibration;
				}
			}
		}
		return bestCalibration;
	}

	private float StepsApart(float distance1, float distance2) {
		float steps1 = TableLookup.lookup (distance1, CalibrationData.distances, CalibrationData.steps);
		float steps2 = TableLookup.lookup (distance2, CalibrationData.distances, CalibrationData.steps);
		return Math.Abs (steps1 - steps2);
	}
}