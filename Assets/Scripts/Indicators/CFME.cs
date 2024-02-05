using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class CFME : MonoBehaviour {

		private Transform aircraftTr;

		[SerializeField]
		private Transform stbyHorizonDrum;
		[SerializeField]
		private RectTransform aoaRibbon;
		[SerializeField]
		private RectTransform vspeedRibbon;
		[SerializeField]
		private Text airspeedText;
		[SerializeField]
		private Text machText;
		[SerializeField]
		private Text altitudeText;
		[SerializeField]
		private Text headingText;

		private void Start () {
			aircraftTr = transform.root;
		}

		void Update () {
			// stby horizon
			var attitude = aircraftTr.eulerAngles;
			var roll = Angle180 (attitude.z);
			var elevation = AngleElevation (attitude.x);
			stbyHorizonDrum.localRotation = Quaternion.Euler (-roll, 0f, -elevation);

			// heading
			var snapshot = GeoPositioning.CurSnapshot;
			headingText.text = snapshot.magHeading.ToString ("0");

			// airspeed
			airspeedText.text = snapshot.airspeed.ToString ("0");
			machText.enabled = snapshot.mach > .6f;
			if (machText.enabled) machText.text = snapshot.mach.ToString ("0.00");

			// altitude
			altitudeText.text = (snapshot.alt * HUD.METERS_TO_FEET).ToString ("0");

			// aoa
			var scale = aoaRibbon.localScale;
			scale.y = Mathf.Clamp01 (snapshot.alpha / 35f);
			aoaRibbon.localScale = scale;

			// vspeed
			scale = vspeedRibbon.localScale;
			var vspeed = snapshot.verticalSpeed / 1000f * 60f; // feet/sec to Kfeet/min
			scale.y = Mathf.Clamp (vspeed, -3, 3);
			vspeedRibbon.localScale = scale;
		}

		private static float Angle180 (float angle) {
			return (angle + 180f) % 360f - 180f;
		}

		private static float AngleElevation (float angle) {
			var angle180 = Angle180 (angle);
			if (angle180 > 90f) return 180f - angle180;
			if (angle180 < -90f) return -180f - angle180;
			return angle180;
		}
	}
}