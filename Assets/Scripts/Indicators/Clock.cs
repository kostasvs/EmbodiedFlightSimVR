using UnityEngine;

namespace Assets.Scripts.Indicators {
	public class Clock : MonoBehaviour {

		[SerializeField]
		private Transform hourHand;
		[SerializeField]
		private Transform minuteHand;
		[SerializeField]
		private Transform secondHand;
		[SerializeField]
		private Vector3 maxRotation = new Vector3 (-360, 0, 0);

		private System.TimeSpan curShownTime = System.TimeSpan.Zero;

		private void Update () {
			var snapshot = GeoPositioning.CurSnapshot;
			if (snapshot.localTime != curShownTime) {
				SetTime (snapshot.localTime);
				curShownTime = snapshot.localTime;
			}
		}

		public void SetTime (System.TimeSpan timeOfDay) {
			hourHand.localEulerAngles = maxRotation * (float)timeOfDay.TotalSeconds / (12 * 3600);
			minuteHand.localEulerAngles = maxRotation * (float)timeOfDay.TotalSeconds / 3600;
			secondHand.localEulerAngles = maxRotation * (float)timeOfDay.TotalSeconds / 60;
		}
	}
}