using Mapbox.Unity.Map;
using Mapbox.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {
	public class GeoPositioning : MonoBehaviour {
		private static GeoPositioning instance;

		[SerializeField]
		private AbstractMap[] maps = new AbstractMap[2];
		private int visibleMapIndex;
		private Vector3 mapInitScale;
		private Transform mapParent;

		private const float secondaryMapSinkBase = 10f;
		private const float secondaryMapSinkFactor = .1f;
		private const float secondaryMapHideDelay = 3f;
		private float secondaryMapHideTimer;

		private const float horizontalRefreshThreshold = 500;
		private const float horizontalRefreshThresholdAltitudeFactor = 1f;

		//private const float maxMapAltitude = 4000;
		private const float minHeightAboveGround = 2;
		private bool isGrounded = true;

		private readonly Dictionary<float, int> altitudeZooms = new Dictionary<float, int> () {
			{ 0, 15 },
			{ 400, 12 },
			{ 3000, 10 },
		};
		const int baseZoomAfterTakeoff = 12;
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

		private const float lastSnapshotMaxDelay = 1f;
		private float lastSnapshotTime = -lastSnapshotMaxDelay;

		private Snapshot curSnapshot;
		public static Snapshot CurSnapshot => instance.curSnapshot;

		private void Awake () {
			instance = this;
		}

		void Start () {
			if (maps.Length != 2 || !maps[0] || !maps[1]) {
				Debug.LogWarning ("invalid maps, disabling component");
				enabled = false;
				return;
			}
			mapParent = maps[0].transform.parent;
			if (!mapParent) {
				Debug.LogWarning ("no maps parent");
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
			if (lastSnapshotTime < Time.time - lastSnapshotMaxDelay) return;

			curSnapshot = GetCurSnapshotRealtime ();
			ApplyTimeDataOnTransform (aircraftTransform);
			if (secondaryMapHideTimer > 0) {
				secondaryMapHideTimer -= Time.deltaTime;
				if (secondaryMapHideTimer <= 0) {
					// hide previous map
					maps[1 - visibleMapIndex].gameObject.SetActive (false);

					// bring up current map
					var pos = maps[visibleMapIndex].transform.localPosition;
					pos.y = 0f;
					maps[visibleMapIndex].transform.localPosition = pos;
				}
			}
		}

		public double GetCurSimTime () => Time.timeAsDouble + curSimTimeOffset;

		public void AddSnapshot (Snapshot snapshot) {

			// match local time to simulation time if they are repeatedly too far apart
			if (Mathf.Abs ((float)(GetCurSimTime () - snapshot.simTime - simLatency)) > maxSimTimeOffset) {
				desyncCounts++;
				if (desyncCounts >= maxDesyncCounts) {
					desyncCounts = 0;
					curSimTimeOffset = snapshot.simTime - Time.timeAsDouble - simLatency;
					Debug.Log ("Resetting time to " + GetCurSimTime ());
				}
			}
			else {
				desyncCounts = 0;
				RemoveStaleSnapshots ();
			}
			if (snapshots.Count > 100) Debug.LogWarning ("too many snapshots being preserved: " + snapshots.Count);

			// add snapshot with new info
			snapshots.Add (snapshot);

			// initialize map if this is first received info
			var map = maps[visibleMapIndex];
			if (!map.enabled) {
				var options = map.Options;
				options.locationOptions.latitudeLongitude = string.Format ("{0:0.000000}, {1:0.000000}", snapshot.lat, snapshot.lon);
				map.Options = options;
				map.enabled = true;
			}

			lastSnapshotTime = Time.time;
		}

		private void RemoveStaleSnapshots () {
			for (int i = snapshots.Count - 1; i >= 0; i--) {
				var s = snapshots[i];
				if (Mathf.Abs ((float)(GetCurSimTime () - s.simTime)) > maxSnapshotTimeOffset) {
					snapshots.RemoveAt (i);
				}
			}
		}

		private Snapshot GetCurSnapshotRealtime () {
			if (!instance) return default;

			var curTime = instance.GetCurSimTime ();
			for (int i = instance.snapshots.Count - 1; i >= 0; i--) {
				var s = instance.snapshots[i];
				if (s.simTime <= curTime) return s;
			}
			return instance.snapshots.Count > 0 ? instance.snapshots[0] : default;
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

			// get interpolated result
			Snapshot result = GetCurTimeSnapshot ();

			// apply map zoom
			var alt = (float)result.alt;
			if (Mathf.Abs (lastZoomChangeAlt - alt) > minZoomChangeAltDelta) {
				var zoom = GetZoomForAltitude (alt);
				if (zoom > 0 && zoom != Mathf.RoundToInt (curMap.Zoom)) {
					// prevent max zoom level from being reached again after takeoff (can cause trouble while landing)
					if (lastZoomChangeAlt == 0) altitudeZooms[0] = baseZoomAfterTakeoff;

					lastZoomChangeAlt = alt;
					SwapMapToZoom (zoom, alt);
				}
			}

			// apply map position
			var horThres = horizontalRefreshThreshold + horizontalRefreshThresholdAltitudeFactor * Mathf.Max (0, alt);
			var pos = curMap.GeoToWorldPosition (new Vector2d (result.lat, result.lon), true) - mapParent.transform.position;
			if (Mathf.Abs (pos.x) > horThres ||
				Mathf.Abs (pos.z) > horThres) {

				foreach (var map in maps) {
					if (!map.enabled) continue;
					map.UpdateMap (new Vector2d (result.lat, result.lon));
					map.transform.localScale = mapInitScale;
				}
				pos = curMap.GeoToWorldPosition (new Vector2d (result.lat, result.lon), true) - mapParent.transform.position;
			}

			// set position y to altitude with a minimum height above ground
			var groundY = pos.y + minHeightAboveGround;
			pos.y = Mathf.Max (alt, groundY);
			var prevGrounded = isGrounded;
			isGrounded = pos.y == groundY;
			if (isGrounded && !prevGrounded) OVRInputWrapper.VibratePulseMed (-1);

			// rotation
			var rot = result.attitude;

			// extrapolate if needed
			var velocity = new Vector3 (result.ve, -result.vd, result.vn);
			var spin = new Vector3 (-result.pitchRate, result.yawRate, -result.rollRate);
			var dt = (float)(GetCurSimTime () - result.simTime);
			pos += velocity * dt;
			rot = Quaternion.Euler (spin * dt) * rot;

			// limit max visualized altitude to prevent z-depth issues
			//pos.y = Mathf.Min (pos.y, maxMapAltitude);

			// apply result rotation to aircraft
			target.rotation = rot;

			// apply reversed result position to map
			// (normally we would move the aircraft and keep the map stationary,
			// but the VR controls become unstable at high speeds)
			mapParent.transform.position = -pos;

			// set sun angle by time and location
			DateTimeSunAngle.SetLocation ((float)result.lon, (float)result.lat);
			DateTimeSunAngle.SetTime (result.localTime);
		}

		private Snapshot GetCurTimeSnapshot () {
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
			if (nearestBefore == -1) return snapshots[nearestAfter];
			if (nearestAfter == -1) return snapshots[nearestBefore];
			return Lerp (snapshots[nearestBefore], snapshots[nearestAfter], GetCurSimTime ());
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
			var pos = currentMap.transform.localPosition;
			pos.y = -secondaryMapSinkBase - secondaryMapSinkFactor * alt;
			currentMap.transform.localPosition = pos;

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
			if (simTime <= s1.simTime) return s1;
			if (simTime >= s2.simTime) return s2;

			float alpha = (float)((simTime - s1.simTime) / (s2.simTime - s1.simTime));
			s1.lat += (s2.lat - s1.lat) * alpha;
			s1.lon += (s2.lon - s1.lon) * alpha;
			s1.alt += (s2.alt - s1.alt) * alpha;
			s1.vn += (s2.vn - s1.vn) * alpha;
			s1.ve += (s2.ve - s1.ve) * alpha;
			s1.vd += (s2.vd - s1.vd) * alpha;
			s1.attitude = Quaternion.Slerp (s1.attitude, s2.attitude, alpha);
			s1.simTime = simTime;
			return s1;
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
		public float magHeading;
		public float apHeading;
		public float displayHeading;
		public float alpha;
		public float beta;
		public float gLoad;
		public float airspeed;
		public float mach;
		public float groundSpeed;
		public float verticalSpeed;
		public float indicatedAltitude;
		public float radarAltitude;
		public float gearPos;
		public bool weightOnWheels;
		public float apTargetSpeed;
		public float acceleration;
		public float afterburner;
		public bool leftGear;
		public bool noseGear;
		public bool rightGear;
		public bool gearRed;
		public float n1;
		public float fuelInternal;
		public float fuelTotal;
		public float voltsDC;
		public float fuelBingo;
		public System.TimeSpan localTime;

		public Snapshot (double lat, double lon, double alt, float vn, float ve, float vd, Quaternion attitude, float yawRate, float pitchRate, float rollRate, double simTime, float magHeading, float apHeading, float displayHeading, float alpha, float beta, float gLoad, float airspeed, float mach, float groundSpeed, float verticalSpeed, float indicatedAltitude, float radarAltitude, float gearPos, bool weightOnWheels, float apTargetSpeed, float acceleration, float afterburner, bool leftGear, bool noseGear, bool rightGear, bool gearRed, float n1, float fuelInternal, float fuelTotal, float voltsDC, float fuelBingo, System.TimeSpan localTime) {
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
			this.magHeading = magHeading;
			this.apHeading = apHeading;
			this.displayHeading = displayHeading;
			this.alpha = alpha;
			this.beta = beta;
			this.gLoad = gLoad;
			this.airspeed = airspeed;
			this.mach = mach;
			this.groundSpeed = groundSpeed;
			this.verticalSpeed = verticalSpeed;
			this.indicatedAltitude = indicatedAltitude;
			this.radarAltitude = radarAltitude;
			this.gearPos = gearPos;
			this.weightOnWheels = weightOnWheels;
			this.apTargetSpeed = apTargetSpeed;
			this.acceleration = acceleration;
			this.afterburner = afterburner;
			this.leftGear = leftGear;
			this.noseGear = noseGear;
			this.rightGear = rightGear;
			this.gearRed = gearRed;
			this.n1 = n1;
			this.fuelInternal = fuelInternal;
			this.fuelTotal = fuelTotal;
			this.voltsDC = voltsDC;
			this.fuelBingo = fuelBingo;
			this.localTime = localTime;
		}
	}
}