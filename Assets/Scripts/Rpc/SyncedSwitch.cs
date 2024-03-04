using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Rpc {
	public class SyncedSwitch : MonoBehaviour {
		private static readonly List<SyncedSwitch> list = new List<SyncedSwitch> ();
		public static List<SyncedSwitch> List => list;
		private byte index;

		public UnityEvent<SwitchInteractable, SwitchInteractable.SwitchDirection> OnInteract =
			new UnityEvent<SwitchInteractable, SwitchInteractable.SwitchDirection> ();
		private SwitchInteractable switchInteractable;
		public SwitchInteractable SwitchInteractable => switchInteractable;

		public void SetupSync (byte newIndex) {
			index = newIndex;
			switchInteractable = GetComponent<SwitchInteractable> ();
			if (!switchInteractable) {
				Debug.LogError ("SyncedSwitch requires a SwitchInteractable component", gameObject);
				return;
			}

			switchInteractable.OnSwitchActuate.AddListener ((interactable, direction) => {
				if (!FlightGearNetworking.IsConnectionReady) return;
				RpcPoll.Send (new byte[] { (byte)RpcIds.Interactable_SwitchActuate, index, (byte)direction });
				if (FlightGearNetworking.Instance.IsMaster == true) {
					OnInteract.Invoke (interactable, direction);
				}
			});
		}
	}
}