using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Rpc {
	public class SyncedButton : MonoBehaviour {
		private static readonly List<SyncedButton> list = new List<SyncedButton> ();
		public static List<SyncedButton> List => list;
		private byte index;

		public UnityEvent OnInteract = new UnityEvent ();

		public void SetupSync (byte newIndex) {
			index = newIndex;
			var interactable = GetComponent<InteractableUnityEventWrapper> ();
			if (!interactable) {
				Debug.LogError ("SyncedButton requires a InteractableUnityEventWrapper component", gameObject);
				return;
			}

			interactable.WhenSelect.AddListener (() => {
				if (!FlightGearNetworking.IsConnectionReady) return;
				RpcPoll.Send (new byte[] { (byte)RpcIds.Interactable_ButtonPress, index });
				if (FlightGearNetworking.Instance.IsMaster == true) {
					OnInteract.Invoke ();
				}
			});
		}
	}
}