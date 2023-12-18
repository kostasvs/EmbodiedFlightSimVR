using Assets.Scripts.Rpc;
using UnityEngine;

namespace Assets.Scripts.Controls {
	public class FlightControls : MonoBehaviour {

		[SerializeField]
		StickControl stick;
		[SerializeField]
		ThrottleControl throttle;
		[SerializeField]
		RudderControl rudder;
		[SerializeField]
		BrakeControl brake;

		private bool gearDown = true;

		public Transform cameraRig;
		public Vector3 addCameraPosInEditor;

		public StickControl Stick => stick;
		public ThrottleControl Throttle => throttle;
		public RudderControl Rudder { get => rudder; set => rudder = value; }
		public bool GearDown { get => gearDown; set => gearDown = value; }
		public BrakeControl Brake { get => brake; set => brake = value; }

		void Start () {
			SetGearDown (gearDown);
			RpcPoll.OnMessageReceived.AddListener (OnMessageReceived);

			if (Application.isEditor && cameraRig) cameraRig.localPosition += addCameraPosInEditor;
		}

		private void Update () {
			if (Input.GetKeyDown (KeyCode.G)) {
				ToggleGear ();
			}
		}

		public void SetGearDown (bool gearDown) {
			this.gearDown = gearDown;
		}

		public void ToggleGear () {
			if (FlightGearNetworking.Instance.IsMaster == true) {
				SetGearDown (!gearDown);
			}
			else if (FlightGearNetworking.Instance.IsMaster == false) {
				RpcPoll.Send ((byte)RpcIds.FlightControls_ToggleGearDown);
			}
		}

		private void OnMessageReceived (byte[] message) {
			if (message[0] == (byte) RpcIds.FlightControls_ToggleGearDown) {
				if (FlightGearNetworking.Instance.IsMaster == true) ToggleGear ();
			}
		}
	}
}