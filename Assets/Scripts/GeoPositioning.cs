using Mapbox.Unity.Map;
using Mapbox.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {
	public class GeoPositioning : MonoBehaviour {

		[SerializeField]
		private AbstractMap[] maps = new AbstractMap[2];
		private int visibleMapIndex;
		private Vector3 mapInitScale;

		private const float secondaryMapSinkBase = 10f;
		private const float secondaryMapSinkFactor = .1f;
		private const float secondaryMapHideDelay = 3f;
		private float secondaryMapHideTimer;

		const float horizontalRefreshThreshold = 500;
		const float horizontalRefreshThresholdAltitudeFactor = 1f;

		private readonly Dictionary<float, int> altitudeZooms = new Dictionary<float, int> () {
			{ 0, 15 },
			{ 400, 12 },
			{ 2000, 9 },
			{ 4000, 7 },
		};
		private float lastZoomChangeAlt;
		const int minZoomChangeAltDelta = 200;

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
			if (maps.Length != 2 || !maps[0] || !maps[1]) {
				Debug.LogWarning ("invalid maps, disabling component");
				enabled = false;
				return;
			}
			if (maps[0].enabled || maps[1].enabled) {
				Debug.LogWarning ("maps should initially be disabled");
			}
			if (maps[1].gameObject.activeSelf) {
				Debug.LogWarning ("second map gameobject should initially be deactivated");
			}
			mapInitScale = maps[0].transform.localScale;
		}

		void Update () {
			ApplyTimeDataOnTransform (aircraftTransform);
			if (secondaryMapHideTimer > 0) {
				secondaryMapHideTimer -= Time.deltaTime;
				if (secondaryMapHideTimer <= 0) {
					// hide previous map
					maps[1 - visibleMapIndex].gameObject.SetActive (false);

					// bring up current map
					var pos = maps[visibleMapIndex].transform.position;
					pos.y = 0f;
					maps[visibleMapIndex].transform.position = pos;
				}
			}
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
			if (snapshots.Count > 100) Debug.LogWarning("too many snapshots being preserved: " +  snapshots.Count);

			// add snapshot with new info
			var attitude = Quaternion.Euler (-pitch, yaw, -roll);
			snapshots.Add (new Snapshot (lat, lon, alt, vn, ve, vd, attitude, yawRate, pitchRate, rollRate, simTime));

			// initialize map if this is first received info
			var map = maps[visibleMapIndex];
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

			var curMap = maps[visibleMapIndex];
			if (!curMap.enabled || snapshots.Count == 0) {
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

			// apply map zoom
			var alt = (float)result.alt;
			if (Mathf.Abs (lastZoomChangeAlt - alt) > minZoomChangeAltDelta) {
				var zoom = GetZoomForAltitude (alt);
				if (zoom > 0 && zoom != Mathf.RoundToInt (curMap.Zoom)) {
					lastZoomChangeAlt = alt;
					SwapMapToZoom (zoom, alt);
				}
			}

			// apply map position
			var horThres = horizontalRefreshThreshold + horizontalRefreshThresholdAltitudeFactor * Mathf.Max (0, alt);
			var pos = curMap.GeoToWorldPosition (new Vector2d (result.lat, result.lon), false);
			if (Mathf.Abs (pos.x) > horThres ||
				Mathf.Abs (pos.z) > horThres) {

				foreach (var map in maps) {
					if (!map.enabled) continue;
					map.UpdateMap (new Vector2d (result.lat, result.lon));
					map.transform.localScale = mapInitScale;
				}
				pos = curMap.GeoToWorldPosition (new Vector2d (result.lat, result.lon), false);
			}
			pos.y = alt;

			// rotation
			var rot = result.attitude;

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
			float bestAlt = -1f;
			int bestZoom = -1;
			foreach (var pair in altitudeZooms) {
				if (pair.Key > altitude) continue;
				if (pair.Key > bestAlt || bestZoom == -1) {
					bestAlt = pair.Key;
					bestZoom = pair.Value;
				}
			}
			return bestZoom;
		}

		private void SwapMapToZoom (int zoom, float alt) {
			Debug.Log ("swap to zoom " + zoom);
			var previousMap = maps[visibleMapIndex];
			visibleMapIndex = 1 - visibleMapIndex;
			var currentMap = maps[visibleMapIndex];

			// sink current map depending on altitude
			var pos = currentMap.transform.position;
			pos.y = -secondaryMapSinkBase - secondaryMapSinkFactor * alt;
			currentMap.transform.position = pos;

			secondaryMapHideTimer = secondaryMapHideDelay;

			// initialize current map using previous map location
			if (!currentMap.enabled) {
				var options = currentMap.Options;
				options.locationOptions.latitudeLongitude = string.Format ("{0:0.000000}, {1:0.000000}",
					previousMap.CenterLatitudeLongitude.x,
					previousMap.CenterLatitudeLongitude.y);
				options.locationOptions.zoom = zoom;
				currentMap.Options = options;
				currentMap.enabled = true;
				currentMap.gameObject.SetActive (true);
			}
			else {
				currentMap.gameObject.SetActive (true);
				currentMap.UpdateMap (previousMap.CenterLatitudeLongitude, zoom);
				currentMap.transform.localScale = mapInitScale;
			}
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
				Quaternion.Slerp (s1.attitude, s2.attitude, alpha),
				// don't interpolate spin rates as there may be edge cases with 360-to-0 wrap
				s1.yawRate,
				s1.pitchRate,
				s1.rollRate,
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
		public Quaternion attitude;
		public float yawRate;
		public float pitchRate;
		public float rollRate;
		public double simTime;

		public Snapshot (double lat, double lon, double alt,
			float vn, float ve, float vd,
			Quaternion attitude,
			float yawRate, float pitchRate, float rollRate,
			double simTime) {
			this.lat = lat;
			this.lon = lon;
			this.alt = alt;
			this.vn = vn;
			this.ve = ve;
			this.vd = vd;
			this.attitude = attitude;
			this.yawRate = yawRate;
			this.pitchRate = pitchRate;
			this.rollRate = rollRate;
			this.simTime = simTime;
		}
	}
}