﻿using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class HUD : MonoBehaviour {

		public const float ANGLES_TO_PIXELS = 512f / 25f;
		public const float METERS_TO_FEET = 3.28084f;

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
		[SerializeField]
		private RawImage compassImage;
		[SerializeField]
		private Text airspeedText;
		[SerializeField]
		private Text machText;
		[SerializeField]
		private Text altitudeText;
		[SerializeField]
		private Text radaltText;
		[SerializeField]
		private RectTransform trackMarker;
		[SerializeField]
		private float trackMarkerOffsetFactor = 1f;
		[SerializeField]
		private RectTransform flightVector;
		[SerializeField]
		private RectTransform vMarkers;
		[SerializeField]
		private float vMarkerOffsetFactor = 1f;
		[SerializeField]
		private float vMarkerOffsetMax = 100f;

		private void Start () {
			if (!cameraTr) {
				Debug.LogError ("No cameraTr defined");
				enabled = false;
				return;
			}

			aircraftTr = transform.root;

			// apply hudColor to all Text and Image children
			foreach (var text in GetComponentsInChildren<Text> (true)) {
				text.color = hudColor;
			}
			foreach (var image in GetComponentsInChildren<Image> (true)) {
				image.color = hudColor;
			}
			foreach (var image in GetComponentsInChildren<RawImage> (true)) {
				image.color = hudColor;
			}
		}

		void Update () {
			var camRelPos = uprightCopy.InverseTransformPoint (cameraTr.position);
			camRelPos.z = 0f;
			offsetParent.localPosition = camRelPos;

			// horizon
			var attitude = aircraftTr.eulerAngles;
			var roll = Angle180 (attitude.z);
			bool upsideDown = roll > 90f || roll < -90f;
			var elevation = AngleElevation (attitude.x);
			horizonLine.anchoredPosition = new Vector2 (0f, (upsideDown ? -elevation : elevation) * ANGLES_TO_PIXELS);
			horizonLine.localRotation = Quaternion.Euler (0f, 0f, -roll);

			// compass
			var snapshot = GeoPositioning.CurSnapshot;
			var rect = compassImage.uvRect;
			rect.x = snapshot.magHeading / 360f - rect.width / 2f;
			compassImage.uvRect = rect;

			// airspeed
			airspeedText.text = snapshot.airspeed.ToString ("0");
			machText.enabled = snapshot.mach > .6f;
			if (machText.enabled) machText.text = snapshot.mach.ToString ("0.00");

			// altitude
			altitudeText.text = (snapshot.alt * METERS_TO_FEET).ToString ("0");
			var radalt = Mathf.Max (0, snapshot.radarAltitude);
			if (radalt > 5000f || Mathf.Abs (roll) > 30f || Mathf.Abs (elevation) > 60f) {
				radaltText.text = "***** H";
			}
			else radaltText.text = radalt.ToString ("0") + " H";

			// track
			var velocity = aircraftTr.InverseTransformDirection (new Vector3 (snapshot.ve, -snapshot.vd, snapshot.vn));
			var pos = trackMarker.anchoredPosition;
			if (velocity.sqrMagnitude < .01f) pos.x = 0f;
			else {
				var track = Mathf.Atan2 (velocity.x, velocity.z) * Mathf.Rad2Deg;
				pos.x = track * ANGLES_TO_PIXELS * trackMarkerOffsetFactor;
			}
			trackMarker.anchoredPosition = pos;

			// flight vector
			if (velocity.sqrMagnitude < .01f) pos = Vector2.zero;
			else {
				var track = Mathf.Atan2 (velocity.x, velocity.z) * Mathf.Rad2Deg;
				var elevationRelative = Mathf.Atan2 (velocity.y, velocity.z) * Mathf.Rad2Deg;
				pos = new Vector2 (track, elevationRelative) * ANGLES_TO_PIXELS;
			}
			flightVector.anchoredPosition = pos;

			// v markers
			var vMarkersPos = vMarkers.anchoredPosition;
			vMarkersPos.y = Mathf.Clamp (snapshot.acceleration * vMarkerOffsetFactor, -vMarkerOffsetMax, vMarkerOffsetMax);
			vMarkers.anchoredPosition = vMarkersPos;
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