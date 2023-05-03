using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
	public class InfoUI : MonoBehaviour {

		private Text hintText;
		[SerializeField]
		private Color successColor = Color.white;
		private Vector3 initialScale;

		[SerializeField]
		private FlightGearNetworking networking;

		private bool hasInvalidData = false;
		private float hideTimer;
		private const float hideDelay = 3f;

		private void Start () {

			if (!networking) {
				Debug.LogWarning ("No networking defined");
				enabled = false;
				return;
			}

			initialScale = transform.localScale;

			hintText = GetComponentInChildren<Text> ();
			hintText.text += "\nLocal port: " + networking.PortToReceive + ", local address:";

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
		}

		private void Update () {
			if (!hasInvalidData) {
				if (networking.HasReceivedAnyData && !networking.HasReceivedValidData) {
					hasInvalidData = true;
					hintText.color = Color.yellow;
					hintText.text += "\nGetting invalid data";
				}
			}

			if (hideTimer <= 0) {
				if (networking.HasReceivedValidData) {
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
	}
}