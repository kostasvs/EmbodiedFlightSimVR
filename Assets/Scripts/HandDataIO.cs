﻿using Oculus.Interaction.Input;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Assets.Scripts {
	public class HandDataIO : MonoBehaviour {

		[SerializeField]
		DataSource<HandDataAsset>[] dataSourcesToSend;
		[SerializeField]
		NetworkHandDataSource[] dataSourcesToReceive;
		[SerializeField]
		GameObject[] networkHandVisuals;
		[SerializeField]
		private Transform rig;

		public const string header = "HandData";

		private bool receivedFirstData;

		public void Transmit (Socket socket, EndPoint targetEndPoint) {
			if (socket == null || targetEndPoint == null) {
				return;
			}

			using (var stream = new System.IO.MemoryStream ()) {
				using (var writer = new System.IO.BinaryWriter (stream)) {
					writer.Write (header.ToCharArray ());
					foreach (var dataSource in dataSourcesToSend) {
						var data = dataSource.GetData ();
						var rootPosition = transform.InverseTransformPoint (data.Root.position);
						var rootRotation = Quaternion.Inverse (transform.rotation) * data.Root.rotation;

						var status = (byte)(
							(data.IsDataValid ? 1 : 0)
							+ (data.IsConnected ? 2 : 0)
							+ (data.IsTracked ? 4 : 0)
							+ (data.IsHighConfidence ? 8 : 0));
						writer.Write (status);
						writer.Write (data.HandScale);

						writer.Write (rootPosition.x);
						writer.Write (rootPosition.y);
						writer.Write (rootPosition.z);

						writer.Write (rootRotation.x);
						writer.Write (rootRotation.y);
						writer.Write (rootRotation.z);
						writer.Write (rootRotation.w);

						foreach (var jointRotation in data.Joints) {
							writer.Write (jointRotation.x);
							writer.Write (jointRotation.y);
							writer.Write (jointRotation.z);
							writer.Write (jointRotation.w);
						}
					}
				}
				socket.SendTo (stream.ToArray (), targetEndPoint);
			}
		}

		public bool IsValidData (byte[] message) {
			if (message == null || message.Length < header.Length) {
				return false;
			}
			var readHeader = Encoding.ASCII.GetString (message, 0, header.Length);
			return readHeader == header;
		}

		public void Receive (byte[] message) {
			using (var stream = new System.IO.MemoryStream (message)) {
				using (var reader = new System.IO.BinaryReader (stream)) {
					var readHeaderBytes = reader.ReadChars (header.Length);

					foreach (var dataSource in dataSourcesToReceive) {
						var status = reader.ReadByte ();
						var isDataValid = (status & 1) != 0;
						var isConnected = (status & 2) != 0;
						var isTracked = (status & 4) != 0;
						var isHighConfidence = (status & 8) != 0;

						var scale = reader.ReadSingle ();

						var rootPosition = new Vector3 (
							reader.ReadSingle (),
							reader.ReadSingle (),
							reader.ReadSingle ());

						var rootRotation = new Quaternion (
							reader.ReadSingle (),
							reader.ReadSingle (),
							reader.ReadSingle (),
							reader.ReadSingle ());

						var joints = new Quaternion[Constants.NUM_HAND_JOINTS];
						for (int i = 0; i < joints.Length; i++) {
							joints[i] = new Quaternion (
								reader.ReadSingle (),
								reader.ReadSingle (),
								reader.ReadSingle (),
								reader.ReadSingle ());
						}

						var data = dataSource.GetData ();
						data.IsDataValid = isDataValid;
						data.IsConnected = isConnected;
						data.IsTracked = isTracked;
						data.IsHighConfidence = isHighConfidence;
						data.HandScale = scale;
						data.Root.position = transform.TransformPoint (rootPosition - new Vector3 (0, rig.position.y, 0));
						data.Root.rotation = transform.rotation * rootRotation;
						for (int i = 0; i < joints.Length; i++) {
							data.Joints[i] = joints[i];
						}
						dataSource.SetData (data);
					}

					if (!receivedFirstData) {
						receivedFirstData = true;
						// enable hand visuals
						foreach (var handVisual in networkHandVisuals) {
							if (!handVisual.activeSelf) handVisual.SetActive (true);
						}
					}
				}
			}
		}
	}
}