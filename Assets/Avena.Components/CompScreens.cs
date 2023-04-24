using UnityEngine;

namespace Avena.Components
{
	public sealed class CompScreens : MonoBehaviour
	{
		public CompScreenGame ScreenGame;
		public CompScreenScore ScreenScore;

		private void Awake()
		{
			Debug.Log($"call Awake(): {name}");

			GetComponentInParent<CompApp>().Screens = this;

			ScreenGame.Hide();
			ScreenScore.Hide();
		}
	}
}
