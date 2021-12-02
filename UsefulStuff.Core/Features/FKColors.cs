using System.Reflection;
using System.Xml;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace HSUS.Features
{
    public class FKColors : IFeature
    {
#if HONEYSELECT
        private static Color _fkHairColor = Color.white;
        private static Color _fkNeckColor = Color.white;
        private static Color _fkChestColor = Color.white;
        private static Color _fkBodyColor = Color.white;
        private static Color _fkRightHandColor = Color.white;
        private static Color _fkLeftHandColor = Color.white;
        private static Color _fkSkirtColor = Color.white;
        private static Color _fkItemsColor = Color.white;
#endif
        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
#if HONEYSELECT
            node = node.FindChildNode("fkColors");
            if (node == null)
                return;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "hair":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkHairColor);
                        break;
                    case "neck":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkNeckColor);
                        break;
                    case "chest":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkChestColor);
                        break;
                    case "body":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkBodyColor);
                        break;
                    case "rightHand":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkRightHandColor);
                        break;
                    case "leftHand":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkLeftHandColor);
                        break;
                    case "skirt":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkSkirtColor);
                        break;
                    case "items":
                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out _fkItemsColor);
                        break;
                }
            }
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if HONEYSELECT
            writer.WriteStartElement("fkColors");
            {
                writer.WriteStartElement("hair");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkHairColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("neck");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkNeckColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("chest");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkChestColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("body");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkBodyColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("rightHand");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkRightHandColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("leftHand");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkLeftHandColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("skirt");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkSkirtColor));
                writer.WriteEndElement();
            }
            {
                writer.WriteStartElement("items");
                writer.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(_fkItemsColor));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
#endif
        }

        public void LevelLoaded()
        {
        }

#if HONEYSELECT
        [HarmonyPatch]
        internal static class BoneInfo_Ctor_Patches
        {
            private static ConstructorInfo TargetMethod()
            {
                return typeof(OCIChar.BoneInfo).GetConstructor(new[] { typeof(GuideObject), typeof(OIBoneInfo) });
            }

            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static void Postfix(GuideObject _guideObject, OIBoneInfo _boneInfo)
            {
                switch (_boneInfo.group)
                {
                    case OIBoneInfo.BoneGroup.Body:
                        _guideObject.guideSelect.color = _fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)3:
                    case OIBoneInfo.BoneGroup.RightLeg:
                        _guideObject.guideSelect.color = _fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)5:
                    case OIBoneInfo.BoneGroup.LeftLeg:
                        _guideObject.guideSelect.color = _fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)9:
                    case OIBoneInfo.BoneGroup.RightArm:
                        _guideObject.guideSelect.color = _fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)17:
                    case OIBoneInfo.BoneGroup.LeftArm:
                        _guideObject.guideSelect.color = _fkBodyColor;
                        break;
                    case OIBoneInfo.BoneGroup.RightHand:
                        _guideObject.guideSelect.color = _fkRightHandColor;
                        break;
                    case OIBoneInfo.BoneGroup.LeftHand:
                        _guideObject.guideSelect.color = _fkLeftHandColor;
                        break;
                    case OIBoneInfo.BoneGroup.Hair:
                        _guideObject.guideSelect.color = _fkHairColor;
                        break;
                    case OIBoneInfo.BoneGroup.Neck:
                        _guideObject.guideSelect.color = _fkNeckColor;
                        break;
                    case OIBoneInfo.BoneGroup.Breast:
                        _guideObject.guideSelect.color = _fkChestColor;
                        break;
                    case OIBoneInfo.BoneGroup.Skirt:
                        _guideObject.guideSelect.color = _fkSkirtColor;
                        break;
                    case (OIBoneInfo.BoneGroup)0:
                        _guideObject.guideSelect.color = _fkItemsColor;
                        break;
                }
            }
        }
#endif
    }
}