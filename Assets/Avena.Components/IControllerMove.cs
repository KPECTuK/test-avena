using UnityEngine;

namespace Avena.Components
{
	public interface IControllerMove
	{
		bool IsComplete { get; }

		Vector3 GetCurrentDelta(Transform actor, float speedWorld);
	}
}