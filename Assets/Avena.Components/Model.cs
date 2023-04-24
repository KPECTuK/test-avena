using System.Collections.Generic;

namespace Avena.Components
{
	public static class Model
	{
		// assuming all the collection are sorted
		public static List<string> NamesAll = new();
		public static List<string> NamesCorrect = new();
		public static List<string> NamesChecked = new();
		public static List<string> NamesFoundForRequest = new();

		public static void Reset(CompApp composition)
		{
			NamesCorrect.Clear();
			NamesChecked.Clear();
			NamesFoundForRequest.Clear();

			composition.Player.transform.position = composition.PlayerInitialPosition;
		}
	}
}
