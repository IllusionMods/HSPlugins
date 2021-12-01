using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToolBox.Extensions
{
	internal static class IMGUIExtensions
	{
		public static void SetGlobalFontSize(int size)
		{
			foreach (GUIStyle style in GUI.skin)
				style.fontSize = size;
			GUI.skin = GUI.skin;
		}

		public static void ResetFontSize()
		{
			SetGlobalFontSize(0);
		}

		private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };
#if HONEYSELECT || PLAYHOME || KOIKATSU
		private static readonly Color _backgroundColor = new Color(1f, 1f, 1f, 0.5f);
#elif AISHOUJO || HONEYSELECT2
        private static readonly Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f);
#endif
		private static readonly Texture2D _simpleTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

		public static void DrawBackground(Rect rect)
		{
			Color c = GUI.backgroundColor;
			GUI.backgroundColor = _backgroundColor;
			GUI.Box(rect, "", _customBoxStyle);
			GUI.backgroundColor = c;

		}


		private class DisableCameraControlOnClick : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler
		{
			private bool _cameraControlEnabled = true;

			public void OnPointerDown(PointerEventData eventData)
			{
				this.SetCameraControlEnabled(false);
			}

			public void OnPointerUp(PointerEventData eventData)
			{
				this.SetCameraControlEnabled(true);
			}

			public void OnEndDrag(PointerEventData eventData)
			{
				this.SetCameraControlEnabled(true);
			}

			private void SetCameraControlEnabled(bool e)
			{
				if (Camera.main != null)
				{
					CameraControl control = Camera.main.GetComponent<CameraControl>();
					if (control != null)
					{
						this._cameraControlEnabled = e;
						control.NoCtrlCondition = this.NoCtrlCondition;
					}
				}
			}

			private bool NoCtrlCondition()
			{
				return !this._cameraControlEnabled;
			}

			public override void OnDisable()
			{
				base.OnDisable();
				if (this._cameraControlEnabled == false && Camera.main?.GetComponent<CameraControl>()?.NoCtrlCondition == this.NoCtrlCondition)
					this.SetCameraControlEnabled(true);
			}
		}
		private static Canvas _canvas = null;
		public static RectTransform CreateUGUIPanelForIMGUI(bool addDisableCameraControlComponent = false)
		{
			if (_canvas == null)
			{
				GameObject g = GameObject.Find("IMGUIBackgrounds");
				if (g != null)
					_canvas = g.GetComponent<Canvas>();
				if (_canvas == null)
				{
					GameObject go = new GameObject("IMGUIBackgrounds", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
					go.hideFlags |= HideFlags.HideInHierarchy;
					_canvas = go.GetComponent<Canvas>();
					_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					_canvas.pixelPerfect = true;
					_canvas.sortingOrder = 999;

					CanvasScaler cs = go.GetComponent<CanvasScaler>();
					cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
					cs.referencePixelsPerUnit = 100;
					cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

					GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
					gr.ignoreReversedGraphics = true;
					gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
					GameObject.DontDestroyOnLoad(go);
				}
			}
			GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
			background.transform.SetParent(_canvas.transform, false);
			background.transform.localPosition = Vector3.zero;
			background.transform.localRotation = Quaternion.identity;
			background.transform.localScale = Vector3.one;
			RawImage image = background.GetComponent<RawImage>();
			image.color = new Color32(127, 127, 127, 2);
			image.raycastTarget = true;
			RectTransform rt = (RectTransform)background.transform;
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(0f, 1f);
			background.gameObject.SetActive(false);

			if (addDisableCameraControlComponent)
				rt.gameObject.AddComponent<DisableCameraControlOnClick>();

			return rt;
		}

		public static void FitRectTransformToRect(RectTransform transform, Rect rect)
		{
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, new Vector2(rect.xMin, rect.yMax), _canvas.worldCamera, out Vector2 min) && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, new Vector2(rect.xMax, rect.yMin), _canvas.worldCamera, out Vector2 max))
			{
				transform.offsetMin = new Vector2(min.x, -min.y);
				transform.offsetMax = new Vector2(max.x, -max.y);
			}
		}

		public static void FloatValue(string label, float value, float left, float right, string valueFormat = "", Action<float> onChanged = null)
		{
			GUILayout.BeginHorizontal();
			if (label != null)
				GUILayout.Label(label, GUILayout.ExpandWidth(false));
			float newValue = GUILayout.HorizontalSlider(value, left, right);
			string valueString = newValue.ToString(valueFormat);
			string newValueString = GUILayout.TextField(valueString, GUILayout.Width(50f));

			if (newValueString != valueString)
			{
				float parseResult;
				if (float.TryParse(newValueString, out parseResult))
					newValue = parseResult;
			}
			GUILayout.EndHorizontal();

			if (onChanged != null && !Mathf.Approximately(value, newValue))
				onChanged(newValue);
		}

		public static void FloatValue(string label, float value, string valueFormat = "", Action<float> onChanged = null)
		{
			GUILayout.BeginHorizontal();
			if (label != null)
				GUILayout.Label(label, GUILayout.ExpandWidth(false));
			string valueString = value.ToString(valueFormat);
			string newValueString = GUILayout.TextField(valueString, GUILayout.ExpandWidth(true));

			if (newValueString != valueString)
			{
				float parseResult;
				if (float.TryParse(newValueString, out parseResult))
				{
					if (onChanged != null)
						onChanged(parseResult);
				}
			}
			GUILayout.EndHorizontal();

		}

		public static void IntValue(string label, int value, int left, int right, string valueFormat = "", Action<int> onChanged = null)
		{
			GUILayout.BeginHorizontal();
			if (label != null)
				GUILayout.Label(label, GUILayout.ExpandWidth(false));
			int newValue = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, left, right));
			string valueString = newValue.ToString(valueFormat);
			string newValueString = GUILayout.TextField(valueString, GUILayout.Width(50f));

			if (newValueString != valueString)
			{
				int parseResult;
				if (int.TryParse(newValueString, out parseResult))
					newValue = parseResult;
			}
			GUILayout.EndHorizontal();

			if (onChanged != null && !Mathf.Approximately(value, newValue))
				onChanged(newValue);
		}

		public static void IntValue(string label, int value, string valueFormat = "", Action<int> onChanged = null)
		{
			GUILayout.BeginHorizontal();
			if (label != null)
				GUILayout.Label(label, GUILayout.ExpandWidth(false));
			string valueString = value.ToString(valueFormat);
			string newValueString = GUILayout.TextField(valueString, GUILayout.ExpandWidth(true));

			if (newValueString != valueString)
			{
				int parseResult;
				if (int.TryParse(newValueString, out parseResult))
				{
					if (onChanged != null)
						onChanged(parseResult);
				}
			}
			GUILayout.EndHorizontal();

		}

		public static void ColorValue(string label,
									  Color color,
									  Color reset,
#if HONEYSELECT
									  UI_ColorInfo.UpdateColor onChanged,
#else
                                      Action<Color> onChanged,
#endif
									  bool simplePicker = false,
									  bool simplePickerShowAlpha = true,
									  bool simplePickerHSV = false)

		{
			ColorValue(label, color, reset, "", onChanged, simplePicker, simplePickerShowAlpha, simplePickerHSV);
		}

		public static void ColorValue(string label,
									  Color color,
									  Color reset,
									  string tooltip,
#if HONEYSELECT
									  UI_ColorInfo.UpdateColor onChanged,
#else
                                      Action<Color> onChanged,
#endif
									  bool simplePicker = false,
									  bool simplePickerShowAlpha = true,
									  bool simplePickerHSV = false)
		{
			if (simplePicker == false)
			{
				GUILayout.BeginHorizontal();
				if (label != null)
					GUILayout.Label(new GUIContent(label, tooltip), GUILayout.ExpandWidth(false));
				if (GUILayout.Button(GUIContent.none, GUILayout.Height(60f)))
				{
#if HONEYSELECT
					if (Studio.Studio.Instance.colorMenu.updateColorFunc == onChanged)
						Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
					else
						Studio.Studio.Instance.colorPaletteCtrl.visible = true;
					if (Studio.Studio.Instance.colorPaletteCtrl.visible)
					{
						Studio.Studio.Instance.colorMenu.updateColorFunc = onChanged;
						Studio.Studio.Instance.colorMenu.SetColor(color, UI_ColorInfo.ControlType.PresetsSample);
					}
#else
	                if (Studio.Studio.Instance.colorPalette.visible)
		                Studio.Studio.Instance.colorPalette.visible = false;
	                else
		                Studio.Studio.Instance.colorPalette.Setup(label, color, onChanged, true);
#endif
				}
				Rect layoutRectangle = GUILayoutUtility.GetLastRect();
				layoutRectangle.xMin += 6;
				layoutRectangle.xMax -= 6;
				layoutRectangle.yMin += 6;
				layoutRectangle.yMax -= 6;
				_simpleTexture.SetPixel(0, 0, color);
				_simpleTexture.Apply(false);
				GUI.DrawTexture(layoutRectangle, _simpleTexture, ScaleMode.StretchToFill, true);
				if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
				{
#if HONEYSELECT
					if (onChanged == Studio.Studio.Instance.colorMenu.updateColorFunc)
						Studio.Studio.Instance.colorMenu.SetColor(reset, UI_ColorInfo.ControlType.PresetsSample);
#endif
					onChanged(reset);
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.BeginVertical();
				GUILayout.BeginHorizontal();
				if (label != null)
					GUILayout.Label(new GUIContent(label, tooltip), GUILayout.ExpandWidth(false));
				GUILayout.FlexibleSpace();
				bool shouldReset = GUILayout.Button("Reset", GUILayout.ExpandWidth(false));
				GUILayout.EndHorizontal();

				Color newColor = color;
				if (simplePickerHSV)
				{
					Color.RGBToHSV(color, out float h, out float s, out float v);
					h *= 360;

					GUILayout.BeginHorizontal();
					GUILayout.Label("H", GUILayout.ExpandWidth(false));
					h = GUILayout.HorizontalSlider(h, 0f, 359.99f);
					if (float.TryParse(GUILayout.TextField(h.ToString("0.0"), GUILayout.Width(50)), out float newValue))
						h = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("S", GUILayout.ExpandWidth(false));
					s = GUILayout.HorizontalSlider(s, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(s.ToString("0.000"), GUILayout.Width(50)), out newValue))
						s = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("V", GUILayout.ExpandWidth(false));
					v = GUILayout.HorizontalSlider(v, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(v.ToString("0.000"), GUILayout.Width(50)), out newValue))
						v = newValue;
					GUILayout.EndHorizontal();

					newColor = Color.HSVToRGB(Mathf.Clamp01(h / 360), Mathf.Clamp01(s), v);
					newColor.a = color.a;
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("R", GUILayout.ExpandWidth(false));
					newColor.r = GUILayout.HorizontalSlider(newColor.r, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.r.ToString("0.000"), GUILayout.Width(50)), out float newValue))
						newColor.r = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("G", GUILayout.ExpandWidth(false));
					newColor.g = GUILayout.HorizontalSlider(newColor.g, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.g.ToString("0.000"), GUILayout.Width(50)), out newValue))
						newColor.g = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("B", GUILayout.ExpandWidth(false));
					newColor.b = GUILayout.HorizontalSlider(newColor.b, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.b.ToString("0.000"), GUILayout.Width(50)), out newValue))
						newColor.b = newValue;
					GUILayout.EndHorizontal();
				}

				if (simplePickerShowAlpha)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("A", GUILayout.ExpandWidth(false));
					newColor.a = GUILayout.HorizontalSlider(newColor.a, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.a.ToString("0.000"), GUILayout.Width(50)), out float newValue))
						newColor.a = newValue;
					GUILayout.EndHorizontal();
				}

				GUILayout.Box("", GUILayout.Height(40));
				_simpleTexture.SetPixel(0, 0, color);
				_simpleTexture.Apply(false);
				GUI.DrawTexture(GUILayoutUtility.GetLastRect(), _simpleTexture, ScaleMode.StretchToFill, true);

				if (color != newColor)
					onChanged(newColor);
				GUILayout.EndVertical();
				if (shouldReset)
					onChanged(reset);
			}
		}

		public static void ColorValue(string label,
									  Color color,
#if HONEYSELECT
									  UI_ColorInfo.UpdateColor onChanged,
#else
                                      Action<Color> onChanged,
#endif
									  bool simplePicker = false,
									  bool simplePickerShowAlpha = true,
									  bool simplePickerHSV = false)
		{
			if (simplePicker == false)
			{
				GUILayout.BeginHorizontal();
				if (label != null)
					GUILayout.Label(label, GUILayout.ExpandWidth(false));
				if (GUILayout.Button(GUIContent.none, GUILayout.Height(60f)))
				{
#if HONEYSELECT
					if (Studio.Studio.Instance.colorMenu.updateColorFunc == onChanged)
						Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
					else
						Studio.Studio.Instance.colorPaletteCtrl.visible = true;
					if (Studio.Studio.Instance.colorPaletteCtrl.visible)
					{
						Studio.Studio.Instance.colorMenu.updateColorFunc = onChanged;
						Studio.Studio.Instance.colorMenu.SetColor(color, UI_ColorInfo.ControlType.PresetsSample);
					}
#else
	                if (Studio.Studio.Instance.colorPalette.visible)
		                Studio.Studio.Instance.colorPalette.visible = false;
	                else
		                Studio.Studio.Instance.colorPalette.Setup(label, color, onChanged, true);
#endif
				}
				Rect layoutRectangle = GUILayoutUtility.GetLastRect();
				layoutRectangle.xMin += 6;
				layoutRectangle.xMax -= 6;
				layoutRectangle.yMin += 6;
				layoutRectangle.yMax -= 6;
				_simpleTexture.SetPixel(0, 0, color);
				_simpleTexture.Apply(false);
				GUI.DrawTexture(layoutRectangle, _simpleTexture, ScaleMode.StretchToFill, true);
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.BeginVertical();
				if (label != null)
					GUILayout.Label(label, GUILayout.ExpandWidth(false));

				Color newColor = color;
				if (simplePickerHSV)
				{
					Color.RGBToHSV(newColor, out float h, out float s, out float v);
					h *= 360;

					GUILayout.BeginHorizontal();
					GUILayout.Label("H", GUILayout.ExpandWidth(false));
					h = GUILayout.HorizontalSlider(h, 0f, 359.99f);
					if (float.TryParse(GUILayout.TextField(h.ToString("0.0"), GUILayout.Width(50)), out float newValue))
						h = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("S", GUILayout.ExpandWidth(false));
					s = GUILayout.HorizontalSlider(s, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(s.ToString("0.000"), GUILayout.Width(50)), out newValue))
						s = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("V", GUILayout.ExpandWidth(false));
					v = GUILayout.HorizontalSlider(v, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(v.ToString("0.000"), GUILayout.Width(50)), out newValue))
						v = newValue;
					GUILayout.EndHorizontal();

					newColor = Color.HSVToRGB(Mathf.Clamp01(h / 360), Mathf.Clamp01(s), v);
					newColor.a = color.a;
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("R", GUILayout.ExpandWidth(false));
					newColor.r = GUILayout.HorizontalSlider(newColor.r, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.r.ToString("0.000"), GUILayout.Width(50)), out float newValue))
						newColor.r = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("G", GUILayout.ExpandWidth(false));
					newColor.g = GUILayout.HorizontalSlider(newColor.g, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.g.ToString("0.000"), GUILayout.Width(50)), out newValue))
						newColor.g = newValue;
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("B", GUILayout.ExpandWidth(false));
					newColor.b = GUILayout.HorizontalSlider(newColor.b, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.b.ToString("0.000"), GUILayout.Width(50)), out newValue))
						newColor.b = newValue;
					GUILayout.EndHorizontal();
				}

				if (simplePickerShowAlpha)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("A", GUILayout.ExpandWidth(false));
					newColor.a = GUILayout.HorizontalSlider(newColor.a, 0f, 1f);
					if (float.TryParse(GUILayout.TextField(newColor.a.ToString("0.000"), GUILayout.Width(50)), out float newValue))
						newColor.a = newValue;
					GUILayout.EndHorizontal();
				}

				GUILayout.Box("", GUILayout.Height(40));
				_simpleTexture.SetPixel(0, 0, color);
				_simpleTexture.Apply(false);
				GUI.DrawTexture(GUILayoutUtility.GetLastRect(), _simpleTexture, ScaleMode.StretchToFill, true);

				if (color != newColor)
					onChanged(newColor);
				GUILayout.EndVertical();
			}
		}

		private static SortedList<int, string> _layerNames;
		public static void LayerMaskValue(string label, int value, int columns = 2, Action<int> onChanged = null)
		{
			if (_layerNames == null)
			{
				_layerNames = new SortedList<int, string>(32);
				for (int i = 0; i < 32; i++)
				{
					string name = LayerMask.LayerToName(i);
					if (string.IsNullOrEmpty(name) == false)
						_layerNames.Add(i, name);
				}
			}
			LayerMaskValue(label, value, _layerNames, columns, onChanged);
		}

		public static void LayerMaskValue(string label, int value, SortedList<int, string> layerNames, int columns = 2, Action<int> onChanged = null)
		{
			GUILayout.BeginVertical();
			if (label != null)
				GUILayout.Label(label);
			int newValue = value;
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			int columnSize = Mathf.CeilToInt((float)layerNames.Count / columns);
			int shown = 0;
			foreach (KeyValuePair<int, string> kvp in layerNames)
			{
				if (shown != 0 && shown % columnSize == 0)
				{
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
				}
				if (GUILayout.Toggle((newValue & (1 << kvp.Key)) != 0, $"{kvp.Key}: {kvp.Value}"))
					newValue |= 1 << kvp.Key;
				else
					newValue &= ~(1 << kvp.Key);
				++shown;
			}
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			if (value != newValue && onChanged != null)
				onChanged(newValue);
		}
	}
}