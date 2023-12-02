using UnityEngine;
using Oculus.Interaction;

namespace Assets.Scripts.Controls {
	public class StickControl : MonoBehaviour {
		public Vector2 Output { get; private set; }
		private Vector2 SlowOutput;
		public bool IsGrabbed { get; private set; }

		public bool invertHor;
		public bool invertVer = true;

		private OneGrabJoystickTransformer transformer;

		private AudioSource au;
		private float auVolumeInitial;

		private void Start () {
			transformer = GetComponent<OneGrabJoystickTransformer> ();

			au = GetComponent<AudioSource> ();
			auVolumeInitial = au.volume;

			var eventWrapper = GetComponent<PointableUnityEventWrapper> ();
			eventWrapper.WhenSelect.AddListener (() => SetIsGrabbed (true));
			eventWrapper.WhenUnselect.AddListener (() => SetIsGrabbed (false));
		}

		void Update () {
			var prevOutput = Output;

			if (transformer) {
				Output = transformer.Output;
			}
			if (Application.isEditor) {
				Output = new Vector2 (
					(Input.GetKey (KeyCode.D) ? 1 : 0) - (Input.GetKey (KeyCode.A) ? 1 : 0),
					(Input.GetKey (KeyCode.S) ? 1 : 0) - (Input.GetKey (KeyCode.W) ? 1 : 0));
				IsGrabbed = Output != Vector2.zero;
			}
			if (invertHor || invertVer) {
				Output = new Vector2 (
				Mathf.Clamp (invertHor ? -Output.x : Output.x, -1f, 1f),
				Mathf.Clamp (invertVer ? -Output.y : Output.y, -1f, 1f));
			}

			bool playSound = false;
			bool plauSoundLoud = false;
			SlowOutput = Vector2.MoveTowards (SlowOutput, Output, Time.deltaTime * 4f);
			if (prevOutput.x != Output.x && Mathf.Abs (Output.x) == 1f) {
				playSound = true;
				if (SlowOutput.x != Output.x) plauSoundLoud = true;
			}
			if (prevOutput.y != Output.y && Mathf.Abs (Output.y) == 1f) {
				playSound = true;
				if (SlowOutput.y != Output.y) plauSoundLoud = true;
			}
			if (playSound) {
				if (plauSoundLoud) {
					au.pitch = 1f;
					au.volume = auVolumeInitial;
				}
				else {
					au.pitch = .6f;
					au.volume = auVolumeInitial * .4f;
				}
				//au.Play (); // disabled due to audio delay
				if (plauSoundLoud) OVRInputWrapper.VibratePulseMed (1);
				else OVRInputWrapper.VibratePulseLow (1);
			}
		}

		public void SetIsGrabbed (bool isGrabbed) {
			IsGrabbed = isGrabbed;
		}

		public Vector2 GetCombinedOutput () {
			if (!FlightGearNetworking.Instance.PeerInput.StickIsGrabbed) {
				return Output;
			}
			if (!IsGrabbed) {
				return FlightGearNetworking.Instance.PeerInput.Stick;
			}
			return (Output + FlightGearNetworking.Instance.PeerInput.Stick) / 2f;
		}
	}
}