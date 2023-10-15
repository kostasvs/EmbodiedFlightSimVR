﻿using Assets.Scripts.Controls;
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

		[SerializeField]
		private bool isMaster = true;
		private EndPoint peerEndpoint = null;

		UdpClient listener;
		IPEndPoint groupEP;

		Socket socket;
		IPEndPoint targetEndpoint = null;

		private string receivedData = null;
		private int packetsReceived = 0;
		private const int minPacketsToStartSending = 10;
		private bool hasReceivedValidData = false;

		[SerializeField]
		private FlightControls controls;

		private GeoPositioning geo;

		public ushort PortToReceive => portToReceive;

		public bool HasReceivedAnyData => packetsReceived > 0;
		public bool HasReceivedValidData => hasReceivedValidData;

		void Start () {
			geo = GetComponent<GeoPositioning> ();

			listener = new UdpClient (portToReceive);
			groupEP = new IPEndPoint (IPAddress.Any, portToReceive);
			var thread = new Thread (Receive);
			thread.Start ();

			socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			InvokeRepeating (nameof (SendOutputs), 0f, sendRate);
		}

		private void OnDestroy () {
			listener.Close ();
			socket.Close ();
		}

		private void Update () {
			if (receivedData != null) {
				packetsReceived++;
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
						// check if this is a connection broker handshake message
						if (readout == "connection_broker") {
							// reply with master/slave message
							byte[] sendbuf = Encoding.ASCII.GetBytes (isMaster ? "master" : "slave");
							socket.SendTo (sendbuf, groupEP);
							continue;
						}

						// check if this is a handshake follow-up message
						if (readout.StartsWith ("clients: ")) {
							// parse the json payload
							var json_payload = readout.Substring (9);
							var clients = JsonUtility.FromJson<Clients> (json_payload);
							string[] addrPort = null;

							// read flightGear address if available, otherwise use the sender address
							var flightGearAddress = groupEP.Address;
							if (!string.IsNullOrEmpty (clients?.flightGear)) {
								addrPort = clients.flightGear.Split (':');
								if (addrPort.Length != 2) {
									Debug.LogWarning ("Invalid flightGear address received");
								}
								else {
									flightGearAddress = IPAddress.Parse (addrPort[0]);
									portToSend = ushort.Parse (addrPort[1]);
								}
							}
							targetEndpoint = new IPEndPoint (flightGearAddress, portToSend);
							Debug.Log ("Begin sending FlightGear outputs to " + targetEndpoint.ToString ());

							// read peer address
							var peerAddrPort = isMaster ? clients?.slave : clients?.master;
							if (string.IsNullOrEmpty (peerAddrPort)) {
								Debug.LogWarning ("No peer address received");
								continue;
							}
							addrPort = peerAddrPort.Split (':');
							if (addrPort.Length != 2) {
								Debug.LogWarning ("Invalid peer address received");
								continue;
							}
							peerEndpoint = new IPEndPoint (IPAddress.Parse (addrPort[0]), ushort.Parse (addrPort[1]));
							Debug.Log ("Connected to peer " + peerEndpoint.ToString ());
							continue;
						}

						receivedData = readout;
					}
				}
			}
			catch (SocketException e) {
				Debug.LogWarning (e);
			}
		}

		private void SendOutputs () {
			if (targetEndpoint == null || !hasReceivedValidData || packetsReceived < minPacketsToStartSending) return;

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
				double.TryParse (parts[12], out var simTime) &&
				float.TryParse (parts[13], out var magHeading) &&
				float.TryParse (parts[14], out var apHeading) &&
				float.TryParse (parts[15], out var displayHeading) &&
				float.TryParse (parts[16], out var alpha) &&
				float.TryParse (parts[17], out var beta) &&
				float.TryParse (parts[18], out var gLoad) &&
				float.TryParse (parts[19], out var airspeed) &&
				float.TryParse (parts[20], out var mach) &&
				float.TryParse (parts[21], out var groundSpeed) &&
				float.TryParse (parts[22], out var verticalSpeed) &&
				float.TryParse (parts[23], out var indicatedAltitude) &&
				float.TryParse (parts[24], out var radarAltitude) &&
				float.TryParse (parts[25], out var gearPos) &&
				float.TryParse (parts[26], out var weightOnWheels) &&
				float.TryParse (parts[27], out var apTargetSpeed) &&
				float.TryParse (parts[28], out var acceleration) &&
				float.TryParse (parts[29], out var afterburner)) {

				hasReceivedValidData = true;
				var attitude = Quaternion.Euler (-pitch, yaw, -roll);
				geo.AddSnapshot (new Snapshot (
					lat, lon, altitude, vn, ve, vd, attitude, yawRate, pitchRate, rollRate, simTime,
					magHeading, apHeading, displayHeading, alpha, beta, gLoad, airspeed, mach, groundSpeed, verticalSpeed,
					indicatedAltitude, radarAltitude, gearPos, weightOnWheels > 0, apTargetSpeed, acceleration, afterburner));
			}
		}

		private string FormulateMessage () {
			return string.Format ("{0:0.000}\t{1:0.000}\t{2:0.000}\t{3:0.000}\t{4}\t{5:0.00}\t{6:0.00}\t{7:0.00}\t{8:0.00}\n",
				controls.Stick.Output.x,
				controls.Stick.Output.y,
				controls.Throttle.Output,
				controls.Rudder.Output,
				controls.GearDown ? 1 : 0,
				0, // parking brake
				controls.Brake.Output, // parking left
				controls.Brake.Output, // parking right
				0 // canopy (0 = fully closed, 1 = fully open)
				);
		}

		private class Clients {
			public string master;
			public string slave;
			public string flightGear;
		}
	}
}