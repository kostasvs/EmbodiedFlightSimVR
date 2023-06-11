using UnityEngine;

namespace Assets.Scripts.Indicators {
	public class HUD : MonoBehaviour {

		[SerializeField]
		private Transform uprightCopy;

		[SerializeField]
		private Transform offsetParent;
		
		[SerializeField]
		private Transform cameraTr;

		void Update () {
			if (!cameraTr) return;
			var camRelPos = uprightCopy.InverseTransformPoint (cameraTr.position);
			camRelPos.z = 0f;
			offsetParent.localPosition = camRelPos;
		}
	}
}