using UnityEngine;

namespace Assets.Scripts.Controls {
	public class StickControlGhost : MonoBehaviour {

		[SerializeField]
		private float maxAngle = 30f;

		private Vector3 initRot;

		private void Start () {
			initRot = transform.localEulerAngles;
		}

		public void UpdateGhost (bool isVisible, Vector2 value) {
			if (gameObject.activeSelf != isVisible) {
				gameObject.SetActive (isVisible);
			}
			if (!isVisible) {
				return;
			}
			transform.localEulerAngles = new Vector3 (
				initRot.x,
				initRot.y + value.x * maxAngle,
				initRot.z - value.y * maxAngle);
		}
	}
}
