using UnityEngine;
using Oculus.Interaction;

namespace Assets.Scripts.Controls {
	public class ThrottleControl : MonoBehaviour {
		public float Output { get; private set; }
		private float SlowOutput;

		private float maxZ;
		private const float zThres = 0.001f;

		private AudioSource au;
		private float auVolumeInitial;

		private void Start () {
			Output = 0f;
			var tr = GetComponent<OneGrabTranslateTransformer> ();
			if (tr) maxZ = tr.Constraints.MaxZ.Value;

			au = GetComponent<AudioSource> ();
			auVolumeInitial = au.volume;
		}

		void Update () {
			var prevOutput = Mathf.Clamp (Output, zThres, 1f - zThres);

			if (Application.isEditor) {
				var dir = (Input.GetKey (KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey (KeyCode.DownArrow) ? -1f : 0f);
				if (dir != 0f) Output = Mathf.Clamp01 (Output + dir * Time.deltaTime);
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
		}
	}
}
