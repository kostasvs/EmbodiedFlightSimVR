using UnityEngine;

namespace Assets.Scripts {
	public class SmoothPosition : MonoBehaviour {

		[SerializeField]
		[Tooltip ("The transform that will be smoothly moved towards this transform")]
		private Transform target;
		public Transform Target => target;

		[SerializeField]
		private float smoothFactor = .1f;

		private void FixedUpdate () {
			if (!target) return;
			target.SetPositionAndRotation (
				Vector3.LerpUnclamped (target.position, transform.position, smoothFactor),
				Quaternion.SlerpUnclamped (target.rotation, transform.rotation, smoothFactor));
		}
	}
}
