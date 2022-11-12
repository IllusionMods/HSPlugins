using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.UI;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT
using CustomMenu;
#elif KOIKATSU
using System;
using System.Collections.Generic;
using ChaCustom;
using KKAPI.Utilities;
using UILib;
#endif

namespace HSUS.Features
{
    public class OptimizeCharaMaker : IFeature
    {
#if HONEYSELECT
        internal static bool _removeIsNew = true;
        internal static bool _asyncLoading = false;
        internal static Color _subFoldersColor = Color.cyan;

        internal static string _currentCharaPathGame = "";
        internal static string _currentClothesPathGame = "";
#endif

        public void Awake()
        {
        }

        public void LevelLoaded()
        {
#if HONEYSELECT
            if (_optimizeCharaMaker && HSUS._self._binary == Binary.Game)
            {
                _currentCharaPathGame = "";
                _currentClothesPathGame = "";
                if (HSUS._self._level == 21)
                {
                    GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu/SubItemTop/Infomation/TabControl/TabItem01/Name/InputField").GetComponent<InputField>().characterLimit = 0;
                    GameObject.Find("CustomScene/CustomControl/CustomUI/CustomCheck/checkPng/checkInputName/InputField").GetComponent<InputField>().characterLimit = 0;
                    GameObject.Find("CustomScene/CustomControl/CustomUI/CustomCheck/checkPng/checkOverwriteWithInput/InputField").GetComponent<InputField>().characterLimit = 0;

                    HSUS._self.ExecuteDelayed(() =>
                    {

                        foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                        {
                            switch (sprite.name)
                            {
                                case "rect_middle":
                                    HSUS._self._searchBarBackground = sprite;
                                    break;
                                case "btn_01":
                                    HSUS._self._buttonBackground = sprite;
                                    break;
                            }
                        }
                        foreach (Mask mask in Resources.FindObjectsOfTypeAll<Mask>()) //Thank you Henk for this tip
                        {
                            mask.gameObject.AddComponent<RectMask2D>();
                            GameObject.DestroyImmediate(mask);
                        }
                        foreach (SmClothesLoad f in Resources.FindObjectsOfTypeAll<SmClothesLoad>())
                        {
                            SmClothesLoad_Data.Init(f);
                            break;
                        }
                        foreach (SmCharaLoad f in Resources.FindObjectsOfTypeAll<SmCharaLoad>())
                        {
                            SmCharaLoad_Data.Init(f);
                            break;
                        }
                        foreach (SmClothes_F f in Resources.FindObjectsOfTypeAll<SmClothes_F>())
                        {
                            SmClothes_F_Data.Init(f);
                            break;
                        }
                        foreach (SmAccessory f in Resources.FindObjectsOfTypeAll<SmAccessory>())
                        {
                            SmAccessory_Data.Init(f);
                            break;
                        }
                        foreach (SmSwimsuit f in Resources.FindObjectsOfTypeAll<SmSwimsuit>())
                        {
                            SmSwimsuit_Data.Init(f);
                            break;
                        }
                        foreach (SmHair_F f in Resources.FindObjectsOfTypeAll<SmHair_F>())
                        {
                            SmHair_F_Data.Init(f);
                            break;
                        }
                        foreach (SmKindColorD f in Resources.FindObjectsOfTypeAll<SmKindColorD>())
                        {
                            SmKindColorD_Data.Init(f);
                            break;
                        }
                        foreach (SmKindColorDS f in Resources.FindObjectsOfTypeAll<SmKindColorDS>())
                        {
                            SmKindColorDS_Data.Init(f);
                            break;
                        }
                        foreach (SmFaceSkin f in Resources.FindObjectsOfTypeAll<SmFaceSkin>())
                        {
                            SmFaceSkin_Data.Init(f);
                            break;
                        }
                    }, 10);
                }
            }
#endif
        }

        //        public void LoadParams(XmlNode node)
        //        {
        //#if HONEYSELECT || KOIKATSU
        //            node = node.FindChildNode("optimizeCharaMaker");
        //            if (node == null)
        //                return;
        //            if (node.Attributes["enabled"] != null)
        //                _optimizeCharaMaker = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
        //#if HONEYSELECT
        //            foreach (XmlNode childNode in node.ChildNodes)
        //            {
        //                switch (childNode.Name)
        //                {
        //                    case "asyncLoading":
        //                        if (childNode.Attributes["enabled"] != null)
        //                            _asyncLoading = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
        //                        break;
        //                    case "removeIsNew":
        //                        if (childNode.Attributes["enabled"] != null)
        //                            _removeIsNew = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
        //                        break;
        //                    case "subFoldersColor":
        //                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["value"].Value, out _subFoldersColor);
        //                        break;
        //                }
        //            }
        //#endif
        //#endif
        //        }

        //        public void SaveParams(XmlTextWriter writer)
        //        {
        //#if HONEYSELECT || KOIKATSU
        //            writer.WriteStartElement("optimizeCharaMaker");
        //            writer.WriteAttributeString("enabled", XmlConvert.ToString(_optimizeCharaMaker));
        //#if HONEYSELECT
        //            writer.WriteStartElement("asyncLoading");
        //            writer.WriteAttributeString("enabled", XmlConvert.ToString(_asyncLoading));
        //            writer.WriteEndElement();

        //            writer.WriteStartElement("removeIsNew");
        //            writer.WriteAttributeString("enabled", XmlConvert.ToString(_removeIsNew));
        //            writer.WriteEndElement();

        //            writer.WriteStartElement("subFoldersColor");
        //            writer.WriteAttributeString("value", ColorUtility.ToHtmlStringRGB(_subFoldersColor));
        //            writer.WriteEndElement();
        //#endif
        //            writer.WriteEndElement();
        //#endif
        //        }

#if KOIKATSU
        [HarmonyPatch(typeof(CustomFileWindow), "Start")]
        internal static class CustomFileWindow_Start_Patches
        {
            private static bool Prepare()
            {
                return HSUS.CharaMakerSearchboxes.Value;
            }

            private static void Prefix(CustomFileWindow __instance)
            {
                RectTransform searchGroup = UIUtility.CreateNewUIObject("Search Group", __instance.transform.Find("WinRect"));
                searchGroup.transform.SetSiblingIndex(1);
                searchGroup.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
                __instance.transform.Find("WinRect/ListArea").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 32f);

                InputField searchBar = UIUtility.CreateInputField("Search Bar", searchGroup, "Search...");
                searchBar.transform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f));
                ((Image)searchBar.targetGraphic).sprite = __instance.transform.Find("WinRect/imgBack/imgName").GetComponent<Image>().sprite;
                foreach (Text text in searchBar.GetComponentsInChildren<Text>())
                    text.color = Color.white;

                Button clearButton = UIUtility.CreateButton("Clear", searchGroup, "X");
                clearButton.transform.SetRect(new Vector2(1f, 0f), Vector2.one, new Vector2(-28f, 0f), Vector2.zero);
                clearButton.GetComponentInChildren<Text>().color = Color.black;
                clearButton.image.sprite = __instance.transform.FindDescendant("btnLoad").GetComponent<Image>().sprite;

                CustomFileListCtrl listCtrl = __instance.GetComponent<CustomFileListCtrl>();
                List<CustomFileInfo> items = null;
                try
                {
                    items = (List<CustomFileInfo>)listCtrl.GetPrivate("lstFileInfo");
                }
                catch
                {
                    items = (List<CustomFileInfo>)listCtrl.GetPrivateProperty("lstFileInfo");
                }

                searchBar.onValueChanged.AddListener(s =>
                {
                    UpdateSearch(searchBar.text, items);
                });

                clearButton.onClick.AddListener(() =>
                {
                    searchBar.text = "";
                    UpdateSearch("", items);
                });
            }

            private static void UpdateSearch(string text, List<CustomFileInfo> items)
            {
                // Only true in KK, in KKP and KKS it is false
                bool isField = typeof(CustomFileInfo).GetField("fic", AccessTools.all) != null;
                foreach (CustomFileInfo info in items)
                {
                    CustomFileInfoComponent cfic;
                    string charaName;
                    if (isField)
                    {
                        cfic = (CustomFileInfoComponent)info.GetPrivate("fic");
                        charaName = (string)info.GetPrivate("name");
                    }
                    else
                    {
                        cfic = (CustomFileInfoComponent)info.GetPrivateProperty("fic");
                        charaName = (string)info.GetPrivateProperty("name");
                    }

                    cfic.Disvisible(!ContainsStringAlsoIfTranslated(charaName, text));
                }
            }

            private static bool ContainsStringAlsoIfTranslated(string input, string searchText)
            {
                if (string.IsNullOrEmpty(searchText)) return true;
                if (string.IsNullOrEmpty(input)) return false;

                return input.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (TranslationHelper.TryTranslate(input, out var tl) && tl.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
#endif
    }
}