
using ToolBox.Extensions;
#if HONEYSELECT
using System;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class PlayShot24ZHNeo : IScreenshotPlugin
    {
        private delegate void CaptureFunctionDelegate(int size);

        private static byte[] _currentBytes;
        private static bool _recordingVideo = false;

        private CaptureFunctionDelegate _captureFunction;
        private CaptureFunctionDelegate _captureFunctionTransparent;
        private bool _transparent = false;
        private int _currentSize = 1;

        public string name { get { return "PlayShot"; } }
        public Vector2 currentSize { get { return new Vector2(Screen.width * this._currentSize, Screen.height * this._currentSize); } }
        public bool transparency { get { return this._transparent; } }
        public string extension { get { return "png"; } }
        public byte bitDepth { get { return 8; } }

        public bool Init(HarmonyInstance harmony)
        {
            Type playShotType = Type.GetType("ScreenShot,PlayShot24ZHNeo");
            if (playShotType == null)
                return false;
            Component c = (Component)GameObject.FindObjectOfType(playShotType);
            if (c == null)
                return false;
            MethodInfo myScreenShot = playShotType.GetMethod("myScreenShot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo myScreenShotTransparent = playShotType.GetMethod("myScreenShotTransparent", BindingFlags.Public | BindingFlags.Instance);
            if (myScreenShot == null || myScreenShotTransparent == null)
            {
                UnityEngine.Debug.LogError("VideoExport: PlayShot24ZHNeo was found but seems out of date, please update it.");
                return false;
            }
            this._captureFunction = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), c, myScreenShot);
            this._captureFunctionTransparent = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), c, myScreenShotTransparent);
            this._transparent = VideoExport._configFile.AddBool("PlayShot24ZHNeo_transparent", false, true);
            this._currentSize = VideoExport._configFile.AddInt("PlayShot24ZHNeo_size", 1, true);
            try
            {
                harmony.Patch(myScreenShot, new HarmonyMethod(typeof(PlayShot24ZHNeo), nameof(myScreenShot_Prefix), new[] {typeof(int)}));
                harmony.Patch(myScreenShotTransparent, new HarmonyMethod(typeof(PlayShot24ZHNeo), nameof(myScreenShotTransparent_Prefix), new[] {typeof(int)}));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("VideoExport: Couldn't patch method.\n" + e);
                return false;
            }
            return true;
        }

        public void UpdateLanguage()
        {
        }

        public void OnStartRecording()
        {
        }

        public byte[] Capture(string saveTo)
        {
            _recordingVideo = true;
            if (this._transparent)
                this._captureFunctionTransparent(this._currentSize);
            else
                this._captureFunction(this._currentSize);
            _recordingVideo = false;
            return _currentBytes;
        }

        public void OnEndRecording()
        {
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            this._transparent = GUILayout.Toggle(this._transparent, VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Transparent));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.SizeMultiplier), GUILayout.ExpandWidth(false));
                this._currentSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._currentSize, 1, 4));
                GUILayout.Label(this._currentSize.ToString(), GUILayout.Width(15));
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetBool("PlayShot24ZHNeo_transparent", this._transparent);
            VideoExport._configFile.SetInt("PlayShot24ZHNeo_size", this._currentSize);
        }

        private static bool myScreenShot_Prefix(int size)
        {
            if (_recordingVideo == false)
                return true;
            Texture2D texture2D = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)3, false);
            RenderTexture renderTexture = new RenderTexture(texture2D.width, texture2D.height, 24);
            Camera main = Camera.main;
            if (main.isActiveAndEnabled && !(main.targetTexture != null))
            {
                RenderTexture targetTexture = main.targetTexture;
                main.targetTexture = renderTexture;
                main.Render();
                main.targetTexture = targetTexture;
                RenderTexture.active = renderTexture;
                texture2D.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
                texture2D.Apply();
                RenderTexture.active = null;
                _currentBytes = texture2D.EncodeToPNG();
            }
            return false;
        }

        private static bool myScreenShotTransparent_Prefix(int size)
        {
            if (_recordingVideo == false)
                return true;
            Texture2D texture2D = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)3, false);
            Texture2D texture2D2 = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)3, false);
            Texture2D texture2D3 = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)5, false);
            RenderTexture renderTexture = new RenderTexture(texture2D.width, texture2D.height, 24);
            RenderTexture renderTexture2 = new RenderTexture(texture2D.width, texture2D.height, 24);
            Camera main = Camera.main;
            if (main.isActiveAndEnabled && !(main.targetTexture != null))
            {
                Color backgroundColor = main.backgroundColor;
                RenderTexture targetTexture = main.targetTexture;
                main.backgroundColor = Color.white;
                main.targetTexture = renderTexture;
                main.Render();
                main.targetTexture = targetTexture;
                RenderTexture.active = renderTexture;
                texture2D.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
                texture2D.Apply();
                main.backgroundColor = Color.black;
                main.targetTexture = renderTexture2;
                main.Render();
                main.targetTexture = targetTexture;
                RenderTexture.active = renderTexture2;
                texture2D2.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
                texture2D2.Apply();
                RenderTexture.active = null;
                for (int i = 0; i < texture2D3.height; i++)
                {
                    for (int j = 0; j < texture2D3.width; j++)
                    {
                        float num = texture2D.GetPixel(j, i).r - texture2D2.GetPixel(j, i).r;
                        num = 1f - num;
                        Color color;
                        if (num == 0f)
                        {
                            color = Color.clear;
                        }
                        else
                        {
                            color = texture2D2.GetPixel(j, i) / num;
                        }
                        color.a = num;
                        texture2D3.SetPixel(j, i, color);
                    }
                }
                _currentBytes = texture2D3.EncodeToPNG();
                main.backgroundColor = backgroundColor;
            }
            return false;
        }

    }
}
#endif
