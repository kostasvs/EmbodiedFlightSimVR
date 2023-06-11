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

	/// <summary>
	/// Vibrate Oculus controller.
	/// </summary>
	/// <param name="amplitude">vibration amplitude</param>
	/// <param name="time">vibration duration in seconds (between 0 and 2)</param>
	/// <param name="handIndex">0 = left hand, 1 = right hand, other values = both hands</param>
	public static void Vibrate (float amplitude, float time, int handIndex = -1) {
		if (!instance || time <= 0f) return;
		if (handIndex < 0 || handIndex > 1) {
			Vibrate (amplitude, time, 0);
			Vibrate (amplitude, time, 1);
			return;
		}
		OVRInput.SetControllerVibration (1f, Mathf.Clamp01 (amplitude),
			handIndex == 1 ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch);
		instance.vibrationTimer[handIndex] = time;
	}

	public static void VibratePulseLow (int handIndex = -1) {
		Vibrate (.5f, .05f, handIndex);
	}

	public static void VibratePulseMed (int handIndex = -1) {
		Vibrate (1f, .1f, handIndex);
	}
}
