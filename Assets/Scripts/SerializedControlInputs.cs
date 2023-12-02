using UnityEngine;

namespace Assets.Scripts {
	public static class SerializedControlInputs {

		public struct Input {
			public Vector2 Stick;
			public bool StickIsGrabbed;
			public float Throttle;
			public bool ThrottleIsGrabbed;
			public float Rudder;
			public float Brake;
		}

		public static void WriteTo (System.IO.BinaryWriter writer, Input input) {
			writer.Write (input.Stick.x);
			writer.Write (input.Stick.y);
			writer.Write (input.StickIsGrabbed);
			writer.Write (input.Throttle);
			writer.Write (input.ThrottleIsGrabbed);
			writer.Write (input.Rudder);
			writer.Write (input.Brake);
		}

		public static Input ReadFrom (System.IO.BinaryReader reader) {
			return new Input {
				Stick = new Vector2 (reader.ReadSingle (), reader.ReadSingle ()),
				StickIsGrabbed = reader.ReadBoolean (),
				Throttle = reader.ReadSingle (),
				ThrottleIsGrabbed = reader.ReadBoolean (),
				Rudder = reader.ReadSingle (),
				Brake = reader.ReadSingle ()
			};
		}
	}
}