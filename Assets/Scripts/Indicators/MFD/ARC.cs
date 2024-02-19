using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class ARC : MonoBehaviour {

		[SerializeField]
		private Text headingText;

		void Update () {
			var snapshot = GeoPositioning.CurSnapshot;
			headingText.text = snapshot.magHeading.ToString ("0");
		}
	}
}