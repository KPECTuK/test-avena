using System;
using UnityEngine;

namespace Avena.Components
{
	[Serializable]
	public struct CxOrigin
	{
		public Vector3 Location;
		public Quaternion Orientation;

		public static CxOrigin Identity =>
			new()
			{
				Orientation = Quaternion.identity,
				Location = Vector3.zero,
			};

		public override string ToString()
		{
			return $"{Location}";
		}
	}
}