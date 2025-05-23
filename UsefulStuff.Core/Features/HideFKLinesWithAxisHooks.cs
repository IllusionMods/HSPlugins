using HarmonyLib;
using Studio;
using System.Reflection;
using ToolBox;

namespace HSUS.Features
{
    public class HideFKLinesWithAxisHooks
    {
        [HarmonyPatch]
        public class HideFKLinesWithAxisPatches
        {
            internal static MethodBase TargetMethod()
            {
                return typeof(BoneLineCtrl).GetMethod(nameof(BoneLineCtrl.OnPostRender), AccessTools.all);
            }

            public static bool Prepare()
            {
                return HSUS._self.binary == Binary.Studio;
            }

            public static bool Prefix()
            {
                if (HSUS.HideFKLinesWithAxis.Value)
                    return Singleton<Studio.Studio>.Instance.m_WorkspaceCtrl.studioScene.cameraInfo.physicsRaycaster.enabled;
                return true;
            }
        }
    }
}
