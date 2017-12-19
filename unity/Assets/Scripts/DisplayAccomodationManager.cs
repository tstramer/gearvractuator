using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VRStandardAssets.Utils;
using SMI;

/**
 * Interface for accomodating to a specified virtual distance in a scene
 * by driving the display to a corresponding lense-display distance.
 * 
 * The mapping from virtual distance to lense-display distance is pre-computed 
 * and stored in {CalibrationData}.
 * 
 * Updates are throttled temporally by {minTimeBetweenUpdatesSecs}
 * and physically by {minDistanceBetweenUpdatesPercentDiopters}.
 * 
 * Temporal smoothing of display movement may be applied to avoid
 * sudden large movement of the display. See {AccommodateVirtualDistance} for details.
 */
public class DisplayAccomodationManager : MonoBehaviour {

	/**
	 * Called when a connection has been made successfully with the motor controller
	 */
	public Action OnDisplayReady;

	/**
	 * Called when the display is driven to a new location. The parameter
	 * is the new virtual distance accomodated to.
	 */
	public Action<float> OnDisplayAccomodationUpdate;

	/**
	 * Minimum time delta between consecutive updates to move display (in seconds)
	 */
	[SerializeField] private float minTimeBetweenUpdatesSecs = .125f; 

	/** 
	 * Minimum virtual distance delta between consecutive updates to move display (in percent of diopters)
	 */
	[SerializeField] private const float minDistanceBetweenUpdatesPercentDiopters = 5f; 

	[SerializeField] private MotorController motorController; 

	private float lastDistanceMeters = -1.0f;
	private float currentDistanceMeters = -1.0f;
	private float currentPerceptualSmoothing = -1.0f;
	private float updateDisplayTimeout = -1.0f;

	private NamedLogger logger = new NamedLogger("DisplayAccomodationManager");

	/**
	 * Sets the current virtual distance to accomodate to in meters.
	 * 
	 * Updates are throttled temporally by {minTimeBetweenUpdatesSecs}
	 * and physically by {minDistanceBetweenUpdatesPercentDiopters}.
	 * 
	 * The {perceptualSmoothingAlpha} param can be set between 0 and 1 and enables
	 * driving the display to the target virtual distance over multiple updates
	 * spaced out by {minTimeBetweenUpdatesSecs} according to the following formula:
	 *	 currentVirtualDistanceDiopters = alpha * targetVirtualDistanceDiopters + (1 - alpha) * currentVirtualDistanceDiopters
	 * This continues until convergence and can prevent sudden large movements of the display.
	 * To disable perceptual smoothing, set perceptualSmoothingAlpha=-1.
	 */
	public void AccommodateVirtualDistance(float virtualDistanceMeters, float perceptualSmoothingAlpha = -1) {
		if (virtualDistanceMeters != currentDistanceMeters) {
			logger.DebugLog ("Setting virtual distance=" + virtualDistanceMeters + " meters, perceptual smoothing alpha=" + perceptualSmoothingAlpha + "");
		}
		currentDistanceMeters = virtualDistanceMeters;
		currentPerceptualSmoothing = perceptualSmoothingAlpha;
	}

	/**
	 * Resets the display to the smallest lense-display distance
	 */
	public void Reset() {
		motorController.ResetStepper ();
		currentDistanceMeters = -1.0f;
		lastDistanceMeters = -1.0f;
		currentPerceptualSmoothing = -1.0f;
		updateDisplayTimeout = minTimeBetweenUpdatesSecs;
	}

	private void Start() {
		lastDistanceMeters = -1.0f;
		currentDistanceMeters = -1.0f;
		updateDisplayTimeout = -1.0f;
		currentPerceptualSmoothing = -1.0f;
	}

	private void OnEnable() {
		motorController.OnConnect += HandleConnectToMotorController;
	}

	private void OnDisable() {
		motorController.OnConnect -= HandleConnectToMotorController;
	}

	private void HandleConnectToMotorController() {
		Reset();
		if (OnDisplayReady != null) {
			OnDisplayReady ();
		}
	}

	private void Update() {
		if (updateDisplayTimeout > 0) {
			updateDisplayTimeout -= Time.deltaTime;
			if (updateDisplayTimeout < 0) {
				AccomodateToCurrentVirtualDistance ();
				updateDisplayTimeout = minTimeBetweenUpdatesSecs;
			}
		}
	}
		
	private void AccomodateToCurrentVirtualDistance() {
		if (!motorController.Connected || currentDistanceMeters <= 0) {
			return;
		}

		if (lastDistanceMeters == -1 || AboveDistanceThreshold (currentDistanceMeters, lastDistanceMeters)) {
			float distance = currentDistanceMeters;
			if (currentPerceptualSmoothing != -1) {
				distance = GetSmoothedDistanceMeters(currentDistanceMeters, lastDistanceMeters);
			}
			SetMotorStepsForVirtualDistance (distance);
			if (OnDisplayAccomodationUpdate != null) {
				OnDisplayAccomodationUpdate(distance);
			}
		}
	}
		
	private float GetSmoothedDistanceMeters(float distanceMeters, float lastDistanceMeters) {
		if (distanceMeters <= 0 || lastDistanceMeters <= 0) {
			return distanceMeters;
		} else {
			float distDiopters = 1 / distanceMeters;
			float lastDistDiopters = 1 / lastDistanceMeters;
			float smoothedDistDiopter = currentPerceptualSmoothing * distDiopters + (1 - currentPerceptualSmoothing) * lastDistDiopters;
			return 1 / smoothedDistDiopter;
		}
	}

	private bool AboveDistanceThreshold(float newDistanceMeters, float oldDistanceMeters) {
		if (newDistanceMeters <= 0 || oldDistanceMeters <= 0) {
			return true;
		} else {
			float newDistanceDiopters = 1 / newDistanceMeters;
			float oldDistanceDiopters = 1 / oldDistanceMeters;
			float percentChange = 100 * (Math.Abs (newDistanceDiopters - oldDistanceDiopters) / oldDistanceDiopters);
			return percentChange >= minDistanceBetweenUpdatesPercentDiopters;
		}
	}

	private void SetMotorStepsForVirtualDistance(float virtualDistance) {
		short motorSteps = (short)TableLookup.lookup (virtualDistance, CalibrationData.distances, CalibrationData.steps);
		if (motorSteps < 0) { motorSteps = 0;}
		logger.DebugLog ("Stepping motor to " + motorSteps + " for virtual distance " + virtualDistance + " meters");
		motorController.SetMotorSteps (motorSteps, () => {
			lastDistanceMeters = virtualDistance;
		});
	}
}