using UnityEngine;

namespace Assets.Scripts {
	public class FlightControls : MonoBehaviour {

		[SerializeField]
		StickControl stick;
		[SerializeField]
		ThrottleControl throttle;
		[SerializeField]
		GameObject[] gearDownLights;

		private bool gearDown = true;

		public Transform cameraRig;
		public Vector3 addCameraPosInEditor;

		public StickControl Stick => stick;
		public ThrottleControl Throttle => throttle;
		public bool GearDown { get => gearDown; set => gearDown = value; }

		void Start () {
			SetGearDown (gearDown);

			if (Application.isEditor && cameraRig) cameraRig.localPosition += addCameraPosInEditor;
		}

		private void Update () {
			if (Input.GetKeyDown (KeyCode.G)) {
				ToggleGear ();
			}
		}

		public void SetGearDown (bool gearDown) {
			this.gearDown = gearDown;
			foreach (var light in gearDownLights) light.SetActive (gearDown);
		}

		public void ToggleGear () {
			SetGearDown (!gearDown);
		}
	}
}