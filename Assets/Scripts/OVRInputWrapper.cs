using UnityEngine;

public class OVRInputWrapper : MonoBehaviour {
	private static OVRInputWrapper instance;

	private readonly float[] vibrationTimer = new float[2];

	private void Awake () {
		instance = this;
	}

	private void FixedUpdate () {
		OVRInput.FixedUpdate ();
	}

	private void Update () {
		OVRInput.Update ();
		for (int i = 0; i < 2; i++) {
			if (vibrationTimer[i] > 0f) {
				vibrationTimer[i] -= Time.deltaTime;
				if (vibrationTimer[i] <= 0f) {
					OVRInput.SetControllerVibration (0f, 0f, i == 1 ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch);
				}
			}
		}
	}

	public static void Vibrate (bool rightHand, float amplitude, float time) {
		if (!instance) return;
		OVRInput.SetControllerVibration (1f, Mathf.Clamp01 (amplitude),
			rightHand ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch);
		instance.vibrationTimer[rightHand ? 1 : 0] = time;
	}

	public static void VibratePulseLow (bool rightHand) {
		Vibrate (rightHand, .5f, .05f);
	}

	public static void VibratePulseMed (bool rightHand) {
		Vibrate (rightHand, 1f, .1f);
	}
}
