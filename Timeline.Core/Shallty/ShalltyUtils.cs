using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;

namespace Timeline
{
    /// <summary>
    /// ShalltyUtils by ShalltyB (github.com/ShalltyB/ShalltyUtils), merged into Timeline with the
    /// HSPlugins maintainers' agreement. What Timeline already did its own way (undo, the graph, the motion
    /// path, key sets, the picker window, rigs, baking, the mesh sequencer, colours) is not brought over;
    /// the rest lives in this folder, and this file is where Timeline calls into it.
    /// </summary>
    public partial class Timeline
    {
        private static void BindShalltyConfig(ConfigFile config)
        {
            BindConstraintConfig(config);
            BindPoseEditorNodeConfig(config);
            BindNodesConfig(config);
        }

        /// <summary>Once Studio is up.</summary>
        private void InitShallty()
        {
            var harmony = new Harmony(GUID + ".shalltyutils");
            InitPoseEditorNode(harmony);
            InitNodes(harmony);
            CreateTreeStateButton();
        }

        /// <summary>Every frame, whether or not the window is open.</summary>
        private void TickShallty()
        {
            TickNodes();
        }

        protected override void OnGUI()
        {
            if (_loaded == false)
                return;
            GUISkin skin = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
            DrawConstraintTools();
            DrawNodesOverlay();
            GUI.skin = skin;
        }
    }
}
