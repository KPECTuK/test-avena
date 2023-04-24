using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Avena.Components
{
	public static class Extensions
	{
		private const float AXIS_GAP = 0.7f;

		public static void Draw(this CameraWorldFrustum source)
		{
			UnityEngine.Debug.DrawLine(source.Origin, source.PosX, Color.red);
			UnityEngine.Debug.DrawLine(source.Origin, source.PosY, Color.green);
			UnityEngine.Debug.DrawLine(source.PosX, source.TR, Color.grey);
			UnityEngine.Debug.DrawLine(source.PosY, source.TR, Color.grey);
		}

		public static unsafe void DrawBuffer8(Vector3* sourcePtr)
		{
			for(var indexX = 0; indexX < 8; indexX++)
			{
				for(var indexY = 0; indexY < 8; indexY++)
				{
					UnityEngine.Debug.DrawLine(
						sourcePtr[indexX],
						sourcePtr[indexY],
						Color.blue,
						10f);
				}
			}
		}

		public static void DrawGUI(this CameraWorldFrustum source)
		{
			source.Draw();

			#if UNITY_EDITOR
			var contentOrigin = new GUIContent($"frustum:\norigin: {source.Origin}\n{source}");
			Handles.Label(source.Origin, contentOrigin);
			Handles.Label(source.PosX, $"x: {source.PosX}");
			Handles.Label(source.PosY, $"y: {source.PosY}");
			Handles.Label(source.TR, $"tr: {source.TR}");
			#endif
		}

		public static void Draw(this Rect sourceInScreenSpace, Camera target, float duration = 0f)
		{
			Vector3 origin;
			Vector3 y;
			Vector3 x;
			Vector3 tr;
			if(target != null)
			{
				origin = target.ScreenToWorldPoint(new Vector3(
					sourceInScreenSpace.position.x,
					sourceInScreenSpace.position.y,
					target.nearClipPlane));
				tr = target.ScreenToWorldPoint(new Vector3(
					sourceInScreenSpace.position.x + sourceInScreenSpace.width,
					sourceInScreenSpace.position.y + sourceInScreenSpace.height,
					target.nearClipPlane));
				x = target.ScreenToWorldPoint(new Vector3(
					sourceInScreenSpace.position.x + sourceInScreenSpace.width,
					sourceInScreenSpace.position.y,
					target.nearClipPlane));
				y = target.ScreenToWorldPoint(new Vector3(
					sourceInScreenSpace.position.x,
					sourceInScreenSpace.position.y + sourceInScreenSpace.height,
					target.nearClipPlane));
			}
			else
			{
				origin = sourceInScreenSpace.position;
				x = (Vector3)sourceInScreenSpace.position + sourceInScreenSpace.width * Vector3.right;
				y = (Vector3)sourceInScreenSpace.position + sourceInScreenSpace.height * Vector3.up;
				tr = x + y - (Vector3)sourceInScreenSpace.position;
			}

			UnityEngine.Debug.DrawLine(origin, x, Color.black, duration);
			UnityEngine.Debug.DrawLine(origin, y, Color.gray, duration);
			UnityEngine.Debug.DrawLine(x, tr, Color.yellow, duration);
			UnityEngine.Debug.DrawLine(y, tr, Color.yellow, duration);
		}

		public static void Draw(this Vector3[] source)
		{
			for(var index = 0; index < source.Length; index++)
			{
				var indexNext = (index + 1) % source.Length;
				UnityEngine.Debug.DrawLine(source[index], source[indexNext], Color.blue, 10f);
			}
		}

		public static unsafe Rect ProjectLocalBounds(this SpriteRenderer source, Camera camera)
		{
			var bufferPtr = stackalloc Vector3[8];
			source.localBounds.ToBuffer(bufferPtr);
			source.transform.ToWorld(bufferPtr);
			camera.ToScreen(bufferPtr);

			var cull = new Rect(0, 0, Screen.width, Screen.height);
			for(var index = 0; index < 8; index++)
			{
				// TODO: bounds cases
				if(!cull.Contains(bufferPtr[index]))
				{
					var current = new Vector2(bufferPtr[index].x, bufferPtr[index].y);
					var origin = cull.Origin();
					var xLocal = cull.XLocal();
					var yLocal = cull.YLocal();
					var x = (current.IsInsideSegment(origin, xLocal) ? current : current.GetClosest(origin, xLocal)).x;
					var y = (current.IsInsideSegment(origin, yLocal) ? current : current.GetClosest(origin, yLocal)).y;
					bufferPtr[index] = new Vector3(x, y, bufferPtr[index].z);
				}
			}

			var result = new Rect(bufferPtr[0].x, bufferPtr[0].y, 0f, 0f);
			for(var index = 1; index < 7; index++)
			{
				result = result.Extend(new Vector2(bufferPtr[index].x, bufferPtr[index].y));
			}

			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this Color source, Color target)
		{
			var result = ((Vector4)source - (Vector4)target).magnitude;
			return result < .01f;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane ToPlane(this Camera source)
		{
			var transform = source.transform;
			var forward = transform.forward;
			return new Plane(
				forward,
				transform.position + forward * source.nearClipPlane);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInsideSegment(this Vector2 source, Vector2 left, Vector2 right)
		{
			var projected3 = Vector3.Project(source, right - left);
			var projected2 = new Vector2(projected3.x, projected3.y);
			return Vector2.Dot(left - projected2, right - projected2) < 0f;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 GetClosest(this Vector2 source, Vector2 left, Vector2 right)
		{
			var projected3 = Vector3.Project(source, right - left);
			var projected2 = new Vector2(projected3.x, projected3.y);
			var toLeft = (left - projected2).magnitude;
			var toRight = (right - projected2).magnitude;
			return toLeft > toRight ? right : left;
		}

		public static Rect Extend(this Rect source, Vector2 point)
		{
			var xMin = source.xMin < point.x ? source.xMin : point.x;
			var xMax = source.xMax > point.x ? source.xMax : point.x;
			var yMin = source.yMin < point.y ? source.yMin : point.y;
			var yMax = source.yMax > point.y ? source.yMax : point.y;
			return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 TR(this Rect source)
		{
			return source.Origin() + source.size;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 XLocal(this Rect source)
		{
			return source.Origin() + source.Right();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 YLocal(this Rect source)
		{
			return source.Origin() + source.Up();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Right(this Rect source)
		{
			return new(0f, source.width);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Up(this Rect source)
		{
			return new(source.height, 0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Origin(this Rect source)
		{
			return source.min;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void ToProject(this Plane source, Vector3* bufferPtr)
		{
			bufferPtr[0] = Vector3.ProjectOnPlane(bufferPtr[0], source.normal);
			bufferPtr[1] = Vector3.ProjectOnPlane(bufferPtr[1], source.normal);
			bufferPtr[2] = Vector3.ProjectOnPlane(bufferPtr[2], source.normal);
			bufferPtr[3] = Vector3.ProjectOnPlane(bufferPtr[3], source.normal);
			bufferPtr[4] = Vector3.ProjectOnPlane(bufferPtr[4], source.normal);
			bufferPtr[5] = Vector3.ProjectOnPlane(bufferPtr[5], source.normal);
			bufferPtr[6] = Vector3.ProjectOnPlane(bufferPtr[6], source.normal);
			bufferPtr[7] = Vector3.ProjectOnPlane(bufferPtr[7], source.normal);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void ToScreen(this Camera source, Vector3* bufferPtr)
		{
			bufferPtr[0] = source.WorldToScreenPoint(bufferPtr[0]);
			bufferPtr[1] = source.WorldToScreenPoint(bufferPtr[1]);
			bufferPtr[2] = source.WorldToScreenPoint(bufferPtr[2]);
			bufferPtr[3] = source.WorldToScreenPoint(bufferPtr[3]);
			bufferPtr[4] = source.WorldToScreenPoint(bufferPtr[4]);
			bufferPtr[5] = source.WorldToScreenPoint(bufferPtr[5]);
			bufferPtr[6] = source.WorldToScreenPoint(bufferPtr[6]);
			bufferPtr[7] = source.WorldToScreenPoint(bufferPtr[7]);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void ToWorld(this Transform source, Vector3* bufferPtr)
		{
			bufferPtr[0] = source.TransformPoint(bufferPtr[0]);
			bufferPtr[1] = source.TransformPoint(bufferPtr[1]);
			bufferPtr[2] = source.TransformPoint(bufferPtr[2]);
			bufferPtr[3] = source.TransformPoint(bufferPtr[3]);
			bufferPtr[4] = source.TransformPoint(bufferPtr[4]);
			bufferPtr[5] = source.TransformPoint(bufferPtr[5]);
			bufferPtr[6] = source.TransformPoint(bufferPtr[6]);
			bufferPtr[7] = source.TransformPoint(bufferPtr[7]);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void ToBuffer(this Bounds source, Vector3* bufferPtr)
		{
			bufferPtr[0] = source.center + new Vector3(source.extents.x, source.extents.y, source.extents.z);
			bufferPtr[1] = source.center + new Vector3(-source.extents.x, source.extents.y, source.extents.z);
			bufferPtr[2] = source.center + new Vector3(source.extents.x, -source.extents.y, source.extents.z);
			bufferPtr[3] = source.center + new Vector3(-source.extents.x, -source.extents.y, source.extents.z);
			bufferPtr[4] = source.center + new Vector3(source.extents.x, source.extents.y, -source.extents.z);
			bufferPtr[5] = source.center + new Vector3(-source.extents.x, source.extents.y, -source.extents.z);
			bufferPtr[6] = source.center + new Vector3(source.extents.x, -source.extents.y, -source.extents.z);
			bufferPtr[7] = source.center + new Vector3(-source.extents.x, -source.extents.y, -source.extents.z);
		}

		public static string ToRichTextFormat(this Color source)
		{
			var r = Mathf.RoundToInt(source.r * 255f);
			var g = Mathf.RoundToInt(source.g * 255f);
			var b = Mathf.RoundToInt(source.b * 255f);
			var a = Mathf.RoundToInt(source.a * 255f);
			return $"{r:X2}{g:X2}{b:X2}{a:X2}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CxOrigin ToOrigin(this Vector3 source)
		{
			return new CxOrigin
			{
				Location = source,
				Orientation = Quaternion.identity,
			};
		}

		public struct Meta<T>
		{
			public enum Shape
			{
				Point,
				Cross,
				Arrow,
				Circle,
				CurveKnot,
			}

			public Shape ShapeType;
			public Vector3 UpVector;
			public Color Color;
			public float Size;
			public float Duration;
			public float Dimpher;
			public bool IsGradient;

			public T Source;
		}

		public static Meta<T> ToMeta<T>(this T source)
		{
			return new()
			{
				Source = source,
				//
				UpVector = Vector3.up,
				Size = .1f,
				Duration = 10f,
				Color = Color.yellow,
				Dimpher = 1f,
				ShapeType = Meta<T>.Shape.Cross,
				IsGradient = false,
			};
		}

		[Conditional("UNITY_EDITOR")]
		public static void Draw(this Meta<CxOrigin> source)
		{
			UnityEngine.Debug.DrawLine(
				source.Source.Location + source.Source.Orientation * Vector3.up * source.Size * AXIS_GAP,
				source.Source.Location + source.Source.Orientation * Vector3.up * source.Size,
				Color.Lerp(Color.black, Color.green, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location + source.Source.Orientation * Vector3.up * source.Size * AXIS_GAP,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location - source.Source.Orientation * Vector3.up * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);

			UnityEngine.Debug.DrawLine(
				source.Source.Location + source.Source.Orientation * Vector3.right * source.Size * AXIS_GAP,
				source.Source.Location + source.Source.Orientation * Vector3.right * source.Size,
				Color.Lerp(Color.black, Color.red, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location + source.Source.Orientation * Vector3.right * source.Size * AXIS_GAP,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location - source.Source.Orientation * Vector3.right * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);

			UnityEngine.Debug.DrawLine(
				source.Source.Location + source.Source.Orientation * Vector3.forward * source.Size * AXIS_GAP,
				source.Source.Location + source.Source.Orientation * Vector3.forward * source.Size,
				Color.Lerp(Color.black, Color.blue, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location + source.Source.Orientation * Vector3.forward * source.Size * AXIS_GAP,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location - source.Source.Orientation * Vector3.forward * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
		}
	}
}