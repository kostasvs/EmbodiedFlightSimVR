﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Rpc {
	public class RpcPoll : MonoBehaviour {

		private static RpcPoll instance;
		private readonly List<KeyValuePair<short, byte[]>> messages = new List<KeyValuePair<short, byte[]>> ();
		public static List<KeyValuePair<short, byte[]>> Messages => instance.messages;
		private short lastMessageId;
		
		private short curSendSequenceId;
		private short lastReceivedSequenceId;
		private int invalidSequenceIdCount;
		private const int invalidSequenceIdMax = 10;

		private readonly List<short> acks = new List<short> ();
		public static List<short> Acks => instance.acks;

		private readonly HashSet<short> ackedMessages = new HashSet<short> ();

		[SerializeField]
		private UnityEvent<byte[]> onMessageReceived = new UnityEvent<byte[]> ();
		public static UnityEvent<byte[]> OnMessageReceived => instance.onMessageReceived;

		private void Awake () {
			instance = this;
		}

		public static void Send (byte[] message) {
			if (message == null || message.Length == 0) {
				Debug.LogError ("Message is null or empty");
				return;
			}
			if (message.Length > byte.MaxValue) {
				Debug.LogError ("Message is too long");
				return;
			}
			if (instance.messages.Count == ushort.MaxValue) {
				Debug.LogError ("Too many messages");
				return;
			}
			instance.lastMessageId++;
			if (instance.lastMessageId == short.MaxValue) instance.lastMessageId = 0;
			instance.messages.Add (new KeyValuePair<short, byte[]> (instance.lastMessageId, message));
		}

		public static void Send (byte message) {
			Send (new byte[] { message });
		}

		public static void Send (byte index, byte message) {
			Send (new byte[] { index, message });
		}

		private void HandleMessageReceived (KeyValuePair<short, byte[]> message) {
			if (instance.acks.Count == byte.MaxValue) {
				// don't accept messages if we have too many acks, we won't be able to ack them
				return;
			}
			if (instance.acks.Contains (message.Key)) return;
			instance.acks.Add (message.Key);
			instance.onMessageReceived.Invoke (message.Value);
		}

		private void HandleAckReceived (short messageId) {
			instance.messages.RemoveAll (m => m.Key == messageId);
		}

		public static void WriteTo (System.IO.BinaryWriter writer) {

			if (instance.curSendSequenceId == short.MaxValue) instance.curSendSequenceId = 0;
			instance.curSendSequenceId++;
			writer.Write (instance.curSendSequenceId);

			writer.Write ((ushort)instance.messages.Count);
			foreach (var message in instance.messages) {
				writer.Write (message.Key);
				writer.Write ((byte)message.Value.Length);
				writer.Write (message.Value);
			}

			writer.Write ((byte)instance.acks.Count);
			foreach (var ack in instance.acks) {
				writer.Write (ack);
			}
		}

		public static void ReadFrom (System.IO.BinaryReader reader) {
			var sequenceId = reader.ReadInt16 ();
			var valid = sequenceId > instance.lastReceivedSequenceId;
			if (!valid) {
				// if too many consecutive invalid sequence ids are received, reset the last received sequence id
				instance.invalidSequenceIdCount++;
				if (instance.invalidSequenceIdCount > invalidSequenceIdMax) {
					valid = true;
				}
			}
			if (valid) {
				instance.invalidSequenceIdCount = 0;
				instance.lastReceivedSequenceId = sequenceId;
			}

			instance.ackedMessages.Clear ();
			var messageCount = reader.ReadUInt16 ();
			for (var i = 0; i < messageCount; i++) {
				var messageId = reader.ReadInt16 ();
				instance.ackedMessages.Add (messageId);
				var messageLength = reader.ReadByte ();
				var message = reader.ReadBytes (messageLength);
				if (valid) {
					instance.HandleMessageReceived (new KeyValuePair<short, byte[]> (messageId, message));
				}
			}

			if (valid) {
				// acks for messages that are no longer received have been synacked can be removed
				instance.acks.RemoveAll (ack => !instance.ackedMessages.Contains (ack));
			}

			var ackCount = reader.ReadByte ();
			for (var i = 0; i < ackCount; i++) {
				var ack = reader.ReadInt16 ();
				if (valid) {
					instance.HandleAckReceived (ack);
				}
			}
		}
	}
}
