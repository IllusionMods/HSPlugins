using System.Linq;
using HarmonyLib;
using Studio;

namespace HSUS.Features
{
    /// <summary>
    /// When multiple nodes are selected and user checks or unchecks one of the selected nodes, check/uncheck all of the selected nodes
    /// </summary>
    [HarmonyPatch]
    public class SetVisibleToAllSelectedWorkspaceObjects : IFeature
    {
        public void Awake()
        {
        }

        public void LevelLoaded()
        {
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TreeNodeObject), nameof(TreeNodeObject.OnClickVisible))]
        public static bool SetVisibleToAllHook(TreeNodeObject __instance)
        {
            var selectNodes = __instance.m_TreeNodeCtrl.selectNodes;
            // If user clicks on a node that is not selected, only affect that node
            if (!selectNodes.Contains(__instance)) return true;

            var newState = !__instance.m_Visible;
            foreach (var selectedObject in selectNodes)
            {
                // Warning: If SetVisible is called on a child node when the parent node is unchecked, the visibility might become wrong (child visible even though parent is invisible).
                // To avoid this, check if the node is under a disabled parent (buttonVisible is set to not interactable) and only update what is necessary in that case
                if (selectedObject.buttonVisible.interactable)
                {
                    selectedObject.SetVisible(newState);
                }
                else
                {
                    selectedObject.m_Visible = newState;
                    selectedObject.imageVisible.sprite = selectedObject.m_SpriteVisible[newState ? 1 : 0];
                }
            }

            return false;
        }
    }
}
