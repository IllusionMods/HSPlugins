using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Studio;
using ToolBox.Extensions;
using UnityEngine;
using Vectrosity;
#if AISHOUJO || HONEYSELECT2
using AIChara;
using AIProject;
#endif
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT || PLAYHOME || KOIKATSU
using DynamicBoneColliderBase = DynamicBoneCollider;
#endif

namespace HSPE.AMModules
{
    public class BoobsEditor : AdvancedModeModule
    {
        #region Constants
#if HONEYSELECT || PLAYHOME
        private const float _dragRadius = 0.05f;
#elif KOIKATSU
        private const float _dragRadius = 0.03f;
#elif AISHOUJO || HONEYSELECT2
        private const float _dragRadius = 0.5f;
#endif
        #endregion

        #region Private Types
        private class BoobData
        {
            public bool enabled;
            public Vector3 gravity;
            public Vector3 force;
            public EditableValue<bool> originalEnabled;
            public EditableValue<Vector3> originalGravity;
            public EditableValue<Vector3> originalForce;

            public BoobData()
            {
            }

            public BoobData(BoobData other)
            {
                enabled = other.enabled;
                force = other.force;
                gravity = other.gravity;
                originalEnabled = other.originalEnabled;
                originalGravity = other.originalGravity;
                originalForce = other.originalForce;
            }
        }

        private enum DynamicBoneDragType
        {
            LeftBoob,
            RightBoob,
#if KOIKATSU || AISHOUJO || HONEYSELECT2
            LeftButtCheek,
            RightButtCheek,
#endif
        }

        private class DebugLines
        {
            public VectorLine leftGravity;
            public VectorLine leftForce;
            public VectorLine leftBoth;
            public VectorLine leftCircle;
            public VectorLine rightGravity;
            public VectorLine rightForce;
            public VectorLine rightBoth;
            public VectorLine rightCircle;

            public DebugLines()
            {
                Vector3 origin = Vector3.zero;
                Vector3 final = Vector3.one;

                leftGravity = VectorLine.SetLine(_redColor, origin, final);
                leftGravity.endCap = "vector";
                leftGravity.lineWidth = 4f;

                leftForce = VectorLine.SetLine(_blueColor, origin, final);
                leftForce.endCap = "vector";
                leftForce.lineWidth = 4f;

                leftBoth = VectorLine.SetLine(_greenColor, origin, final);
                leftBoth.endCap = "vector";
                leftBoth.lineWidth = 4f;

                rightGravity = VectorLine.SetLine(_redColor, origin, final);
                rightGravity.endCap = "vector";
                rightGravity.lineWidth = 4f;

                rightForce = VectorLine.SetLine(_blueColor, origin, final);
                rightForce.endCap = "vector";
                rightForce.lineWidth = 4f;

                rightBoth = VectorLine.SetLine(_greenColor, origin, final);
                rightBoth.endCap = "vector";
                rightBoth.lineWidth = 4f;

                leftCircle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                rightCircle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                leftCircle.lineWidth = 4f;
                rightCircle.lineWidth = 4f;
                leftCircle.MakeCircle(Vector3.zero, Camera.main.transform.forward, _dragRadius);
                rightCircle.MakeCircle(Vector3.zero, Camera.main.transform.forward, _dragRadius);
            }

            public void Draw(DynamicBone_Ver02 left, DynamicBone_Ver02 right, int index)
            {
                float scale = 20f;
                Vector3 origin = left.Bones[left.Bones.Count - 1].position;
                Vector3 final = origin + (left.Gravity) * scale;

                leftGravity.points3[0] = origin;
                leftGravity.points3[1] = final;
                leftGravity.Draw();

                origin = final;
                final += left.Force * scale;

                leftForce.points3[0] = origin;
                leftForce.points3[1] = final;
                leftForce.Draw();

                origin = left.Bones[left.Bones.Count - 1].position;

                leftBoth.points3[0] = origin;
                leftBoth.points3[1] = final;
                leftBoth.Draw();

                origin = right.Bones[right.Bones.Count - 1].position;
                final = origin + (right.Gravity) * scale;

                rightGravity.points3[0] = origin;
                rightGravity.points3[1] = final;
                rightGravity.Draw();

                origin = final;
                final += right.Force * scale;

                rightForce.points3[0] = origin;
                rightForce.points3[1] = final;
                rightForce.Draw();

                origin = right.Bones[right.Bones.Count - 1].position;

                rightBoth.points3[0] = origin;
                rightBoth.points3[1] = final;
                rightBoth.Draw();

                leftCircle.MakeCircle(left.Bones[index].position, Camera.main.transform.forward, _dragRadius);
                leftCircle.Draw();
                rightCircle.MakeCircle(right.Bones[index].position, Camera.main.transform.forward, _dragRadius);
                rightCircle.Draw();
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref leftGravity);
                VectorLine.Destroy(ref leftForce);
                VectorLine.Destroy(ref leftBoth);
                VectorLine.Destroy(ref leftCircle);
                VectorLine.Destroy(ref rightGravity);
                VectorLine.Destroy(ref rightForce);
                VectorLine.Destroy(ref rightBoth);
                VectorLine.Destroy(ref rightCircle);
            }

            public void SetActive(bool active)
            {
                leftGravity.active = active;
                leftForce.active = active;
                leftBoth.active = active;
                leftCircle.active = active;
                rightGravity.active = active;
                rightForce.active = active;
                rightBoth.active = active;
                rightCircle.active = active;
            }
        }
        [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
        private class DynamicBone_Ver02_LateUpdate_Patches
        {
            public delegate void VoidDelegate(DynamicBone_Ver02 db, ref bool b);

            public static event VoidDelegate shouldExecuteLateUpdate;
            public static bool Prefix(DynamicBone_Ver02 __instance)
            {
                bool result = true;
                if (shouldExecuteLateUpdate != null)
                    shouldExecuteLateUpdate(__instance, ref result);
                return result;
            }
        }
        #endregion

        #region Private Variables

        private readonly GenericOCITarget _target;
        private readonly DynamicBone_Ver02 _rightBoob;
        private readonly DynamicBone_Ver02 _leftBoob;
        private readonly Action _rightBoobInitTransforms;
        private readonly Action<float> _rightBoobUpdateDynamicBones;
        private readonly Action _leftBoobInitTransforms;
        private readonly Action<float> _leftBoobUpdateDynamicBones;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private readonly DynamicBone_Ver02 _rightButtCheek;
        private readonly DynamicBone_Ver02 _leftButtCheek;
        private readonly Action _rightButtInitTransforms;
        private readonly Action<float> _rightButtUpdateDynamicBones;
        private readonly Action _leftButtInitTransforms;
        private readonly Action<float> _leftButtUpdateDynamicBones;
#endif
        private readonly Dictionary<DynamicBone_Ver02, BoobData> _dirtyDynamicBones = new Dictionary<DynamicBone_Ver02, BoobData>(4);
        private DynamicBoneDragType _dynamicBoneDragType;
        private Vector3 _dragDynamicBoneStartPosition;
        private Vector3 _dragDynamicBoneEndPosition;
        private Vector3 _lastDynamicBoneGravity;
        private static DebugLines _debugLines;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private static DebugLines _debugLinesButt;
        private Vector2 _scroll;
#endif
        private bool _alternativeUpdateMode = false;
        internal DynamicBone_Ver02[] _dynamicBones;
        private Action _preDragAction = null;
        #endregion

        #region Public Fields        
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BoobsEditor; } }
#if HONEYSELECT || PLAYHOME
        public override string displayName { get { return "Boobs"; } }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        public override string displayName { get { return "Boobs & Butt"; } }
#endif
        public bool isDraggingDynamicBone { get; private set; }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        #endregion

        #region Unity Methods
        public BoobsEditor(CharaPoseController parent, GenericOCITarget target) : base(parent)
        {
            _target = target;
            _parent.onLateUpdate += LateUpdate;
            if (_debugLines == null)
            {
                _debugLines = new DebugLines();
                _debugLines.SetActive(false);
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                _debugLinesButt = new DebugLines();
                _debugLinesButt.SetActive(false);
#endif
            }

#if HONEYSELECT
            this._leftBoob = ((CharFemaleBody)this._target.ociChar.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL);
            this._rightBoob = ((CharFemaleBody)this._target.ociChar.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR);
#elif PLAYHOME
            this._leftBoob = this._target.ociChar.charInfo.human.body.bustDynamicBone_L;
            this._rightBoob = this._target.ociChar.charInfo.human.body.bustDynamicBone_R;
#elif KOIKATSU
            _leftBoob = _target.ociChar.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastL);
            _rightBoob = _target.ociChar.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastR);
            _leftButtCheek = _target.ociChar.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.HipL); // "Hip" ( ͡° ͜ʖ ͡°)
            _rightButtCheek = _target.ociChar.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.HipR);
#elif AISHOUJO || HONEYSELECT2
            this._leftBoob = this._target.ociChar.charInfo.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.BreastL);
            this._rightBoob = this._target.ociChar.charInfo.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.BreastR);
            this._leftButtCheek = this._target.ociChar.charInfo.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.HipL);
            this._rightButtCheek = this._target.ociChar.charInfo.GetDynamicBoneBustAndHip(ChaControlDefine.DynamicBoneKind.HipR);
#endif

            DynamicBone_Ver02_LateUpdate_Patches.shouldExecuteLateUpdate += ShouldExecuteDynamicBoneLateUpdate;
            MethodInfo initTransforms = typeof(DynamicBone_Ver02).GetMethod("InitTransforms", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            MethodInfo updateDynamicBones = typeof(DynamicBone_Ver02).GetMethod("UpdateDynamicBones", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            _leftBoobInitTransforms = (Action)Delegate.CreateDelegate(typeof(Action), _leftBoob, initTransforms);
            _leftBoobUpdateDynamicBones = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), _leftBoob, updateDynamicBones);
            _rightBoobInitTransforms = (Action)Delegate.CreateDelegate(typeof(Action), _rightBoob, initTransforms);
            _rightBoobUpdateDynamicBones = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), _rightBoob, updateDynamicBones);
#if KOIKATSU || AISHOUJO || HONEYSELECT2
            _leftButtInitTransforms = (Action)Delegate.CreateDelegate(typeof(Action), _leftButtCheek, initTransforms);
            _leftButtUpdateDynamicBones = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), _leftButtCheek, updateDynamicBones);
            _rightButtInitTransforms = (Action)Delegate.CreateDelegate(typeof(Action), _rightButtCheek, initTransforms);
            _rightButtUpdateDynamicBones = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), _rightButtCheek, updateDynamicBones);
#endif
            _dynamicBones = _parent.GetComponentsInChildren<DynamicBone_Ver02>(true);

            foreach (KeyValuePair<DynamicBoneColliderBase, CollidersEditor> pair in CollidersEditor._loneColliders)
            {
                DynamicBoneCollider normalCollider = pair.Key as DynamicBoneCollider;
                if (normalCollider == null)
                    continue;
                Dictionary<object, bool> dynamicBonesDic;
                if (pair.Value._dirtyColliders.TryGetValue(pair.Key, out CollidersEditor.ColliderDataBase data) == false || data.allDynamicBones.TryGetValue(_parent, out dynamicBonesDic) == false)
                    dynamicBonesDic = null;
                foreach (DynamicBone_Ver02 bone in _dynamicBones)
                {
                    if (dynamicBonesDic != null)
                    {
                        if (dynamicBonesDic.ContainsKey(bone) == false)
                            dynamicBonesDic.Add(bone, pair.Value._addNewDynamicBonesAsDefault);

                        if (dynamicBonesDic[bone] == false)
                        {
                            if (bone.Colliders.Contains(normalCollider))
                                bone.Colliders.Remove(normalCollider);
                        }
                        else
                        {
                            if (bone.Colliders.Contains(normalCollider) == false)
                                bone.Colliders.Add(normalCollider);
                        }
                    }
                    else
                    {
                        pair.Value.SetIgnoreDynamicBone(pair.Key, _parent, bone, !pair.Value._addNewDynamicBonesAsDefault);
                    }
                }
            }
            _incIndex = -3;
        }

        private void LateUpdate()
        {
            if (_dirtyDynamicBones.Count != 0)
            {
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in _dirtyDynamicBones)
                {
                    if (kvp.Value.originalEnabled.hasValue)
                        kvp.Key.enabled = kvp.Value.enabled;
                    if (kvp.Value.originalGravity.hasValue)
                        kvp.Key.Gravity = kvp.Value.gravity;
                    if (kvp.Value.originalForce.hasValue)
                        kvp.Key.Force = kvp.Value.force;
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _parent.onLateUpdate -= LateUpdate;
            DynamicBone_Ver02_LateUpdate_Patches.shouldExecuteLateUpdate -= ShouldExecuteDynamicBoneLateUpdate;
        }
        #endregion

        #region Public Methods
        public override void IKSolverOnPostUpdate()
        {
            if (_alternativeUpdateMode)
            {
                if (_leftBoob.GetWeight() > 0f)
                {
                    _leftBoobInitTransforms();
                    _leftBoobUpdateDynamicBones(Time.deltaTime);
                }
                if (_rightBoob.GetWeight() > 0f)
                {
                    _rightBoobInitTransforms();
                    _rightBoobUpdateDynamicBones(Time.deltaTime);
                }
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                if (_leftButtCheek.GetWeight() > 0f)
                {
                    _leftButtInitTransforms();
                    _leftButtUpdateDynamicBones(Time.deltaTime);
                }
                if (_rightButtCheek.GetWeight() > 0f)
                {
                    _rightButtInitTransforms();
                    _rightButtUpdateDynamicBones(Time.deltaTime);
                }
#endif
            }
        }

        public override void FKCtrlOnPreLateUpdate()
        {
            if (_alternativeUpdateMode && _target.ociChar.oiCharInfo.enableIK == false)
            {
                if (_leftBoob.GetWeight() > 0f)
                {
                    _leftBoobInitTransforms();
                    _leftBoobUpdateDynamicBones(Time.deltaTime);
                }
                if (_rightBoob.GetWeight() > 0f)
                {
                    _rightBoobInitTransforms();
                    _rightBoobUpdateDynamicBones(Time.deltaTime);
                }
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                if (_leftButtCheek.GetWeight() > 0f)
                {
                    _leftButtInitTransforms();
                    _leftButtUpdateDynamicBones(Time.deltaTime);
                }
                if (_rightButtCheek.GetWeight() > 0f)
                {
                    _rightButtInitTransforms();
                    _rightButtUpdateDynamicBones(Time.deltaTime);
                }
#endif
            }
        }

        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(BoobsEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public override void GUILogic()
        {
            GUILayout.BeginVertical();

#if KOIKATSU || AISHOUJO || HONEYSELECT2
            GUILayout.BeginHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);
#endif

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            DisplaySingle(_rightBoob, "Right boob");
            GUILayout.EndVertical();

#if HONEYSELECT || PLAYHOME
            //GUILayout.BeginVertical();
            //GUILayout.FlexibleSpace();
            this.IncEditor(150, true);
            //GUILayout.FlexibleSpace();
            //GUILayout.EndVertical();
#endif

            GUILayout.BeginVertical(GUI.skin.box);
            DisplaySingle(_leftBoob, "Left boob");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

#if KOIKATSU || AISHOUJO || HONEYSELECT2
            //// BUTT
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            DisplaySingle(_rightButtCheek, "Right butt cheek");
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            DisplaySingle(_leftButtCheek, "Left butt cheek");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
#endif
            GUILayout.BeginHorizontal();
            _alternativeUpdateMode = GUILayout.Toggle(_alternativeUpdateMode, "Alternative update mode");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy to FK", GUILayout.ExpandWidth(false)))
                CopyToFK();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        public void LoadFrom(BoobsEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
#if HONEYSELECT
                CharFemale charFemale = this._target.ociChar.charInfo as CharFemale;
                CharFemale otherFemale = other._target.ociChar.charInfo as CharFemale;
#elif PLAYHOME
                ChaControl charFemale = this._target.ociChar.charInfo;
                ChaControl otherFemale = other._target.ociChar.charInfo;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                ChaControl charFemale = _target.ociChar.charInfo;
                ChaControl otherFemale = other._target.ociChar.charInfo;
#endif
                _alternativeUpdateMode = other._alternativeUpdateMode;
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in other._dirtyDynamicBones)
                {
                    DynamicBone_Ver02 db = GetDynamicBone(other.GetID(kvp.Key));

                    if (db != null)
                    {
                        if (kvp.Value.originalEnabled.hasValue)
                            db.enabled = other.GetEnabled(kvp.Key);
                        if (kvp.Value.originalForce.hasValue)
                            db.Force = other.GetForce(kvp.Key);
                        if (kvp.Value.originalGravity.hasValue)
                            db.Gravity = other.GetGravity(kvp.Key);
                        _dirtyDynamicBones.Add(db, new BoobData(kvp.Value));
                    }
                }
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            xmlWriter.WriteStartElement("boobs");
            xmlWriter.WriteAttributeString("alternativeUpdateMode", XmlConvert.ToString(_alternativeUpdateMode));
            written++;
            if (_dirtyDynamicBones.Count != 0)
            {
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in _dirtyDynamicBones)
                {
                    string name = "";
                    if (kvp.Key == _leftBoob)
                        name = "left";
                    else if (kvp.Key == _rightBoob)
                        name = "right";
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                    else if (kvp.Key == _leftButtCheek)
                        name = "leftButt";
                    else if (kvp.Key == _rightButtCheek)
                        name = "rightButt";
#endif
                    xmlWriter.WriteStartElement(name);

                    if (kvp.Value.originalEnabled.hasValue)
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(kvp.Value.enabled));

                    if (kvp.Value.originalGravity.hasValue)
                    {
                        xmlWriter.WriteAttributeString("gravityX", XmlConvert.ToString(kvp.Value.gravity.x));
                        xmlWriter.WriteAttributeString("gravityY", XmlConvert.ToString(kvp.Value.gravity.y));
                        xmlWriter.WriteAttributeString("gravityZ", XmlConvert.ToString(kvp.Value.gravity.z));
                    }

                    if (kvp.Value.originalForce.hasValue)
                    {
                        xmlWriter.WriteAttributeString("forceX", XmlConvert.ToString(kvp.Value.force.x));
                        xmlWriter.WriteAttributeString("forceY", XmlConvert.ToString(kvp.Value.force.y));
                        xmlWriter.WriteAttributeString("forceZ", XmlConvert.ToString(kvp.Value.force.z));
                    }

                    xmlWriter.WriteEndElement();
                    ++written;
                }
            }
            xmlWriter.WriteEndElement();
            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            SetNotDirty(_leftBoob);
            SetNotDirty(_rightBoob);
#if KOIKATSU || AISHOUJO || HONEYSELECT2
            SetNotDirty(_leftButtCheek);
            SetNotDirty(_rightButtCheek);
#endif

            bool changed = false;
            XmlNode boobs = xmlNode.FindChildNode("boobs");
            if (boobs != null)
            {
                if (boobs.Attributes != null && boobs.Attributes["alternativeUpdateMode"] != null)
                {
                    changed = true;
                    _alternativeUpdateMode = XmlConvert.ToBoolean(boobs.Attributes["alternativeUpdateMode"].Value);
                }
                foreach (XmlNode node in boobs.ChildNodes)
                {
                    try
                    {
                        DynamicBone_Ver02 boob = null;
                        switch (node.Name)
                        {
                            case "left":
                                boob = _leftBoob;
                                break;
                            case "right":
                                boob = _rightBoob;
                                break;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                            case "leftButt":
                                boob = _leftButtCheek;
                                break;
                            case "rightButt":
                                boob = _rightButtCheek;
                                break;
#endif
                        }

                        if (boob != null)
                        {
                            BoobData data = new BoobData();
                            if (node.Attributes["enabled"] != null)
                            {
                                bool e = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                                data.originalEnabled = boob.enabled;
                                data.enabled = e;
                            }
                            if (node.Attributes["gravityX"] != null && node.Attributes["gravityY"] != null && node.Attributes["gravityZ"] != null)
                            {
                                Vector3 gravity;
                                gravity.x = XmlConvert.ToSingle(node.Attributes["gravityX"].Value);
                                gravity.y = XmlConvert.ToSingle(node.Attributes["gravityY"].Value);
                                gravity.z = XmlConvert.ToSingle(node.Attributes["gravityZ"].Value);
                                data.originalGravity = boob.Gravity;
                                data.gravity = gravity;
                            }
                            if (node.Attributes["forceX"] != null && node.Attributes["forceY"] != null && node.Attributes["forceZ"] != null)
                            {
                                Vector3 force;
                                force.x = XmlConvert.ToSingle(node.Attributes["forceX"].Value);
                                force.y = XmlConvert.ToSingle(node.Attributes["forceY"].Value);
                                force.z = XmlConvert.ToSingle(node.Attributes["forceZ"].Value);
                                data.originalForce = boob.Force;
                                data.force = force;
                            }
                            if (data.originalEnabled.hasValue || data.originalGravity.hasValue || data.originalForce.hasValue)
                            {
                                changed = true;
                                _dirtyDynamicBones.Add(boob, data);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        HSPE.Logger.LogError("Couldn't load boob for character " + _parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            return changed;
        }

        public string GetID(DynamicBone_Ver02 db)
        {
            if (db == _leftBoob)
                return "BreastL";
            if (db == _rightBoob)
                return "BreastR";
#if KOIKATSU || AISHOUJO || HONEYSELECT2
            if (db == _leftButtCheek)
                return "HipL";
            if (db == _rightButtCheek)
                return "HipR";
#endif
            return null;
        }

        public DynamicBone_Ver02 GetDynamicBone(string id)
        {
            switch (id)
            {
                case "BreastL":
                    return _leftBoob;
                case "BreastR":
                    return _rightBoob;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                case "HipL":
                    return _leftButtCheek;
                case "HipR":
                    return _rightButtCheek;
#endif
            }
            return null;
        }
        #endregion

        #region Private Methods
        private void ShouldExecuteDynamicBoneLateUpdate(DynamicBone_Ver02 bone, ref bool b)
        {
            if (_parent.enabled && _alternativeUpdateMode &&
                (
                        bone == _leftBoob
                        || bone == _rightBoob
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                        || bone == _leftButtCheek
                        || bone == _rightButtCheek
#endif
                )
            )
                b = _target.ociChar.oiCharInfo.enableIK == false && _target.ociChar.oiCharInfo.enableFK == false;
        }

        private void CopyToFK()
        {
            _preDragAction = () =>
            {
                List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
                foreach (DynamicBone_Ver02 bone in _dynamicBones)
                {
                    foreach (Transform t in bone.Bones)
                    {
                        OCIChar.BoneInfo boneInfo;
                        if (t != null && _target.fkObjects.TryGetValue(t.gameObject, out boneInfo))
                        {
                            Vector3 oldValue = boneInfo.guideObject.changeAmount.rot;
                            boneInfo.guideObject.changeAmount.rot = t.localEulerAngles;
                            infos.Add(new GuideCommand.EqualsInfo()
                            {
                                dicKey = boneInfo.guideObject.dicKey,
                                oldValue = oldValue,
                                newValue = boneInfo.guideObject.changeAmount.rot
                            });
                        }
                    }
                }
                UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos.ToArray()));
            };
        }

        public override void UpdateGizmos()
        {
            if (GizmosEnabled() == false)
                return;
            DynamicBoneDraggingLogic();
            _debugLines.Draw(_leftBoob, _rightBoob, 2);
#if KOIKATSU || AISHOUJO || HONEYSELECT2
            _debugLinesButt.Draw(_leftButtCheek, _rightButtCheek, 1);
#endif
        }

        private void DisplaySingle(DynamicBone_Ver02 elem, string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + (IsDirty(elem) ? "*" : ""));
            GUILayout.FlexibleSpace();
            bool e = GetEnabled(elem);
            bool newE = GUILayout.Toggle(e, "Enabled");
            if (newE != e)
                SetEnabled(elem, newE);
            GUILayout.EndHorizontal();

            GUILayout.Label("Gravity");
            Vector3 gravity = GetGravity(elem);
            Vector3 newGravity = Vector3Editor(gravity, AdvancedModeModule._redColor, "X:   ", "Y:   ", "Z:   ");
            if (newGravity != gravity)
                SetGravity(elem, newGravity);

            GUILayout.Label("Force");
            Vector3 force = GetForce(elem);
            Vector3 newForce = Vector3Editor(force, AdvancedModeModule._blueColor, "X:   ", "Y:   ", "Z:   ");
            if (newForce != force)
                SetForce(elem, newForce);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Go to root", GUILayout.ExpandWidth(false)))
            {
                _parent.EnableModule(_parent._bonesEditor);
                _parent._bonesEditor.GoToObject(elem.Root.gameObject);
            }

            if (GUILayout.Button("Go to tail", GUILayout.ExpandWidth(false)))
            {
                _parent.EnableModule(_parent._bonesEditor);
                _parent._bonesEditor.GoToObject(elem.Root.GetDeepestLeaf().gameObject);
            }

            GUILayout.FlexibleSpace();
            Color c = GUI.color;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                SetNotDirty(elem);
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        private bool GetEnabled(DynamicBone_Ver02 bone)
        {
            BoobData data;
            if (_dirtyDynamicBones.TryGetValue(bone, out data) && data.originalEnabled.hasValue)
                return data.enabled;
            return bone.enabled;
        }

        private Vector3 GetGravity(DynamicBone_Ver02 bone)
        {
            BoobData data;
            if (_dirtyDynamicBones.TryGetValue(bone, out data) && data.originalGravity.hasValue)
                return data.gravity;
            return bone.Gravity;
        }

        private Vector3 GetForce(DynamicBone_Ver02 bone)
        {
            BoobData data;
            if (_dirtyDynamicBones.TryGetValue(bone, out data) && data.originalForce.hasValue)
                return data.force;
            return bone.Force;
        }

        private void SetEnabled(DynamicBone_Ver02 bone, bool e)
        {
            BoobData data = SetDirty(bone);
            if (data.originalEnabled.hasValue == false)
                data.originalEnabled = bone.enabled;
            data.enabled = e;
        }

        private void SetGravity(DynamicBone_Ver02 bone, Vector3 gravity)
        {
            BoobData data = SetDirty(bone);
            if (data.originalGravity.hasValue == false)
                data.originalGravity = bone.Gravity;
            data.gravity = gravity;
        }

        private void SetForce(DynamicBone_Ver02 bone, Vector3 force)
        {
            BoobData data = SetDirty(bone);
            if (data.originalForce.hasValue == false)
                data.originalForce = bone.Force;
            data.force = force;
        }

        private bool IsDirty(DynamicBone_Ver02 boob)
        {
            return (_dirtyDynamicBones.ContainsKey(boob));
        }

        private BoobData SetDirty(DynamicBone_Ver02 boob)
        {
            BoobData data;
            if (_dirtyDynamicBones.TryGetValue(boob, out data) == false)
            {
                data = new BoobData();
                _dirtyDynamicBones.Add(boob, data);
            }
            return data;
        }

        private void SetNotDirty(DynamicBone_Ver02 boob)
        {
            if (IsDirty(boob))
            {
                BoobData data = _dirtyDynamicBones[boob];
                if (data.originalEnabled.hasValue)
                {
                    boob.enabled = data.originalEnabled;
                    data.originalEnabled.Reset();
                }
                if (data.originalGravity.hasValue)
                {
                    boob.Gravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                if (data.originalForce.hasValue)
                {
                    boob.Force = data.originalForce;
                    data.originalForce.Reset();
                }
                _dirtyDynamicBones.Remove(boob);
            }
        }

        private void DynamicBoneDraggingLogic()
        {
            if (_preDragAction != null)
                _preDragAction();
            _preDragAction = null;
            if (Input.GetMouseButtonDown(0))
            {
                float distanceFromCamera = float.PositiveInfinity;

                Vector3 leftBoobRaycastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(_leftBoob.Bones[2].position - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                if ((leftBoobRaycastPos - _leftBoob.Bones[2].position).sqrMagnitude < (_dragRadius * _dragRadius))
                {
                    isDraggingDynamicBone = true;
                    distanceFromCamera = (leftBoobRaycastPos - Camera.main.transform.position).sqrMagnitude;
                    _dynamicBoneDragType = DynamicBoneDragType.LeftBoob;
                    _dragDynamicBoneStartPosition = leftBoobRaycastPos;
                    _lastDynamicBoneGravity = GetGravity(_leftBoob);
                }

                Vector3 rightBoobRaycastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(_rightBoob.Bones[2].position - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                if ((rightBoobRaycastPos - _rightBoob.Bones[2].position).sqrMagnitude < (_dragRadius * _dragRadius) &&
                    (rightBoobRaycastPos - Camera.main.transform.position).sqrMagnitude < distanceFromCamera)
                {
                    isDraggingDynamicBone = true;
                    distanceFromCamera = (leftBoobRaycastPos - Camera.main.transform.position).sqrMagnitude;
                    _dynamicBoneDragType = DynamicBoneDragType.RightBoob;
                    _dragDynamicBoneStartPosition = rightBoobRaycastPos;
                    _lastDynamicBoneGravity = GetGravity(_rightBoob);
                }

#if KOIKATSU || AISHOUJO || HONEYSELECT2
                Vector3 leftButtCheekRaycastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(_leftButtCheek.Bones[1].position - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                if ((leftButtCheekRaycastPos - _leftButtCheek.Bones[1].position).sqrMagnitude < (_dragRadius * _dragRadius))
                {
                    isDraggingDynamicBone = true;
                    distanceFromCamera = (leftButtCheekRaycastPos - Camera.main.transform.position).sqrMagnitude;
                    _dynamicBoneDragType = DynamicBoneDragType.LeftButtCheek;
                    _dragDynamicBoneStartPosition = leftButtCheekRaycastPos;
                    _lastDynamicBoneGravity = GetGravity(_leftButtCheek);
                }

                Vector3 rightButtCheekRaycastPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(_rightButtCheek.Bones[1].position - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                if ((rightButtCheekRaycastPos - _rightButtCheek.Bones[1].position).sqrMagnitude < (_dragRadius * _dragRadius) &&
                    (rightButtCheekRaycastPos - Camera.main.transform.position).sqrMagnitude < distanceFromCamera)
                {
                    isDraggingDynamicBone = true;
                    distanceFromCamera = (leftButtCheekRaycastPos - Camera.main.transform.position).sqrMagnitude;
                    _dynamicBoneDragType = DynamicBoneDragType.RightButtCheek;
                    _dragDynamicBoneStartPosition = rightButtCheekRaycastPos;
                    _lastDynamicBoneGravity = GetGravity(_rightButtCheek);
                }
#endif
            }
            else if (Input.GetMouseButton(0) && isDraggingDynamicBone)
            {
                _dragDynamicBoneEndPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(_dragDynamicBoneStartPosition - Camera.main.transform.position, Camera.main.transform.forward).magnitude));
                DynamicBone_Ver02 db = null;
                switch (_dynamicBoneDragType)
                {
                    case DynamicBoneDragType.LeftBoob:
                        db = _leftBoob;
                        break;
                    case DynamicBoneDragType.RightBoob:
                        db = _rightBoob;
                        break;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                    case DynamicBoneDragType.LeftButtCheek:
                        db = _leftButtCheek;
                        break;
                    case DynamicBoneDragType.RightButtCheek:
                        db = _rightButtCheek;
                        break;
#endif
                }
                SetGravity(db, _lastDynamicBoneGravity + (_dragDynamicBoneEndPosition - _dragDynamicBoneStartPosition) * (_inc * 1000f) / 12f);
            }
            else if (Input.GetMouseButtonUp(0))
                isDraggingDynamicBone = false;
        }

        private static void UpdateDebugLinesState(BoobsEditor self)
        {
            if (_debugLines != null)
            {
                bool flag = self != null && self.GizmosEnabled();
                _debugLines.SetActive(flag);
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                _debugLinesButt.SetActive(flag);
#endif
            }
        }

        private bool GizmosEnabled()
        {
            return _isEnabled && PoseController._drawAdvancedMode && _parent != null;
        }

        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "leftBoobEnabled",
                        name: "Left Boob Enabled",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            BoobsEditor p = ((BoobsEditor)parameter);
                            if (p.GetEnabled(p._leftBoob) != (bool)leftValue)
                                p.SetEnabled(((BoobsEditor)parameter)._leftBoob, (bool)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetEnabled(((BoobsEditor)parameter)._leftBoob),
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                        );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "leftBoobGravity",
                        name: "Left Boob Gravity",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetGravity(((BoobsEditor)parameter)._leftBoob, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetGravity(((BoobsEditor)parameter)._leftBoob),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                        );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "leftBoobForce",
                        name: "Left Boob Force",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetForce(((BoobsEditor)parameter)._leftBoob, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetForce(((BoobsEditor)parameter)._leftBoob),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "rightBoobEnabled",
                        name: "Right Boob Enabled",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            BoobsEditor p = ((BoobsEditor)parameter);
                            if (p.GetEnabled(p._rightBoob) != (bool)leftValue)
                                p.SetEnabled(((BoobsEditor)parameter)._rightBoob, (bool)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetEnabled(((BoobsEditor)parameter)._rightBoob),
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "rightBoobGravity",
                        name: "Right Boob Gravity",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetGravity(((BoobsEditor)parameter)._rightBoob, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetGravity(((BoobsEditor)parameter)._rightBoob),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "rightBoobForce",
                        name: "Right Boob Force",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetForce(((BoobsEditor)parameter)._rightBoob, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetForce(((BoobsEditor)parameter)._rightBoob),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "leftButtEnabled",
                        name: "Left Butt Enabled",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            BoobsEditor p = ((BoobsEditor)parameter);
                            if (p.GetEnabled(p._leftButtCheek) != (bool)leftValue)
                                p.SetEnabled(((BoobsEditor)parameter)._leftButtCheek, (bool)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetEnabled(((BoobsEditor)parameter)._leftButtCheek),
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                        );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "leftButtGravity",
                        name: "Left Butt Gravity",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetGravity(((BoobsEditor)parameter)._leftButtCheek, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetGravity(((BoobsEditor)parameter)._leftButtCheek),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                        );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "leftButtForce",
                        name: "Left Butt Force",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetForce(((BoobsEditor)parameter)._leftButtCheek, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetForce(((BoobsEditor)parameter)._leftButtCheek),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "rightButtEnabled",
                        name: "Right Butt Enabled",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            BoobsEditor p = ((BoobsEditor)parameter);
                            if (p.GetEnabled(p._rightButtCheek) != (bool)leftValue)
                                p.SetEnabled(((BoobsEditor)parameter)._rightButtCheek, (bool)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetEnabled(((BoobsEditor)parameter)._rightButtCheek),
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "rightButtGravity",
                        name: "Right Butt Gravity",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetGravity(((BoobsEditor)parameter)._rightButtCheek, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetGravity(((BoobsEditor)parameter)._rightButtCheek),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE.Name,
                        id: "rightButtForce",
                        name: "Right Butt Force",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((BoobsEditor)parameter).SetForce(((BoobsEditor)parameter)._rightButtCheek, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((BoobsEditor)parameter).GetForce(((BoobsEditor)parameter)._rightButtCheek),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: null,
                        writeParameterToXml: null,
                        checkIntegrity: CheckIntegrity
                );
#endif                
            }

            private static bool CheckIntegrity(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                return parameter != null;
            }

            private static bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
            {
                CharaPoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<CharaPoseController>()) != null && controller._target.isFemale;
            }

            private static object GetParameter(ObjectCtrlInfo oci)
            {
                return oci.guideObject.transformTarget.GetComponent<CharaPoseController>()._boobsEditor;
            }
        }
        #endregion
    }
}
