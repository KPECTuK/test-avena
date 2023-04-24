using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Avena.Components
{
	public sealed class CompScreenGame : CompScreenBase
	{
		public TextMeshProUGUI Text;
		public TextMeshProUGUI List;

		private void Awake()
		{
			var trigger = List.GetComponent<EventTrigger>();
			var entry = new EventTrigger.Entry();
			entry.callback.AddListener(OnListClick);
			entry.eventID = EventTriggerType.PointerClick;
			trigger.triggers.Add(entry);

			Text.alpha = 0f;
		}

		public void OnDetected(bool isDetected)
		{
			Text.alpha = isDetected ? 1f : 0f;
		}

		public void UpdateList()
		{
			var builder = new StringBuilder();

			foreach(var item in Model.NamesAll)
			{
				var color = Color.white;

				if(Model.NamesChecked.Contains(item))
				{
					color = Model.NamesCorrect.Contains(item)
						? Color.green
						: Color.red;
				}

				builder
					.Append("<color=#")
					.Append(color.ToRichTextFormat())
					.Append(">")
					.Append(item)
					.Append("</color>")
					.AppendLine();
			}

			List.text = builder.ToString();

			Debug.Log($"list is updated with: {string.Join(", ", Model.NamesAll)}");
		}

		private unsafe void OnListClick(BaseEventData data)
		{
			if(data is ExtendedPointerEventData cast)
			{
				var world = cast.pointerPressRaycast.worldPosition;
				world.ToOrigin().ToMeta().Draw();

				var bufferPtr = stackalloc Vector3[8];
				List.textBounds.ToBuffer(bufferPtr);
				List.transform.ToWorld(bufferPtr);
				Extensions.DrawBuffer8(bufferPtr);

				// unreliable but no time 
				var origin = transform.localPosition;
				var up = transform.up;
				var right = transform.right;
				var plane = new Plane(transform.forward, origin);
				var xMin = float.PositiveInfinity;
				var xMax = float.NegativeInfinity;
				var yMin = float.PositiveInfinity;
				var yMax = float.NegativeInfinity;

				Debug.DrawLine(origin, origin + up, Color.green, 10f);
				Debug.DrawLine(origin, origin + right, Color.red, 10f);

				for(var index = 0; index < 8; index++)
				{
					var projected = Vector3.ProjectOnPlane(bufferPtr[index], plane.normal) - origin;
					var xProjected = Vector3.Project(projected, right);
					var yProjected = Vector3.Project(projected, up);

					var xCurrent = xProjected.magnitude;
					var yCurrent = yProjected.magnitude;

					xMin = xMin < xCurrent ? xMin : xCurrent;
					xMax = xMax > xCurrent ? xMax : xCurrent;
					yMin = yMin < yCurrent ? yMin : yCurrent;
					yMax = yMax > yCurrent ? yMax : yCurrent;
				}
				var allRect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

				var projectedClick = Vector3.ProjectOnPlane(world, plane.normal) - origin;
				var rectClick = new Vector2(
					Vector3.Project(projectedClick, right).magnitude,
					Vector3.Project(projectedClick, up).magnitude);

				var isContains = allRect.Contains(rectClick);
				Debug.Log(isContains ? "contains" : "not contains");

				allRect.Draw(null, 10f);
				var meta = new CxOrigin { Location = rectClick }.ToMeta();
				meta.Color = Color.cyan;
				meta.Draw();

				if(isContains)
				{
					var indexName = Mathf.FloorToInt(Model.NamesAll.Count * (allRect.size.y - rectClick.y + allRect.yMin) / allRect.size.y);
					
					Debug.Log($"index is: {indexName}");
					
					var nameSelected = Model.NamesAll[indexName];
					if(!Model.NamesChecked.Contains(nameSelected))
					{
						Model.NamesChecked.Add(nameSelected);
					}
					else
					{
						Model.NamesChecked.Remove(nameSelected);
					}

					UpdateList();
				}
			}
		}
	}
}
