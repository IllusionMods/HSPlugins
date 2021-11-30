using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using HSPE.AMModules;
using Studio;
using ToolBox.Extensions;
using UnityEngine;

namespace HSPE
{
    public class PoseController : MonoBehaviour
    {
        #region Static Variables
        internal static readonly HashSet<PoseController> _poseControllers = new HashSet<PoseController>();
        #endregion

        #region Events
        public static event Action<TreeNodeObject, TreeNodeObject> onParentage;
        public event Action onUpdate;
        public event Action onLateUpdate;
        public event Action onDestroy;
        public event Action onDisable;
        #endregion

        #region Public Types
        public enum DragType
        {
            None,
            Position,
            Rotation,
            Both
        }
        #endregion

        #region Protected Variables
        internal BonesEditor _bonesEditor;
        internal DynamicBonesEditor _dynamicBonesEditor;
        internal BlendShapesEditor _blendShapesEditor;
        internal CollidersEditor _collidersEditor;
        internal IKEditor _ikEditor;
        protected readonly List<AdvancedModeModule> _modules = new List<AdvancedModeModule>();
        protected AdvancedModeModule _currentModule;
        internal GenericOCITarget _target;
        protected readonly Dictionary<int, Vector3> _oldRotValues = new Dictionary<int, Vector3>();
        protected readonly Dictionary<int, Vector3> _oldPosValues = new Dictionary<int, Vector3>();
        protected List<GuideCommand.EqualsInfo> _additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
        protected bool _lockDrag = false;
        internal DragType _currentDragType;
        internal int _oldInstanceId = 0;
        #endregion

        #region Private Variables
        internal static bool _drawAdvancedMode = false;
        internal readonly HashSet<GameObject> _childObjects = new HashSet<GameObject>();
        private static bool _onPreRenderCallbackAdded = false;
        #endregion

        #region Public Accessors
        public virtual bool isDraggingDynamicBone { get { return this._dynamicBonesEditor.isDraggingDynamicBone; } }
        public GenericOCITarget target { get { return this._target; } }
        #endregion

        #region Unity Methods
        protected virtual void Awake()
        {
            if (_onPreRenderCallbackAdded == false)
            {
                _onPreRenderCallbackAdded = true;
                MainWindow._self._cameraEventsDispatcher.onPreRender += UpdateGizmosIf;
            }

            _poseControllers.Add(this);
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Value.guideObject.transformTarget.gameObject == this.gameObject)
                {
                    this._target = new GenericOCITarget(pair.Value);
                    break;
                }
            }

            this.FillChildObjects();

            this._bonesEditor = new BonesEditor(this, this._target);
            this._modules.Add(this._bonesEditor);

            this._collidersEditor = new CollidersEditor(this, this._target);
            this._modules.Add(this._collidersEditor);

            this._dynamicBonesEditor = new DynamicBonesEditor(this, this._target);
            this._modules.Add(this._dynamicBonesEditor);

            this._blendShapesEditor = new BlendShapesEditor(this, this._target);
            this._modules.Add(this._blendShapesEditor);

            this._ikEditor = new IKEditor(this, this._target);
            this._modules.Add(this._ikEditor);

            if (this._collidersEditor._isLoneCollider)
                this._currentModule = this._collidersEditor;
            else
                this._currentModule = this._bonesEditor;
            this._currentModule.isEnabled = true;

            onParentage += this.OnParentage;
        }

        protected virtual void Start()
        {
            this.FillChildObjects();
        }

        protected virtual void Update()
        {
            this.onUpdate();
        }

        protected virtual void LateUpdate()
        {
            this.onLateUpdate();
        }

        private void OnGUI()
        {
            if (_drawAdvancedMode && MainWindow._self._poseTarget == this)
            {
                if (this._blendShapesEditor._isEnabled)
                    this._blendShapesEditor.OnGUI();
            }
        }

        private static void UpdateGizmosIf()
        {
            if (MainWindow._self._poseTarget == null)
                return;
            MainWindow._self._poseTarget.UpdateGizmos();
        }

        protected virtual void UpdateGizmos()
        {
            foreach (AdvancedModeModule module in this._modules)
                module.UpdateGizmos();
        }

        private void OnDisable()
        {
            this.onDisable();
        }

        protected virtual void OnDestroy()
        {
            onParentage -= this.OnParentage;
            this.onDestroy();
            _poseControllers.Remove(this);
        }
        #endregion

        #region Public Methods
        public virtual void LoadFrom(PoseController other)
        {
            if (other == null)
                return;
            this._bonesEditor.LoadFrom(other._bonesEditor);
            this._collidersEditor.LoadFrom(other._collidersEditor);
            this._dynamicBonesEditor.LoadFrom(other._dynamicBonesEditor);
            this._blendShapesEditor.LoadFrom(other._blendShapesEditor);
            this._ikEditor.LoadFrom(other._ikEditor);
            foreach (GameObject ignoredObject in other._childObjects)
            {
                if (ignoredObject == null)
                    continue;
                Transform obj = this.transform.Find(ignoredObject.transform.GetPathFrom(other.transform));
                if (obj != null && obj != this.transform)
                    this._childObjects.Add(obj.gameObject);
            }
        }

        public void AdvancedModeWindow(int id)
        {
            if (this.enabled == false)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("In order to optimize things, the Advanced Mode is disabled on this object, you can enable it below.");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color co = GUI.color;
                GUI.color = Color.magenta;
                if (GUILayout.Button("Enable", GUILayout.ExpandWidth(false)))
                    this.enabled = true;
                GUI.color = co;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }
            GUILayout.BeginHorizontal();
            Color c = GUI.color;
            foreach (AdvancedModeModule module in this._modules)
            {
                if (module == this._currentModule)
                    GUI.color = Color.cyan;
                if (module.shouldDisplay && GUILayout.Button(module.displayName))
                    this.EnableModule(module);
                GUI.color = c;
            }

            GUI.color = Color.magenta;
            if (GUILayout.Button("Disable", GUILayout.ExpandWidth(false)))
                this.enabled = false;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
                this.ToggleAdvancedMode();
            GUI.color = c;
            GUILayout.EndHorizontal();
            this._currentModule.GUILogic();
            GUI.DragWindow();
        }

        public void ToggleAdvancedMode()
        {
            _drawAdvancedMode = !_drawAdvancedMode;
            foreach (AdvancedModeModule module in this._modules)
                module.DrawAdvancedModeChanged();
        }

        public void EnableModule(AdvancedModeModule module)
        {
            if (module.shouldDisplay == false)
                return;
            this._currentModule = module;
            module.isEnabled = true;
            foreach (AdvancedModeModule module2 in this._modules)
            {
                if (module2 != module)
                    module2.isEnabled = false;
            }
        }

        public static void SelectionChanged(PoseController self)
        {
            if (self != null)
            {
                BonesEditor.SelectionChanged(self._bonesEditor);
                CollidersEditor.SelectionChanged(self._collidersEditor);
                DynamicBonesEditor.SelectionChanged(self._dynamicBonesEditor);
                CharaPoseController self2 = self as CharaPoseController;
                BoobsEditor.SelectionChanged(self2 != null ? self2._boobsEditor : null);
            }
            else
            {
                BonesEditor.SelectionChanged(null);
                CollidersEditor.SelectionChanged(null);
                DynamicBonesEditor.SelectionChanged(null);
                BoobsEditor.SelectionChanged(null);
            }
        }

        public static void InstallOnParentageEvent()
        {
            Action<TreeNodeObject, TreeNodeObject> oldDelegate = Studio.Studio.Instance.treeNodeCtrl.onParentage;
            Studio.Studio.Instance.treeNodeCtrl.onParentage = (parent, node) => PoseController.onParentage?.Invoke(parent, node);
            onParentage += oldDelegate;
        }

        public void StartDrag(DragType dragType)
        {
            if (this._lockDrag)
                return;
            this._currentDragType = dragType;
        }

        public void StopDrag()
        {
            if (this._lockDrag)
                return;
            GuideCommand.EqualsInfo[] moveCommands = new GuideCommand.EqualsInfo[this._oldPosValues.Count];
            int i = 0;
            if (this._currentDragType == DragType.Position || this._currentDragType == DragType.Both)
            {
                foreach (KeyValuePair<int, Vector3> kvp in this._oldPosValues)
                {
                    moveCommands[i] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = kvp.Value,
                        newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].pos
                    };
                    ++i;
                }
            }
            GuideCommand.EqualsInfo[] rotateCommands = new GuideCommand.EqualsInfo[this._oldRotValues.Count + this._additionalRotationEqualsCommands.Count];
            i = 0;
            if (this._currentDragType == DragType.Rotation || this._currentDragType == DragType.Both)
            {
                foreach (KeyValuePair<int, Vector3> kvp in this._oldRotValues)
                {
                    rotateCommands[i] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = kvp.Value,
                        newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot
                    };
                    ++i;
                }
            }
            foreach (GuideCommand.EqualsInfo info in this._additionalRotationEqualsCommands)
            {
                rotateCommands[i] = info;
                ++i;
            }
            UndoRedoManager.Instance.Push(new Commands.MoveRotateEqualsCommand(moveCommands, rotateCommands));
            this._currentDragType = DragType.None;
            this._oldPosValues.Clear();
            this._oldRotValues.Clear();
            this._additionalRotationEqualsCommands.Clear();
        }

        public void SeFKBoneTargetRotation(GuideObject bone, Quaternion targetRotation)
        {
            OCIChar.BoneInfo info;
            if (this._target.fkObjects.TryGetValue(bone.transformTarget.gameObject, out info) == false)
                return;
            if (this._target.fkEnabled && info.active)
            {
                if (this._currentDragType != DragType.None)
                {
                    if (this._oldRotValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldRotValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.rot);
                    info.guideObject.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetFKBoneTargetRotation(GuideObject bone)
        {
            OCIChar.BoneInfo info;
            if (this._target.fkObjects.TryGetValue(bone.transformTarget.gameObject, out info) == false || !this._target.fkEnabled || info.active == false)
                return Quaternion.identity;
            return info.guideObject.transformTarget.localRotation;
        }

        public void ScheduleLoad(XmlNode node, Action<bool> onLoadEnd)
        {
            MainWindow._self.StartCoroutine(this.LoadDefaultVersion_Routine(node, onLoadEnd));
        }

        public virtual void SaveXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteAttributeString("uniqueId", XmlConvert.ToString(this.GetInstanceID()));
            foreach (AdvancedModeModule module in this._modules)
                module.SaveXml(xmlWriter);
        }

        // Using this directly will load the data on the same frame, only use this if you know exactly what you're doing.
        public virtual bool LoadXml(XmlNode xmlNode)
        {
            bool changed = false;
            this._oldInstanceId = xmlNode.Attributes["uniqueId"] == null ? 0 : XmlConvert.ToInt32(xmlNode.Attributes["uniqueId"].Value);
            foreach (AdvancedModeModule module in this._modules)
                changed = module.LoadXml(xmlNode) || changed;
            return changed;
        }
        #endregion

        #region Private Methods
        private void FillChildObjects()
        {
            foreach (KeyValuePair<TreeNodeObject, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicInfo)
            {
                if (pair.Value.guideObject.transformTarget != this.transform)
                    continue;
                foreach (TreeNodeObject child in pair.Key.child)
                {
                    this.RecurseChildObjects(child, childInfo =>
                    {
                        if (this._childObjects.Contains(childInfo.guideObject.transformTarget.gameObject) == false)
                            this._childObjects.Add(childInfo.guideObject.transformTarget.gameObject);
                    });
                }
                break;
            }
        }

        private void RecurseChildObjects(TreeNodeObject obj, Action<ObjectCtrlInfo> action)
        {
            ObjectCtrlInfo objInfo;
            if (Studio.Studio.Instance.dicInfo.TryGetValue(obj, out objInfo))
            {
                action(objInfo);
                return; //When the first "real" object is found, return to ignore its children;
            }
            foreach (TreeNodeObject child in obj.child)
                this.RecurseChildObjects(child, action);
        }

        private void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            if (parent == null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && this._childObjects.Contains(info.guideObject.transformTarget.gameObject))
                    this._childObjects.Remove(info.guideObject.transformTarget.gameObject);
            }
            else
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && info.guideObject.transformTarget != this.transform && info.guideObject.transformTarget.IsChildOf(this.transform))
                    this._childObjects.Add(info.guideObject.transformTarget.gameObject);
            }

            foreach (AdvancedModeModule module in this._modules)
                module.OnParentage(parent, child);
        }

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode, Action<bool> onLoadEnd)
        {
            yield return null;
            yield return null;
            yield return null;
            bool changed = this.LoadXml(xmlNode);
            if (onLoadEnd != null)
                onLoadEnd(changed);
        }
        #endregion

    }
}
