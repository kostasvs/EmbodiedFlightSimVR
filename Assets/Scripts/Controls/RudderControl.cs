using UnityEngine;

namespace Assets.Scripts.Controls {
	public class RudderControl : MonoBehaviour {
		public float Output { get; private set; }

		void Update () {
			if (Application.isEditor) {
				Output = (Input.GetKey (KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey (KeyCode.LeftArrow) ? 1 : 0);
			}
			else {
				Output = Mathf.Clamp (OVRInput.Get (OVRInput.Axis2D.SecondaryThumbstick).x, -1f, 1f);
			}
		}

		public float GetCombinedOutput () {
			if (Mathf.Abs (FlightGearNetworking.Instance.PeerInput.Rudder) < .1f) {
				return Output;
			}
			if (Mathf.Abs (Output) < .1f) {
				return FlightGearNetworking.Instance.PeerInput.Rudder;
			}
			return (Output + FlightGearNetworking.Instance.PeerInput.Rudder) / 2f;
		}
	}
}