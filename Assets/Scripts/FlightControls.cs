using UnityEngine;

namespace Assets.Scripts {
	public class FlightControls : MonoBehaviour {

		StickControl stick;
		ThrottleControl throttle;

		public Transform cameraRig;
		public Vector3 addCameraPosInEditor;

		public float maxAcceleration = 1;
		public float maxTurn = 90;
		public float frictionFactor = .99f;

		private Vector2 turnSmooth;
		public float turnLerpFactor = .1f;
		public float minTurnSpeed = 10f;
		public float maxTurnSpeed = 50f;

		private Vector3 velocity;

		void Start () {
			stick = GetComponentInChildren<StickControl> ();
			throttle = GetComponentInChildren<ThrottleControl> ();

			if (Application.isEditor && cameraRig) cameraRig.localPosition += addCameraPosInEditor;
		}

		void FixedUpdate () {
			var turn = Time.deltaTime * stick.Output;
			turnSmooth = Vector2.Lerp (turnSmooth, turn, turnLerpFactor);
			var turnSpeed = maxTurn * Mathf.Clamp01 ((velocity.magnitude - minTurnSpeed) / (maxTurnSpeed - minTurnSpeed));
			transform.Rotate (turnSpeed * new Vector3 (turnSmooth.y, 0, -turnSmooth.x), Space.Self);

			velocity += maxAcceleration * throttle.Output * transform.forward;
			velocity *= frictionFactor;
			transform.position += velocity * Time.deltaTime;
		}
	}
}