using System;
using UnityEngine;

namespace Avena.Components
{
	public class ControllerMoveDirect : IControllerMove
	{
		private Vector3 _currentDirectionWorld;

		// newer lose keyboard input if any
		public bool IsComplete => false;

		public Vector3 GetCurrentDelta(Transform actor, float speedWorld)
		{
			return _currentDirectionWorld.normalized * (speedWorld * Time.deltaTime);
		}

		public void SetValue(Vector2 inputValue, Transform actor, Camera spectator)
		{
			if(inputValue == Vector2.zero)
			{
				_currentDirectionWorld = Vector3.zero;
				return;
			}

			var actorWorldPosition = actor.position;
			var actorScreenPos = spectator.WorldToScreenPoint(actorWorldPosition);
			actorScreenPos -= new Vector3(inputValue.x, inputValue.y);
			var ray = spectator.ScreenPointToRay(actorScreenPos);
			var actorPlane = new Plane(actor.forward, actorWorldPosition);
			if(!actorPlane.Raycast(ray, out var distance))
			{
				throw new Exception("player is perpendicular to camera cast");
			}
			var @new = ray.origin + ray.direction.normalized * distance;
			_currentDirectionWorld = actorWorldPosition - @new;
		}
	}
}