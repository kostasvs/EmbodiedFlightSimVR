using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class FuelEngineIndicators : MonoBehaviour {

		private const float refreshRate = 0.2f;

		[SerializeField]
		private Image n1Needle;
		private float n1NeedleMaxFill;
		[SerializeField]
		private Text n1IntegerText;
		[SerializeField]
		private Text n1FracText;
		[SerializeField]
		private Text fuelInternalText;
		[SerializeField]
		private Text fuelTotalText;
		[SerializeField]
		private Text fuelBingoText;

		private void Start () {
			n1NeedleMaxFill = n1Needle.fillAmount;
			InvokeRepeating (nameof (UpdateDisplays), 0f, refreshRate);
		}

		void UpdateDisplays () {
			// engine n1
			var snapshot = GeoPositioning.CurSnapshot;
			n1Needle.fillAmount = Mathf.Clamp01 (snapshot.n1 / 100f) * n1NeedleMaxFill;
			n1IntegerText.text = snapshot.n1.ToString ("0");
			n1FracText.text = "." + (snapshot.n1 * 10 % 10).ToString ("0");

			// fuel
			fuelInternalText.text = snapshot.fuelInternal.ToString ("0");
			fuelTotalText.text = snapshot.fuelTotal.ToString ("0");
			fuelBingoText.text = snapshot.fuelBingo.ToString ("0");
		}
	}
}