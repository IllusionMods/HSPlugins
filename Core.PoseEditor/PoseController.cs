using HSPE.AMModules;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
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
        internal ClothesTransformEditor _clothesTransformEditor;
        protected readonly List<AdvancedModeModule> _modules = new List<AdvancedModeModule>();
        protected AdvancedModeModule _currentModule;
        internal GenericOCITarget _target;
        protected readonly Dictionary<int, Vector3> _oldRotValues = new Dictionary<int, Vector3>();
        protected readonly Dictionary<int, Vector3> _oldPosValues = new Dictionary<int, Vector3>();
        protected List<GuideCommand.EqualsInfo> _additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
        protected bool _lockDrag = false;
        internal DragType _currentDragType;
        internal int _oldInstanceId = 0;
        internal int _loadId = 0;
        #endregion

        #region Private Variables
        internal static bool _drawAdvancedMode = false;
        internal readonly HashSet<GameObject> _childObjects = new HashSet<GameObject>();
        private static bool _onPreRenderCallbackAdded = false;
        #endregion

        #region Public Accessors
        public virtual bool isDraggingDynamicBone { get { return _dynamicBonesEditor.isDraggingDynamicBone; } }
        public GenericOCITarget target { get { return _target; } }
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
                if (pair.Value.guideObject.transformTarget.gameObject == gameObject)
                {
                    _target = new GenericOCITarget(pair.Value);
                    break;
                }
            }

            FillChildObjects();

            _bonesEditor = new BonesEditor(this, _target);
            _modules.Add(_bonesEditor);

            _collidersEditor = new CollidersEditor(this, _target);
            _modules.Add(_collidersEditor);

            _dynamicBonesEditor = new DynamicBonesEditor(this, _target);
            _modules.Add(_dynamicBonesEditor);

            _blendShapesEditor = new BlendShapesEditor(this, _target);
            _modules.Add(_blendShapesEditor);

            _ikEditor = new IKEditor(this, _target);
            _modules.Add(_ikEditor);

#if AISHOUJO || HONEYSELECT2
            _clothesTransformEditor = new ClothesTransformEditor(this, _target);
            _modules.Add(_clothesTransformEditor);
#endif

            if (_collidersEditor._isLoneCollider)
            {
                _currentModule = _collidersEditor;
            }
            else
            {
                _currentModule = _bonesEditor;
                // Disable by default on static studio items
                if (!(this is CharaPoseController))
                    this.enabled = false;
            }

            _currentModule.isEnabled = true;

            onParentage += OnParentage;
        }

        protected virtual void Start()
        {
            FillChildObjects();
        }

        protected virtual void Update()
        {
            onUpdate();
        }

        protected virtual void LateUpdate()
        {
            onLateUpdate();
        }

        private void OnGUI()
        {
            if (_drawAdvancedMode && MainWindow._self._poseTarget == this)
            {
                if (_blendShapesEditor._isEnabled)
                    _blendShapesEditor.OnGUI();
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
            foreach (AdvancedModeModule module in _modules)
                module.UpdateGizmos();
        }

        private void OnDisable()
        {
            onDisable();
        }

        protected virtual void OnDestroy()
        {
            onParentage -= OnParentage;
            onDestroy();
            _poseControllers.Remove(this);
        }
        #endregion

        #region Public Methods
        public virtual void LoadFrom(PoseController other)
        {
            if (other == null)
                return;
            _bonesEditor.LoadFrom(other._bonesEditor);
            _collidersEditor.LoadFrom(other._collidersEditor);
            _dynamicBonesEditor.LoadFrom(other._dynamicBonesEditor);
            _blendShapesEditor.LoadFrom(other._blendShapesEditor);
            _ikEditor.LoadFrom(other._ikEditor);

            //Register as a child when a parent exists
            var otherParent = other.transform.parent?.GetComponentInParent<PoseController>();

            if (otherParent != null && otherParent._childObjects.Contains(other.gameObject))
            {
                var parent = transform.parent?.GetComponentInParent<PoseController>();
                parent?._childObjects.Add(gameObject);
            }
        }

        public void AdvancedModeWindow(int id)
        {
            if (enabled == false)
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
                    enabled = true;
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
            foreach (AdvancedModeModule module in _modules)
            {
                if (module == _currentModule)
                    GUI.color = Color.cyan;
                if (module.shouldDisplay && GUILayout.Button(module.displayName))
                    EnableModule(module);
                GUI.color = c;
            }

            GUI.color = Color.magenta;
            if (GUILayout.Button("Disable", GUILayout.ExpandWidth(false)))
                enabled = false;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
                ToggleAdvancedMode();
            GUI.color = c;
            GUILayout.EndHorizontal();
            _currentModule.GUILogic();
            GUI.DragWindow();
        }

        public void ToggleAdvancedMode()
        {
            _drawAdvancedMode = !_drawAdvancedMode;
            foreach (AdvancedModeModule module in _modules)
                module.DrawAdvancedModeChanged();
        }

        public void EnableModule(AdvancedModeModule module)
        {
            if (module.shouldDisplay == false)
                return;
            _currentModule = module;
            module.isEnabled = true;
            foreach (AdvancedModeModule module2 in _modules)
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
            Studio.Studio.Instance.treeNodeCtrl.onParentage = OnParentageRoot;
            onParentage += oldDelegate;
        }

        private static void OnParentageRoot(TreeNodeObject parent, TreeNodeObject child)
        {
            PoseController.onParentage?.Invoke(parent, child);

            var dicInfo = Studio.Studio.Instance.dicInfo;

            if (dicInfo.TryGetValue(child, out var childInfo))
            {
                //Body part does not have OCI. So look for OCI while moving to the parent.
                ObjectCtrlInfo parentInfo = null;
                while (parent != null && !dicInfo.TryGetValue(parent, out parentInfo))
                    parent = parent.parent;

                PoseController parentController = parentInfo?.guideObject.transformTarget.GetComponentInParent<PoseController>();

                if (parentController != null)
                {
                    var childTransform = childInfo.guideObject.transformTarget;
                    var childGObj = childTransform.gameObject;
                    parentController._childObjects.Add(childGObj);
                }
            }
        }

        public void StartDrag(DragType dragType)
        {
            if (_lockDrag)
                return;
            _currentDragType = dragType;
        }

        public void StopDrag()
        {
            if (_lockDrag)
                return;
            GuideCommand.EqualsInfo[] moveCommands = new GuideCommand.EqualsInfo[_oldPosValues.Count];
            int i = 0;
            if (_currentDragType == DragType.Position || _currentDragType == DragType.Both)
            {
                foreach (KeyValuePair<int, Vector3> kvp in _oldPosValues)
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
            GuideCommand.EqualsInfo[] rotateCommands = new GuideCommand.EqualsInfo[_oldRotValues.Count + _additionalRotationEqualsCommands.Count];
            i = 0;
            if (_currentDragType == DragType.Rotation || _currentDragType == DragType.Both)
            {
                foreach (KeyValuePair<int, Vector3> kvp in _oldRotValues)
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
            foreach (GuideCommand.EqualsInfo info in _additionalRotationEqualsCommands)
            {
                rotateCommands[i] = info;
                ++i;
            }
            UndoRedoManager.Instance.Push(new Commands.MoveRotateEqualsCommand(moveCommands, rotateCommands));
            _currentDragType = DragType.None;
            _oldPosValues.Clear();
            _oldRotValues.Clear();
            _additionalRotationEqualsCommands.Clear();
        }

        public void SeFKBoneTargetRotation(GuideObject bone, Quaternion targetRotation)
        {
            OCIChar.BoneInfo info;
            if (_target.fkObjects.TryGetValue(bone.transformTarget.gameObject, out info) == false)
                return;
            if (_target.fkEnabled && info.active)
            {
                if (_currentDragType != DragType.None)
                {
                    if (_oldRotValues.ContainsKey(info.guideObject.dicKey) == false)
                        _oldRotValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.rot);
                    info.guideObject.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetFKBoneTargetRotation(GuideObject bone)
        {
            OCIChar.BoneInfo info;
            if (_target.fkObjects.TryGetValue(bone.transformTarget.gameObject, out info) == false || !_target.fkEnabled || info.active == false)
                return Quaternion.identity;
            return info.guideObject.transformTarget.localRotation;
        }

        public void ScheduleLoad(XmlNode node, Action<bool> onLoadEnd)
        {
            MainWindow._self.StartCoroutine(LoadDefaultVersion_Routine(node, onLoadEnd));
        }

        public virtual void SaveXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteAttributeString("uniqueId", XmlConvert.ToString(GetInstanceID()));
            foreach (AdvancedModeModule module in _modules)
                module.SaveXml(xmlWriter);
        }

        // Using this directly will load the data on the same frame, only use this if you know exactly what you're doing.
        public virtual bool LoadXml(XmlNode xmlNode)
        {
            bool changed = false;
            _oldInstanceId = xmlNode.Attributes["uniqueId"] == null ? 0 : XmlConvert.ToInt32(xmlNode.Attributes["uniqueId"].Value);
            _loadId = MainWindow._lastLoadId;
            foreach (AdvancedModeModule module in _modules)
                changed = module.LoadXml(xmlNode) || changed;
            return changed;
        }
        #endregion

        #region Private Methods
        private void FillChildObjects()
        {
            foreach (KeyValuePair<TreeNodeObject, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicInfo)
            {
                if (pair.Value.guideObject.transformTarget != transform)
                    continue;
                foreach (TreeNodeObject child in pair.Key.child)
                {
                    RecurseChildObjects(child, childInfo =>
                    {
                        if (_childObjects.Contains(childInfo.guideObject.transformTarget.gameObject) == false)
                            _childObjects.Add(childInfo.guideObject.transformTarget.gameObject);
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
                RecurseChildObjects(child, action);
        }

        private void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            var dicInfo = Studio.Studio.Instance.dicInfo;

            if (dicInfo.TryGetValue(child, out var childInfo))
            {
                var childTransform = childInfo.guideObject.transformTarget;
                var childGObj = childTransform.gameObject;
                _childObjects.Remove(childGObj);   //If it doesn't exist, it does nothing.
            }

            foreach (AdvancedModeModule module in _modules)
                module.OnParentage(parent, child);
        }

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode, Action<bool> onLoadEnd)
        {
            yield return null;
            yield return null;
            yield return null;
            bool changed = LoadXml(xmlNode);
            if (onLoadEnd != null)
                onLoadEnd(changed);
        }
        #endregion

    }
}
