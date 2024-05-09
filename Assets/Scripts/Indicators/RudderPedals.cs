using Assets.Scripts.Controls;
using UnityEngine;

namespace Assets.Scripts.Indicators {
	public class RudderPedals : MonoBehaviour {

		[SerializeField]
		private RudderControl rudderControl;
		[SerializeField]
		private BrakeControl brakeControl;

		[SerializeField]
		private Transform leftPedal;
		[SerializeField]
		private Transform rightPedal;

		private Vector3 leftPedalInitPos;
		private Vector3 rightPedalInitPos;
		private Quaternion pedalInitRot;

		[SerializeField]
		private Vector3 maxOffset = new Vector3 (.1f, 0, 0);
		[SerializeField]
		private float maxRotation = 30;

		private float curRudder;
		private float curBrake;

		void Start () {
			leftPedalInitPos = leftPedal.localPosition;
			rightPedalInitPos = rightPedal.localPosition;
			pedalInitRot = leftPedal.localRotation;
		}

		void Update () {
			float rudder = rudderControl.GetCombinedOutput ();
			float brake = brakeControl.GetCombinedOutput ();
			if (rudder != curRudder || brake != curBrake) {
				curRudder = rudder;
				curBrake = brake;

				leftPedal.localPosition = leftPedalInitPos - maxOffset * rudder;
				rightPedal.localPosition = rightPedalInitPos + maxOffset * rudder;

				leftPedal.localRotation = pedalInitRot * Quaternion.Euler (maxRotation * brake, 0, 0);
				rightPedal.localRotation = pedalInitRot * Quaternion.Euler (maxRotation * brake, 0, 0);
			}
		}
	}
}