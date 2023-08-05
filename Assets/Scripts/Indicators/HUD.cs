using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class HUD : MonoBehaviour {

		public const float ANGLES_TO_PIXELS = 512f / 20f;
		private Transform aircraftTr;

		[SerializeField]
		private Transform uprightCopy;
		[SerializeField]
		private Transform offsetParent;
		[SerializeField]
		private Transform cameraTr;

		[SerializeField]
		private Color hudColor = Color.white;

		[SerializeField]
		private RectTransform horizonLine;

		private void Start () {
			aircraftTr = transform.root;

			// apply hudColor to all Text and Image children
			foreach (var text in GetComponentsInChildren<Text> (true)) {
				text.color = hudColor;
			}
			foreach (var image in GetComponentsInChildren<Image> (true)) {
				image.color = hudColor;
			}
		}

		void Update () {
			if (!cameraTr) return;
			var camRelPos = uprightCopy.InverseTransformPoint (cameraTr.position);
			camRelPos.z = 0f;
			offsetParent.localPosition = camRelPos;

			//var snapshot = GeoPositioning.GetCurSnapshot ();
			var attitude = aircraftTr.eulerAngles;
			var roll = Angle180 (attitude.z);
			bool upsideDown = roll > 90f || roll < -90f;
			var elevation = AngleElevation (attitude.x);
			horizonLine.anchoredPosition = new Vector2 (0f, (upsideDown ? -elevation : elevation) * ANGLES_TO_PIXELS);
			horizonLine.localRotation = Quaternion.Euler (0f, 0f, -roll);
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