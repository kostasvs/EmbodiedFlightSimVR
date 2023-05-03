using UnityEngine;

namespace Assets.Scripts {
	public class FlightControls : MonoBehaviour {

		[SerializeField]
		StickControl stick;
		[SerializeField]
		ThrottleControl throttle;

		private bool gearDown = true;

		public Transform cameraRig;
		public Vector3 addCameraPosInEditor;

		public StickControl Stick => stick;
		public ThrottleControl Throttle => throttle;
		public bool GearDown { get => gearDown; set => gearDown = value; }

		void Start () {
			if (Application.isEditor && cameraRig) cameraRig.localPosition += addCameraPosInEditor;
		}

		private void Update () {
			if (Input.GetKeyDown (KeyCode.G)) {
				gearDown = !gearDown;
			}
		}
	}
}