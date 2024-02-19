using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts {
	public class SwitchInteractable : MonoBehaviour {

		private Hand lastHand;
		private PokeInteractable pokeInteractable;

		[SerializeField]
		private float releaseDistance = 0.03f;

		[SerializeField]
		private bool actuateHorizontal = true;
		[SerializeField]
		private bool actuateVertical = true;

		public enum SwitchDirection {
			Up,
			Down,
			Left,
			Right
		}
		public UnityEvent<SwitchInteractable, SwitchDirection> OnSwitchActuate = new UnityEvent<SwitchInteractable, SwitchDirection> ();

		void Start () {
			pokeInteractable = GetComponent<PokeInteractable> ();
			var wrapper = GetComponent<InteractableUnityEventWrapper> ();
			wrapper.WhenSelect.AddListener (OnSelect);
			wrapper.WhenUnselect.AddListener (OnUnselect);

			pokeInteractable.InjectOptionalReleaseDistance (releaseDistance);
		}

		private void OnSelect () {
			foreach (var i in pokeInteractable.Interactors) {
				var handRef = i.GetComponent<HandRef> ();
				if (handRef && handRef.Hand != null && handRef.Hand is Hand hand) {
					lastHand = hand;
					break;
				}
			}
		}

		private void OnUnselect () {
			if (lastHand == null) return;
			if (lastHand.GetJointPose (HandJointId.HandIndexTip, out Pose jointPose)) {
				var delta = transform.InverseTransformPointUnscaled (jointPose.position);
				if (actuateHorizontal && Mathf.Abs (delta.x) > Mathf.Max (Mathf.Abs (delta.y), -delta.z)) {
					if (delta.x > 0) {
						OnSwitchActuate.Invoke (this, SwitchDirection.Right);
					}
					else {
						OnSwitchActuate.Invoke (this, SwitchDirection.Left);
					}
				}
				else if (actuateVertical && Mathf.Abs (delta.y) > Mathf.Max (Mathf.Abs (delta.x), -delta.z)) {
					if (delta.y > 0) {
						OnSwitchActuate.Invoke (this, SwitchDirection.Up);
					}
					else {
						OnSwitchActuate.Invoke (this, SwitchDirection.Down);
					}
				}
			}
		}
	}
}