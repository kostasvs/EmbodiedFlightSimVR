using Mapbox.Unity.Map;
using Mapbox.Utils;
using System.Collections.Generic;
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
		private Transform aircraftTransform;

		private readonly List<Snapshot> snapshots = new List<Snapshot> ();
		private double curSimTimeOffset;
		private const double simLatency = .2f;
		private const float maxSimTimeOffset = .5f;
		private const float maxSnapshotTimeOffset = 1f;

		private int desyncCounts;
		private const int maxDesyncCounts = 3;

		void Start () {
			mapInitScale = map.transform.localScale;
		}

		void Update () {
			ApplyTimeDataOnTransform (aircraftTransform);
		}

		public double GetCurSimTime () => Time.timeAsDouble + curSimTimeOffset;

		public void AddSnapshot (double lat, double lon, double alt,
			float vn, float ve, float vd,
			float yaw, float pitch, float roll,
			float yawRate, float pitchRate, float rollRate,
			double simTime) {

			// match local time to simulation time if they are repeatedly too far apart
			if (Mathf.Abs ((float)(GetCurSimTime () - simTime - simLatency)) > maxSimTimeOffset) {
				desyncCounts++;
				if (desyncCounts >= maxDesyncCounts) {
					desyncCounts = 0;
					curSimTimeOffset = simTime - Time.timeAsDouble - simLatency;
					Debug.Log ("Resetting time to " + GetCurSimTime ());
				}
			}
			else {
				desyncCounts = 0;
				RemoveStaleSnapshots ();
			}

			// add snapshot with new info
			snapshots.Add (new Snapshot (lat, lon, alt, vn, ve, vd, yaw, pitch, roll, yawRate, pitchRate, rollRate, simTime));

			// initialize map if this is first received info
			if (!map.enabled) {
				var options = map.Options;
				options.locationOptions.latitudeLongitude = string.Format ("{0:0.000000}, {1:0.000000}", lat, lon);
				map.Options = options;
				map.enabled = true;
			}
		}

		private void RemoveStaleSnapshots () {
			for (int i = snapshots.Count - 1; i >= 0; i--) {
				var s = snapshots[i];
				if (Mathf.Abs ((float)(GetCurSimTime () - s.simTime)) > maxSnapshotTimeOffset) {
					snapshots.RemoveAt (i);
				}
			}
		}

		public void ApplyTimeDataOnTransform (Transform target) {

			if (!target) {
				Debug.LogWarning ("no target");
				return;
			}

			if (!map.enabled || snapshots.Count == 0) {
				// skip until information received
				return;
			}

			// find nearest before/after snapshots
			int nearestBefore = -1, nearestAfter = -1;
			double timeBefore = 0, timeAfter = 0;
			for (int i = 0; i < snapshots.Count; i++) {
				var s = snapshots[i];
				if (s.simTime < GetCurSimTime ()) {
					if (nearestBefore == -1 || s.simTime > timeBefore) {
						nearestBefore = i;
						timeBefore = s.simTime;
					}
				}
				else {
					if (nearestAfter == -1 || s.simTime < timeAfter) {
						nearestAfter = i;
						timeAfter = s.simTime;
					}
				}
			}

			// get interpolated result
			Snapshot result;
			if (nearestBefore == -1) result = snapshots[nearestAfter];
			else if (nearestAfter == -1) result = snapshots[nearestBefore];
			else result = Lerp (snapshots[nearestBefore], snapshots[nearestAfter], GetCurSimTime ());

			// apply map position/zoom
			var alt = (float)result.alt;
			var zoom = GetZoomForAltitude (alt);
			var horThres = horizontalRefreshThreshold + horizontalRefreshThresholdAltitudeFactor * Mathf.Max (0, alt);

			var pos = map.GeoToWorldPosition (new Vector2d (result.lat, result.lon), false);
			if (Mathf.Abs (pos.x) > horThres ||
				Mathf.Abs (pos.z) > horThres ||
				Mathf.RoundToInt (map.Zoom) != zoom) {

				map.UpdateMap (new Vector2d (result.lat, result.lon), zoom);
				map.transform.localScale = mapInitScale;
				pos = map.GeoToWorldPosition (new Vector2d (result.lat, result.lon), false);
			}
			pos.y = alt;

			// rotation
			var rot = Quaternion.Euler (-result.pitch, result.yaw, -result.roll);

			// extrapolate if needed
			var velocity = new Vector3 (result.ve, -result.vd, result.vn);
			var spin = new Vector3 (-result.pitchRate, result.yawRate, -result.rollRate);
			var dt = (float)(GetCurSimTime () - result.simTime);
			pos += velocity * dt;
			rot = Quaternion.Euler (spin * dt) * rot;

			// apply result
			target.SetPositionAndRotation (pos, rot);
		}

		private int GetZoomForAltitude (float altitude) {
			float lerp = Mathf.Clamp01 ((altitude - maxZoomAltitude) / (minZoomAltitude - maxZoomAltitude));
			return Mathf.RoundToInt (Mathf.Lerp (maxZoom, minZoom, lerp) / zoomStep) * zoomStep;
		}

		private static Snapshot Lerp (Snapshot s1, Snapshot s2, double simTime) {
			if (simTime < s1.simTime) return s1;
			if (simTime > s2.simTime) return s2;
			float alpha = (float)((simTime - s1.simTime) / (s2.simTime - s1.simTime));
			return new Snapshot (
				s1.lat + (s2.lat - s1.lat) * alpha,
				s1.lon + (s2.lon - s1.lon) * alpha,
				s1.alt + (s2.alt - s1.alt) * alpha,
				s1.vn + (s2.vn - s1.vn) * alpha,
				s1.ve + (s2.ve - s1.ve) * alpha,
				s1.vd + (s2.vd - s1.vd) * alpha,
				s1.yaw + (s2.yaw - s1.yaw) * alpha,
				s1.pitch + (s2.pitch - s1.pitch) * alpha,
				s1.roll + (s2.roll - s1.roll) * alpha,
				s1.yawRate + (s2.yawRate - s1.yawRate) * alpha,
				s1.pitchRate + (s2.pitchRate - s1.pitchRate) * alpha,
				s1.rollRate + (s2.rollRate - s1.rollRate) * alpha,
				simTime);
		}
	}

	public struct Snapshot {
		public double lat;
		public double lon;
		public double alt;
		public float vn;
		public float ve;
		public float vd;
		public float yaw;
		public float pitch;
		public float roll;
		public float yawRate;
		public float pitchRate;
		public float rollRate;
		public double simTime;

		public Snapshot (double lat, double lon, double alt,
			float vn, float ve, float vd,
			float yaw, float pitch, float roll,
			float yawRate, float pitchRate, float rollRate,
			double simTime) {
			this.lat = lat;
			this.lon = lon;
			this.alt = alt;
			this.vn = vn;
			this.ve = ve;
			this.vd = vd;
			this.yaw = yaw;
			this.pitch = pitch;
			this.roll = roll;
			this.yawRate = yawRate;
			this.pitchRate = pitchRate;
			this.rollRate = rollRate;
			this.simTime = simTime;
		}
	}
}