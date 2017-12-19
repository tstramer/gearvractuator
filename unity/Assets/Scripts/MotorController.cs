using System;
using UnityEngine;
using UnityEngine.UI;
using VRStandardAssets.Utils;
using SMI;
using System.Collections;
using UnityEngine.SceneManagement;

/**
 * Interface for communicating with a motor controller over bluetooth.
 * Allows for setting the absolute number of steps the motor should be
 * rotated since last reset.
 */
public class MotorController : MonoBehaviour {

	private class MotorControlSignals {
		public static readonly short ResetStepper = -1; 
		public static readonly short DisengageStepper = -2; 
		public static readonly short EngageStepper = -3; 
	}

	/**
	 * Called when a connection has been made successfully with the motor controller
	 */
	public Action OnConnect;

	[SerializeField]
	private string deviceName = "raspberrypi"; // device name of the bluetooth peripheral
	[SerializeField]
	private string serviceUUID = "ec00"; // service uuid of the bluetooth peripheral
	[SerializeField]
	private string writeCharacteristicUUID = "ec0e";  // write characterstic uuid of the bluetooth peripheral
	[SerializeField]
	private string readCharacteristicUUID = "ec0e"; // read characterstic uuid of the bluetooth peripheral

	[SerializeField]
	private BluetoothPeripheralManager bluetoothPeripheralManager;

	private NamedLogger logger = new NamedLogger("MotorController");

	public void SetMotorSteps(short motorSteps, Action onSuccess) {
		bluetoothPeripheralManager.SendInt16LE (motorSteps, onSuccess);
	}

	public void ResetStepper() {
		bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.ResetStepper);
	}

	public void EngageStepper() {
		bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.EngageStepper);
	}

	public void DisengageStepper() {
		bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.DisengageStepper);
	}

	public bool Connected { 
		get { 
			return bluetoothPeripheralManager.Connected;
		} 
	}

	private void Start() {
		BluetoothPeripheralParams peripheral = new BluetoothPeripheralParams (
			deviceName, 
			serviceUUID, 
			writeCharacteristicUUID, 
			readCharacteristicUUID
		);
		bluetoothPeripheralManager.ConnectTo (peripheral);
	}

	private void OnEnable()
	{
		bluetoothPeripheralManager.OnConnect += HandleConnectToController;
		bluetoothPeripheralManager.BeforeDisconnect += HandleBeforeDisconnectFromController;
	}

	private void OnDisable()
	{
		bluetoothPeripheralManager.OnConnect -= HandleConnectToController;
		bluetoothPeripheralManager.BeforeDisconnect -= HandleBeforeDisconnectFromController;
	}

	private void OnApplicationPause( bool isPaused )
	{
		OnApplicationFocusChange (isPaused);
	}

	private void OnApplicationFocusChange(bool isPaused) {
		if (!Connected) {
			return;
		}

		if (isPaused) {
			logger.DebugLog ("Application paused. Disengaging motor.");
			bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.DisengageStepper);
			bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.DisengageStepper);
		} else {
			logger.DebugLog ("Application resumed. Re-engaging motor.");
			bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.EngageStepper);
		}
	}

	private void HandleConnectToController() {
		if (OnConnect != null) {
			OnConnect ();
		}
	}

	private void HandleBeforeDisconnectFromController() {
		bluetoothPeripheralManager.SendInt16LE (MotorControlSignals.DisengageStepper);
	}
}