using UnityEngine;

namespace Assets.Scripts.Indicators {
	public class GearIndicator : MonoBehaviour {
		[SerializeField]
		MeshRenderer[] gearDownLights;
		[SerializeField]
		Material matGreen;
		[SerializeField]
		Material matRed;

		private void Update () {
			var snapshot = GeoPositioning.CurSnapshot;
			SetGearLights (snapshot.leftGear, snapshot.noseGear, snapshot.rightGear, snapshot.gearRed);
		}

		private void SetGearLights (bool left, bool nose, bool right, bool isRed) {
			SetGearLight (0, left, isRed);
			SetGearLight (1, nose, isRed);
			SetGearLight (2, right, isRed);
		}

		private void SetGearLight (int index, bool on, bool isRed) {
			gearDownLights[index].enabled = on || isRed;
			gearDownLights[index].sharedMaterial = isRed ? matRed : matGreen;
		}
	}
}