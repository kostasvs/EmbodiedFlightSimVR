using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class EADI : MonoBehaviour {

		public const float METERS_TO_FEET = 3.28084f;

		private Transform aircraftTr;

		[SerializeField]
		private Text machText;
		[SerializeField]
		private Text airspeedText;
		[SerializeField]
		private Text groundSpeedText;
		[SerializeField]
		private Text altitudeText;
		[SerializeField]
		private Text vertSpeedTextUp;
		[SerializeField]
		private Text vertSpeedTextDown;
		[SerializeField]
		private Text headingText;

		[SerializeField]
		private MeshRenderer ascendArrow;
		[SerializeField]
		private MeshRenderer descendArrow;

		[SerializeField]
		private Transform eadiSphere;
		[SerializeField]
		private Transform eadiRollIndex;

		private void Start () {
			aircraftTr = transform.root;
		}

		void Update () {
			var snapshot = GeoPositioning.CurSnapshot;

			// speeds
			airspeedText.text = snapshot.airspeed.ToString ("0");
			machText.text = snapshot.mach.ToString ("0.00");
			groundSpeedText.text = "Gspd " + snapshot.groundSpeed.ToString ("0");

			// vertical speed
			vertSpeedTextDown.text = (snapshot.verticalSpeed * 60).ToString ("0");
			vertSpeedTextUp.text = "+" + vertSpeedTextDown.text;
			vertSpeedTextUp.enabled = snapshot.verticalSpeed > 0;
			vertSpeedTextDown.enabled = snapshot.verticalSpeed < 0;
			ascendArrow.enabled = snapshot.verticalSpeed > 0;
			descendArrow.enabled = snapshot.verticalSpeed < 0;

			// altitude
			altitudeText.text = (snapshot.alt * METERS_TO_FEET).ToString ("0");

			// heading
			headingText.text = snapshot.magHeading.ToString ("0");

			// attitude
			var attitude = aircraftTr.eulerAngles;
			var roll = Angle180 (attitude.z);
			eadiSphere.localEulerAngles = new Vector3 (-attitude.x, 0, -attitude.z);
			eadiRollIndex.localEulerAngles = new Vector3 (-roll, 0, 0);
		}

		private static float Angle180 (float angle) {
			return (angle + 180f) % 360f - 180f;
		}
	}
}