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
		IPEndPoint targetEndpoint;

		[SerializeField]
		private StickControl stickControl;

		[SerializeField]
		private ThrottleControl throttleControl;

		void Start () {
			listener = new UdpClient (portToReceive);
			groupEP = new IPEndPoint (IPAddress.Any, portToReceive);
			var thread = new Thread (Receive);
			thread.Start ();

			socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			var targetAddress = IPAddress.Parse ("127.0.0.1");
			targetEndpoint = new IPEndPoint (targetAddress, portToSend);

			InvokeRepeating (nameof (Send), 0f, sendRate);
		}

		private void OnDestroy () {
			listener.Close ();
			socket.Close ();
		}

		private void Receive () {
			try {
				while (true) {
					byte[] bytes = listener.Receive (ref groupEP);
					var readout = Encoding.ASCII.GetString (bytes, 0, bytes.Length);
					if (!string.IsNullOrEmpty (readout)) {
						InterpretMessage (readout.Split ('\t'));
					}
				}
			}
			catch (SocketException e) {
				Debug.LogWarning (e);
			}
		}

		private void Send () {
			string msg = FormulateMessage ();
			Debug.Log (msg);
			byte[] sendbuf = Encoding.ASCII.GetBytes (msg);
			socket.SendTo (sendbuf, targetEndpoint);
		}

		private void InterpretMessage (string[] parts) {
			if (parts.Length == 0) return;
			// TODO: apply incoming values
		}

		private string FormulateMessage () {
			return string.Format ("{0:0.000}\t{1:0.000}\t{2:0.000}\t{3:0.000}\n",
				stickControl.Output.x,
				stickControl.Output.y,
				throttleControl.Output,
				0f);
		}
	}
}