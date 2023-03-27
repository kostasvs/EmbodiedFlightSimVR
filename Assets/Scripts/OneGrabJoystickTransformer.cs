using UnityEngine;

namespace Oculus.Interaction {
	/// <summary>
	/// A Transformer that allows only rotation of the target in X & Z axes and within given limit.
	/// Also returns the rotation to identity when not grabbed.
	/// </summary>
	public class OneGrabJoystickTransformer : MonoBehaviour, ITransformer {

		[SerializeField]
		private float maxAngle = 30f;

		[SerializeField]
		private float resetSpeed = 500f;

		private IGrabbable _grabbable;
		private Vector3 _grabDeltaInLocalSpace;

		private Vector2 output;
		public Vector2 Output => output;

		private bool grabbed;
		public bool Grabbed => grabbed;

		public void Initialize (IGrabbable grabbable) {
			_grabbable = grabbable;
		}

		public void BeginTransform () {
			Pose grabPoint = _grabbable.GrabPoints[0];
			var targetTransform = _grabbable.Transform;
			_grabDeltaInLocalSpace = targetTransform.parent.InverseTransformPoint (grabPoint.position);
			_grabDeltaInLocalSpace.z = Mathf.Max (.01f, _grabDeltaInLocalSpace.z);
			grabbed = true;
		}

		public void UpdateTransform () {
			Pose grabPoint = _grabbable.GrabPoints[0];
			var targetTransform = _grabbable.Transform;
			var localGrab = targetTransform.parent.InverseTransformPoint (grabPoint.position) - _grabDeltaInLocalSpace;
			float maxOffset = Mathf.Sin (maxAngle * Mathf.Deg2Rad) * _grabDeltaInLocalSpace.z;
			localGrab.x = Mathf.Clamp (localGrab.x, -maxOffset, maxOffset);
			localGrab.y = Mathf.Clamp (localGrab.y, -maxOffset, maxOffset);
			localGrab.z = _grabDeltaInLocalSpace.z;

			targetTransform.localRotation = Quaternion.LookRotation (localGrab);

			output.x = localGrab.x / maxOffset;
			output.y = localGrab.y / maxOffset;
		}

		public void EndTransform () {
			grabbed = false;
		}

		private void Update () {
			if (grabbed || _grabbable.Transform.localRotation == Quaternion.identity) return;

			_grabbable.Transform.localRotation = Quaternion.RotateTowards (_grabbable.Transform.localRotation, Quaternion.identity,
				resetSpeed * Time.deltaTime);
			if (maxAngle > 0) output = Vector2.MoveTowards (output, Vector2.zero, resetSpeed / maxAngle * Time.deltaTime);
		}
	}
}
