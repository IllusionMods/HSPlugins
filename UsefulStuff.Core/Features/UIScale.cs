using System.Collections.Generic;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS.Features
{
    public class UIScale : IFeature
    {
        private class CanvasData
        {
            public float scaleFactor;
            public float scaleFactor2;
            public Vector2 referenceResolution;
        }

        private Dictionary<Canvas, CanvasData> _scaledCanvases = new Dictionary<Canvas, CanvasData>();
        private HashSet<RectTransform> _alreadyProcessed;

        #region Public Methods
        public void Awake()
        {
        }

        public void RefreshCanvases()
        {
            HSUS._self.ExecuteDelayed(() =>
            {
#if HONEYSELECT
                GameObject go;
                switch (HSUS._self._binary)
                {
                    case Binary.Game:
                        go = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu");
                        if (go != null)
                        {
                            RectTransform rt = (RectTransform)go.transform;
                            Vector3 cachedPosition = rt.position;
                            rt.anchorMax = Vector2.one;
                            rt.anchorMin = Vector2.one;
                            rt.position = cachedPosition;
                        }
                        go = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSystem/W_System");
                        if (go != null)
                        {
                            RectTransform rt = (RectTransform)go.transform;
                            Vector3 cachedPosition = rt.position;
                            rt.anchorMax = Vector2.zero;
                            rt.anchorMin = Vector2.zero;
                            rt.position = cachedPosition;
                        }
                        go = GameObject.Find("CustomScene/CustomControl/CustomUI/ColorMenu/BasePanel");
                        if (go != null)
                        {
                            RectTransform rt = (RectTransform)go.transform;
                            Vector3 cachedPosition = rt.position;
                            rt.anchorMax = new Vector2(1, 0);
                            rt.anchorMin = new Vector2(1, 0);
                            rt.position = cachedPosition;
                        }
                        break;
                    case Binary.Studio:
                        go = GameObject.Find("StudioScene/Canvas Object List/Image Bar");
                        if (go != null)
                        {
                            RectTransform rt = (RectTransform)go.transform;
                            Vector3 cachedPosition = rt.position;
                            rt.anchorMax = Vector2.one;
                            rt.anchorMin = Vector2.one;
                            rt.position = cachedPosition;
                        }
                        break;
                }
#endif

                foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
                {
                    if (_scaledCanvases.ContainsKey(c) == false && ShouldScaleUI(c))
                    {
                        CanvasScaler cs = c.GetComponent<CanvasScaler>();
                        if (cs != null)
                        {
                            switch (cs.uiScaleMode)
                            {
                                case CanvasScaler.ScaleMode.ConstantPixelSize:
                                    _scaledCanvases.Add(c, new CanvasData() { scaleFactor = c.scaleFactor, scaleFactor2 = cs.scaleFactor });
                                    break;
                                case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                    _scaledCanvases.Add(c, new CanvasData() { scaleFactor = c.scaleFactor, referenceResolution = cs.referenceResolution });
                                    break;
                            }
                        }
                        else
                        {
                            _scaledCanvases.Add(c, new CanvasData() { scaleFactor = c.scaleFactor });
                        }
                    }
                }
                Dictionary<Canvas, CanvasData> newScaledCanvases = new Dictionary<Canvas, CanvasData>();
                foreach (KeyValuePair<Canvas, CanvasData> pair in _scaledCanvases)
                {
                    if (pair.Key != null)
                        newScaledCanvases.Add(pair.Key, pair.Value);
                }
                _scaledCanvases = newScaledCanvases;
                Scale();
            }, 10);
        }

        public void Scale()
        {
            float usedScale = HSUS._self.binary == Binary.Game ? HSUS.UIScaleGame.Value : HSUS.UIScaleStudio.Value;
            foreach (KeyValuePair<Canvas, CanvasData> pair in _scaledCanvases)
            {
                if (pair.Key != null && ShouldScaleUI(pair.Key))
                {
                    CanvasScaler cs = pair.Key.GetComponent<CanvasScaler>();
                    if (cs != null)
                    {
                        switch (cs.uiScaleMode)
                        {
                            case CanvasScaler.ScaleMode.ConstantPixelSize:
                                cs.scaleFactor = pair.Value.scaleFactor2 * usedScale;
                                break;
                            case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                cs.referenceResolution = pair.Value.referenceResolution / usedScale;
                                break;
                        }
                    }
                    else
                    {
                        pair.Key.scaleFactor = pair.Value.scaleFactor * usedScale;
                    }
                }
            }
            SpecialOperations(usedScale);
        }

        public void LevelLoaded()
        {
            _alreadyProcessed = new HashSet<RectTransform>();
            Scale();
        }
        #endregion

        #region Private Methods
        private bool ShouldScaleUI(Canvas c)
        {
            bool ok = true;
            string path = c.transform.GetPathFrom((Transform)null);
            switch (HSUS._self.binary)
            {
                case Binary.Studio:
                    switch (path)
                    {
#if HONEYSELECT
                        case "StartScene/Canvas":
                        case "New Game Object": // AdjustMod and SkintexMod
#elif KOIKATSU
                        case "SceneLoadScene/Canvas Load":
                        case "SceneLoadScene/Canvas Load Work":
                        case "ExitScene/Canvas":
                        case "NotificationScene/Canvas":
                        case "CheckScene/Canvas":
#elif AISHOUJO || HONEYSELECT2
                        case "SceneLoadScene/Canvas Load":
                        case "SceneLoadScene/Canvas Load Work":
                        case "ExitScene/Canvas":
#endif
                        case "VectorCanvas":
                            ok = false;
                            break;
                    }
                    break;
                case Binary.Game:
                    switch (path)
                    {
#if HONEYSELECT
                        case "LogoScene/Canvas":
                        case "LogoScene/Canvas (1)":
                        case "CustomScene/CustomControl/CustomUI/BackGround":
                        case "CustomScene/CustomControl/CustomUI/Fusion":
                        case "GameScene/Canvas":
                        case "MapSelectScene/Canvas":
                        case "SubtitleUserInterface":
                        case "ADVScene/Canvas":
#elif KOIKATSU
                        case "CustomScene/CustomRoot/BackUIGroup/CvsBackground":
                        case "CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCharaName":
                        case "AssetBundleManager/scenemanager/Canvas":
                        case "FreeHScene/Canvas":
                        case "ExitScene":
                        case "CustomScene/CustomRoot/SaveFrame/BackSpCanvas":
                        case "CustomScene/CustomRoot/SaveFrame/FrontSpCanvas":
                        case "CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront":
                        case "ConfigScene/Canvas":
#elif AISHOUJO || HONEYSELECT2
                        case "CharaCustom/CustomControl/SaveFrame/BackSpCanvas":
                        case "CharaCustom/CustomControl/SaveFrame/FrontSpCanvas":
                        case "CharaCustom/CustomControl/CanvasInputCoordinate":
                        case "CharaCustom/CustomControl/Canvas_PopupCheck":
#endif
                        case "VectorCanvas":
                        case "TitleScene/Canvas":
                            ok = false;
                            break;
                    }
                    break;
            }
            Canvas parent = c.GetComponentInParent<Canvas>();
            return ok && c.isRootCanvas && (parent == null || parent == c);
        }

        private void SpecialOperations(float scale)
        {
#if AISHOUJO || HONEYSELECT2
            if (HSUS._self.binary == Binary.Game && HSUS._self.level == 4)
            {
                RectTransform rt = GameObject.Find("CharaCustom/CustomControl/CanvasMain/SubMenu")?.transform as RectTransform;
                if (rt != null && _alreadyProcessed.Contains(rt) == false)
                {
                    rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMax.y + (rt.offsetMin.y - rt.offsetMax.y) / scale);
                    _alreadyProcessed.Add(rt);
                }
            }
#endif
        }
        #endregion
    }
}
