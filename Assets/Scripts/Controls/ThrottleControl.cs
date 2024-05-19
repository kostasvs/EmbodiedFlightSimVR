using UnityEngine;
using Oculus.Interaction;

namespace Assets.Scripts.Controls {
	public class ThrottleControl : MonoBehaviour {
		public float Output { get; private set; }
		private float SlowOutput;
		public bool IsGrabbed { get; private set; }

		private float maxZ;
		private const float zThres = 0.001f;

		[SerializeField]
		private ThrottleControlGhost ghost;

		private AudioSource au;
		private float auVolumeInitial;

		private void Start () {
			Output = 0f;
			var tr = GetComponent<OneGrabTranslateTransformer> ();
			if (tr) maxZ = tr.Constraints.MaxZ.Value;

			au = GetComponent<AudioSource> ();
			auVolumeInitial = au.volume;

			var eventWrapper = GetComponent<PointableUnityEventWrapper> ();
			eventWrapper.WhenSelect.AddListener (() => SetIsGrabbed (true));
			eventWrapper.WhenUnselect.AddListener (() => SetIsGrabbed (false));
		}

		void Update () {
			var prevOutput = Mathf.Clamp (Output, zThres, 1f - zThres);

			if (Application.isEditor) {
				var dir = (Input.GetKey (KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey (KeyCode.DownArrow) ? -1f : 0f);
				if (dir != 0f) SetOutput (Output + dir * Time.deltaTime);
				IsGrabbed = dir != 0f;
			}
			else if (maxZ > 0) Output = Mathf.Clamp01 (transform.localPosition.z / maxZ);

			SlowOutput = Mathf.MoveTowards (SlowOutput, Output, Time.deltaTime * 2f);
			var outputThres = Mathf.Clamp (Output, zThres, 1f - zThres);
			if (prevOutput != outputThres && (outputThres == zThres || outputThres == 1f - zThres)) {
				if (SlowOutput != Output) {
					au.pitch = 1f;
					au.volume = auVolumeInitial;
				}
				else {
					au.pitch = .6f;
					au.volume = auVolumeInitial * .4f;
				}
				//au.Play (); // disabled due to audio delay
				if (SlowOutput != Output) OVRInputWrapper.VibratePulseMed (0);
				else OVRInputWrapper.VibratePulseLow (0);
			}

			bool showGhost = FlightGearNetworking.Instance.PeerInput.ThrottleIsGrabbed;
			if (showGhost) {
				var diff = Mathf.Abs (Output - FlightGearNetworking.Instance.PeerInput.Throttle);
				showGhost = diff > .1f;
			}
			ghost.UpdateGhost (showGhost, FlightGearNetworking.Instance.PeerInput.Throttle);
		}

		public void SetIsGrabbed (bool isGrabbed) {
			IsGrabbed = isGrabbed;
		}

		public void SetOutput (float output) {
			Output = Mathf.Clamp01 (output);
			var pos = transform.localPosition;
			pos.z = output * maxZ;
			transform.localPosition = pos;
		}
	}
}
