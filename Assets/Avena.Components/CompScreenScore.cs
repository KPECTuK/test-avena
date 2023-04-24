using UnityEngine.UI;

namespace Avena.Components
{
	public sealed class CompScreenScore : CompScreenBase
	{
		public Button Button;

		private CompApp _composition;

		private void Awake()
		{
			Button.onClick.AddListener(OnClick);

			_composition = GetComponentInParent<CompApp>();
		}

		private void OnDestroy()
		{
			Button.onClick.RemoveAllListeners();
		}

		private void OnClick()
		{
			_composition.ModeSet<ControllerGameGame>();
		}
	}
}
