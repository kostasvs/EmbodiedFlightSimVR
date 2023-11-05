using Oculus.Interaction.Input;
using UnityEngine;

namespace Assets.Scripts {
	public class NetworkHandDataSource : DataSource<HandDataAsset> {
		private readonly HandDataAsset _handDataAsset = new HandDataAsset ();
		protected override HandDataAsset DataAsset => _handDataAsset;

		[SerializeField]
		private Hand copyHandDataFrom;

		protected virtual void Awake () {
			_handDataAsset.CopyFrom (copyHandDataFrom.GetData ());
			_handDataAsset.HandScale = 1f;
		}

		protected override void UpdateData () {}
		
		public void SetData (HandDataAsset source) {
			_handDataAsset.IsDataValid = source.IsDataValid;
			_handDataAsset.IsConnected = source.IsConnected;
			_handDataAsset.IsTracked = source.IsTracked;
			_handDataAsset.IsHighConfidence = source.IsHighConfidence;
			_handDataAsset.CopyPosesFrom (source);
		}
	}
}
