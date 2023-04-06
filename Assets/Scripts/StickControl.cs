using UnityEngine;
using Oculus.Interaction;

namespace Assets.Scripts {
	public class StickControl : MonoBehaviour {
		public Vector2 Output { get; private set; }
		public bool invertHor;
		public bool invertVer = true;

		private OneGrabJoystickTransformer transformer;

		private void Start () {
			transformer = GetComponent<OneGrabJoystickTransformer> ();
		}

		void Update () {
			if (transformer) {
				Output = transformer.Output;
			}
			if (Application.isEditor) {
				Output = new Vector2 (
					(Input.GetKey (KeyCode.D) ? 1 : 0) - (Input.GetKey (KeyCode.A) ? 1 : 0),
					(Input.GetKey (KeyCode.S) ? 1 : 0) - (Input.GetKey (KeyCode.W) ? 1 : 0));
			}
			if (invertHor || invertVer) {
				Output = new Vector2 (
				invertHor ? -Output.x : Output.x,
				invertVer ? -Output.y : Output.y);
			}
		}
	}
}