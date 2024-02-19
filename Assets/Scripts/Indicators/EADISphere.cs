using UnityEngine;

namespace Assets.Scripts.Indicators {
	public class EADISphere : MonoBehaviour {

		[SerializeField]
		private Transform aircraftTr;

		void Update () {
			var attitude = aircraftTr.eulerAngles;
			transform.localEulerAngles = new Vector3 (-attitude.x, 0, -attitude.z);
		}
	}
}