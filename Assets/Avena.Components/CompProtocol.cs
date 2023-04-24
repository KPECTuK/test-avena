#define WEB_CHECK_BYPASS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;

namespace Avena.Components
{
	public class CompProtocol : MonoBehaviour
	{
		[NonSerialized] public CompApp Controller;
		[NonSerialized] public Camera Camera;
		[NonSerialized] public SpriteRenderer Player;
		[NonSerialized] public SpriteRenderer[] Figures;

		private Camera _testerCamera;
		private RenderTexture _testerScreen;

		public void Start()
		{
			_testerScreen = new RenderTexture(
				Screen.width,
				Screen.height,
				0,
				GraphicsFormat.R8G8B8A8_SRGB);

			_testerCamera = gameObject.AddComponent<Camera>();
			_testerCamera.CopyFrom(Camera);
			_testerCamera.clearFlags = CameraClearFlags.SolidColor;
			_testerCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			_testerCamera.targetTexture = _testerScreen;
			_testerCamera.cullingMask = LayerMask.GetMask("layer_figures");
			_testerCamera.enabled = false;

			StartCoroutine(Detect());
		}

		public void OnDestroy()
		{
			DestroyImmediate(_testerScreen);
		}

		private void RenderCamera()
		{
			_testerCamera.RenderWithShader(
				Shader.Find("Avena/Detect"),
				"Mask");
		}

		private Texture2D CreateTesterTexture(Rect rect)
		{
			var size = new Vector2Int(
				Mathf.FloorToInt(rect.width),
				Mathf.FloorToInt(rect.height));
			var result = new Texture2D(size.x, size.y);

			// Debug.Log($"testing rect: {rect} with texture: {tester.width}, {tester.height} over buffer: {_testerScreen.width:####}, {_testerScreen.height:####}");

			RenderTexture.active = _testerScreen;
			result.ReadPixels(new Rect(
					rect.x,
					_testerScreen.height - rect.y - rect.height,
					rect.width,
					rect.height),
				0,
				0);
			result.Apply(false);
			RenderTexture.active = null;
			return result;
		}

		private List<string> ScanTexture(Texture2D tester)
		{
			// scanline
			var result = new List<string>();
			var count = tester.width * tester.height - 1;
			while(count > 0)
			{
				var x = count % tester.width;
				var y = count / tester.width;
				count--;
				var color = tester.GetPixel(x, y);

				for(var index = 0; index < Figures.Length; index++)
				{
					var colorFigure = Figures[index].color;
					if(color.Approx(colorFigure))
					{
						var actorName = Figures[index].name;
						if(!result.Contains(actorName))
						{
							result.Add(actorName);
						}
					}
				}
			}

			return result;
		}

		private void DrawDebug(RenderTexture textureScreen, Texture2D textureTester, Rect rectProjection)
		{
			var offset = 0f;
			if(textureScreen != null)
			{
				var size = new Vector2(textureScreen.width, textureScreen.height) * .6f;
				Graphics.DrawTexture(
					new Rect(
						Screen.width - size.x,
						offset,
						size.x,
						size.y),
					textureScreen);
				offset += size.y;
			}

			if(textureTester != null)
			{
				var size = new Vector2(textureTester.width, textureTester.height) * 2f;
				Graphics.DrawTexture(
					new Rect(
						Screen.width - size.x,
						offset,
						size.x,
						size.y),
					textureTester);
				offset += size.y;
			}

			rectProjection.Draw(Camera);
		}

		private IEnumerator Detect()
		{
			yield return new WaitForEndOfFrame();

			RenderCamera();
			var rect = Player.ProjectLocalBounds(Camera);
			var tester = CreateTesterTexture(rect);
			var names = ScanTexture(tester);
			
			// DrawDebug(_testerScreen, tester, rect);
			
			DestroyImmediate(tester);

			Debug.Log($"names found: {string.Join(", ", names)}");

			names.Sort();
			if(names.SequenceEqual(Model.NamesFoundForRequest))
			{
				yield break;
			}

			Model.NamesFoundForRequest = names;
			Controller.ProtocolPromote(this);

			// server is not available

			#if WEB_CHECK_BYPASS
			Model.NamesCorrect = names;
			#else
			string id;
			using(var requestFirst = BuildRequestFirst(names))
			{
				yield return requestFirst.SendWebRequest();

				if(requestFirst.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError("first request error");

					Controller.StopProtocol();
					yield break;
				}

				id = Encoding.UTF8.GetString(requestFirst.downloadHandler.data);
			}

			using(var requestSecond = BuildRequestSecond(id))
			{
				yield return requestSecond.SendWebRequest();

				if(requestSecond.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError("second request error");

					Controller.StopProtocol();
					yield break;
				}

				var result = Encoding.UTF8.GetString(requestSecond.downloadHandler.data);

				Debug.Log(result);
			}
			#endif

			Controller.OnRequestComplete();

			Destroy(gameObject);
		}

		private UnityWebRequest BuildRequestFirst(List<string> names)
		{
			var result = UnityWebRequest.Post(
				"http://158.160.3.255:8021/exercises/set_exercise_data",
				$"[ {string.Join(", ", names)} ]");
			result.SetRequestHeader("ContentType", "application/json");
			return result;
		}

		private UnityWebRequest BuildRequestSecond(string id)
		{
			return UnityWebRequest.Get(
				$"http://158.160.3.255:8021/exercises/get_exercise_data?record_id={id}");
		}
	}
}
