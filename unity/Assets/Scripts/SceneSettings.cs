using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SceneSettings : MonoBehaviour {

	[SerializeField]
	private Vector3 cameraPosition = Vector3.zero;

	[SerializeField]
	private Vector3 cameraRotation = Vector3.zero;

	[SerializeField]
	private Material skybox = null;

	[SerializeField]
	private bool enableFog = false;

	private Transform CameraContainer {
		get {
			return Camera.main.transform.parent;
		}
	}

	private void OnEnable () {
		if (skybox != null) {
			RenderSettings.skybox = skybox;
		}
		RenderSettings.fog = enableFog;

		CameraContainer.position = cameraPosition;
		CameraContainer.rotation = Quaternion.Euler(cameraRotation);
	}
}