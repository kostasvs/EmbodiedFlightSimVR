using UnityEngine;
using Oculus.Interaction;

namespace Assets.Scripts {
	public class ThrottleControl : MonoBehaviour {
		public float Output { get; private set; }

		private float maxZ;

		private void Start () {
			var tr = GetComponent<OneGrabTranslateTransformer> ();
			if (tr) maxZ = tr.Constraints.MaxZ.Value;
		}

		void Update () {
			if (Application.isEditor) {
				var dir = (Input.GetKey (KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey (KeyCode.DownArrow) ? -1f : 0f);
				if (dir != 0f) Output = Mathf.Clamp01 (Output + dir * Time.deltaTime);
			}
			else if (maxZ > 0) Output = transform.localPosition.z / maxZ;
		}
	}
}
