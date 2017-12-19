using System;
using UnityEngine;
using SMI;

/**
 * Tracks where the user is currently gazing in the scene and accomodates
 * the display to the gazed at object.
 */
public class GazeContingentFocus : MonoBehaviour {
	
	[SerializeField] 
	private Transform m_Camera;               

	[SerializeField] 
	private float perceptualSmoothingAlpha = -1;               

	[SerializeField] 
	private DisplayAccomodationManager accomodationManager;    

	private void Update() {
		RaycastHit hitInformation;
		SMIEyeTrackingMobile.Instance.smi_GetRaycastHitFromGaze(out hitInformation);
		if (hitInformation.collider != null) {
			float distance = Vector3.Distance (m_Camera.position, hitInformation.point);
			accomodationManager.AccommodateVirtualDistance (distance, perceptualSmoothingAlpha);
		} else {
			accomodationManager.AccommodateVirtualDistance (1.5f, perceptualSmoothingAlpha);
		}
	}

}