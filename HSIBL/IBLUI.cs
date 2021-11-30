using UnityEngine;
using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;

//using System.IO;
namespace HSIBL
{
    public static class UIUtils
    {
        internal static void InitStyle()
        {
            scale.x = UnityEngine.Screen.width / Screen.width;
            scale.y = UnityEngine.Screen.height / Screen.height;
            scale.z = 1f;


            font = Font.CreateDynamicFontFromOSFont(new string[] { "Segeo UI", "Microsoft YaHei UI", "Microsoft YaHei" }, 20);
            toggleButtonOn = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                stretchHeight = false,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                font = font,
                margin = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(6, 6, 6, 12),
                fontSize = 22
            };
            toggleButtonOff = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                stretchHeight = false,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                font = font,
                margin = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(6, 6, 6, 12),
                fontSize = 22
            };
            toggleButtonOn.onNormal.textColor = selected;
            toggleButtonOn.onHover.textColor = selectedOnHover;
            toggleButtonOn.normal = toggleButtonOn.onNormal;
            toggleButtonOn.hover = toggleButtonOn.onHover;
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                stretchHeight = false,
                stretchWidth = true,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                font = font,
                fontSize = 22,
                padding = new RectOffset(6, 6, 6, 12)
            };
            windowStyle = new GUIStyle(GUI.skin.window);
            selectStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                font = font
            };
            selectStyle.onNormal.textColor = selected;
            selectStyle.onHover.textColor = selectedOnHover;
            buttonStyleNoStretch = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                stretchHeight = false,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                font = font,
                padding = new RectOffset(12, 12, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                fontSize = 22
            };

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontStyle = FontStyle.Bold,
                font = font,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                fontSize = 22,
                alignment = TextAnchor.MiddleRight
            };

            textFieldStyle2 = new GUIStyle(GUI.skin.textField)
            {
                fontStyle = FontStyle.Bold,
                font = font,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                fontSize = 22,
                alignment = TextAnchor.MiddleLeft
            };

            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                padding = new RectOffset(-10, -10, -4, -4),
                fixedHeight = 16f,
                margin = new RectOffset(22, 22, 22, 22)
            };
            thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedHeight = 24f,
                padding = new RectOffset(14, 14, 12, 12)

            };
            labelStyle2 = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 20,
                margin = new RectOffset(16, 16, 8, 8)
            };
            titleStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                font = font,
                fontSize = 30,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.onNormal.textColor = selected;
            titleStyle.onHover.textColor = selectedOnHover;
            titleStyle2 = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                font = font,
                fontSize = 30,
                padding = new RectOffset(6, 6, 6, 12),
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 24,
                padding = new RectOffset(6, 6, 6, 12),
                alignment = TextAnchor.MiddleLeft
            };
            labelStyleNoStretch = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 24,
                padding = new RectOffset(6, 6, 6, 12),
                alignment = TextAnchor.MiddleLeft,
                stretchWidth = false
            };
            buttonStyleStrechWidth = new GUIStyle(GUI.skin.button)
            {
                stretchHeight = false,
                stretchWidth = true,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                font = font,
                fontSize = 22,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(6, 6, 6, 12)
            };
            buttonStyleStrechWidth.onNormal.textColor = selected;
            buttonStyleStrechWidth.onHover.textColor = selectedOnHover;
            buttonStyleStrechWidthAlignLeft = new GUIStyle(GUI.skin.button)
            {
                stretchHeight = false,
                stretchWidth = true,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                font = font,
                fontSize = 22,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(6, 6, 6, 12),
                alignment = TextAnchor.MiddleLeft
            };
            buttonStyleStrechWidthAlignLeft.onNormal.textColor = selected;
            buttonStyleStrechWidthAlignLeft.onHover.textColor = selectedOnHover;
            labelStyle3 = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 22
            };

            space = 12f;
            minWidth = Mathf.Round(0.27f * Screen.width);
            styleInitialized = true;

            windowRect.x = ModPrefs.GetFloat("HSIBL", "Window.x", windowRect.x, true);
            windowRect.y = ModPrefs.GetFloat("HSIBL", "Window.y", windowRect.y, true);
            windowRect.width = Mathf.Min(Screen.width - 10f, ModPrefs.GetFloat("HSIBL", "Window.width", windowRect.width, true));
            windowRect.height = Mathf.Clamp(ModPrefs.GetFloat("HSIBL", "Window.height", windowRect.height, true), Screen.height * 0.2f, Screen.height * 0.9f);
        }

        internal static float SliderGUI(float value, float min, float max, string labeltext, string valuedecimals)
        {
            return SliderGUI(value, min, max, labeltext, "", valuedecimals);
        }
        internal static float SliderGUI(float value, float min, float max, GUIContent guiContent, string valuedecimals)
        {
            GUILayout.Label(guiContent, labelStyle);
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderStyle, thumbStyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, string labeltext, string tooltip, string valuedecimals)
        {
            GUILayout.Label(new GUIContent(labeltext, tooltip), labelStyle);
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderStyle, thumbStyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, float reset, string labeltext, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, labeltext, "", valuedecimals);
        }

        internal static float SliderGUI(float value, float min, float max, Func<float> reset, string labeltext, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, labeltext, "", valuedecimals);
        }
        internal static float SliderGUI(float value, float min, float max, Func<float> reset, GUIContent label, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, label.text, label.tooltip, valuedecimals);
        }
        internal static float SliderGUI(float value, float min, float max, Func<float> reset, string labeltext, string tooltip, string valuedecimals)
        {
            if (reset == null)
            {
                throw new ArgumentNullException(nameof(reset));
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(labeltext, tooltip), labelStyle);
            GUILayout.Space(space);
            if (GUILayout.Button(new GUIContent(GUIStrings.Reset, tooltip), buttonStyleNoStretch))
            {
                value = reset();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderStyle, thumbStyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, float reset, GUIContent guiContent, string valuedecimals)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(guiContent, labelStyle);
            GUILayout.Space(space);
            if (GUILayout.Button(new GUIContent(GUIStrings.Reset, guiContent.tooltip), buttonStyleNoStretch))
            {
                value = reset;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderStyle, thumbStyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, float reset, string labeltext, string tooltip, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, new GUIContent(labeltext, tooltip), valuedecimals);
        }
        internal static void ColorPickerGUI(Color value, Color reset, string labeltext, UI_ColorInfo.UpdateColor onSet)
        {
            ColorPickerGUI(value, reset, labeltext, "", onSet);
        }

        internal static void ColorPickerGUI(Color value, Color reset, string labeltext, string tooltip, UI_ColorInfo.UpdateColor onSet)
        {
            if (HSIBL._isStudio)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(labeltext, tooltip), labelStyle, GUILayout.ExpandWidth(false));
                if (GUILayout.Button(GUIContent.none, GUILayout.Height(100f)))
                {
                    if (Studio.Studio.Instance.colorMenu.updateColorFunc == onSet)
                        Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
                    else
                        Studio.Studio.Instance.colorPaletteCtrl.visible = true;
                    if (Studio.Studio.Instance.colorPaletteCtrl.visible)
                    {
                        Studio.Studio.Instance.colorMenu.updateColorFunc = onSet;
                        Studio.Studio.Instance.colorMenu.SetColor(value, UI_ColorInfo.ControlType.PresetsSample);
                    }
                }
                Rect layoutRectangle = GUILayoutUtility.GetLastRect();
                layoutRectangle.xMin += 6;
                layoutRectangle.xMax -= 6;
                layoutRectangle.yMin += 6;
                layoutRectangle.yMax -= 6;
                simpleTexture.SetPixel(0, 0, value);
                simpleTexture.Apply(false);
                GUI.DrawTexture(layoutRectangle, simpleTexture, ScaleMode.StretchToFill, true);
                if (GUILayout.Button(new GUIContent(GUIStrings.Reset, tooltip), buttonStyleNoStretch))
                {
                    if (onSet == Studio.Studio.Instance.colorMenu.updateColorFunc)
                        Studio.Studio.Instance.colorMenu.SetColor(reset, UI_ColorInfo.ControlType.PresetsSample);
                    onSet(reset);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(labeltext, tooltip), labelStyle, GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
                bool shouldReset = GUILayout.Button(new GUIContent(GUIStrings.Reset, tooltip), buttonStyleNoStretch);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("R", labelStyle, GUILayout.ExpandWidth(false));
                value.r = GUILayout.HorizontalSlider(value.r, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.r.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out float newValue))
                    value.r = newValue;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("G", labelStyle, GUILayout.ExpandWidth(false));
                value.g = GUILayout.HorizontalSlider(value.g, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.g.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out newValue))
                    value.g = newValue;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("B", labelStyle, GUILayout.ExpandWidth(false));
                value.b = GUILayout.HorizontalSlider(value.b, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.b.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out newValue))
                    value.b = newValue;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("A", labelStyle, GUILayout.ExpandWidth(false));
                value.a = GUILayout.HorizontalSlider(value.a, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.a.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out newValue))
                    value.a = newValue;
                GUILayout.EndHorizontal();

                GUILayout.Box("", GUILayout.Height(40));
                simpleTexture.SetPixel(0, 0, value);
                simpleTexture.Apply(false);
                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), simpleTexture, ScaleMode.StretchToFill, true);

                onSet(value);
                GUILayout.EndVertical();
                if (shouldReset)
                    onSet(reset);
            }
        }

        internal static void ColorPickerGUI(Color value, string labeltext, UI_ColorInfo.UpdateColor onSet)
        {
            if (HSIBL._isStudio)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(labeltext, labelStyle, GUILayout.ExpandWidth(false));
                if (GUILayout.Button(GUIContent.none, GUILayout.ExpandHeight(true)))
                {
                    if (Studio.Studio.Instance.colorMenu.updateColorFunc == onSet)
                        Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
                    else
                        Studio.Studio.Instance.colorPaletteCtrl.visible = true;
                    if (Studio.Studio.Instance.colorPaletteCtrl.visible)
                    {
                        Studio.Studio.Instance.colorMenu.updateColorFunc = onSet;
                        Studio.Studio.Instance.colorMenu.SetColor(value, UI_ColorInfo.ControlType.PresetsSample);
                    }
                }
                Rect layoutRectangle = GUILayoutUtility.GetLastRect();
                layoutRectangle.xMin += 6;
                layoutRectangle.xMax -= 6;
                layoutRectangle.yMin += 6;
                layoutRectangle.yMax -= 6;
                simpleTexture.SetPixel(0, 0, value);
                simpleTexture.Apply(false);
                GUI.DrawTexture(layoutRectangle, simpleTexture, ScaleMode.StretchToFill, true);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.Label(labeltext, labelStyle, GUILayout.ExpandWidth(false));

                GUILayout.BeginHorizontal();
                GUILayout.Label("R", labelStyle, GUILayout.ExpandWidth(false));
                value.r = GUILayout.HorizontalSlider(value.r, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.r.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out float newValue))
                    value.r = newValue;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("G", labelStyle, GUILayout.ExpandWidth(false));
                value.g = GUILayout.HorizontalSlider(value.g, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.g.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out newValue))
                    value.g = newValue;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("B", labelStyle, GUILayout.ExpandWidth(false));
                value.b = GUILayout.HorizontalSlider(value.b, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(value.b.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out newValue))
                    value.b = newValue;
                GUILayout.EndHorizontal();

                //GUILayout.BeginHorizontal();
                //GUILayout.Label("A", labelStyle, GUILayout.ExpandWidth(false));
                //value.a = GUILayout.HorizontalSlider(value.a, 0f, 1f, sliderStyle, thumbStyle);
                //if (float.TryParse(GUILayout.TextField(value.a.ToString("0.000"), textFieldStyle, GUILayout.Width(90)), out newValue))
                //    value.a = newValue;
                //GUILayout.EndHorizontal();

                GUILayout.Box("", GUILayout.Height(40));
                simpleTexture.SetPixel(0, 0, value);
                simpleTexture.Apply(false);
                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), simpleTexture, ScaleMode.StretchToFill, true);

                onSet(value);
                GUILayout.EndVertical();
            }
        }

        internal static int SelectGUI(int selected, GUIContent title, string[] selections)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle);
            GUILayout.FlexibleSpace();
            GUIContent[] selectionGUIContent = new GUIContent[selections.Length];
            uint num = 0;
            foreach (string s in selections)
            {
                selectionGUIContent[num] = new GUIContent(s, title.tooltip);
                num++;
            }
            selected = GUILayout.SelectionGrid(selected, selectionGUIContent, selections.Length, selectStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            return selected;
        }
        internal static int SelectGUI(int selected, GUIContent title, string[] selections, Action<int> Action)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle);
            GUILayout.FlexibleSpace();
            GUIContent[] selectionGUIContent = new GUIContent[selections.Length];
            uint num = 0;
            foreach (string s in selections)
            {
                selectionGUIContent[num] = new GUIContent(s, title.tooltip);
                num++;
            }
            int temp = GUILayout.SelectionGrid(selected, selectionGUIContent, selections.Length, selectStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (temp == selected)
            {
                return selected;
            }
            else
            {
                Action(temp);
                return temp;
            }
        }
        internal static bool ToggleButton(bool toggle, GUIContent label, Action<bool> Action)
        {
            if (GUILayout.Button(label, (toggle ? toggleButtonOn : toggleButtonOff)))
            {
                toggle = !toggle;
                Action(toggle);
            }
            return toggle;
        }
        internal static bool ToggleButton(bool toggle, GUIContent label)
        {
            if (GUILayout.Button(label, (toggle ? toggleButtonOn : toggleButtonOff)))
            {
                toggle = !toggle;
            }
            return toggle;
        }
        internal static bool ToggleGUI(bool toggle, GUIContent title, string option1 = "Disable", string option2 = "Enable")
        {
            return ToggleGUI(toggle, title, labelStyle, option1, option2);
        }

        internal static bool ToggleGUI(bool toggle, GUIContent title, GUIStyle titleStyle, string option1 = "Disable", string option2 = "Enable")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, titleStyle);
            GUILayout.FlexibleSpace();
            int temp = 0;
            if (toggle)
            {
                temp = 1;
            }
            temp = GUILayout.SelectionGrid(temp, new[]
            {
                new GUIContent(option1, title.tooltip),
                new GUIContent(option2, title.tooltip)
            }, 2, selectStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (temp == 0)
            {
                return false;
            }
            return true;
        }

        internal static bool ToggleGUI(bool toggle, GUIContent title, string[] switches, Action<bool> Action)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle);
            GUILayout.FlexibleSpace();
            int temp = Convert.ToInt32(toggle);
            temp = GUILayout.SelectionGrid(temp, new GUIContent[]
            {
                new GUIContent(switches[0], title.tooltip),
                new GUIContent(switches[1], title.tooltip)
            }, 2, selectStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if ((temp != 0) == toggle)
            {
                return toggle;
            }
            else if (temp == 0)
            {
                Action(false);
                return false;
            }
            Action(true);
            return true;

        }
        internal static bool ToggleGUITitle(bool toggle, GUIContent title, string[] switches, GUIStyle titleStyle, Action onUp, Action onDown)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("▲", UIUtils.buttonStyleNoStretch, GUILayout.ExpandWidth(false)))
            {
                if (onUp != null)
                    onUp();
            }
            if (GUILayout.Button("▼", UIUtils.buttonStyleNoStretch, GUILayout.ExpandWidth(false)))
            {
                if (onDown != null)
                    onDown();
            }

            int temp = GUILayout.SelectionGrid(toggle ? 1 : 0, new[]
            {
                new GUIContent(switches[0], title.tooltip),
                new GUIContent(switches[1], title.tooltip)
            }, 2, selectStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            return temp != 0;
        }

        internal static int LayerMaskValue(int value, GUIContent label, SortedList<int, string> layerNames, string option1 = "Disable", string option2 = "Enable", int columns = 2)
        {
	        GUILayout.BeginVertical();
	        if (label != null)
		        GUILayout.Label(label, labelStyle);
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
		        if (ToggleGUI((newValue & (1 << kvp.Key)) != 0, new GUIContent($"{kvp.Key}: {kvp.Value}"), option1, option2))
			        newValue |= 1 << kvp.Key;
		        else
			        newValue &= ~(1 << kvp.Key);
		        ++shown;
	        }
	        GUILayout.EndVertical();
	        GUILayout.EndHorizontal();
	        GUILayout.EndVertical();
	        return newValue;
        }

        internal static void Gradient(Gradient gradient, string labeltext, string tooltip, bool showAlphas = true, Action onChange = null, Texture2D userGeneratedTexture = null)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(new GUIContent(labeltext, tooltip), UIUtils.labelStyle);

            if (userGeneratedTexture != null)
            {
                GUILayout.Label(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(60));
                Rect layoutRectangle = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(layoutRectangle, userGeneratedTexture, ScaleMode.StretchToFill, true);
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Colors", UIUtils.labelStyle);
            for (int i = 0; i < gradient.colorKeys.Length; i++)
            {
                GradientColorKey colorKey = gradient.colorKeys[i];
                int cachedI = i;
                UIUtils.ColorPickerGUI(colorKey.color, i.ToString(), (c) =>
                {
                    GradientColorKey newKey = gradient.colorKeys[cachedI];
                    newKey.color = c;
                    GradientColorKey[] newColorKeys = new GradientColorKey[gradient.colorKeys.Length];
                    Array.Copy(gradient.colorKeys, newColorKeys, gradient.colorKeys.Length);
                    newColorKeys[cachedI] = newKey;
                    gradient.SetKeys(newColorKeys, gradient.alphaKeys);
                    if (onChange != null)
                        onChange();
                });
                GUILayout.BeginHorizontal();
                float newTime = GUILayout.HorizontalSlider(colorKey.time, 0f, 1f, sliderStyle, thumbStyle);
                if (float.TryParse(GUILayout.TextField(newTime.ToString("N3"), textFieldStyle, GUILayout.Width(75)), out float newValue))
                    newTime = newValue;
                GUILayout.EndHorizontal();
                if (Mathf.Approximately(newTime, colorKey.time) == false)
                {
                    colorKey.time = newTime;
                    GradientColorKey[] newColorKeys = new GradientColorKey[gradient.colorKeys.Length];
                    Array.Copy(gradient.colorKeys, newColorKeys, gradient.colorKeys.Length);
                    newColorKeys[i] = colorKey;
                    gradient.SetKeys(newColorKeys, gradient.alphaKeys);
                    if (onChange != null)
                        onChange();
                }
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Size", UIUtils.labelStyle, GUILayout.ExpandWidth(false));
            if (GUILayout.Button("-", buttonStyleStrechWidth))
                gradient.SetKeys(gradient.colorKeys.Take(gradient.colorKeys.Length - 1).ToArray(), gradient.alphaKeys);
            if (GUILayout.Button("+", buttonStyleStrechWidth))
            {
                List<GradientColorKey> gradientColorKeys = gradient.colorKeys.ToList();
                gradientColorKeys.Add(new GradientColorKey());
                gradient.SetKeys(gradientColorKeys.ToArray(), gradient.alphaKeys);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (showAlphas)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Alphas", labelStyle);
                for (int i = 0; i < gradient.alphaKeys.Length; i++)
                {
                    GradientAlphaKey alphaKey = gradient.alphaKeys[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i.ToString(), labelStyle, GUILayout.ExpandWidth(false));
                    float newAlpha = GUILayout.HorizontalSlider(alphaKey.alpha, 0f, 1f, sliderStyle, thumbStyle);
                    if (float.TryParse(GUILayout.TextField(newAlpha.ToString("N3"), textFieldStyle, GUILayout.Width(75)), out float newValue))
                        newAlpha = newValue;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    float newTime = GUILayout.HorizontalSlider(alphaKey.time, 0f, 1f, sliderStyle, thumbStyle);
                    if (float.TryParse(GUILayout.TextField(newTime.ToString("N3"), textFieldStyle, GUILayout.Width(75)), out newValue))
                        newTime = newValue;
                    GUILayout.EndHorizontal();

                    if (Mathf.Approximately(newAlpha, alphaKey.alpha) == false || Mathf.Approximately(newTime, alphaKey.time) == false)
                    {
                        alphaKey.alpha = newAlpha;
                        alphaKey.time = newTime;
                        GradientAlphaKey[] newAlphaKeys = new GradientAlphaKey[gradient.alphaKeys.Length];
                        Array.Copy(gradient.alphaKeys, newAlphaKeys, gradient.alphaKeys.Length);
                        newAlphaKeys[i] = alphaKey;
                        gradient.SetKeys(gradient.colorKeys, newAlphaKeys);
                        if (onChange != null)
                            onChange();
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Size", labelStyle, GUILayout.ExpandWidth(false));
                if (GUILayout.Button("-", buttonStyleStrechWidth))
                    gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys.Take(gradient.alphaKeys.Length - 1).ToArray());
                if (GUILayout.Button("+", buttonStyleStrechWidth))
                {
                    List<GradientAlphaKey> gradientAlphaKeys = gradient.alphaKeys.ToList();
                    gradientAlphaKeys.Add(new GradientAlphaKey());
                    gradient.SetKeys(gradient.colorKeys, gradientAlphaKeys.ToArray());
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        internal static void HorizontalLine()
        {
            GUILayout.Label("", GUILayout.Height(4));
            Color c = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = c;
        }

        internal static class Screen
        {
            internal static float width = 3840;
            internal static float height = 2160;
        }
        static internal Rect LimitWindowRect(Rect windowrect)
        {
            if (windowrect.x <= 0)
            {
                windowrect.x = 5f;
            }
            if (windowrect.y <= 0)
            {
                windowrect.y = 5f;
            }
            if (windowrect.xMax >= Screen.width)
            {
                windowrect.x -= 5f + windowrect.xMax - Screen.width;
            }
            if (windowrect.yMax >= Screen.height)
            {
                windowrect.y -= 5f + windowrect.yMax - Screen.height;
            }
            return windowrect;
        }

        internal static int hsvcolorpicker = 0;
        //internal static float customscale;
        internal static Vector3 scale;
        static Font font;
        static Color selected = new Color(0.1f, 0.75f, 1f);
        static Color selectedOnHover = new Color(0.1f, 0.6f, 0.8f);
        internal static float minWidth;
        internal static Rect tooltipRect = new Rect(Screen.width * 0.35f, Screen.height * 0.64f, Screen.width * 0.15f, Screen.height * 0.45f);
        internal static Rect windowRect = new Rect(Screen.width / 2 - 550f, Screen.height / 2 - 300f, 1100f, 600f);
        internal static Rect warningRect = new Rect(Screen.width * 0.425f, Screen.height * 0.45f, Screen.width * 0.15f, Screen.height * 0.1f);
        internal static GUIStyle selectStyle;
        internal static GUIStyle buttonStyleNoStretch;
        internal static GUIStyle sliderStyle;
        internal static GUIStyle thumbStyle;
        internal static GUIStyle labelStyle2;
        internal static GUIStyle titleStyle;
        internal static GUIStyle titleStyle2;
        internal static GUIStyle labelStyle;
        internal static GUIStyle labelStyleNoStretch;
        internal static GUIStyle buttonStyleStrechWidth;
        internal static GUIStyle buttonStyleStrechWidthAlignLeft;
        internal static GUIStyle toggleButtonOn;
        internal static GUIStyle toggleButtonOff;
        internal static GUIStyle windowStyle;
        internal static Vector2[] scrollPosition = new Vector2[5];
        internal static bool styleInitialized = false;
        internal static float space;
        internal static GUIStyle labelStyle3;
        internal static GUIStyle boxStyle;
        internal static GUIStyle textFieldStyle;
        internal static GUIStyle textFieldStyle2;
        internal static Rect cmWarningRect = new Rect(0, 0, Screen.width * 0.2f, Screen.height * 0.1f);
        internal static Rect errorWindowRect = new Rect(Screen.width * 0.4f, Screen.height * 0.45f, Screen.width * 0.2f, Screen.height * 0.1f);
        internal static readonly Texture2D simpleTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
    }
}