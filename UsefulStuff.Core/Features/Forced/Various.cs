#if HONEYSELECT
using ToolBox.Extensions;
using System;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using Studio;
using ToolBox;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HSUS.Features
{
    public static class Various
    {
        // Various fixes and safeguards

        // Fixing a harmless exception each time a character is loaded
        [HarmonyPatch(typeof(SetRenderQueue), "Awake")]
        public class SetRenderQueue_Awake_Patches
        {
            public static bool Prefix(SetRenderQueue __instance, int[] ___m_queues)
            {
                Renderer renderer = __instance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material[] materials = renderer.materials;
                    int num = 0;
                    while (num < materials.Length && num < ___m_queues.Length)
                    {
                        materials[num].renderQueue = ___m_queues[num];
                        num++;
                    }
                }
                else
                {
                    __instance.ExecuteDelayed(() =>
                    {
                        renderer = __instance.GetComponent<Renderer>();
                        if (renderer == null)
                            return;
                        Material[] materials = renderer.materials;
                        int num = 0;
                        while (num < materials.Length && num < ___m_queues.Length)
                        {
                            materials[num].renderQueue = ___m_queues[num];
                            num++;
                        }
                    }, 3);
                }
                return false;
            }
        }
        //

        
        // Changing the way windows are dragged around (helpful for the UI scale feature)
        [HarmonyPatch(typeof(DragObject), "OnBeginDrag", typeof(PointerEventData))]
        internal static class DragObject_OnBeginDrag_Patches
        {
            internal static Vector2 _cachedDragPosition;
            internal static Vector2 _cachedMousePosition;

            private static bool Prefix(DragObject __instance)
            {
                _cachedDragPosition = __instance.transform.position;
                _cachedMousePosition = Input.mousePosition;
                return false;
            }
        }

        [HarmonyPatch(typeof(DragObject), "OnDrag", typeof(PointerEventData))]
        internal static class DragObject_OnDrag_Patches
        {
            private static bool Prefix(DragObject __instance)
            {
                __instance.transform.position = DragObject_OnBeginDrag_Patches._cachedDragPosition + ((Vector2)Input.mousePosition - DragObject_OnBeginDrag_Patches._cachedMousePosition);
                return false;
            }
        }

        [HarmonyPatch(typeof(UI_DragWindow), "OnBeginDrag", typeof(PointerEventData))]
        internal static class UI_DragWindow_OnBeginDrag_Patches
        {
            internal static Vector2 _cachedDragPosition;
            internal static Vector2 _cachedMousePosition;

            private static bool Prefix(UI_DragWindow __instance)
            {
                _cachedDragPosition = __instance.rtMove.position;
                _cachedMousePosition = Input.mousePosition;
                return false;
            }
        }

        [HarmonyPatch(typeof(UI_DragWindow), "OnDrag", typeof(PointerEventData))]
        internal static class UI_DragWindow_OnDrag_Patches
        {
            private static bool Prefix(UI_DragWindow __instance)
            {
                __instance.rtMove.position = UI_DragWindow_OnBeginDrag_Patches._cachedDragPosition + ((Vector2)Input.mousePosition - UI_DragWindow_OnBeginDrag_Patches._cachedMousePosition);
                return false;
            }
        }
        //

        // Making sure objects and their children duplicate correctly under certain conditions
        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        internal static class Studio_Duplicate_Patches
        {
            private static void Prefix(Studio.Studio __instance)
            {
                foreach (TreeNodeObject treeNodeObject in __instance.treeNodeCtrl.selectNodes)
                {
                    Recurse(treeNodeObject, (n) =>
                    {
                        ObjectCtrlInfo objectCtrlInfo;
                        if (__instance.dicInfo.TryGetValue(n, out objectCtrlInfo))
                            objectCtrlInfo.OnSavePreprocessing();
                    });
                }
            }

            private static void Recurse(TreeNodeObject node, Action<TreeNodeObject> onItem)
            {
                onItem(node);
                foreach (TreeNodeObject treeNodeObject in node.child)
                {
                    Recurse(treeNodeObject, onItem);
                }
            }
        }
    }
}
#endif
