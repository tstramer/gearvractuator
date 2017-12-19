/**
 * Connection params for a bluetooth peripheral
 */
public struct BluetoothPeripheralParams {
	public string deviceName;
	public string serviceUUID;
	public string writeCharacteristicUUID;
	public string readCharacteristicUUID;

	public BluetoothPeripheralParams(
		string deviceName, 
		string serviceUUID, 
		string writeCharacteristicUUID, 
		string readCharacteristicUUID
	) {
		this.deviceName = deviceName;
		this.serviceUUID = serviceUUID;
		this.writeCharacteristicUUID = writeCharacteristicUUID;
		this.readCharacteristicUUID = readCharacteristicUUID;
	}
}