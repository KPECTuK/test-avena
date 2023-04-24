using System;
using UnityEngine;

namespace Avena.Components
{
	public class ControllerMoveTarget : IControllerMove
	{
		private Vector3 _targetPos;

		public bool IsComplete { get; private set; }

		public Vector3 GetCurrentDelta(Transform actor, float speedWorld)
		{
			var actorPos = actor.position;
			var path = _targetPos - actorPos;
			var direction = path.normalized;
			var delta = direction * (speedWorld * Time.deltaTime);
			var @new = actorPos + delta;
			IsComplete = Vector3.Dot(direction, (_targetPos - @new).normalized) < 0f;
			return IsComplete ? _targetPos - actorPos : delta;
		}

		public void SetTarget(Vector2 pointOnScreen, Transform actor, Camera spectator)
		{
			var ray = spectator.ScreenPointToRay(new Vector3(pointOnScreen.x, pointOnScreen.y, spectator.nearClipPlane));
			var actorPlane = new Plane(actor.forward, actor.position);
			if(!actorPlane.Raycast(ray, out var distance))
			{
				throw new Exception("player is perpendicular to camera cast");
			}
			_targetPos = ray.origin + ray.direction.normalized * distance;
		}
	}
}