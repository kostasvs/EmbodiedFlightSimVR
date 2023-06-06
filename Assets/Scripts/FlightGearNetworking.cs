using Assets.Scripts.Controls;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Scripts {
	public class FlightGearNetworking : MonoBehaviour {

		[SerializeField]
		private ushort portToSend = 5500;
		[SerializeField]
		private ushort portToReceive = 5501;
		[SerializeField]
		private float sendRate = 1f / 25f;

		UdpClient listener;
		IPEndPoint groupEP;

		Socket socket;
		IPEndPoint targetEndpoint = null;
		IPAddress lastReceivedAddress = null;

		private string receivedData = null;
		private bool hasReceivedAnyData = false;
		private bool hasReceivedValidData = false;

		[SerializeField]
		private FlightControls controls;

		private GeoPositioning geo;

		public ushort PortToReceive => portToReceive;

		public bool HasReceivedAnyData => hasReceivedAnyData;
		public bool HasReceivedValidData => hasReceivedValidData;

		void Start () {
			geo = GetComponent<GeoPositioning> ();

			listener = new UdpClient (portToReceive);
			groupEP = new IPEndPoint (IPAddress.Any, portToReceive);
			var thread = new Thread (Receive);
			thread.Start ();

			socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			InvokeRepeating (nameof (Send), 0f, sendRate);
		}

		private void OnDestroy () {
			listener.Close ();
			socket.Close ();
		}

		private void Update () {
			if (receivedData != null) {
				hasReceivedAnyData = true;
				InterpretMessage (receivedData.Split ('\t'));
				receivedData = null;
			}
		}

		private void Receive () {
			try {
				while (true) {
					byte[] bytes = listener.Receive (ref groupEP);
					var readout = Encoding.ASCII.GetString (bytes, 0, bytes.Length);
					if (!string.IsNullOrEmpty (readout)) {
						receivedData = readout;
						if (lastReceivedAddress == null) {
							lastReceivedAddress = groupEP.Address;
							Debug.Log ("Begin sending data to " + lastReceivedAddress.ToString ());
							targetEndpoint = new IPEndPoint (lastReceivedAddress, portToSend);
						}
					}
				}
			}
			catch (SocketException e) {
				Debug.LogWarning (e);
			}
		}

		private void Send () {
			if (targetEndpoint == null) return;

			string msg = FormulateMessage ();
			byte[] sendbuf = Encoding.ASCII.GetBytes (msg);
			socket.SendTo (sendbuf, targetEndpoint);
		}

		private void InterpretMessage (string[] parts) {
			if (parts.Length == 0) return;

			if (double.TryParse (parts[0], out var lat) &&
				double.TryParse (parts[1], out var lon) &&
				double.TryParse (parts[2], out var altitude) &&
				float.TryParse (parts[3], out var vn) &&
				float.TryParse (parts[4], out var ve) &&
				float.TryParse (parts[5], out var vd) &&
				float.TryParse (parts[6], out var yaw) &&
				float.TryParse (parts[7], out var pitch) &&
				float.TryParse (parts[8], out var roll) &&
				float.TryParse (parts[9], out var yawRate) &&
				float.TryParse (parts[10], out var pitchRate) &&
				float.TryParse (parts[11], out var rollRate) &&
				double.TryParse (parts[12], out var simTime)) {

				hasReceivedValidData = true;
				geo.AddSnapshot (lat, lon, altitude, vn, ve, vd, yaw, pitch, roll, yawRate, pitchRate, rollRate, simTime);
			}
		}

		private string FormulateMessage () {
			return string.Format ("{0:0.000}\t{1:0.000}\t{2:0.000}\t{3:0.000}\t{4}\n",
				controls.Stick.Output.x,
				controls.Stick.Output.y,
				controls.Throttle.Output,
				controls.Rudder.Output,
				controls.GearDown ? 1 : 0,
				0, // parking brake
				0, // parking left
				0, // parking right
				0 // canopy
				);
		}
	}
}