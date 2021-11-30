using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Xml;
using Studio;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS.Features
{
    public class DeleteConfirmation : IFeature
    {
        private static readonly HarmonyExtensions.Replacement[] _replacements = 
        {
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Callvirt, typeof(Studio.WorkspaceCtrl).GetMethod("OnClickDelete", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(DeleteConfirmation).GetMethod(nameof(DeleteReplacement), BindingFlags.Static | BindingFlags.NonPublic)),
                }
            },
        };

        private static bool _deleteConfirmationButton = true;
        private static bool _deleteConfirmationKey = true;

        public void Awake()
        {
        }

        public void LevelLoaded()
        {
#if !PLAYHOME
            if (_deleteConfirmationButton && HSUS._self._binary == Binary.Studio &&
#if HONEYSELECT
            HSUS._self._level == 3
#elif KOIKATSU
            HSUS._self._level == 1
#elif AISHOUJO
            HSUS._self._level == 2
#elif HONEYSELECT2
            HSUS._self._level == 2
#endif
            )
            {
                Button deleteButton = GameObject.Find("StudioScene/Canvas Object List/Image Bar/Button Delete").GetComponent<Button>();
                Button.ButtonClickedEvent originalDeleteAction = deleteButton.onClick;
                deleteButton.onClick = new Button.ButtonClickedEvent();
                deleteButton.onClick.AddListener(() =>
                {
                    UIUtility.DisplayConfirmationDialog(result =>
                    {
                        if (result)
                            originalDeleteAction.Invoke();
                    }, "Are you sure you want to delete this object?");
                });
            }
#endif
        }

        private static void DeleteReplacement(Studio.WorkspaceCtrl workspaceCtrl)
        {
            if (Studio.Studio.Instance.treeNodeCtrl.selectNodes.Length != 0)
            {
                UIUtility.DisplayConfirmationDialog(result =>
                {
                    if (result)
                        workspaceCtrl.OnClickDelete();
                }, "Are you sure you want to delete this object?");
            }
        }

        public void LoadParams(XmlNode node)
        {
#if !PLAYHOME
            node = node.FindChildNode("deleteConfirmation");
            if (node == null)
                return;
            if (node.Attributes["buttonEnabled"] != null)
                _deleteConfirmationButton = XmlConvert.ToBoolean(node.Attributes["buttonEnabled"].Value);
            else if (node.Attributes["enabled"] != null)
                _deleteConfirmationButton = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            if (node.Attributes["keyEnabled"] != null)
                _deleteConfirmationKey = XmlConvert.ToBoolean(node.Attributes["keyEnabled"].Value);
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if !PLAYHOME
            writer.WriteStartElement("deleteConfirmation");
            writer.WriteAttributeString("buttonEnabled", XmlConvert.ToString(_deleteConfirmationButton));
            writer.WriteAttributeString("keyEnabled", XmlConvert.ToString(_deleteConfirmationKey));
            writer.WriteEndElement();
#endif
        }

#if !PLAYHOME
        [HarmonyPatch(typeof(ShortcutKeyCtrl), "Update")]
        private static class ShortcutKeyCtrl_Update_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio && _deleteConfirmationKey;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
            }
        }
#endif

#if HONEYSELECT
        [HarmonyPatch]
        public static class HSSNAShortcutKeyCtrlOverride_Update_Patches
        {
            private static bool Prepare()
            {
                Type t = Type.GetType("HSStudioNEOAddon.ShortcutKey.HSSNAShortcutKeyCtrlOverride,HSStudioNEOAddon");
                return HSUS._self._binary == Binary.Studio && _deleteConfirmationKey && t != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("HSStudioNEOAddon.ShortcutKey.HSSNAShortcutKeyCtrlOverride,HSStudioNEOAddon").GetMethod("Update");
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
            }
        }

#endif
    }
}
