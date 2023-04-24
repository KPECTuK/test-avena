using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Avena.Components
{
	[Preserve]
	public sealed class ControllerGameGame : IControllerGame
	{
		public void Enable(CompApp composition)
		{
			Model.Reset(composition);

			composition.Screens.ScreenGame.Show();

			// do not want to add specialties for it
			var all = Model.NamesAll;
			Model.NamesAll = new List<string>();
			composition.Screens.ScreenGame.UpdateList();
			Model.NamesAll = all;

			composition.InputEnable();
			composition.DetectionStart();

			Debug.Log($"<color=yellow>controller is enabled</color>: {GetType().Name}");
		}

		public void Disable(CompApp composition)
		{
			composition.ProtocolStopAll();

			composition.DetectionStop();
			composition.InputDisable();

			composition.Screens.ScreenGame.Hide();

			Debug.Log($"<color=#FF8000>controller is disabled</color>: {GetType().Name}");
		}

		public void Update(CompApp composition)
		{
			Model.NamesCorrect.Sort();
			Model.NamesChecked.Sort();

			var result = true;
			result = result && Model.NamesCorrect.Count > 0;
			result = result && Model.NamesCorrect.SequenceEqual(Model.NamesChecked);

			if(result)
			{
				composition.ModeSet<ControllerGameScore>();
			}

			composition.PawnMove();
		}

		public void OnDetected(CompApp composition)
		{
			composition.Screens.ScreenGame.OnDetected(composition.IsTargetDetected);
		}

		public void OnAction(CompApp composition)
		{
			Model.NamesCorrect.Clear();
			Model.NamesChecked.Clear();

			// do not want to add specialties for it
			var all = Model.NamesAll;
			Model.NamesAll = new List<string>();
			composition.Screens.ScreenGame.UpdateList();
			Model.NamesAll = all;

			composition.ProtocolStart();
		}

		public void OnRequestComplete(CompApp composition)
		{
			composition.Screens.ScreenGame.UpdateList();
		}
	}
}