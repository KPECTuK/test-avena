using UnityEngine;

namespace Avena.Components
{
	public struct CameraWorldFrustum
	{
		public Vector3 Origin;
		public Vector3 PosX;
		public Vector3 PosY;
		//
		public Vector3 TR => PosX + PosY - Origin;
		public Vector3 XLocal => PosX - Origin;
		public Vector3 YLocal => PosY - Origin;
		public Vector2 Size => new(XLocal.magnitude, YLocal.magnitude);
		public Vector3 Center => Origin + (TR - Origin) * .5f;

		public void Extend(Vector3 point)
		{
			var plane = new Plane(Origin, PosX, PosY);
			var projection = Vector3.ProjectOnPlane(point, plane.normal);
			var projectionLocal = projection - Origin;
			var projectionXLocal = Vector3.Project(projectionLocal, XLocal);
			var projectionYLocal = Vector3.Project(projectionLocal, YLocal);

			if(Vector3.Dot(projectionXLocal.normalized, XLocal.normalized) < 0f)
			{
				Origin += projectionXLocal;
				PosY += projectionXLocal;
			}
			else if(projectionXLocal.magnitude > XLocal.magnitude)
			{
				PosX = Origin + projectionXLocal;
			}

			if(Vector3.Dot(projectionYLocal.normalized, YLocal.normalized) < 0f)
			{
				Origin += projectionYLocal;
				PosX += projectionYLocal;
			}
			else if(projectionYLocal.magnitude > YLocal.magnitude)
			{
				PosY = Origin + projectionYLocal;
			}
		}

		public override string ToString()
		{
			return $"size: ( width {XLocal.magnitude:F3}, height {YLocal.magnitude:F3} )";
		}
	}
}