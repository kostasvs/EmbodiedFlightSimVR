using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
	public class SimControlUI : MonoBehaviour {

		private Camera mainCamera;

		[SerializeField]
		private GameObject simPanel;
		private CanvasGroup simPanelCG;
		[SerializeField]
		private float appearDistance = .7f;
		[SerializeField]
		private float disappearDistance = 1f;
		private float curAlpha;
		private const float curAlphaMultiplier = 2f;
		private const float fadeSpeed = 1f;

		[SerializeField]
		private TextMeshProUGUI timeText;
		[SerializeField]
		private float timeWarpSpeed = 50f;
		private int targetHour = -1;
		[SerializeField]
		private int timeWarpHourStep = 4;
		[SerializeField]
		private Image hourglass;
		[SerializeField]
		private float hourglassRotSpeed = .6f;

		[SerializeField]
		private TextMeshProUGUI speedText;
		[SerializeField]
		private Image playBtnImage;
		private Color playColor;
		[SerializeField]
		private Color pauseColor = Color.red;

		void Awake () {
			mainCamera = Camera.main;
			simPanelCG = simPanel.GetComponent<CanvasGroup> ();
			simPanelCG.alpha = curAlpha;
			playColor = playBtnImage.color;
		}

		void Update () {
			// show/hide panel
			var distanceSqr = (mainCamera.transform.position - simPanel.transform.position).sqrMagnitude;
			var limit = simPanel.activeSelf ? disappearDistance : appearDistance;
			var inRange = distanceSqr < limit * limit && FlightGearNetworking.IsConnectionReady && FlightGearNetworking.Instance.IsMaster == true;
			curAlpha = Mathf.MoveTowards (curAlpha, inRange ? 1 : 0, Time.deltaTime * fadeSpeed);
			simPanelCG.alpha = Mathf.Clamp01 (curAlpha * curAlphaMultiplier);
			var enableGO = simPanelCG.alpha > 0;
			if (simPanel.activeSelf != enableGO) {
				simPanel.SetActive (enableGO);
			}

			// update time
			System.TimeSpan time = GeoPositioning.CurSnapshot.localTime;
			timeText.text = time.ToString ("hh':'mm':'ss");

			// stop time warp when target time is reached
			if (targetHour >= 0) {
				hourglass.transform.localEulerAngles = new Vector3 (0, 0, -360 * Time.time * hourglassRotSpeed * Mathf.Sign (FlightGearNetworking.TimeWarp));
				if (time.Hours == targetHour) {
					targetHour = -1;
					hourglass.enabled = false;
					FlightGearNetworking.SetTimeWarp (0);
				}
			}

			// debug keyboard shortcuts
			if (Input.GetKeyDown (KeyCode.Alpha1)) {
				AddTimeWarp (-1);
			} else if (Input.GetKeyDown (KeyCode.Alpha2)) {
				AddTimeWarp (1);
			} else if (Input.GetKeyDown (KeyCode.Alpha3)) {
				AddSimSpeed (-1);
			} else if (Input.GetKeyDown (KeyCode.Alpha4)) {
				AddSimSpeed (1);
			} else if (Input.GetKeyDown (KeyCode.Alpha5)) {
				ToggleSimPause ();
			}
		}

		public void AddTimeWarp (int hours) {
			if (hours == 0) {
				return;
			}
			if (targetHour < 0) {
				targetHour = GeoPositioning.CurSnapshot.localTime.Hours;
			}
			targetHour = (targetHour + hours * timeWarpHourStep) % 24;
			if (targetHour < 0) {
				targetHour += 24;
			}
			hourglass.enabled = true;
			var time = GeoPositioning.CurSnapshot.localTime;
			var diff = targetHour - time.Hours;
			if (diff < -12) {
				diff += 24;
			} else if (diff > 12) {
				diff -= 24;
			}
			FlightGearNetworking.SetTimeWarp (Mathf.Sign (diff) * timeWarpSpeed);
		}

		public void AddSimSpeed (float speed) {
			FlightGearNetworking.SetSimSpeed (Mathf.Round (FlightGearNetworking.SimSpeed + speed));
			UpdateSimSpeedIndicators ();
		}

		public void ToggleSimPause () {
			FlightGearNetworking.SetSimSpeed (FlightGearNetworking.SimSpeed == 0 ? 1 : 0);
			UpdateSimSpeedIndicators ();
		}

		void UpdateSimSpeedIndicators () {
			playBtnImage.color = FlightGearNetworking.SimSpeed == 0 ? pauseColor : playColor;
			speedText.text = Mathf.RoundToInt (FlightGearNetworking.SimSpeed) + "x";
		}

		public void RestartScene () {
			UnityEngine.SceneManagement.SceneManager.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name);
		}
	}
}