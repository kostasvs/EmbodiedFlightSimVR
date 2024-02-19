using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class EICAS : MonoBehaviour {

		[SerializeField]
		private Transform n1Needle;
		[SerializeField]
		private Vector3 n1NeedleMaxRotation;
		[SerializeField]
		private Transform n2Needle;
		[SerializeField]
		private Vector3 n2NeedleMaxRotation;
		[SerializeField]
		private Text voltsText;

		void Update () {
			var snapshot = GeoPositioning.CurSnapshot;
			// engine n1
			n1Needle.localEulerAngles = Vector3.Lerp (Vector3.zero, n1NeedleMaxRotation, snapshot.n1 / 100f);
			// engine n2 (in FlightGear M2000-5, n2 is the same as n1)
			n2Needle.localEulerAngles = Vector3.Lerp (Vector3.zero, n2NeedleMaxRotation, snapshot.n1 / 100f);
			// volts
			voltsText.text = snapshot.voltsDC.ToString ("0");
		}
	}
}