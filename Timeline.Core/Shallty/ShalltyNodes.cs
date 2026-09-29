using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using Vectrosity;

namespace Timeline
{
    /// <summary>
    /// ShalltyUtils' nodes in the scene, from ShalltyUtils by ShalltyB (github.com/ShalltyB/ShalltyUtils):
    /// the nodes of the picker's buttons drawn as dots in their colour over the scene. A dot selects its
    /// node when clicked, and alt + dragging a box selects every dot in it (control adds to what is
    /// selected). Pages can each hide their dots; all of them go with Studio's axis button if wanted.
    ///
    /// For boxes to select several nodes at once every node is made able to be one of several, and
    /// the ones selected show it on their sphere.
    /// </summary>
    public partial class Timeline
    {
        internal static ConfigEntry<float> ConfigNodeSize { get; private set; }
        internal static ConfigEntry<bool> ConfigNodesWithAxis { get; private set; }
        internal static ConfigEntry<bool> ConfigNodeTooltip { get; private set; }
        internal static ConfigEntry<bool> ConfigNodeTooltipPage { get; private set; }
        internal static ConfigEntry<string> ConfigNodeTexture { get; private set; }

        /// <summary>Whether the dots show at all, from the picker window or Studio's axis button.</summary>
        internal static bool _showPickerNodes = true;

        private static readonly Color _selectedSphere = new Color(0f, 1f, 1f, 0.5f);
        private static VectorLine _nodesLine;
        private static Texture _nodeDefaultTexture;
        private static readonly List<KeyValuePair<PickerButton, PickerPage>> _nodesLineButtons = new List<KeyValuePair<PickerButton, PickerPage>>();
        private static bool _nodeTooltipShown;
        private static string _nodeTooltip = "";
        private static Vector2 _boxStart;
        private static Rect _boxRect;
        private static bool _boxSelecting;

        private static void BindNodesConfig(ConfigFile config)
        {
            const string section = "Picker";
            ConfigNodeSize = config.Bind(section, "Nodes Size", 20f, "The size of the dots drawn in the scene for the picker's buttons.");
            ConfigNodesWithAxis = config.Bind(section, "Link Nodes with Axis Button", true, "Show and hide the dots with Studio's axis button.");
            ConfigNodeTooltip = config.Bind(section, "Enable Tooltips", true, "Name a dot's button when the mouse is over it.");
            ConfigNodeTooltipPage = config.Bind(section, "Display Page in Tooltip", true, "Name the page of a dot's button too.");
            ConfigNodeTexture = config.Bind(section, "Node Texture Path", "", "A .png file to draw the dots with instead of the default one.");
            ConfigNodeTexture.SettingChanged += (sender, args) => LoadNodesTexture();
        }

        private static void InitNodes(Harmony harmony)
        {
            _nodesLine = new VectorLine("TimelinePickerNodes", new List<Vector3> { Vector3.zero }, 20f, LineType.Points);
            _nodesLine.color = Color.white;
            _nodeDefaultTexture = ResourceUtils.GetEmbeddedResource("guideObjectPickerNode.png").LoadTexture();
            LoadNodesTexture();
            _nodesLine.active = false;

            PatchIfThere(harmony, AccessTools.Method(typeof(StudioScene), "OnClickAxis"), nameof(OnClickAxisPostfix), false);
            PatchIfThere(harmony, AccessTools.Method(typeof(CameraControl), "InputMouseProc"), nameof(InputMouseProcPrefix), true);
            PatchIfThere(harmony, AccessTools.Method(typeof(GuideObjectManager), "Add"), nameof(GuideObjectAddPostfix), false);
            PatchIfThere(harmony, AccessTools.Method(typeof(GuideBase), "OnPointerEnter"), nameof(GuideBasePointerPostfix), false);
            PatchIfThere(harmony, AccessTools.Method(typeof(GuideBase), "OnPointerExit"), nameof(GuideBasePointerPostfix), false);
            PatchIfThere(harmony, AccessTools.Method(typeof(GuideObjectManager), "SetSelectObject"), nameof(SelectionChangedPostfix), false);
            PatchIfThere(harmony, AccessTools.Method(typeof(GuideObjectManager), "SetDeselectObject"), nameof(SelectionChangedPostfix), false);
            // Nodes made before Timeline started.
            foreach (GuideObject node in GuideObjectManager.Instance.dicGuideObject.Values)
                node.enableMaluti = true;
        }

        /// <summary>Patches what the game has; one game missing a method costs that detail only.</summary>
        private static void PatchIfThere(Harmony harmony, MethodBase original, string patch, bool prefix)
        {
            if (original == null)
            {
                Logger.LogInfo("Not in this game, skipped: " + patch);
                return;
            }
            var method = new HarmonyMethod(typeof(Timeline), patch);
            if (prefix)
                harmony.Patch(original, prefix: method);
            else
                harmony.Patch(original, postfix: method);
        }

        private static void LoadNodesTexture()
        {
            if (_nodesLine == null)
                return;
            Texture texture = _nodeDefaultTexture;
            string file = ConfigNodeTexture.Value;
            if (string.IsNullOrEmpty(file) == false && File.Exists(file))
            {
                try
                {
                    texture = File.ReadAllBytes(file).LoadTexture() ?? _nodeDefaultTexture;
                }
                catch
                {
                    texture = _nodeDefaultTexture;
                }
            }
            _nodesLine.texture = texture;
        }

        #region Hooks
        private static void OnClickAxisPostfix()
        {
            if (ConfigNodesWithAxis.Value)
                _showPickerNodes = Studio.Studio.Instance.workInfo.visibleAxis;
        }

        /// <summary>The camera stays put while a box is dragged.</summary>
        private static bool InputMouseProcPrefix()
        {
            return _boxSelecting == false;
        }

        private static void GuideObjectAddPostfix(GuideObject __result)
        {
            if (__result != null)
                _self.StartCoroutine(EnableMultipleNextFrame(__result));
        }

        /// <summary>Studio sets it up after adding the node, so it is changed a frame later.</summary>
        private static System.Collections.IEnumerator EnableMultipleNextFrame(GuideObject node)
        {
            yield return null;
            if (node != null)
                node.enableMaluti = true;
        }

        private static void GuideBasePointerPostfix(GuideBase __instance)
        {
            if (__instance.name == "Sphere" && GuideObjectManager.Instance.hashSelectObject.Contains(__instance.guideObject) && GuideObjectManager.Instance.isOperationTarget == false)
                __instance.colorNow = _selectedSphere;
        }

        private static void SelectionChangedPostfix()
        {
            foreach (GuideObject node in GuideObjectManager.Instance.dicGuideObject.Values)
            {
                if (node.guideSelect == null)
                    continue;
                node.guideSelect.colorNow = GuideObjectManager.Instance.hashSelectObject.Contains(node.guideSelect.guideObject) ? _selectedSphere : node.guideSelect.colorNormal;
            }
        }
        #endregion

        #region Drawing and picking
        /// <summary>Every frame: the dots where their nodes are, a click on one, and the box.</summary>
        private void TickNodes()
        {
            if (_nodesLine == null)
                return;
            _nodesLineButtons.Clear();
            _nodesLine.points3.Clear();
            if (_showPickerNodes)
            {
                foreach (PickerPage page in _pickerPages)
                {
                    if (page.showNodes == false)
                        continue;
                    foreach (PickerButton button in page.buttons)
                    {
                        if (button.target == null || button.target.transformTarget == null)
                            continue;
                        _nodesLine.points3.Add(button.target.transformTarget.position);
                        _nodesLineButtons.Add(new KeyValuePair<PickerButton, PickerPage>(button, page));
                    }
                }
            }
            if (_nodesLine.points3.Count == 0)
            {
                _nodesLine.active = false;
                _nodeTooltipShown = false;
                _boxSelecting = false;
                return;
            }

            _nodesLine.active = true;
            _nodesLine.SetWidth(ConfigNodeSize.Value);
            HashSet<GuideObject> selected = GuideObjectManager.Instance.hashSelectObject;
            for (int i = 0; i < _nodesLineButtons.Count; i++)
            {
                PickerButton button = _nodesLineButtons[i].Key;
                Color c = button.color;
                _nodesLine.SetColor(selected.Contains(button.target) ? new Color(c.r, c.g, c.b, 1f) : new Color(c.r, c.g, c.b, 0.5f), i);
            }
            _nodesLine.Draw();

            int index;
            if (_boxSelecting == false && _nodesLine.Selected(Input.mousePosition, out index) && index < _nodesLineButtons.Count)
            {
                PickerButton button = _nodesLineButtons[index].Key;
                PickerPage page = _nodesLineButtons[index].Value;
                _nodeTooltipShown = ConfigNodeTooltip.Value;
                string label = string.IsNullOrEmpty(button.label) ? button.target.transformTarget.name : button.label;
                _nodeTooltip = "<b><color=#" + ColorUtility.ToHtmlStringRGB(button.color) + ">" + label + "</color>" +
                               (ConfigNodeTooltipPage.Value ? " - <color=#" + ColorUtility.ToHtmlStringRGB(page.color) + ">[" + page.name + "]</color>" : "") + "</b>";
                if (Input.GetMouseButtonDown(0))
                    PickNode(button.target, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
            }
            else
                _nodeTooltipShown = false;

            if (_boxSelecting == false && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || EventSystem.current.IsPointerOverGameObject() == false))
            {
                _boxSelecting = true;
                _boxStart = Input.mousePosition;
            }
            if (_boxSelecting)
            {
                Vector2 end = Input.mousePosition;
                _boxRect = new Rect(Mathf.Min(_boxStart.x, end.x), Mathf.Min(Screen.height - _boxStart.y, Screen.height - end.y),
                                    Mathf.Abs(_boxStart.x - end.x), Mathf.Abs(_boxStart.y - end.y));
                if (Input.GetMouseButtonUp(0) || Input.GetMouseButton(0) == false)
                    EndBoxSelection();
            }
        }

        private void EndBoxSelection()
        {
            _boxSelecting = false;
            Camera camera = Camera.main;
            var toSelect = new List<GuideObject>();
            for (int i = 0; i < _nodesLine.points3.Count && i < _nodesLineButtons.Count; i++)
            {
                Vector3 screen = camera.WorldToScreenPoint(_nodesLine.points3[i]);
                if (screen.z > 0f && _boxRect.Contains(new Vector2(screen.x, Screen.height - screen.y)))
                    toSelect.Add(_nodesLineButtons[i].Key.target);
            }

            TreeNodeCtrl tree = Studio.Studio.Instance.treeNodeCtrl;
            GuideObjectManager guides = GuideObjectManager.Instance;
            if (Input.GetKey(KeyCode.LeftControl) == false && Input.GetKey(KeyCode.RightControl) == false)
            {
                foreach (TreeNodeObject node in tree.hashSelectNode)
                    node.OnDeselect();
                tree.hashSelectNode.Clear();
                foreach (GuideObject node in new HashSet<GuideObject>(guides.hashSelectObject))
                    guides.SetDeselectObject(node);
            }
            if (toSelect.Count == 0)
                return;
            Dictionary<TreeNodeObject, ObjectCtrlInfo> ocis = Studio.Studio.Instance.dicInfo;
            foreach (GuideObject node in toSelect)
            {
                bool hasParent = node.parentGuide != null;
                TreeNodeObject treeNode = ocis.FirstOrDefault(p => ReferenceEquals(p.Value.guideObject, hasParent ? node.parentGuide : node)).Key;
                if (treeNode == null)
                    continue;
                if (hasParent)
                {
                    if (guides.hashSelectObject.Contains(node) == false)
                        guides.AddSelectMultiple(node);
                    else
                        guides.SetDeselectObject(node);
                }
                else
                    tree.AddSelectNode(treeNode, true);
            }
            GuideObject last = toSelect.Last();
            guides.StopSelectObject();
            last.isActive = true;
            this.ExecuteDelayed2(() => last.SetLayer(last.gameObject, LayerMask.NameToLayer("Studio/Select")));
            SelectionChangedPostfix();
        }

        /// <summary>In OnGUI: the name under the mouse, and the box being dragged.</summary>
        private static void DrawNodesOverlay()
        {
            if (_nodeTooltipShown)
            {
                GUIStyle style = new GUIStyle(IMGUIUtils.SolidBackgroundGuiSkin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
                Vector2 mouse = Event.current.mousePosition;
                Vector2 size = style.CalcSize(new GUIContent(_nodeTooltip));
                const float padding = 10f;
                GUI.Box(new Rect(mouse.x + padding, mouse.y + padding, size.x + padding * 2f, size.y + padding * 2f), _nodeTooltip, style);
            }
            if (_boxSelecting)
            {
                Color color = GUI.color;
                GUI.color = _selectedSphere;
                GUI.Box(_boxRect, "", IMGUIUtils.SolidBackgroundGuiSkin.box);
                GUI.color = color;
            }
        }
        #endregion
    }
}
