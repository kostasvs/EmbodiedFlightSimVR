using UnityEngine;

namespace Assets.Scripts.Controls {
	public class BrakeControl : MonoBehaviour {
		public float Output { get; private set; }

		void Update () {
			if (Application.isEditor) {
				Output = Input.GetKey (KeyCode.B) ? 1 : 0;
			}
			else {
				Output = Mathf.Clamp01 (-OVRInput.Get (OVRInput.Axis2D.SecondaryThumbstick).y);
			}
		}
	}
}