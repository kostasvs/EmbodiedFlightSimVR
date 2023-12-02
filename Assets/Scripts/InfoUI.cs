using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;

namespace Assets.Scripts {
	public class InfoUI : MonoBehaviour {

		private Text hintText;
		[SerializeField]
		private Color successColor = Color.white;
		private Vector3 initialScale;

		private bool hasInvalidData = false;
		private float hideTimer;
		private const float hideDelay = 3f;

		[SerializeField]
		private PokeInteractable btnInstructor;
		[SerializeField]
		private PokeInteractable btnStudent;
		[SerializeField]
		private Color buttonHoverColor = Color.cyan;

		private void Start () {

			if (!FlightGearNetworking.Instance) {
				Debug.LogWarning ("No networking defined");
				enabled = false;
				return;
			}

			initialScale = transform.localScale;

			hintText = GetComponentInChildren<Text> ();
			if (Application.isEditor) {
				hintText.text += " (TAB to toggle)";
			}
			hintText.text += "\nLocal port: " + FlightGearNetworking.Instance.PortToReceive + ", local address:";

			bool foundIP = false;
			var host = Dns.GetHostEntry (Dns.GetHostName ());
			foreach (var ip in host.AddressList) {
				switch (ip.AddressFamily) {
					case AddressFamily.InterNetwork:
					case AddressFamily.InterNetworkV6:
						hintText.text += "\n" + ip.ToString ();
						foundIP = true;
						break;
					default:
						break;
				}
			}
			if (!foundIP) {
				hintText.text += " unknown";
			}

			btnInstructor.GetComponentInChildren<InteractableUnityEventWrapper> ().WhenSelect.AddListener (() => SetIsInstructor (true));
			btnStudent.GetComponentInChildren<InteractableUnityEventWrapper> ().WhenSelect.AddListener (() => SetIsInstructor (false));
		}

		private void Update () {
			if (Application.isEditor) {
				if (Input.GetKeyDown (KeyCode.Tab)) {
					SetIsInstructor (FlightGearNetworking.Instance.IsMaster == false);
					Debug.Log (FlightGearNetworking.Instance.IsMaster == true ? "Instructor" : "Student");
				}
			}

			if (!hasInvalidData) {
				if (FlightGearNetworking.Instance.HasReceivedAnyData && !FlightGearNetworking.Instance.HasReceivedValidData) {
					hasInvalidData = true;
					hintText.color = Color.yellow;
					hintText.text += "\nGetting invalid data";
				}
			}

			if (hideTimer <= 0) {
				if (FlightGearNetworking.Instance.HasReceivedValidData) {
					hideTimer = hideDelay;
					hintText.color = successColor;
					hintText.text = "Connection successful";
				}
			}
			else {
				hideTimer -= Time.deltaTime;
				transform.localScale = initialScale * Mathf.Clamp01 (hideTimer / .5f);
				if (hideTimer <= 0) {
					gameObject.SetActive (false);
				}
			}
		}

		public void SetIsInstructor (bool? isInstructor) {
			hintText.text = "Waiting for connection";
			FlightGearNetworking.Instance.SetIsMaster (isInstructor);
			var newState = FlightGearNetworking.Instance.IsMaster;
			SetButtonActive (btnInstructor, newState == true);
			SetButtonActive (btnStudent, newState == false);
		}

		private void SetButtonActive (PokeInteractable btn, bool isActive) {
			var editor = btn.GetComponentInChildren<MaterialPropertyBlockEditor> ();
			editor.ColorProperties.Clear ();
			if (isActive) editor.ColorProperties.Add (new MaterialPropertyColor { name = "_Color", value = buttonHoverColor });
		}
	}
}