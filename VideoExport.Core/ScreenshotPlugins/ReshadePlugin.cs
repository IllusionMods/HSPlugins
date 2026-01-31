using System;
using UnityEngine;
using VideoExport.Core;
using System.Reflection;
using System.Linq;


#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace VideoExport.ScreenshotPlugins
{
    public class ReshadePlugin : IScreenshotPlugin
    {
        public string name => "Reshade";

        public Vector2 currentSize
        {
            get
            {
                int width = Mathf.RoundToInt(Screen.width);
                int height = Mathf.RoundToInt(Screen.height);

                if (width % 2 != 0)
                    width += 1;
                if (height % 2 != 0)
                    height += 1;

                return new Vector2(width, height);
            }
        }
        public bool transparency => !_removeAlphaChannel;

        public string extension
        {
            get
            {
                switch (this._imageFormat)
                {
                    default:
                    case VideoExport.ImgFormat.BMP:
                        return "bmp";
                    case VideoExport.ImgFormat.PNG:
                        return "png";
                    case VideoExport.ImgFormat.JPG:
                        return "jpg";
                }
            }
        }
        public byte bitDepth => 8;

        public VideoExport.ImgFormat imageFormat => _imageFormat;
        private VideoExport.ImgFormat _imageFormat;
        private bool _autoHideUI;
        private bool _removeAlphaChannel;
        private string[] _imageFormatNames;
        public bool vFlip;

#if IPA
        public bool Init(HarmonyInstance harmony)
#elif BEPINEX
        public bool Init(Harmony harmony)
#endif
        {
            this._imageFormat = (VideoExport.ImgFormat)VideoExport._configFile.AddInt("reshadeImageFormat", (int)VideoExport.ImgFormat.BMP, true);
            this._imageFormatNames = Enum.GetNames(typeof(VideoExport.ImgFormat)).Where(x => x != nameof(VideoExport.ImgFormat.EXR)).ToArray();
            this._autoHideUI = VideoExport._configFile.AddBool("autoHideUI", true, true);
            this._removeAlphaChannel = VideoExport._configFile.AddBool("removeAlphaChannel", true, true);

            bool opened = ReshadeAPI.OpenSharedMemory();
            ReshadeAPI.SetCapture(false);
            
            return opened;
        }

        public void UpdateLanguage()
        {
        }

        public byte[] Capture(string saveTo)
        {
            Texture2D texture = CaptureTexture();
            byte[] result = TextureEncoder.EncodeTexture(texture, imageFormat);
            UnityEngine.Object.Destroy(texture);
            return result;
        }

        public bool IsTextureCaptureAvailable()
        {
            return true;
        }

        public bool IsRenderTextureCaptureAvailable()
        {
            return false;
        }

        public bool IsVFlipNeeded()
        {
            return false;
        }

        public Texture2D CaptureTexture()
        {
            return ReshadeAPI.RequestScreenshot(_removeAlphaChannel, vFlip);
        }

        public RenderTexture CaptureRenderTexture()
        {
            return null;
        }

        public void OnStartRecording()
        {
            if (_autoHideUI)
            {
                VideoExport.ShowUI = false;
                SetStudioUIVisibility(false);
            }
            ReshadeAPI.SetCapture(true);
        }

        public void OnEndRecording()
        {
            ReshadeAPI.SetCapture(false);
            if (_autoHideUI)
            {
                VideoExport.ShowUI = true;
                SetStudioUIVisibility(true);
            }
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormat), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormatTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                this._imageFormat = (VideoExport.ImgFormat)GUILayout.SelectionGrid((int)this._imageFormat, this._imageFormatNames, this._imageFormatNames.Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                this._autoHideUI = GUILayout.Toggle(_autoHideUI, VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AutoHideUI));
                this._removeAlphaChannel = GUILayout.Toggle(_removeAlphaChannel, new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.RemoveAlphaChannel), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.RemoveAlphaChannelTooltip).Replace("\\n", "\n")));
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("reshadeImageFormat", (int)_imageFormat);
            VideoExport._configFile.SetBool("autoHideUI", _autoHideUI);
            VideoExport._configFile.SetBool("removeAlphaChannel", _removeAlphaChannel);
        }

        private void SetStudioUIVisibility(bool target_visibility)
        {
#if KOIKATSU
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.Koikatu");
#elif SUNSHINE
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.KoikatsuSunshine");
#elif HONEYSELECT2
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.HoneySelect2");
#elif AISHOUJO
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.AISyoujyo");
#endif

            if (hideUI == null) return;

            var handlerField = hideUI.GetField("currentUIHandler", BindingFlags.NonPublic | BindingFlags.Static);
            if (handlerField == null) return;

            object handlerInstance = handlerField.GetValue(null);
            if (handlerInstance == null) return;

            var visibleField = handlerInstance.GetType().GetField("visible", BindingFlags.NonPublic | BindingFlags.Instance);
            if (visibleField == null) return;

            bool current_visibility = (bool)visibleField.GetValue(handlerInstance);
            if (current_visibility != target_visibility)
            {
                var toggleMethod = handlerInstance.GetType().GetMethod("ToggleUI", BindingFlags.Public | BindingFlags.Instance);
                if (toggleMethod == null) return;

                Action _toggleUI = (Action)Delegate.CreateDelegate(typeof(Action), handlerInstance, toggleMethod);
                _toggleUI();
            }
        }
    }
}
