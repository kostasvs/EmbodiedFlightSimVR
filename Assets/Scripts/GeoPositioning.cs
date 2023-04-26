using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;

namespace Assets.Scripts {
	public class GeoPositioning : MonoBehaviour {

		[SerializeField]
		private AbstractMap map;
		private Vector3 mapInitScale;

		const float horizontalRefreshThreshold = 500;
		const float horizontalRefreshThresholdAltitudeFactor = 1f;

		const int minZoom = 6;
		const int maxZoom = 15;
		const int zoomStep = 3;
		const int minZoomAltitude = 2000;
		const int maxZoomAltitude = 0;

		[SerializeField]
		private SmoothPosition aircraftTransform;

		private Vector3 velocity;
		private Vector3 spin;

		private float lastUpdatedTime = -1f;
		const float positionResetDelay = .6f;
		const float speedResetDelay = 1f;

		private double lastSimTime = -1;

		void Start () {
			mapInitScale = map.transform.localScale;
		}

		private void Update () {
			if (Time.time > lastUpdatedTime + speedResetDelay) {
				velocity = Vector3.zero;
				spin = Vector3.zero;
			}
			else {
				aircraftTransform.transform.SetPositionAndRotation (
					aircraftTransform.transform.position + velocity * Time.deltaTime,
					Quaternion.Euler (spin * Time.deltaTime) * aircraftTransform.transform.rotation);
			}
		}

		public void UpdateData (double lat, double lon, double alt,
			float vn, float ve, float vd,
			float yaw, float pitch, float roll,
			float yawRate, float pitchRate, float rollRate,
			double simTime) {

			if (simTime <= lastSimTime) return;
			lastSimTime = simTime;

			if (!map.enabled) {
				var options = map.Options;
				options.locationOptions.latitudeLongitude = string.Format ("{0:0.000000}, {1:0.000000}", lat, lon);
				map.Options = options;
				map.enabled = true;
				return;
			}

			var pos = aircraftTransform.transform.position;
			velocity = new Vector3 (ve, -vd, vn);
			spin = new Vector3 (-pitchRate, yawRate, -rollRate);

			var zoom = GetZoomForAltitude ((float)alt);
			var horThres = horizontalRefreshThreshold + horizontalRefreshThresholdAltitudeFactor * Mathf.Max (0, pos.y);

			if (!map.enabled) {
				map.Options.locationOptions.latitudeLongitude = string.Format ("{0:0.000000}, {1:0.000000}", lat, lon);
				map.enabled = true;
			}
			else if (Time.time > lastUpdatedTime + positionResetDelay ||
				Mathf.Abs (pos.x) > horThres ||
				Mathf.Abs (pos.z) > horThres ||
				Mathf.RoundToInt (map.Zoom) != zoom) {

				map.UpdateMap (new Vector2d (lat, lon), zoom);
				map.transform.localScale = mapInitScale;
			}
			var prevPos = pos;
			pos = map.GeoToWorldPosition (new Vector2d (lat, lon), false);
			pos.y = (float)alt;
			aircraftTransform.transform.SetPositionAndRotation (pos, Quaternion.Euler (-pitch, yaw, -roll));
			aircraftTransform.Target.position += pos - prevPos;
			lastUpdatedTime = Time.time;
		}

		private int GetZoomForAltitude (float altitude) {
			float lerp = Mathf.Clamp01 ((altitude - maxZoomAltitude) / (minZoomAltitude - maxZoomAltitude));
			return Mathf.RoundToInt (Mathf.Lerp (maxZoom, minZoom, lerp) / zoomStep) * zoomStep;
		}
	}
}