using UnityEngine;

namespace Assets.Scripts {
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
	}
}