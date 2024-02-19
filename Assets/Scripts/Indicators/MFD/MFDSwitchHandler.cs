using UnityEngine;
using UnityEngine.Events;
using static Assets.Scripts.SwitchInteractable;

namespace Assets.Scripts.Indicators {
	public class MFDSwitchHandler : MonoBehaviour {

		public SwitchInteractable ForSwitch;
		public SwitchDirection ForSwitchDirection;
		public bool AnyDirection = false;
		public UnityEvent OnSwitchActuate = new UnityEvent ();

		public void OnSwitchActuated (SwitchInteractable switchInteractable, SwitchDirection direction) {
			if (switchInteractable == ForSwitch && (AnyDirection || direction == ForSwitchDirection)) {
				OnSwitchActuate.Invoke ();
			}
		}
	}
}