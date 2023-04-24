using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;

namespace Avena.Components
{
	public sealed class CompApp : MonoBehaviour
	{
		// assumptions:
		// assuming none of the target or figures are intersected
		// assuming figures are all of different colors
		// assuming figures are all just a colors (not colorful textures)

		public bool IsInEditMode;
		public bool IsInPlayMode;
		public bool IsTexturesInPlayMode;

		public Camera Camera;

		public SpriteRenderer Player;
		public SpriteRenderer Center;

		[NonSerialized] public Vector3 PlayerInitialPosition;

		public SpriteRenderer Target01;
		public Color MaskColorTarget01;
		public SpriteRenderer Target02;
		public Color MaskColorTarget02;
		public SpriteRenderer Target03;
		public Color MaskColorTarget03;

		public float MaxSpeedPlayerWorld = 4f;

		public InputAction InputActionMoveDirect;
		public InputAction InputActionMoveTarget;
		public InputAction InputActionAction;

		// usually, that's a separate prefab
		[NonSerialized] public CompScreens Screens;

		private Action<InputAction.CallbackContext> _callbackDirectPerformed;
		private Action<InputAction.CallbackContext> _callbackDirectCanceled;
		private Action<InputAction.CallbackContext> _callbackTarget;
		private Action<InputAction.CallbackContext> _callbackAction;

		private SpriteRenderer[] _figures;
		private SpriteRenderer _detected;
		private Coroutine _detection;
		private Camera _testerCamera;
		private RenderTexture _testerScreen;
		private CameraWorldFrustum _frustum;

		private readonly Queue<IControllerMove> _controllerMove = new();
		private readonly Queue<IControllerGame> _controllerGame = new();
		private IControllerGame _current;

		public bool IsTargetDetected => _detected != null;

		private static bool Filter(SpriteRenderer component)
		{
			return
				component.gameObject.layer == LayerMask.NameToLayer("layer_figures") &&
				component.sharedMaterial.shader.name == "Avena/Sprite.Target";
		}

		public unsafe void Awake()
		{
			// last call on initialize

			// any kind of gathering
			var sprites = FindObjectsByType<SpriteRenderer>(
				FindObjectsInactive.Exclude,
				FindObjectsSortMode.None);
			_figures = sprites.Where(Filter).ToArray();

			UnityEngine.Debug.Log($"found figures: {_figures.Length}\n{string.Join("\n", sprites.Where(Filter).Select(_ => _.name))}");

			Model.NamesAll.AddRange(_figures.Select(_ => _.name).OrderBy(_ => _));

			for(var index = 0; index < _figures.Length; index++)
			{
				var component = _figures[index];
				component.material.SetColor("_MaskColor", component.color);
			}

			Target01.material.SetColor("_MaskColor", MaskColorTarget01);
			Target02.material.SetColor("_MaskColor", MaskColorTarget02);
			Target03.material.SetColor("_MaskColor", MaskColorTarget03);

			Center.sortingOrder = -1;
			Player.sortingOrder = -1;
			PlayerInitialPosition = Player.transform.position;

			_frustum = new CameraWorldFrustum
			{
				Origin = Camera.ViewportPointToRay(Vector3.zero).origin,
				PosX = Camera.ViewportPointToRay(Vector3.right).origin,
				PosY = Camera.ViewportPointToRay(Vector3.up).origin,
			};

			var bufferPtr = stackalloc Vector3[8];
			for(var index = 0; index < sprites.Length; index++)
			{
				var bounds = sprites[index].localBounds;
				bounds.ToBuffer(bufferPtr);
				sprites[index].transform.ToWorld(bufferPtr);
				for(var indexBuffer = 0; indexBuffer < 8; indexBuffer++)
				{
					_frustum.Extend(bufferPtr[indexBuffer]);
				}
			}

			var transformCamera = Camera.transform;
			var size = _frustum.Size;
			transformCamera.position = _frustum.Center - transformCamera.forward * Camera.nearClipPlane;
			Camera.orthographicSize = (size.x > size.y ? size.x / Camera.aspect : size.y) * .5f;

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
			_testerCamera.cullingMask = LayerMask.GetMask("layer_targets");
			_testerCamera.enabled = false;

			ModeSet<ControllerGameGame>();
		}

		private void OnDestroy()
		{
			DetectionStop();

			// protocols will be destroyed with the branch

			if(_testerScreen != null)
			{
				DestroyImmediate(_testerScreen);
				_testerScreen = null;
			}
		}

		public void Update()
		{
			if(_controllerGame.TryDequeue(out var next))
			{
				_current?.Disable(this);
				_current = next;
				_current.Enable(this);
			}

			_current?.Update(this);
		}

		private void CameraRender()
		{
			_testerCamera.RenderWithShader(
				Shader.Find("Avena/Detect"),
				"Mask");
		}

		private Texture2D TesterTextureCreate(Rect rect)
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

		private SpriteRenderer TesterTextureScan(Texture2D tester)
		{
			SpriteRenderer result = null;
			// scanline
			var count = tester.width * tester.height - 1;
			while(count > 0)
			{
				var x = count % tester.width;
				var y = count / tester.width;
				count--;
				var color = tester.GetPixel(x, y);

				// Debug.Log($"check color: {color} over {MaskColorTarget01} : {MaskColorTarget02} : {MaskColorTarget03}");

				if(color.Approx(MaskColorTarget01))
				{
					result = Target01;

					break;
				}

				if(color.Approx(MaskColorTarget02))
				{
					result = Target02;

					break;
				}

				if(color.Approx(MaskColorTarget03))
				{
					result = Target03;

					break;
				}
			}

			return result;
		}

		private void DebugDraw(RenderTexture textureScreen, Texture2D textureTester, Rect rectProjection)
		{
			var offset = 0f;
			if(textureScreen != null)
			{
				var size = new Vector2(textureScreen.width, textureScreen.height) * .6f;
				//Debug.Log($"drawing at: {size}");
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
				//Debug.Log($"drawing at: {size}");
				Graphics.DrawTexture(
					new Rect(
						Screen.width - size.x,
						offset,
						size.x,
						size.y),
					textureTester);
				offset += size.y;
			}

			//Debug.Log($"drawing at: {rectProjection}");
			rectProjection.Draw(Camera);
		}

		private IEnumerator Detect()
		{
			while(gameObject.activeSelf)
			{
				yield return new WaitForEndOfFrame();

				CameraRender();
				var rect = Center.ProjectLocalBounds(Camera);
				var tester = TesterTextureCreate(rect);
				var detected = TesterTextureScan(tester);

				if(IsTexturesInPlayMode)
				{
					// broken: will not fix to not to store textures and of limited time - keep as ref
					DebugDraw(_testerScreen, tester, rect);
				}

				DestroyImmediate(tester);

				if(_detected != detected)
				{
					_detected = detected;

					if(_current is ControllerGameGame cast)
					{
						cast.OnDetected(this);
					}
				}
			}
		}

		public void ModeSet<T>() where T : IControllerGame
		{
			_controllerGame.Enqueue(Activator.CreateInstance<T>());
		}

		public void InputEnable()
		{
			_callbackDirectCanceled = OnDirectCanceled;
			_callbackDirectPerformed = OnDirectPerformed;
			_callbackTarget = OnTarget;
			_callbackAction = OnAction;

			InputActionMoveTarget.performed += _callbackTarget;
			InputActionMoveDirect.performed += _callbackDirectPerformed;
			InputActionMoveDirect.canceled += _callbackDirectCanceled;
			InputActionAction.performed += _callbackAction;

			InputActionMoveDirect.Enable();
			InputActionMoveTarget.Enable();
			InputActionAction.Enable();
		}

		public void InputDisable()
		{
			InputActionMoveDirect.Disable();
			InputActionMoveTarget.Disable();
			InputActionAction.Disable();

			InputActionMoveTarget.performed -= _callbackTarget;
			InputActionMoveDirect.performed -= _callbackDirectPerformed;
			InputActionMoveDirect.canceled -= _callbackDirectCanceled;
			InputActionAction.performed -= _callbackAction;

			_callbackDirectCanceled = null;
			_callbackDirectPerformed = null;
			_callbackTarget = null;
			_callbackAction = null;

			_controllerMove.Clear();
		}

		private void OnDirectPerformed(InputAction.CallbackContext context)
		{
			if(!_controllerMove.TryPeek(out var controller) || controller is not ControllerMoveDirect cast)
			{
				while(_controllerMove.TryDequeue(out _)) { }
				cast = new ControllerMoveDirect();
				_controllerMove.Enqueue(cast);
			}

			cast.SetValue(context.ReadValue<Vector2>(), Player.transform, Camera);
		}

		private void OnDirectCanceled(InputAction.CallbackContext context)
		{
			foreach(var controller in _controllerMove)
			{
				if(controller is ControllerMoveDirect cast)
				{
					cast.SetValue(Vector2.zero, Player.transform, Camera);
				}
			}
		}

		private void OnTarget(InputAction.CallbackContext context)
		{
			while(_controllerMove.TryDequeue(out _)) { }
			var cast = new ControllerMoveTarget();
			_controllerMove.Enqueue(cast);

			cast.SetTarget(Pointer.current.position.value, Player.transform, Camera);
		}

		private void OnAction(InputAction.CallbackContext context)
		{
			if(IsTargetDetected && _current is ControllerGameGame cast)
			{
				cast.OnAction(this);
			}
		}

		public void DetectionStart()
		{
			DetectionStop();
			_detection = StartCoroutine(Detect());
		}

		public void DetectionStop()
		{
			if(_detection != null)
			{
				StopCoroutine(_detection);
			}
		}

		public void ProtocolStart()
		{
			var instance = new GameObject("protocol", typeof(CompProtocol));
			instance.transform.SetParent(transform);
			var protocol = instance.GetComponent<CompProtocol>();
			protocol.Controller = this;
			protocol.Camera = Camera;
			protocol.Player = Player;
			protocol.Figures = _figures;
		}

		public void ProtocolPromote(CompProtocol protocol)
		{
			var toDestroy = new List<CompProtocol>();
			var count = transform.childCount;
			for(var index = 0; index < count; index++)
			{
				var current = transform.GetChild(index).GetComponent<CompProtocol>();
				if(current != null && current != protocol)
				{
					toDestroy.Add(current);
				}
			}

			for(var index = 0; index < toDestroy.Count; index++)
			{
				DestroyImmediate(toDestroy[index].gameObject);
			}
		}

		public void ProtocolStopAll()
		{
			ProtocolPromote(null);
		}

		public void OnRequestComplete()
		{
			UnityEngine.Debug.Log("on received");

			if(_current is ControllerGameGame cast)
			{
				cast.OnRequestComplete(this);
			}
		}

		public void PawnMove()
		{
			if(_controllerMove.TryPeek(out var controller))
			{
				Player.transform.position += controller.GetCurrentDelta(
					Player.transform,
					MaxSpeedPlayerWorld);
				if(controller.IsComplete)
				{
					_controllerMove.Dequeue();
				}
			}
		}

		#if UNITY_EDITOR
		public void OnValidate()
		{
			MaskColorTarget01 = new Color(
				Mathf.Clamp(MaskColorTarget01.r, .1f, 1f),
				Mathf.Clamp(MaskColorTarget01.g, .1f, 1f),
				Mathf.Clamp(MaskColorTarget01.b, .1f, 1f),
				1f);
			MaskColorTarget02 = new Color(
				Mathf.Clamp(MaskColorTarget02.r, .1f, 1f),
				Mathf.Clamp(MaskColorTarget02.g, .1f, 1f),
				Mathf.Clamp(MaskColorTarget02.b, .1f, 1f),
				1f);
			MaskColorTarget03 = new Color(
				Mathf.Clamp(MaskColorTarget03.r, .1f, 1f),
				Mathf.Clamp(MaskColorTarget03.g, .1f, 1f),
				Mathf.Clamp(MaskColorTarget03.b, .1f, 1f),
				1f);

			// TODO: differ colors
			// TODO: check Approx() interval
		}

		private void OnDrawGizmos()
		{
			if(Camera == null)
			{
				return;
			}

			if(!IsInEditMode && !Application.isPlaying)
			{
				return;
			}

			if(!IsInPlayMode && Application.isPlaying)
			{
				return;
			}

			var sprites = FindObjectsByType<SpriteRenderer>(
				FindObjectsInactive.Exclude,
				FindObjectsSortMode.None);

			var frustum = new CameraWorldFrustum
			{
				Origin = Camera.ViewportPointToRay(Vector3.zero).origin,
				PosX = Camera.ViewportPointToRay(Vector3.right).origin,
				PosY = Camera.ViewportPointToRay(Vector3.up).origin,
			};

			for(var index = 0; index < sprites.Length; index++)
			{
				var bounds = sprites[index].localBounds;
				frustum.Extend(sprites[index].transform.TransformPoint(bounds.min));
				frustum.Extend(sprites[index].transform.TransformPoint(bounds.max));
			}

			frustum.DrawGUI();

			var plane = Camera.ToPlane();
			var offsetPlane = plane.normal * plane.distance;
			for(var index = 0; index < sprites.Length; index++)
			{
				var bounds = sprites[index].bounds;
				var sizes = bounds.max - bounds.min;

				var projectedMin = Vector3.ProjectOnPlane(bounds.min, plane.normal) - offsetPlane;
				var projectedMax = Vector3.ProjectOnPlane(bounds.max, plane.normal) - offsetPlane;

				var xMin = Vector3.ProjectOnPlane(bounds.min + Vector3.right * sizes.x, plane.normal) - offsetPlane;
				var yMin = Vector3.ProjectOnPlane(bounds.min + Vector3.up * sizes.y, plane.normal) - offsetPlane;
				var xMax = Vector3.ProjectOnPlane(bounds.max - Vector3.right * sizes.x, plane.normal) - offsetPlane;
				var yMax = Vector3.ProjectOnPlane(bounds.max - Vector3.up * sizes.y, plane.normal) - offsetPlane;

				UnityEngine.Debug.DrawLine(projectedMin, xMin, Color.red);
				UnityEngine.Debug.DrawLine(projectedMin, yMin, Color.green);
				UnityEngine.Debug.DrawLine(xMax, projectedMax, Color.grey);
				UnityEngine.Debug.DrawLine(yMax, projectedMax, Color.grey);
				UnityEngine.Debug.DrawLine(xMax, yMin, Color.grey);
				UnityEngine.Debug.DrawLine(yMax, xMin, Color.grey);
			}
		}
		#endif
	}
}
