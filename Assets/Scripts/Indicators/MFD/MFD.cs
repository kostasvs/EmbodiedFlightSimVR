using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Indicators {
	public class MFD : MonoBehaviour {

		[SerializeField]
		private MaskableGraphic[] menuLabels;

		[SerializeField]
		private Color selectedColor = Color.cyan;
		[SerializeField]
		private Color deselectedColor = Color.white;

		private Transform[] childMenus;
		[SerializeField]
		private int childMenusFirstIndex = 2;
		[SerializeField]
		private int initialMenu = 0;
		private int currentMenu = 0;

		[SerializeField]
		private SwitchInteractable menuSwitch;

		void Awake () {
			// get all child menus
			childMenus = new Transform[Mathf.Max (0, transform.childCount - childMenusFirstIndex)];
			for (int i = childMenusFirstIndex; i < transform.childCount; i++) {
				childMenus[i - childMenusFirstIndex] = transform.GetChild (i);
			}
		}

		private void Start () {
			// enable screens only when connection is ready
			if (FlightGearNetworking.IsConnectionReady) {
				SelectMenu (initialMenu);
			}
			else {
				SelectMenu (-1);
				FlightGearNetworking.Instance.OnConnectionReady.AddListener (() => {
					SelectMenu (initialMenu);
				});
			}
		}

		public void SelectMenu (int index) {
			currentMenu = index;
			for (int i = 0; i < menuLabels.Length; i++) {
				if (menuLabels[i]) menuLabels[i].color = i == index ? selectedColor : deselectedColor;
			}
			for (int i = 0; i < childMenus.Length; i++) {
				childMenus[i].gameObject.SetActive (i == index);
			}
		}

		public void CycleThroughMenus (int[] indices) {
			for (int i = 0; i < indices.Length; i++) {
				if (indices[i] == currentMenu) {
					SelectMenu (indices[(i + 1) % indices.Length]);
					return;
				}
			}
			SelectMenu (indices[0]);
		}

		public void CycleThroughMenus (string indicesCommaSep) {
			CycleThroughMenus (System.Array.ConvertAll (indicesCommaSep.Split (','), int.Parse));
		}

		public void OnSwitchActuated (SwitchInteractable switchInteractable, SwitchInteractable.SwitchDirection direction) {
			if (switchInteractable == menuSwitch) {
				SelectMenu (0);
				return;
			}
			var handlers = childMenus[currentMenu].GetComponentsInChildren<MFDSwitchHandler> ();
			foreach (var handler in handlers) {
				handler.OnSwitchActuated (switchInteractable, direction);
			}
		}
	}
}