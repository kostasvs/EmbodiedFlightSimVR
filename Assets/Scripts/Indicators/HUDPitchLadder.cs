using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class HUDPitchLadder : MonoBehaviour {

		[SerializeField]
		private RectTransform stepTemplate;

		[SerializeField]
		private Sprite belowImage;

		void Start () {
			CreateSteps (true);
			CreateSteps (false);
		}

		void CreateSteps (bool below) {
			for (int i = 0; i < 16; i++) {
				var step = Instantiate (stepTemplate.gameObject, stepTemplate.transform.parent);
				int degrees = 5 + i * 5;
				step.GetComponent<RectTransform> ().anchoredPosition = new Vector2 (0,
					(below ? -degrees : degrees) * HUD.ANGLES_TO_PIXELS);
				if (i % 2 != 0) {
					foreach (var text in step.GetComponentsInChildren<Text> (true)) {
						text.text = degrees.ToString ();
						text.gameObject.SetActive (true);
					}
				}
				if (below) {
					step.GetComponent<Image> ().sprite = belowImage;
				}
				step.SetActive (true);
			}
		}
	}
}