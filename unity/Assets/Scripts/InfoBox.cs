using System;
using UnityEngine;
using VRStandardAssets.Utils;
using System.Collections;
using UnityEngine.UI;

public class InfoBox : MonoBehaviour {

	[SerializeField]
	private Text textComponent;

	[SerializeField]
	public UIFader uiFader;

	private Action CurrentOnFadeOutCompleteAction;

	public void ShowText(string text) {
		this.transform.position = Camera.main.ScreenToWorldPoint (
			new Vector3(.5f * Screen.width, .55f * Screen.height, 6.0f)
		);
		this.GetComponent<UIFader>().SetVisible ();
		textComponent.text = text;
	}

	public void ShowTextAndFadeOut(string text, float showTimeSecs, Action onFadeOutComplete) {
		CurrentOnFadeOutCompleteAction = onFadeOutComplete;
		ShowText (text);
		StartCoroutine (FadeOut (showTimeSecs));
	}

	public void Hide() {
		uiFader.SetInvisible ();
	}

	private IEnumerator FadeOut(float showTimeSecs) {
		yield return new WaitForSeconds (showTimeSecs);
		yield return (uiFader.FadeOut ());
	}

	private void OnEnable() {
		uiFader.OnFadeOutComplete += OnFadeOutComplete;
	}

	private void OnDisable() {
		uiFader.OnFadeOutComplete -= OnFadeOutComplete;
	}

	private void OnFadeOutComplete() {
		if (CurrentOnFadeOutCompleteAction != null) {
			CurrentOnFadeOutCompleteAction ();
		}
	}
}