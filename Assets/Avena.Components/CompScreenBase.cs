using UnityEngine;

namespace Avena.Components
{
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class CompScreenBase : MonoBehaviour
	{
		public virtual void Hide()
		{
			var group = GetComponent<CanvasGroup>();
			group.alpha = 0f;
			group.interactable = false;

			Debug.Log($"screen hide: {GetType().Name}");
		}

		public virtual void Show()
		{
			var group = GetComponent<CanvasGroup>();
			group.alpha = 1f;
			group.interactable = true;

			Debug.Log($"screen show: {GetType().Name}");
		}
	}
}
