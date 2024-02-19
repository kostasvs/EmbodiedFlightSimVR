using UnityEngine;

namespace Assets.Scripts.Indicators {
	public class HSI : MonoBehaviour {

		[SerializeField]
		private Transform ehsiCompass;

		void Update () {
			var snapshot = GeoPositioning.CurSnapshot;
			ehsiCompass.localEulerAngles = new Vector3 (snapshot.magHeading, 0, 0);
		}
	}
}