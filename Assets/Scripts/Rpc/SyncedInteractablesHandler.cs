using UnityEngine;

namespace Assets.Scripts.Rpc {
	public class SyncedInteractablesHandler {
		public static void SetupSyncs (Transform rootTr) {
			var switches = rootTr.GetComponentsInChildren<SyncedSwitch> (true);
			if (switches.Length > byte.MaxValue) {
				Debug.LogError ("Too many switches in scene, max is " + byte.MaxValue);
				return;
			}
			var buttons = rootTr.GetComponentsInChildren<SyncedButton> (true);
			if (buttons.Length > byte.MaxValue) {
				Debug.LogError ("Too many buttons in scene, max is " + byte.MaxValue);
				return;
			}
			SyncedSwitch.List.AddRange (switches);
			for (byte i = 0; i < SyncedSwitch.List.Count; i++) {
				switches[i].SetupSync (i);
			}
			SyncedButton.List.AddRange (buttons);
			for (byte i = 0; i < SyncedButton.List.Count; i++) {
				buttons[i].SetupSync (i);
			}
		}

		public static void HandleMessageReceived (byte[] message) {
			if (message[0] == (byte)RpcIds.Interactable_ButtonPress) {
				InteractableButtonPress (message);
				if (FlightGearNetworking.Instance.IsMaster == true) {
					RpcPoll.Send (message);
				}
			}
			else if (message[0] == (byte)RpcIds.Interactable_SwitchActuate) {
				InteractableSwitchActuate (message);
				if (FlightGearNetworking.Instance.IsMaster == true) {
					RpcPoll.Send (message);
				}
			}
		}

		private static void InteractableButtonPress (byte[] message) {
			var btn = SyncedButton.List[message[1]];
			if (btn) {
				btn.OnInteract.Invoke ();
			}
			else {
				Debug.LogError ("Button id not found: " + message[1]);
			}
		}

		private static void InteractableSwitchActuate (byte[] message) {
			var sw = SyncedSwitch.List[message[1]];
			if (sw) {
				var dir = (SwitchInteractable.SwitchDirection)message[2];
				sw.OnInteract.Invoke (sw.SwitchInteractable, dir);
			}
			else {
				Debug.LogError ("Switch id not found: " + message[1]);
			}
		}
	}
}