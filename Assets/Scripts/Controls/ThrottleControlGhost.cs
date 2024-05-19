using UnityEngine;

namespace Assets.Scripts.Controls {
	public class ThrottleControlGhost : MonoBehaviour {

		private Vector3 initPos;

		private void Start () {
			initPos = transform.localPosition;
		}

		public void UpdateGhost (bool isVisible, float z) {
			if (gameObject.activeSelf != isVisible) {
				gameObject.SetActive (isVisible);
			}
			if (!isVisible) {
				return;
			}
			transform.localPosition = initPos + new Vector3 (0, 0, z);
		}
	}
}