using Assets.Scripts.Controls;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts {
	public class FlightGearNetworking : MonoBehaviour {
		public static FlightGearNetworking Instance { get; private set; }

		[SerializeField]
		private ushort portToSend = 5500;
		[SerializeField]
		private ushort portToReceive = 5501;
		[SerializeField]
		private ushort portToReplyBroker = 5502;
		[SerializeField]
		private float sendRate = 1f / 25f;
		[SerializeField]
		private float sendHandsRate = 1f / 20f;

		private bool? isMaster;
		public bool? IsMaster => isMaster;
		private EndPoint peerEndpoint = null;

		UdpClient listener;

		Socket socket;
		IPEndPoint targetEndpoint = null;

		private string receivedData = null;
		private byte[] receivedHandsData = null;
		private int packetsReceived = 0;
		[SerializeField]
		private int anyPacketsReceived = 0;
		private const int minPacketsToStartSending = 10;
		private bool hasReceivedValidData = false;
		private bool hasReceivedBrokerHandshake = false;

		[SerializeField]
		private FlightControls controls;
		public FlightControls Controls => controls;

		[SerializeField]
		private HandDataIO handDataIO;
		public HandDataIO HandDataIO => handDataIO;

		private GeoPositioning geo;

		public ushort PortToReceive => portToReceive;

		public bool HasReceivedAnyData => packetsReceived > 0;
		public bool HasReceivedValidData => hasReceivedValidData;

		public SerializedControlInputs.Input PeerInput { get; private set; }
		private float timerToResetPeerInput = 0f;
		private const float resetPeerInputDelay = 1f;

		public UnityEvent OnConnectionReady = new UnityEvent ();
		private bool isConnectionReady = false;
		public static bool IsConnectionReady => Instance.isConnectionReady;
		private bool triggerConnectionReady = false;

		private void Awake () {
			Instance = this;
		}

		void Start () {
			geo = GetComponent<GeoPositioning> ();
			if (!geo) {
				Debug.LogError ("FlightGearNetworking requires a GeoPositioning component on the same GameObject");
			}

			if (!controls) {
				Debug.LogError ("FlightGearNetworking requires a FlightControls component");
			}
			if (!handDataIO) {
				Debug.LogError ("FlightGearNetworking requires a HandDataIO component");
			}

			listener = new UdpClient (portToReceive, AddressFamily.InterNetwork);
			var thread = new Thread (Receive);
			thread.Start ();

			socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			InvokeRepeating (nameof (SendOutputs), 0f, sendRate);
			InvokeRepeating (nameof (SendHandsOutputs), 0f, sendHandsRate);
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
			if (receivedHandsData != null) {
				handDataIO.Receive (receivedHandsData);
				receivedHandsData = null;
			}
			if (timerToResetPeerInput > 0f) {
				timerToResetPeerInput -= Time.deltaTime;
				if (timerToResetPeerInput <= 0f) {
					PeerInput = default;
				}
			}
			if (triggerConnectionReady) {
				triggerConnectionReady = false;
				OnConnectionReady.Invoke ();
			}
		}

		private void Receive () {
			try {
				while (true) {
					var groupEP = new IPEndPoint (IPAddress.Any, portToReceive);
					byte[] bytes = listener.Receive (ref groupEP);
					anyPacketsReceived++;
					// check if message is VR hands data
					if (handDataIO.IsValidData (bytes)) {
						receivedHandsData = bytes;
						// if no peer endpoint, set it to the sender address
						if (peerEndpoint == null) {
							peerEndpoint = new IPEndPoint (groupEP.Address, portToReceive);
							Debug.LogWarning ("Begin sending VR hands data to " + peerEndpoint);
						}
						continue;
					}

					if (isMaster == null) continue;

					var readout = Encoding.ASCII.GetString (bytes, 0, bytes.Length);
					if (!string.IsNullOrEmpty (readout)) {
						// check if this is a connection broker handshake message
						if (readout == "connection_broker") {
							// reply with master/slave message
							byte[] sendbuf = Encoding.ASCII.GetBytes (isMaster == true ? "master" : "slave");
							socket.SendTo (sendbuf, new IPEndPoint (groupEP.Address, portToReplyBroker));
							hasReceivedBrokerHandshake = true;
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
							Debug.Log ("Begin sending FlightGear outputs to " + targetEndpoint);
							if (!isConnectionReady) triggerConnectionReady = true;
							isConnectionReady = true;

							// read peer address (override port with portToReceive)
							var peerAddrPort = isMaster == true ? clients?.slave : clients?.master;
							if (string.IsNullOrEmpty (peerAddrPort)) {
								Debug.LogWarning ("No peer address received");
								continue;
							}
							addrPort = peerAddrPort.Split (':');
							if (addrPort.Length != 1 && addrPort.Length != 2) {
								Debug.LogWarning ("Invalid peer address received");
								continue;
							}
							peerEndpoint = new IPEndPoint (IPAddress.Parse (addrPort[0]), portToReceive);
							Debug.Log ("Connected to peer " + peerEndpoint);
							continue;
						}

						receivedData = readout;

						// if no FlightGear address has been received yet, use the sender address
						// this is a fallback for the case that the connection broker is not used
						// TODO: remove this fallback once the connection broker is used by default
						if (targetEndpoint == null) {
							var lastReceivedAddress = groupEP.Address;
							targetEndpoint = new IPEndPoint (lastReceivedAddress, portToSend);
							Debug.LogWarning ("Begin sending FlightGear outputs to " + targetEndpoint);
							isConnectionReady = true;
							triggerConnectionReady = true;
						}
					}
				}
			}
			catch (SocketException e) {
				Debug.LogWarning (e);
			}
		}

		private void SendOutputs () {
			if (isMaster != true || targetEndpoint == null || !hasReceivedValidData || packetsReceived < minPacketsToStartSending) return;

			string msg = FormulateMessage ();
			byte[] sendbuf = Encoding.ASCII.GetBytes (msg);
			socket.SendTo (sendbuf, targetEndpoint);
		}
		
		private void SendHandsOutputs () {
			if (handDataIO) handDataIO.Transmit (socket, peerEndpoint);
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
				float.TryParse (parts[29], out var afterburner) &&
				int.TryParse (parts[30], out var leftGear) &&
				int.TryParse (parts[31], out var noseGear) &&
				int.TryParse (parts[32], out var rightGear) &&
				int.TryParse (parts[33], out var gearRed) &&
				float.TryParse (parts[34], out var n1) &&
				float.TryParse (parts[35], out var fuelInternal) &&
				float.TryParse (parts[36], out var fuelTotal) &&
				float.TryParse (parts[37], out var voltsDC) &&
				float.TryParse (parts[38], out var fuelBingo)) {

				var localTimeString = parts[39]; // hh:mm:ss
                System.TimeSpan	timeOfDay = System.TimeSpan.Parse(localTimeString);

                hasReceivedValidData = true;
				var attitude = Quaternion.Euler (-pitch, yaw, -roll);
				geo.AddSnapshot (new Snapshot (
					lat, lon, altitude, vn, ve, vd, attitude, yawRate, pitchRate, rollRate, simTime,
					magHeading, apHeading, displayHeading, alpha, beta, gLoad, airspeed, mach, groundSpeed, verticalSpeed,
					indicatedAltitude, radarAltitude, gearPos, weightOnWheels > 0, apTargetSpeed, acceleration, afterburner,
					leftGear > 0, noseGear > 0, rightGear > 0, gearRed > 0, n1, fuelInternal, fuelTotal, voltsDC, fuelBingo,
					timeOfDay));
			}
		}

		private string FormulateMessage () {
			var stickCombined = controls.Stick.GetCombinedOutput ();
			var brakeCombined = controls.Brake.GetCombinedOutput ();
			return string.Format ("{0:0.000}\t{1:0.000}\t{2:0.000}\t{3:0.000}\t{4}\t{5:0.00}\t{6:0.00}\t{7:0.00}\t{8:0.00}\n",
				stickCombined.x,
				stickCombined.y,
				controls.Throttle.Output,
				controls.Rudder.GetCombinedOutput (),
				controls.GearDown ? 1 : 0,
				0, // parking brake
				brakeCombined, // parking left
				brakeCombined, // parking right
				0 // canopy (0 = fully closed, 1 = fully open)
				); ;
		}

		public void SetIsMaster (bool? isMaster) {
			if (hasReceivedBrokerHandshake) return;
			this.isMaster = isMaster;
		}

		public void UpdatePeerInput (SerializedControlInputs.Input input) {
			PeerInput = input;
			timerToResetPeerInput = resetPeerInputDelay;
			if (!controls.Throttle.IsGrabbed && (isMaster == false || input.ThrottleIsGrabbed)) {
				controls.Throttle.SetOutput (input.Throttle);
			}
		}

		private class Clients {
			public string master;
			public string slave;
			public string flightGear;
		}
	}
}