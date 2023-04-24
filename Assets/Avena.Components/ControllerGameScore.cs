using UnityEngine;
using UnityEngine.Scripting;

namespace Avena.Components
{
	[Preserve]
	public sealed class ControllerGameScore : IControllerGame
	{
		public void Enable(CompApp composition)
		{
			composition.Screens.ScreenScore.Show();

			Debug.Log($"<color=yellow>controller is enabled</color>: {GetType().Name}");
		}

		public void Disable(CompApp composition)
		{
			composition.Screens.ScreenScore.Hide();

			Debug.Log($"<color=#FF8000>controller is disabled</color>: {GetType().Name}");
		}

		public void Update(CompApp composition) { }
	}
}