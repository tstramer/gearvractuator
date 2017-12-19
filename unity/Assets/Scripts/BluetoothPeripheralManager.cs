using System;
using UnityEngine;
using System.Collections;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;

/**
 * Interface for finding, connecting to, writing to, and reading from a bluetooth
 * peripheral. Currently can only connect to one peripheral at a time.
 */
public class BluetoothPeripheralManager : MonoBehaviour {

	public static BluetoothPeripheralManager Instance;
			
	/**
	 * Called after connection is made with the peripheral
	 */
	public event Action OnConnect; 

	/**
	 * Called before connection is terminated with the peripheral
	 */
	public event Action BeforeDisconnect; 

	/**
	 * How long to wait before retrying connection to the peripheral
	 */
	[SerializeField] private float connectRetryTimeoutSecs = 10f; 

	private enum States
	{
		None,
		Scan,
		Connect,
		Disconnect,
	}

	private BluetoothPeripheralParams? _peripheralParams = null;

	private States _state = States.None;
	private bool _connected = false;
	private bool _foundWriteID = false;
	private bool _foundReadID = false;
	private float _timeout = 0f;
	private string _deviceAddress;

	private NamedLogger logger = new NamedLogger("BluetoothPeripheralManager");

	public bool Connected { 
		get { 
			return _connected;
		} 
	}

	public void ConnectTo (BluetoothPeripheralParams newPeripheralParams)
	{
		if (_peripheralParams.HasValue && newPeripheralParams.Equals(_peripheralParams.Value)) {
			logger.Warning ("Already connected to this peripheral: " + newPeripheralParams); 
			return; 
		}

		#if UNITY_EDITOR
			_state = States.None;
			_connected = true;
			if (OnConnect != null) {
				OnConnect();
			}
			return;
		#endif

		ResetState (() => {
			_peripheralParams = newPeripheralParams;
			BluetoothLEHardwareInterface.Initialize (true, false, () => {
				logger.DebugLog ("Initialized bluetooth hardware interface.");
				SetState (States.Scan, 0.1f);
			}, (error) => {
				logger.Error ("Error initializing bluetooth hardware interface: " + error);
			});
		});
	}

	public void SendText (string text, Action onSuccess = null) {
		logger.DebugLog ("Attempting to write text to periphal: " + text);
		SendBytes (Encoding.ASCII.GetBytes (text), onSuccess);
	}

	public void SendInt16LE(short num, Action onSuccess = null) { // little endian
		logger.DebugLog ("Attempting to write int16 to peripheral: " + num);
		byte[] bytes = DataConversions.ToBytesLittleEndian (num);
		SendBytes (bytes, onSuccess);
	}

	private void Awake () {
		if (Instance == null) {
			Instance = this;
			DontDestroyOnLoad (gameObject);
		} else {
			Destroy (gameObject);
		}
	}

	private void Update ()
	{
		if (_timeout > 0f)
		{
			_timeout -= Time.deltaTime;
			if (_timeout <= 0f)
			{
				_timeout = 0f;

				switch (_state)
				{
				case States.None:
					break;
				case States.Scan:
					ScanForPeripherals ();
					break;
				case States.Connect:
					ConnectToPeripheral ();
					break;
				case States.Disconnect:
					DisconnectFromPeripheral();
					break;
				}
			}
		}
	}

	private void OnDestroy()
	{
		logger.DebugLog("Destroying bluetooth peripheral manager");
		OnConnect = null;
		BeforeDisconnect = null;
	}

	private void OnApplicationQuit() {
		if (BeforeDisconnect != null) {
			BeforeDisconnect ();
		}
		DisconnectFromPeripheral ();
	}

	/**
	 * Scans for peripherals, stopping once the target peripheral is found
	 */ 
	private void ScanForPeripherals() {
		logger.DebugLog ("Scanning for peripherals...");

		string[] serviceUUIDs = new string[1];
		serviceUUIDs[0] = _peripheralParams.Value.serviceUUID;

		BluetoothLEHardwareInterface.ScanForPeripheralsWithServices (serviceUUIDs, (address, name) => {
			// if your device does not advertise the rssi and manufacturer specific data
			// then you must use this callback because the next callback only gets called
			// if you have manufacturer specific data

			logger.DebugLog("Found peripheral: address=[" + address + "], name=[" + name + "])");

			if (name.ToLower().Contains (_peripheralParams.Value.deviceName.ToLower())) {
				logger.DebugLog("Peripheral matches target device name");
				BluetoothLEHardwareInterface.StopScan ();
				// found a device with the name we want
				_deviceAddress = address;
				SetState (States.Connect, 0.5f);
			}

		}, (address, name, rssi, bytes) => {
			// use this one if the device responses with manufacturer specific data and the rssi

			logger.DebugLog("Found peripheral: address=[" + address + "], name=[" + name + "]) rssi=[" + rssi + "], bytes=[" + bytes + "]");

			if (name.ToLower().Contains (_peripheralParams.Value.deviceName.ToLower()))
			{
				logger.DebugLog("Peripheral matches target device name");
				BluetoothLEHardwareInterface.StopScan ();
				// found a device with the name we want
				_deviceAddress = address;
				SetState (States.Connect, .5f);
			}
		}, /*rssi=*/false); // this last setting allows RFduino to send RSSI without having manufacturer data
	}

	private void ConnectToPeripheral() {
		logger.DebugLog("Connecting to peripheral with device address: " + _deviceAddress);

		// set these flags
		_foundWriteID = false;
		_foundReadID = false;

		// Try to connect again later
		SetState (States.Connect, connectRetryTimeoutSecs);

		BluetoothLEHardwareInterface.ConnectToPeripheral (_deviceAddress, null, null, (address, serviceUUID, characteristicUUID) => {
			if (_connected) {
				logger.Warning("Already connected to peripheral!");
				return;
			}

			logger.DebugLog("Found peripheral service and characterstic: address=[" + address + "], serviceUUID=[" + serviceUUID + "], characteristicUUID=[" + characteristicUUID + "]");

			if (IsEqual (serviceUUID, _peripheralParams.Value.serviceUUID))
			{
				logger.DebugLog("Peripheral service matches target service UUID");

				bool matchesWriteID = IsEqual (characteristicUUID, _peripheralParams.Value.writeCharacteristicUUID);
				bool matchesReadID = IsEqual (characteristicUUID, _peripheralParams.Value.readCharacteristicUUID);
				_foundWriteID = _foundWriteID || matchesWriteID;
				_foundReadID = _foundReadID || matchesReadID;

				if (matchesWriteID) {
					logger.DebugLog("Peripheral characterstic matches write characterstic UUID");
				}
				if (matchesReadID) {
					logger.DebugLog("Peripheral characterstic matches read characterstic UUID");
				}

				// if we have found both characteristics that we are waiting for
				// set the state. make sure there is enough timeout that if the
				// device is still enumerating other characteristics it finishes
				// before we try to subscribe
				if (_foundWriteID && _foundReadID)
				{
					logger.DebugLog("Found target peripheral read and write characteristics. Connection complete.");
					_connected = true;
					if (OnConnect != null) {
						OnConnect();
					}
					_state = States.None;
				}
			}
		});
	}

	private void DisconnectFromPeripheral() {
		ResetState (() => {});
	}

	private void ResetState (Action afterResetAction) {
		if (_peripheralParams.HasValue) {
			if (_connected) {
				logger.DebugLog ("Disconnecting from peripheral with device address: " + _deviceAddress);
				BluetoothLEHardwareInterface.DisconnectPeripheral (_deviceAddress, (address) => {
					logger.DebugLog ("De-initializing bluetooth hardware interface");
					BluetoothLEHardwareInterface.DeInitialize (() => {
						ResetStateVars();
						afterResetAction();
					});
				});
			} else {
				logger.DebugLog ("De-initializing bluetooth hardware interface");
				BluetoothLEHardwareInterface.DeInitialize (() => {
					ResetStateVars();
					afterResetAction();
				});
			}
		} else {
			ResetStateVars ();
			afterResetAction();
		}
	}

	private void ResetStateVars() {
		_peripheralParams = null;
		_connected = false;
		_timeout = 0f;
		_state = States.None;
		_deviceAddress = null;
		_foundWriteID = false;
		_foundReadID = false;
	}

	private void SetState (States newState, float timeout)
	{
		logger.DebugLog ("Setting state to " + newState + " in " + timeout + " seconds");
		_state = newState;
		_timeout = timeout;
	}

	private void SendBytes(byte[] data, Action onSuccess = null) {
		#if UNITY_EDITOR
			if (onSuccess != null) {
				onSuccess();
			}
			return;
		#endif

		if (!_connected) {
			logger.Warning ("Not yet connected to peripheral. Dropping data.");
			return; 
		}

		BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, _peripheralParams.Value.serviceUUID, _peripheralParams.Value.writeCharacteristicUUID, data, data.Length, true, (characteristicUUID) => {
			if (onSuccess != null) {
				onSuccess();
			}
		});
	}

	private string FullUUID (string uuid)
	{
		return "0000" + uuid + "-0000-1000-8000-00805f9b34fb";
	}

	private bool IsEqual(string uuid1, string uuid2)
	{
		if (uuid1.Length == 4)
			uuid1 = FullUUID (uuid1);
		if (uuid2.Length == 4)
			uuid2 = FullUUID (uuid2);

		return (uuid1.ToUpper().CompareTo(uuid2.ToUpper()) == 0);
	}
}