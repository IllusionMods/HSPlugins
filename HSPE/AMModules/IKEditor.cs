using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RootMotion.FinalIK;
using Studio;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

namespace HSPE.AMModules
{
    public class IKEditor : AdvancedModeModule
    {
        #region Private Types
        private enum IKType
        {
            Unknown,
            FABRIK,
            FABRIKRoot,
            AimIK,
            CCDIK,
            FullBodyBipedIK,
            LimbIK
        }

        private class IKWrapper
        {
            public readonly IK ik;
            public readonly IKSolver solver;
            public readonly IKType type;

            public IKWrapper(IK ik)
            {
                this.ik = ik;
                this.solver = this.ik.GetIKSolver();
                if (this.ik is CCDIK)
                    this.type = IKType.CCDIK;
                else if (this.ik is FABRIK)
                    this.type = IKType.FABRIK;
                else if (this.ik is FABRIKRoot)
                    this.type = IKType.FABRIKRoot;
                else if (this.ik is AimIK)
                    this.type = IKType.AimIK;
                else if (this.ik is FullBodyBipedIK)
                    this.type = IKType.FullBodyBipedIK;
                else if (this.ik is LimbIK)
                    this.type = IKType.LimbIK;
            }
        }

        private class IKData
        {
            public EditableValue<bool> originalEnabled;
            public EditableValue<float> originalWeight;
            public EditableValue<bool> originalFixTransforms;

            protected IKData() { }

            protected IKData(IKData other)
            {
                this.originalEnabled = other.originalEnabled;
                this.originalWeight = other.originalWeight;
                this.originalFixTransforms = other.originalFixTransforms;
            }
        }

        private class CCDIKData : IKData
        {
            public EditableValue<float> originalTolerance;
            public EditableValue<int> originalMaxIterations;

            public CCDIKData() { }

            public CCDIKData(CCDIKData other) : base(other)
            {
                this.originalTolerance = other.originalTolerance;
                this.originalMaxIterations = other.originalMaxIterations;
            }
        }

        private class FABRIKData : IKData
        {
            public EditableValue<float> originalTolerance;
            public EditableValue<int> originalMaxIterations;

            public FABRIKData() { }

            public FABRIKData(FABRIKData other) : base(other)
            {
                this.originalTolerance = other.originalTolerance;
                this.originalMaxIterations = other.originalMaxIterations;
            }
        }

        private class FABRIKRootData : IKData
        {
            public EditableValue<float> originalRootPin;
            public EditableValue<int> originalIterations;

            public FABRIKRootData() { }

            public FABRIKRootData(FABRIKRootData other) : base(other)
            {
                this.originalRootPin = other.originalRootPin;
                this.originalIterations = other.originalIterations;
            }
        }

        private class AimIKData : IKData
        {
            public EditableValue<float> originalTolerance;
            public EditableValue<int> originalMaxIterations;
            public EditableValue<Vector3> originalAxis;
            public EditableValue<float> originalClampWeight;
            public EditableValue<int> originalClampSmoothing;
            public EditableValue<Vector3> originalPoleAxis;
            public EditableValue<Vector3> originalPolePosition;
            public EditableValue<float> originalPoleWeight;

            public AimIKData() { }

            public AimIKData(AimIKData other) : base(other)
            {
                this.originalTolerance = other.originalTolerance;
                this.originalMaxIterations = other.originalMaxIterations;
                this.originalAxis = other.originalAxis;
                this.originalClampWeight = other.originalClampWeight;
                this.originalClampSmoothing = other.originalClampSmoothing;
                this.originalPoleAxis = other.originalPoleAxis;
                this.originalPolePosition = other.originalPolePosition;
                this.originalPoleWeight = other.originalPoleWeight;
            }
        }

        private class FullBodyBipedIKData : IKData
        {
            public class EffectorData
            {
                public EditableValue<float> originalPositionWeight;
                public float currentPositionWeight;
                public EditableValue<float> originalRotationWeight;
                public float currentRotationWeight;

                public EffectorData() { }

                public EffectorData(EffectorData other)
                {
                    this.originalPositionWeight = other.originalPositionWeight;
                    this.originalRotationWeight = other.originalRotationWeight;

                    this.currentPositionWeight = other.currentPositionWeight;
                    this.currentRotationWeight = other.currentRotationWeight;
                }
            }

            public class ConstraintBendData
            {
                public EditableValue<float> originalWeight;
                public float currentWeight;

                public ConstraintBendData() { }

                public ConstraintBendData(ConstraintBendData other)
                {
                    this.originalWeight = other.originalWeight;
                    this.currentWeight = other.currentWeight;
                }
            }

            public readonly EffectorData body;
            public readonly EffectorData leftShoulder;
            public readonly EffectorData leftHand;
            public readonly ConstraintBendData leftArm;
            public readonly EffectorData rightShoulder;
            public readonly EffectorData rightHand;
            public readonly ConstraintBendData rightArm;
            public readonly EffectorData leftThigh;
            public readonly EffectorData leftFoot;
            public readonly ConstraintBendData leftLeg;
            public readonly EffectorData rightThigh;
            public readonly EffectorData rightFoot;
            public readonly ConstraintBendData rightLeg;

            public FullBodyBipedIKData()
            {
                this.body = new EffectorData();
                this.leftShoulder = new EffectorData();
                this.leftHand = new EffectorData();
                this.leftArm = new ConstraintBendData();
                this.rightShoulder = new EffectorData();
                this.rightHand = new EffectorData();
                this.rightArm = new ConstraintBendData();
                this.leftThigh = new EffectorData();
                this.leftFoot = new EffectorData();
                this.leftLeg = new ConstraintBendData();
                this.rightThigh = new EffectorData();
                this.rightFoot = new EffectorData();
                this.rightLeg = new ConstraintBendData();
            }

            public FullBodyBipedIKData(FullBodyBipedIKData other) : base(other)
            {
                this.body = new EffectorData(other.body);
                this.leftShoulder = new EffectorData(other.leftShoulder);
                this.leftHand = new EffectorData(other.leftHand);
                this.leftArm = new ConstraintBendData(other.leftArm);
                this.rightShoulder = new EffectorData(other.rightShoulder);
                this.rightHand = new EffectorData(other.rightHand);
                this.rightArm = new ConstraintBendData(other.rightArm);
                this.leftThigh = new EffectorData(other.leftThigh);
                this.leftFoot = new EffectorData(other.leftFoot);
                this.leftLeg = new ConstraintBendData(other.leftLeg);
                this.rightThigh = new EffectorData(other.rightThigh);
                this.rightFoot = new EffectorData(other.rightFoot);
                this.rightLeg = new ConstraintBendData(other.rightLeg);
            }
        }

        private class LimbIKData : IKData
        {
            public EditableValue<float> originalRotationWeight;
            public EditableValue<float> originalBendModifierWeight;

            public LimbIKData() { }

            public LimbIKData(LimbIKData other) : base(other)
            {
                this.originalRotationWeight = other.originalRotationWeight;
                this.originalBendModifierWeight = other.originalBendModifierWeight;
            }
        }
        #endregion

        #region Private Variables
        private readonly GenericOCITarget _target;
        private readonly bool _isCharacter;
        private readonly bool _registered = false;
        private readonly Dictionary<IK, IKWrapper> _iks = new Dictionary<IK, IKWrapper>();
        private readonly Dictionary<IKWrapper, IKData> _dirtyIks = new Dictionary<IKWrapper, IKData>();
        private Vector2 _scroll;
        private Action _updateAction;
        private IKWrapper _ikTarget;
        private Vector2 _iksScroll;
        #endregion

        #region Public Accessors
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.IK; } }
        public override string displayName { get { return "IK"; } }
        public override bool shouldDisplay { get { return this._iks.Count != 0; } }
        #endregion

        #region Unity Methods
        public IKEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            this._target = target;
            this.RefreshIKList();

            if (this._iks.Any(ik => ik.Value.type == IKType.FullBodyBipedIK))
            {
                this._parent.onLateUpdate += this.LateUpdate;
                this._registered = true;
            }
            this._isCharacter = this._target.type == GenericOCITarget.Type.Character;
            this._incIndex = -1;
        }

        private void Update()
        {
        }

        private void LateUpdate()
        {
            foreach (KeyValuePair<IKWrapper, IKData> pair in this._dirtyIks)
            {
                if (pair.Key.type != IKType.FullBodyBipedIK)
                    continue;
                IKSolverFullBodyBiped solver = (IKSolverFullBodyBiped)pair.Key.solver;
                FullBodyBipedIKData data = (FullBodyBipedIKData)pair.Value;
                if (this._isCharacter == false || this._target.ociChar.oiCharInfo.enableIK)
                {
                    if (this._isCharacter == false || this._target.ociChar.oiCharInfo.activeIK[0])
                        this.ApplyFullBodyBipedEffectorData(solver.bodyEffector, data.body);
                    if (this._isCharacter == false || this._target.ociChar.oiCharInfo.activeIK[4])
                    {
                        this.ApplyFullBodyBipedEffectorData(solver.leftShoulderEffector, data.leftShoulder);
                        this.ApplyFullBodyBipedConstraintBendData(solver.leftArmChain.bendConstraint, data.leftArm);
                        this.ApplyFullBodyBipedEffectorData(solver.leftHandEffector, data.leftHand);
                    }
                    if (this._isCharacter == false || this._target.ociChar.oiCharInfo.activeIK[3])
                    {
                        this.ApplyFullBodyBipedEffectorData(solver.rightShoulderEffector, data.rightShoulder);
                        this.ApplyFullBodyBipedConstraintBendData(solver.rightArmChain.bendConstraint, data.rightArm);
                        this.ApplyFullBodyBipedEffectorData(solver.rightHandEffector, data.rightHand);
                    }
                    if (this._isCharacter == false || this._target.ociChar.oiCharInfo.activeIK[2])
                    {
                        this.ApplyFullBodyBipedEffectorData(solver.leftThighEffector, data.leftThigh);
                        this.ApplyFullBodyBipedConstraintBendData(solver.leftLegChain.bendConstraint, data.leftLeg);
                        this.ApplyFullBodyBipedEffectorData(solver.leftFootEffector, data.leftFoot);
                    }
                    if (this._isCharacter == false || this._target.ociChar.oiCharInfo.activeIK[1])
                    {
                        this.ApplyFullBodyBipedEffectorData(solver.rightThighEffector, data.rightThigh);
                        this.ApplyFullBodyBipedConstraintBendData(solver.rightLegChain.bendConstraint, data.rightLeg);
                        this.ApplyFullBodyBipedEffectorData(solver.rightFootEffector, data.rightFoot);
                    }
                }
            }
        }

        public override void GUILogic()
        {
            Color c = GUI.color;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            this._iksScroll = GUILayout.BeginScrollView(this._iksScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (KeyValuePair<IK, IKWrapper> pair in this._iks)
            {
                if (this._dirtyIks.ContainsKey(pair.Value))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(pair.Value, this._ikTarget))
                    GUI.color = Color.cyan;
                string dName = pair.Value.solver.GetRoot().name;
                string newName;
                if (BonesEditor._boneAliases.TryGetValue(dName, out newName))
                    dName = newName;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(dName + (this._dirtyIks.ContainsKey(pair.Value) ? "*" : "")))
                {
                    this._ikTarget = pair.Value;
                    ToolBox.TimelineCompatibility.RefreshInterpolablesList();
                }
                GUILayout.Space(GUI.skin.verticalScrollbar.fixedWidth);
                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Copy to FK"))
                this.CopyToFK();
            if (GUILayout.Button("Force refresh list"))
                this.RefreshIKList();

            {
                GUI.color = Color.red;
                if (GUILayout.Button("Reset all") && this._ikTarget != null)
                    this.ResetAll();
                GUI.color = c;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            this._scroll = GUILayout.BeginScrollView(this._scroll);
            if (this._ikTarget != null)
            {
                IKData data;
                this._dirtyIks.TryGetValue(this._ikTarget, out data);
                if (this._isCharacter == false)
                {
                    bool b = this.GetIKEnabled(this._ikTarget); ;
                    GUILayout.BeginHorizontal();
                    if (data != null && data.originalEnabled.hasValue)
                        GUI.color = Color.magenta;
                    b = GUILayout.Toggle(b, "Enabled", GUILayout.ExpandWidth(false));
                    if (b != this._ikTarget.ik.enabled)
                        this.SetIKEnabled(this._ikTarget, b);
                    GUI.color = c;

                    GUILayout.FlexibleSpace();

                    GUI.color = Color.red;
                    if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                    {
                        if (data != null && data.originalEnabled.hasValue)
                        {
                            this._ikTarget.ik.enabled = data.originalEnabled;
                            data.originalEnabled.Reset();
                            if (this._ikTarget.ik.enabled == false)
                                this._ikTarget.solver.FixTransforms();
                        }
                    }
                    GUI.color = c;
                    GUILayout.EndHorizontal();

                }

                {
                    bool b = this.GetIKFixTransforms(this._ikTarget);
                    GUILayout.BeginHorizontal();
                    if (data != null && data.originalFixTransforms.hasValue)
                        GUI.color = Color.magenta;
                    b = GUILayout.Toggle(b, "Fix Transforms", GUILayout.ExpandWidth(false));
                    if (b != this._ikTarget.ik.fixTransforms)
                        this.SetIKFixTransforms(this._ikTarget, b);
                    GUI.color = c;

                    GUILayout.FlexibleSpace();

                    GUI.color = Color.red;
                    if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                    {
                        if (data != null && data.originalFixTransforms.hasValue)
                        {
                            this._ikTarget.ik.fixTransforms = data.originalFixTransforms;
                            data.originalFixTransforms.Reset();
                        }
                    }
                    GUI.color = c;
                    GUILayout.EndHorizontal();

                }


                {
                    if (data != null && data.originalWeight.hasValue)
                        GUI.color = Color.magenta;
                    float v = this._ikTarget.solver.GetIKPositionWeight();
                    v = this.FloatEditor(v, 0f, 1f, "Pos Weight\t", "0.0000", onReset: (value) =>
                    {
                        if (data != null && data.originalWeight.hasValue)
                        {
                            this._ikTarget.solver.SetIKPositionWeight(data.originalWeight);
                            data.originalWeight.Reset();
                            return this._ikTarget.solver.GetIKPositionWeight();
                        }
                        return value;
                    });
                    GUI.color = c;
                    if (!Mathf.Approximately(this._ikTarget.solver.GetIKPositionWeight(), v))
                        this.SetIKPositionWeight(this._ikTarget, v);
                }

                switch (this._ikTarget.type)
                {
                    case IKType.FABRIK:
                        this.FABRIKFields(this._ikTarget, data);
                        break;
                    case IKType.FABRIKRoot:
                        this.FABRIKRootFields(this._ikTarget, data);
                        break;
                    case IKType.CCDIK:
                        this.CCDIKFields(this._ikTarget, data);
                        break;
                    case IKType.AimIK:
                        this.AimIKFields(this._ikTarget, data);
                        break;
                    case IKType.FullBodyBipedIK:
                        this.FullBodyBipedIKFields(this._ikTarget, data);
                        break;
                    case IKType.LimbIK:
                        this.LimbIKFields(this._ikTarget, data);
                        break;
                }

            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUI.color = Color.red;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._ikTarget != null)
                this.SetIKNotDirty(this._ikTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        public override void UpdateGizmos()
        {
            if (this._updateAction != null)
            {
                this._updateAction();
                this._updateAction = null;
            }
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            if (this._registered)
                this._parent.onLateUpdate -= this.LateUpdate;
            this._parent.onUpdate -= this.Update;
        }
        #endregion

        #region Public Methods
        public void LoadFrom(IKEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<IKWrapper, IKData> otherPair in other._dirtyIks)
                {
                    Transform t = this._parent.transform.Find(otherPair.Key.solver.GetRoot().GetPathFrom(other._parent.transform));
                    IKWrapper ik = this._iks.FirstOrDefault(i => i.Value.solver.GetRoot() == t && this._dirtyIks.ContainsKey(i.Value) == false).Value;
                    if (ik == null || ik.type != otherPair.Key.type || ik.type == IKType.Unknown)
                        continue;

                    if (otherPair.Value.originalEnabled.hasValue)
                        ik.ik.enabled = otherPair.Key.ik.enabled;
                    if (otherPair.Value.originalWeight.hasValue)
                        ik.solver.SetIKPositionWeight(otherPair.Key.solver.GetIKPositionWeight());

                    IKData ikData = null;
                    switch (ik.type)
                    {
                        case IKType.FABRIK:
                            {
                                IKSolverFABRIK otherSolver = (IKSolverFABRIK)otherPair.Key.solver;
                                FABRIKData otherData = (FABRIKData)otherPair.Value;
                                IKSolverFABRIK solver = (IKSolverFABRIK)ik.solver;
                                if (otherData.originalTolerance.hasValue)
                                    solver.tolerance = otherSolver.tolerance;
                                if (otherData.originalMaxIterations.hasValue)
                                    solver.maxIterations = otherSolver.maxIterations;
                                ikData = new FABRIKData(otherData);
                                break;
                            }
                        case IKType.FABRIKRoot:
                            {
                                IKSolverFABRIKRoot otherSolver = (IKSolverFABRIKRoot)otherPair.Key.solver;
                                FABRIKRootData otherData = (FABRIKRootData)otherPair.Value;
                                IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)ik.solver;
                                if (otherData.originalRootPin.hasValue)
                                    solver.rootPin = otherSolver.rootPin;
                                if (otherData.originalIterations.hasValue)
                                    solver.iterations = otherSolver.iterations;
                                ikData = new FABRIKRootData(otherData);
                                break;
                            }
                        case IKType.AimIK:
                            {
                                IKSolverAim otherSolver = (IKSolverAim)otherPair.Key.solver;
                                AimIKData otherData = (AimIKData)otherPair.Value;
                                IKSolverAim solver = (IKSolverAim)ik.solver;
                                if (otherData.originalTolerance.hasValue)
                                    solver.tolerance = otherSolver.tolerance;
                                if (otherData.originalMaxIterations.hasValue)
                                    solver.maxIterations = otherSolver.maxIterations;
                                if (otherData.originalAxis.hasValue)
                                    solver.axis = otherSolver.axis;
                                if (otherData.originalClampWeight.hasValue)
                                    solver.clampWeight = otherSolver.clampWeight;
                                if (otherData.originalClampSmoothing.hasValue)
                                    solver.clampSmoothing = otherSolver.clampSmoothing;
                                if (otherData.originalPoleAxis.hasValue)
                                    solver.poleAxis = otherSolver.poleAxis;
                                if (otherData.originalPolePosition.hasValue)
                                    solver.polePosition = otherSolver.polePosition;
                                if (otherData.originalPoleWeight.hasValue)
                                    solver.poleWeight = otherSolver.poleWeight;
                                ikData = new AimIKData(otherData);
                                break;
                            }
                        case IKType.CCDIK:
                            {
                                IKSolverCCD otherSolver = (IKSolverCCD)otherPair.Key.solver;
                                CCDIKData otherData = (CCDIKData)otherPair.Value;
                                IKSolverCCD solver = (IKSolverCCD)ik.solver;
                                if (otherData.originalTolerance.hasValue)
                                    solver.tolerance = otherSolver.tolerance;
                                if (otherData.originalMaxIterations.hasValue)
                                    solver.maxIterations = otherSolver.maxIterations;
                                ikData = new CCDIKData(otherData);
                                break;
                            }
                        case IKType.FullBodyBipedIK:
                            {
                                //IKSolverFullBodyBiped otherSolver = (IKSolverFullBodyBiped)otherPair.Key.solver;
                                FullBodyBipedIKData otherData = (FullBodyBipedIKData)otherPair.Value;
                                //IKSolverFullBodyBiped solver = (IKSolverFullBodyBiped)ik.solver;
                                ikData = new FullBodyBipedIKData(otherData);
                                break;
                            }
                        case IKType.LimbIK:
                            {
                                IKSolverLimb otherSolver = (IKSolverLimb)otherPair.Key.solver;
                                LimbIKData otherData = (LimbIKData)otherPair.Value;
                                IKSolverLimb solver = (IKSolverLimb)ik.solver;
                                if (otherData.originalRotationWeight.hasValue)
                                    solver.IKRotationWeight = otherSolver.IKRotationWeight;
                                if (otherData.originalBendModifierWeight.hasValue)
                                    solver.bendModifierWeight = otherSolver.bendModifierWeight;
                                ikData = new LimbIKData(otherData);
                                break;
                            }
                    }
                    this._dirtyIks.Add(ik, ikData);
                }

            }, 2);
        }
        #endregion

        #region Private Methods

        private void FABRIKFields(IKWrapper target, IKData d)
        {
            IKSolverFABRIK solver = (IKSolverFABRIK)target.solver;
            FABRIKData data = d as FABRIKData;
            Color c = GUI.color;

            {
                if (data != null && data.originalTolerance.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetFABRIKTolerance(target);
                v = this.FloatEditor(v, 0f, 1f, "Tolerance\t\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalTolerance.hasValue)
                    {
                        solver.tolerance = data.originalTolerance;
                        data.originalTolerance.Reset();
                        return solver.tolerance;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.tolerance, v))
                    this.SetFABRIKTolerance(target, v);
            }

            {
                if (data != null && data.originalMaxIterations.hasValue)
                    GUI.color = Color.magenta;
                int v = this.GetFABRIKMaxIterations(target);
                v = Mathf.RoundToInt(this.FloatEditor(v, 0f, 100f, "Max iterations\t", "0", onReset: (value) =>
                {
                    if (data != null && data.originalMaxIterations.hasValue)
                    {
                        solver.maxIterations = data.originalMaxIterations;
                        data.originalMaxIterations.Reset();
                        return solver.maxIterations;
                    }
                    return value;
                }));
                GUI.color = c;
                if (v != solver.maxIterations)
                    this.SetFABRIKMaxIterations(target, v);
            }
        }

        private void FABRIKRootFields(IKWrapper target, IKData d)
        {
            IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)target.solver;
            FABRIKRootData data = d as FABRIKRootData;
            Color c = GUI.color;

            {
                if (data != null && data.originalRootPin.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetFABRIKRootRootPin(target);
                v = this.FloatEditor(v, 0f, 1f, "Tolerance\t\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalRootPin.hasValue)
                    {
                        solver.rootPin = data.originalRootPin;
                        data.originalRootPin.Reset();
                        return solver.rootPin;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.rootPin, v))
                    this.SetFABRIKRootRootPin(target, v);
            }

            {
                if (data != null && data.originalIterations.hasValue)
                    GUI.color = Color.magenta;
                int v = this.GetFABRIKRootIterations(target);
                v = Mathf.RoundToInt(this.FloatEditor(v, 0f, 100f, "Max iterations\t", "0", onReset: (value) =>
                {
                    if (data != null && data.originalIterations.hasValue)
                    {
                        solver.iterations = data.originalIterations;
                        data.originalIterations.Reset();
                        return solver.iterations;
                    }
                    return value;
                }));
                GUI.color = c;
                if (v != solver.iterations)
                    this.SetFABRIKRootIterations(target, v);
            }
        }

        private void CCDIKFields(IKWrapper target, IKData d)
        {
            IKSolverCCD solver = (IKSolverCCD)target.solver;
            CCDIKData data = d as CCDIKData;
            Color c = GUI.color;

            {
                if (data != null && data.originalTolerance.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetCCDIKTolerance(target);
                v = this.FloatEditor(v, 0f, 1f, "Tolerance\t\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalTolerance.hasValue)
                    {
                        solver.tolerance = data.originalTolerance;
                        data.originalTolerance.Reset();
                        return solver.tolerance;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.tolerance, v))
                    this.SetCCDIKTolerance(target, v);
            }

            {
                if (data != null && data.originalMaxIterations.hasValue)
                    GUI.color = Color.magenta;
                int v = this.GetCCDIKMaxIterations(target);
                v = Mathf.RoundToInt(this.FloatEditor(v, 0f, 100f, "Max iterations\t", "0", onReset: (value) =>
                {
                    if (data != null && data.originalMaxIterations.hasValue)
                    {
                        solver.maxIterations = data.originalMaxIterations;
                        data.originalMaxIterations.Reset();
                        return solver.maxIterations;
                    }
                    return value;
                }));
                GUI.color = c;
                if (v != solver.maxIterations)
                    this.SetCCDIKMaxIterations(target, v);
            }
        }

        private void AimIKFields(IKWrapper target, IKData d)
        {
            IKSolverAim solver = (IKSolverAim)target.solver;
            AimIKData data = d as AimIKData;
            Color c = GUI.color;

            {
                if (data != null && data.originalTolerance.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetAimIKTolerance(target);
                v = this.FloatEditor(v, 0f, 1f, "Tolerance\t\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalTolerance.hasValue)
                    {
                        solver.tolerance = data.originalTolerance;
                        data.originalTolerance.Reset();
                        return solver.tolerance;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.tolerance, v))
                    this.SetAimIKTolerance(target, v);
            }

            {
                if (data != null && data.originalMaxIterations.hasValue)
                    GUI.color = Color.magenta;
                int v = this.GetAimIKMaxIterations(target);
                v = Mathf.RoundToInt(this.FloatEditor(v, 0f, 100f, "Max iterations\t", "0", onReset: (value) =>
                {
                    if (data != null && data.originalMaxIterations.hasValue)
                    {
                        solver.maxIterations = data.originalMaxIterations;
                        data.originalMaxIterations.Reset();
                        return solver.maxIterations;
                    }
                    return value;
                }));
                GUI.color = c;
                if (v != solver.maxIterations)
                    this.SetAimIKMaxIterations(target, v);
            }

            {
                GUILayout.BeginVertical();
                if (data != null && data.originalAxis.hasValue)
                    GUI.color = Color.magenta;
                GUILayout.Label("Axis");
                GUILayout.BeginHorizontal();
                Vector3 v = this.Vector3Editor(this.GetAimIKAxis(target));
                if (v != solver.axis)
                    this.SetAimIKAxis(target, v);
                GUI.color = c;
                this.IncEditor();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && data != null && data.originalAxis.hasValue)
                {
                    solver.axis = data.originalAxis;
                    data.originalAxis.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            {
                if (data != null && data.originalClampWeight.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetAimIKClampWeight(target);
                v = this.FloatEditor(v, 0f, 1f, "Clamp Weight\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalClampWeight.hasValue)
                    {
                        solver.clampWeight = data.originalClampWeight;
                        data.originalClampWeight.Reset();
                        return solver.clampWeight;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.clampWeight, v))
                    this.SetAimIKClampWeight(target, v);
            }

            {
                if (data != null && data.originalClampSmoothing.hasValue)
                    GUI.color = Color.magenta;
                int v = this.GetAimIKClampSmoothing(target);
                v = Mathf.RoundToInt(this.FloatEditor(v, 0f, 2f, "Clamp Smoothing\t", "0", onReset: (value) =>
                {
                    if (data.originalClampSmoothing.hasValue)
                    {
                        solver.clampSmoothing = data.originalClampSmoothing;
                        data.originalClampSmoothing.Reset();
                        return solver.clampSmoothing;
                    }
                    return value;
                }));
                GUI.color = c;
                if (solver.clampSmoothing != v)
                    this.SetAimIKClampSmoothing(target, v);
            }

            {
                GUILayout.BeginVertical();
                if (data != null && data.originalPoleAxis.hasValue)
                    GUI.color = Color.magenta;
                GUILayout.Label("Pole Axis");
                GUILayout.BeginHorizontal();
                Vector3 v = this.Vector3Editor(this.GetAimIKPoleAxis(target));
                if (v != solver.poleAxis)
                    this.SetAimIKPoleAxis(target, v);
                GUI.color = c;
                this.IncEditor();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && data != null && data.originalPoleAxis.hasValue)
                {
                    solver.poleAxis = data.originalPoleAxis;
                    data.originalPoleAxis.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            if (solver.poleTarget == null)
            {
                GUILayout.BeginVertical();
                if (data != null && data.originalPolePosition.hasValue)
                    GUI.color = Color.magenta;
                GUILayout.Label("Pole Position");

                GUILayout.BeginHorizontal();
                Vector3 v = this.Vector3Editor(this.GetAimIKPolePosition(target));
                if (v != solver.polePosition)
                    this.SetAimIKPolePosition(target, v);
                GUI.color = c;
                this.IncEditor();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && data != null && data.originalPolePosition.hasValue)
                {
                    solver.polePosition = data.originalPolePosition;
                    data.originalPolePosition.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            {
                if (data != null && data.originalPoleWeight.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetAimIKPoleWeight(target);
                v = this.FloatEditor(v, 0f, 1f, "Pole Weight\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalPoleWeight.hasValue)
                    {
                        solver.poleWeight = data.originalPoleWeight;
                        data.originalPoleWeight.Reset();
                        return solver.poleWeight;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.poleWeight, v))
                    this.SetAimIKPoleWeight(target, v);
            }
        }

        private void FullBodyBipedIKFields(IKWrapper target, IKData d)
        {
            IKSolverFullBodyBiped solver = (IKSolverFullBodyBiped)target.solver;
            FullBodyBipedIKData data = d as FullBodyBipedIKData;
            GUILayout.BeginHorizontal();
            this.DisplayEffectorFields("Right shoulder", solver.rightShoulderEffector, data?.rightShoulder, true, false);
            this.DisplayEffectorFields("Left shoulder", solver.leftShoulderEffector, data?.leftShoulder, true, false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this.DisplayConstraintBendFields(solver.rightArmChain.bendConstraint, data?.rightArm);
            this.DisplayConstraintBendFields(solver.leftArmChain.bendConstraint, data?.leftArm);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this.DisplayEffectorFields("Right hand", solver.rightHandEffector, data?.rightHand);
            this.DisplayEffectorFields("Left hand", solver.leftHandEffector, data?.leftHand);
            GUILayout.EndHorizontal();

            this.DisplayEffectorFields("Body", solver.bodyEffector, data?.body, true, false);

            GUILayout.BeginHorizontal();
            this.DisplayEffectorFields("Right thigh", solver.rightThighEffector, data?.rightThigh, true, false);
            this.DisplayEffectorFields("Left thigh", solver.leftThighEffector, data?.leftThigh, true, false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this.DisplayConstraintBendFields(solver.rightLegChain.bendConstraint, data?.rightLeg);
            this.DisplayConstraintBendFields(solver.leftLegChain.bendConstraint, data?.leftLeg);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this.DisplayEffectorFields("Right foot", solver.rightFootEffector, data?.rightFoot);
            this.DisplayEffectorFields("Left foot", solver.leftFootEffector, data?.leftFoot);
            GUILayout.EndHorizontal();
        }

        private void DisplayEffectorFields(string name, IKEffector effector, FullBodyBipedIKData.EffectorData data, bool showPos = true, bool showRot = true)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();

            GUILayout.Box(name, GUILayout.ExpandWidth(false));
            //GUILayout.FlexibleSpace();
            //effector.effectChildNodes = GUILayout.Toggle(effector.effectChildNodes, "Affect child nodes", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            //effector.maintainRelativePositionWeight = this.FloatEditor(effector.maintainRelativePositionWeight, 0f, 1f, "Maintain relative pos", onReset: value => { return value; });
            Color c = GUI.color;
            if (showPos)
            {
                if (data != null && data.originalPositionWeight.hasValue)
                    GUI.color = Color.magenta;
                float newWeight = this.FloatEditor(this.GetEffectorPositionWeight(effector), 0f, 1f, "Pos weight", onReset: value =>
                {
                    if (data != null && data.originalPositionWeight.hasValue)
                    {
                        effector.positionWeight = data.originalPositionWeight;
                        data.originalPositionWeight.Reset();
                        return effector.positionWeight;
                    }
                    return value;
                });
                GUI.color = c;
                if (Mathf.Approximately(newWeight, effector.positionWeight) == false)
                    this.SetEffectorPositionWeight(newWeight, effector, data);
            }
            if (showRot)
            {
                if (data != null && data.originalRotationWeight.hasValue)
                    GUI.color = Color.magenta;
                float newWeight = this.FloatEditor(this.GetEffectorRotationWeight(effector), 0f, 1f, "Rot weight", onReset: value =>
                {
                    if (data != null && data.originalRotationWeight.hasValue)
                    {
                        effector.rotationWeight = data.originalRotationWeight;
                        data.originalRotationWeight.Reset();
                        return effector.rotationWeight;
                    }
                    return value;
                });
                GUI.color = c;
                if (Mathf.Approximately(newWeight, effector.rotationWeight) == false)
                    this.SetEffectorRotationWeight(newWeight, effector, data);
            }
            GUILayout.EndVertical();
        }

        private void DisplayConstraintBendFields(IKConstraintBend constraint, FullBodyBipedIKData.ConstraintBendData data)
        {
            Color c = GUI.color;
            if (data != null && data.originalWeight.hasValue)
                GUI.color = Color.magenta;
            float newWeight = this.FloatEditor(this.GetConstraintBendWeight(constraint), 0f, 1f, "Bend Weight", onReset: value =>
            {
                if (data != null && data.originalWeight.hasValue)
                {
                    constraint.weight = data.originalWeight;
                    data.originalWeight.Reset();
                    return constraint.weight;
                }
                return value;
            });
            GUI.color = c;
            if (Mathf.Approximately(newWeight, constraint.weight) == false)
                this.SetConstraintBendWeight(newWeight, constraint, data);
        }

        private void LimbIKFields(IKWrapper target, IKData d)
        {
            IKSolverLimb solver = (IKSolverLimb)target.solver;
            LimbIKData data = d as LimbIKData;
            Color c = GUI.color;


            {
                if (data != null && data.originalRotationWeight.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetLimbIKRotationWeight(target);
                v = this.FloatEditor(v, 0f, 1f, "Rot Weight\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalRotationWeight.hasValue)
                    {
                        solver.SetIKRotationWeight(data.originalRotationWeight);
                        data.originalRotationWeight.Reset();
                        return solver.GetIKRotationWeight();
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.GetIKRotationWeight(), v))
                    this.SetLimbIKRotationWeight(target, v);
            }

            {
                if (data != null && data.originalBendModifierWeight.hasValue)
                    GUI.color = Color.magenta;
                float v = this.GetLimbIKBendModifierWeight(target);
                v = this.FloatEditor(v, 0f, 1f, "Bend Weight\t", "0.0000", onReset: (value) =>
                {
                    if (data != null && data.originalBendModifierWeight.hasValue)
                    {
                        solver.bendModifierWeight = data.originalBendModifierWeight;
                        data.originalBendModifierWeight.Reset();
                        return solver.bendModifierWeight;
                    }
                    return value;
                });
                GUI.color = c;
                if (!Mathf.Approximately(solver.bendModifierWeight, v))
                    this.SetLimbIKBendModifierWeight(target, v);
            }

        }

        private IKData SetIKDirty(IKWrapper ik)
        {
            IKData data;
            if (this._dirtyIks.TryGetValue(ik, out data) == false)
            {
                switch (ik.type)
                {
                    case IKType.FABRIK:
                        data = new FABRIKData();
                        break;
                    case IKType.FABRIKRoot:
                        data = new FABRIKRootData();
                        break;
                    case IKType.AimIK:
                        data = new AimIKData();
                        break;
                    case IKType.CCDIK:
                        data = new CCDIKData();
                        break;
                    case IKType.FullBodyBipedIK:
                        data = new FullBodyBipedIKData();
                        break;
                    case IKType.LimbIK:
                        data = new LimbIKData();
                        break;
                    default:
                        return null;
                }
                this._dirtyIks.Add(ik, data);
            }
            return data;
        }

        private bool GetIKEnabled(IKWrapper ik)
        {
            return ik.ik.enabled;
        }

        private void SetIKEnabled(IKWrapper ik, bool b)
        {
            IKData data = this.SetIKDirty(ik);
            if (data.originalEnabled.hasValue == false)
                data.originalEnabled = ik.ik.enabled;
            ik.ik.enabled = b;
            if (ik.ik.enabled == false)
                ik.solver.FixTransforms();
        }

        private bool GetIKFixTransforms(IKWrapper ik)
        {
            return ik.ik.fixTransforms;
        }

        private void SetIKFixTransforms(IKWrapper ik, bool b)
        {
            IKData data = this.SetIKDirty(ik);
            if (data.originalFixTransforms.hasValue == false)
                data.originalFixTransforms = ik.ik.fixTransforms;
            ik.ik.fixTransforms = b;
        }

        private float GetIKPositionWeight(IKWrapper ik)
        {
            return ik.solver.GetIKPositionWeight();
        }

        private void SetIKPositionWeight(IKWrapper ik, float v)
        {
            IKData data = this.SetIKDirty(ik);
            if (data.originalWeight.hasValue == false)
                data.originalWeight = ik.solver.GetIKPositionWeight();
            ik.solver.SetIKPositionWeight(v);
        }

        private float GetFABRIKTolerance(IKWrapper ik)
        {
            return ((IKSolverFABRIK)ik.solver).tolerance;
        }

        private void SetFABRIKTolerance(IKWrapper ik, float v)
        {
            IKSolverFABRIK solver = (IKSolverFABRIK)ik.solver;
            FABRIKData data = (FABRIKData)this.SetIKDirty(ik);
            if (data.originalTolerance.hasValue == false)
                data.originalTolerance = solver.tolerance;
            solver.tolerance = v;
        }

        private int GetFABRIKMaxIterations(IKWrapper ik)
        {
            return ((IKSolverFABRIK)ik.solver).maxIterations;
        }

        private void SetFABRIKMaxIterations(IKWrapper ik, int v)
        {
            IKSolverFABRIK solver = (IKSolverFABRIK)ik.solver;
            FABRIKData data = (FABRIKData)this.SetIKDirty(ik);
            if (data.originalMaxIterations.hasValue == false)
                data.originalMaxIterations = solver.maxIterations;
            solver.maxIterations = v;
        }

        private float GetFABRIKRootRootPin(IKWrapper ik)
        {
            return ((IKSolverFABRIKRoot)ik.solver).rootPin;
        }

        private void SetFABRIKRootRootPin(IKWrapper ik, float v)
        {
            IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)ik.solver;
            FABRIKRootData data = (FABRIKRootData)this.SetIKDirty(ik);
            if (data.originalRootPin.hasValue == false)
                data.originalRootPin = solver.rootPin;
            solver.rootPin = v;
        }

        private int GetFABRIKRootIterations(IKWrapper ik)
        {
            return ((IKSolverFABRIKRoot)ik.solver).iterations;
        }

        private void SetFABRIKRootIterations(IKWrapper ik, int v)
        {
            IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)ik.solver;
            FABRIKRootData data = (FABRIKRootData)this.SetIKDirty(ik);
            if (data.originalIterations.hasValue == false)
                data.originalIterations = solver.iterations;
            solver.iterations = v;
        }

        private float GetCCDIKTolerance(IKWrapper ik)
        {
            return ((IKSolverCCD)ik.solver).tolerance;
        }

        private void SetCCDIKTolerance(IKWrapper ik, float v)
        {
            IKSolverCCD solver = (IKSolverCCD)ik.solver;
            CCDIKData data = (CCDIKData)this.SetIKDirty(ik);
            if (data.originalTolerance.hasValue == false)
                data.originalTolerance = solver.tolerance;
            solver.tolerance = v;
        }

        private int GetCCDIKMaxIterations(IKWrapper ik)
        {
            return ((IKSolverCCD)ik.solver).maxIterations;
        }

        private void SetCCDIKMaxIterations(IKWrapper ik, int v)
        {
            IKSolverCCD solver = (IKSolverCCD)ik.solver;
            CCDIKData data = (CCDIKData)this.SetIKDirty(ik);
            if (data.originalMaxIterations.hasValue == false)
                data.originalMaxIterations = solver.maxIterations;
            solver.maxIterations = v;
        }

        private float GetAimIKTolerance(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).tolerance;
        }

        private void SetAimIKTolerance(IKWrapper ik, float v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalTolerance.hasValue == false)
                data.originalTolerance = solver.tolerance;
            solver.tolerance = v;
        }

        private int GetAimIKMaxIterations(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).maxIterations;
        }

        private void SetAimIKMaxIterations(IKWrapper ik, int v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalMaxIterations.hasValue == false)
                data.originalMaxIterations = solver.maxIterations;
            solver.maxIterations = v;
        }

        private Vector3 GetAimIKAxis(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).axis;
        }

        private void SetAimIKAxis(IKWrapper ik, Vector3 v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalAxis.hasValue == false)
                data.originalAxis = solver.axis;
            solver.axis = v;
        }

        private float GetAimIKClampWeight(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).clampWeight;
        }

        private void SetAimIKClampWeight(IKWrapper ik, float v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalClampWeight.hasValue == false)
                data.originalClampWeight = solver.clampWeight;
            solver.clampWeight = v;
        }

        private int GetAimIKClampSmoothing(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).clampSmoothing;
        }

        private void SetAimIKClampSmoothing(IKWrapper ik, int v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalClampSmoothing.hasValue == false)
                data.originalClampSmoothing = solver.clampSmoothing;
            solver.clampSmoothing = v;
        }

        private Vector3 GetAimIKPoleAxis(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).poleAxis;
        }

        private void SetAimIKPoleAxis(IKWrapper ik, Vector3 v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalPoleAxis.hasValue == false)
                data.originalPoleAxis = solver.poleAxis;
            solver.poleAxis = v;
        }

        private Vector3 GetAimIKPolePosition(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).polePosition;
        }

        private void SetAimIKPolePosition(IKWrapper ik, Vector3 v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalPolePosition.hasValue == false)
                data.originalPolePosition = solver.polePosition;
            solver.polePosition = v;
        }

        private float GetAimIKPoleWeight(IKWrapper ik)
        {
            return ((IKSolverAim)ik.solver).poleWeight;
        }

        private void SetAimIKPoleWeight(IKWrapper ik, float v)
        {
            IKSolverAim solver = (IKSolverAim)ik.solver;
            AimIKData data = (AimIKData)this.SetIKDirty(ik);
            if (data.originalPoleWeight.hasValue == false)
                data.originalPoleWeight = solver.poleWeight;
            solver.poleWeight = v;
        }

        private float GetEffectorPositionWeight(IKEffector effector)
        {
            return effector.positionWeight;
        }

        private void SetEffectorPositionWeight(float newWeight, IKEffector effector, FullBodyBipedIKData.EffectorData data)
        {
            if (data.originalPositionWeight.hasValue == false)
                data.originalPositionWeight = effector.positionWeight;
            data.currentPositionWeight = newWeight;
        }

        private float GetEffectorRotationWeight(IKEffector effector)
        {
            return effector.rotationWeight;
        }

        private void SetEffectorRotationWeight(float newWeight, IKEffector effector, FullBodyBipedIKData.EffectorData data)
        {
            if (data.originalRotationWeight.hasValue == false)
                data.originalRotationWeight = effector.rotationWeight;
            data.currentRotationWeight = newWeight;
        }

        private float GetConstraintBendWeight(IKConstraintBend constraint)
        {
            return constraint.weight;
        }

        private void SetConstraintBendWeight(float newWeight, IKConstraintBend constraint, FullBodyBipedIKData.ConstraintBendData data)
        {
            if (data.originalWeight.hasValue == false)
                data.originalWeight = constraint.weight;
            data.currentWeight = newWeight;
        }

        private float GetLimbIKRotationWeight(IKWrapper ik)
        {
            return ((IKSolverLimb)ik.solver).GetIKRotationWeight();
        }

        private void SetLimbIKRotationWeight(IKWrapper ik, float v)
        {
            IKSolverLimb solver = (IKSolverLimb)ik.solver;
            LimbIKData data = (LimbIKData)this.SetIKDirty(ik);
            if (data.originalRotationWeight.hasValue == false)
                data.originalRotationWeight = solver.GetIKRotationWeight();
            solver.SetIKRotationWeight(v);
        }

        private float GetLimbIKBendModifierWeight(IKWrapper ik)
        {
            return ((IKSolverLimb)ik.solver).bendModifierWeight;
        }

        private void SetLimbIKBendModifierWeight(IKWrapper ik, float v)
        {
            IKSolverLimb solver = (IKSolverLimb)ik.solver;
            LimbIKData data = (LimbIKData)this.SetIKDirty(ik);
            if (data.originalBendModifierWeight.hasValue == false)
                data.originalBendModifierWeight = solver.bendModifierWeight;
            solver.bendModifierWeight = v;
        }

        private void RefreshIKList()
        {
            IK[] iks = this._parent.GetComponentsInChildren<IK>(true);
            List<IK> toDelete = null;
            foreach (KeyValuePair<IK, IKWrapper> pair in this._iks)
            {
                if (iks.Contains(pair.Key) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<IK>();
                    toDelete.Add(pair.Key);
                }
            }
            if (toDelete != null)
            {
                foreach (IK ik in toDelete)
                    this._iks.Remove(ik);
            }
            List<IKWrapper> toAdd = null;
            foreach (IK ik in iks)
            {
                if (this._iks.ContainsKey(ik) == false && this._parent._childObjects.All(child => ik.transform.IsChildOf(child.transform) == false))
                {
                    if (toAdd == null)
                        toAdd = new List<IKWrapper>();
                    IKWrapper wrapper = new IKWrapper(ik);
                    if (wrapper.type != IKType.Unknown)
                        toAdd.Add(wrapper);
                }
            }
            if (toAdd != null)
            {
                foreach (IKWrapper ik in toAdd)
                    this._iks.Add(ik.ik, ik);
            }
            if (this._iks.Count != 0 && this._ikTarget == null)
                this._ikTarget = this._iks.FirstOrDefault().Value;
        }

        private void CopyToFK()
        {
            this._updateAction = () =>
            {
                List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
                {
                    foreach (KeyValuePair<IK, IKWrapper> pair in this._iks)
                    {
                        if (pair.Value.type == IKType.FullBodyBipedIK)
                        {
                            FullBodyBipedIK fbbik = (FullBodyBipedIK)pair.Value.ik;

                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.pelvis));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.leftThigh));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.leftCalf));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.leftFoot));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.rightThigh));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.rightCalf));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.rightFoot));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.leftUpperArm));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.leftForearm));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.leftHand));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.rightUpperArm));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.rightForearm));
                            infos.Add(this.GetEqualsCommandForTransform(fbbik.references.rightHand));
                            foreach (Transform transform in fbbik.references.spine)
                            {
                                infos.Add(this.GetEqualsCommandForTransform(transform));
                            }
                        }
                        else
                        {
                            foreach (IKSolver.Point point in pair.Value.solver.GetPoints())
                                if (point != null)
                                    infos.Add(this.GetEqualsCommandForTransform(point.transform));
                        }
                    }
                }
                UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos.ToArray()));
            };
        }

        private GuideCommand.EqualsInfo GetEqualsCommandForTransform(Transform t)
        {
            OCIChar.BoneInfo boneInfo;
            if (this._target.fkObjects.TryGetValue(t.gameObject, out boneInfo))
            {
                Vector3 oldValue = boneInfo.guideObject.changeAmount.rot;
                boneInfo.guideObject.changeAmount.rot = t.localEulerAngles;
                return new GuideCommand.EqualsInfo()
                {
                    dicKey = boneInfo.guideObject.dicKey,
                    oldValue = oldValue,
                    newValue = boneInfo.guideObject.changeAmount.rot
                };
            }
            return null;
        }

        private void ApplyFullBodyBipedEffectorData(IKEffector effector, FullBodyBipedIKData.EffectorData data)
        {
            if (data.originalPositionWeight.hasValue)
                effector.positionWeight = data.currentPositionWeight;
            if (data.originalRotationWeight.hasValue)
                effector.rotationWeight = data.currentRotationWeight;
        }

        private void ApplyFullBodyBipedConstraintBendData(IKConstraintBend constraint, FullBodyBipedIKData.ConstraintBendData data)
        {
            if (data.originalWeight.hasValue)
                constraint.weight = data.currentWeight;
        }
        #endregion

        #region Saves
        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            {
                xmlWriter.WriteStartElement("iks");
                foreach (KeyValuePair<IKWrapper, IKData> pair in this._dirtyIks)
                {
                    xmlWriter.WriteStartElement("ik");
                    xmlWriter.WriteAttributeString("root", pair.Key.solver.GetRoot().GetPathFrom(this._parent.transform));
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString((int)pair.Key.type));

                    if (pair.Value.originalEnabled.hasValue)
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(pair.Key.ik.enabled));
                    if (pair.Value.originalFixTransforms.hasValue)
                        xmlWriter.WriteAttributeString("fixTransforms", XmlConvert.ToString(pair.Key.ik.fixTransforms));
                    if (pair.Value.originalWeight.hasValue)
                        xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(pair.Key.solver.GetIKPositionWeight()));

                    switch (pair.Key.type)
                    {
                        case IKType.FABRIK:
                            {
                                IKSolverFABRIK solver = (IKSolverFABRIK)pair.Key.solver;
                                FABRIKData data = (FABRIKData)pair.Value;
                                if (data.originalTolerance.hasValue)
                                    xmlWriter.WriteAttributeString("tolerance", XmlConvert.ToString(solver.tolerance));
                                if (data.originalMaxIterations.hasValue)
                                    xmlWriter.WriteAttributeString("maxIterations", XmlConvert.ToString(solver.maxIterations));
                                break;
                            }
                        case IKType.FABRIKRoot:
                            {
                                IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)pair.Key.solver;
                                FABRIKRootData data = (FABRIKRootData)pair.Value;
                                if (data.originalRootPin.hasValue)
                                    xmlWriter.WriteAttributeString("tolerance", XmlConvert.ToString(solver.rootPin));
                                if (data.originalIterations.hasValue)
                                    xmlWriter.WriteAttributeString("maxIterations", XmlConvert.ToString(solver.iterations));
                                break;
                            }
                        case IKType.AimIK:
                            {
                                IKSolverAim solver = (IKSolverAim)pair.Key.solver;
                                AimIKData data = (AimIKData)pair.Value;
                                if (data.originalTolerance.hasValue)
                                    xmlWriter.WriteAttributeString("tolerance", XmlConvert.ToString(solver.tolerance));
                                if (data.originalMaxIterations.hasValue)
                                    xmlWriter.WriteAttributeString("maxIterations", XmlConvert.ToString(solver.maxIterations));
                                if (data.originalAxis.hasValue)
                                {
                                    xmlWriter.WriteAttributeString("axisX", XmlConvert.ToString(solver.axis.x));
                                    xmlWriter.WriteAttributeString("axisY", XmlConvert.ToString(solver.axis.y));
                                    xmlWriter.WriteAttributeString("axisZ", XmlConvert.ToString(solver.axis.z));
                                }
                                if (data.originalClampWeight.hasValue)
                                    xmlWriter.WriteAttributeString("clampWeight", XmlConvert.ToString(solver.clampWeight));
                                if (data.originalClampSmoothing.hasValue)
                                    xmlWriter.WriteAttributeString("clampSmoothing", XmlConvert.ToString(solver.clampSmoothing));
                                if (data.originalPoleAxis.hasValue)
                                {
                                    xmlWriter.WriteAttributeString("poleAxisX", XmlConvert.ToString(solver.poleAxis.x));
                                    xmlWriter.WriteAttributeString("poleAxisY", XmlConvert.ToString(solver.poleAxis.y));
                                    xmlWriter.WriteAttributeString("poleAxisZ", XmlConvert.ToString(solver.poleAxis.z));
                                }
                                if (data.originalPolePosition.hasValue)
                                {
                                    xmlWriter.WriteAttributeString("polePositionX", XmlConvert.ToString(solver.polePosition.x));
                                    xmlWriter.WriteAttributeString("polePositionY", XmlConvert.ToString(solver.polePosition.y));
                                    xmlWriter.WriteAttributeString("polePositionZ", XmlConvert.ToString(solver.polePosition.z));
                                }
                                if (data.originalPoleWeight.hasValue)
                                    xmlWriter.WriteAttributeString("poleWeight", XmlConvert.ToString(solver.poleWeight));

                                break;
                            }
                        case IKType.CCDIK:
                            {
                                IKSolverCCD solver = (IKSolverCCD)pair.Key.solver;
                                CCDIKData data = (CCDIKData)pair.Value;
                                if (data.originalTolerance.hasValue)
                                    xmlWriter.WriteAttributeString("tolerance", XmlConvert.ToString(solver.tolerance));
                                if (data.originalMaxIterations.hasValue)
                                    xmlWriter.WriteAttributeString("maxIterations", XmlConvert.ToString(solver.maxIterations));
                                break;
                            }
                        case IKType.FullBodyBipedIK:
                            {
                                FullBodyBipedIKData data = (FullBodyBipedIKData)pair.Value;
                                xmlWriter.WriteStartElement("bodyEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.body);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("leftShoulderEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.leftShoulder);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("leftHandEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.leftHand);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("rightShoulderEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.rightShoulder);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("rightHandEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.rightHand);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("leftThighEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.leftThigh);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("leftFootEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.leftFoot);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("rightThighEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.rightThigh);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("rightFootEffector");
                                this.SaveFullBodyBipedEffectorData(xmlWriter, data.rightFoot);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("leftArmConstraint");
                                this.SaveFullBodyBipedConstraintBend(xmlWriter, data.leftArm);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("rightArmConstraint");
                                this.SaveFullBodyBipedConstraintBend(xmlWriter, data.rightArm);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("leftLegConstraint");
                                this.SaveFullBodyBipedConstraintBend(xmlWriter, data.leftLeg);
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("rightLegConstraint");
                                this.SaveFullBodyBipedConstraintBend(xmlWriter, data.rightLeg);
                                xmlWriter.WriteEndElement();

                                break;
                            }
                        case IKType.LimbIK:
                            {
                                IKSolverLimb solver = (IKSolverLimb)pair.Key.solver;
                                LimbIKData data = (LimbIKData)pair.Value;
                                if (data.originalRotationWeight.hasValue)
                                    xmlWriter.WriteAttributeString("rotationWeight", XmlConvert.ToString(solver.GetIKRotationWeight()));
                                if (data.originalBendModifierWeight.hasValue)
                                    xmlWriter.WriteAttributeString("bendModifierWeight", XmlConvert.ToString(solver.bendModifierWeight));
                                break;
                            }
                    }
                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }

            return written;
        }

        private void SaveFullBodyBipedEffectorData(XmlTextWriter writer, FullBodyBipedIKData.EffectorData data)
        {
            if (data.originalPositionWeight.hasValue)
                writer.WriteAttributeString("positionWeight", XmlConvert.ToString(data.currentPositionWeight));
            if (data.originalRotationWeight.hasValue)
                writer.WriteAttributeString("rotationWeight", XmlConvert.ToString(data.currentRotationWeight));
        }

        private void SaveFullBodyBipedConstraintBend(XmlTextWriter writer, FullBodyBipedIKData.ConstraintBendData data)
        {
            if (data.originalWeight.hasValue)
                writer.WriteAttributeString("weight", XmlConvert.ToString(data.currentWeight));
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            bool changed = false;

            this.ResetAll();
            this.RefreshIKList();

            XmlNode ikNodes = xmlNode.FindChildNode("iks");

            if (ikNodes != null)
            {
                foreach (XmlNode node in ikNodes.ChildNodes)
                {
                    try
                    {
                        string root = node.Attributes["root"].Value;
                        Transform t = this._parent.transform.Find(root);
                        IKWrapper ik = this._iks.FirstOrDefault(i => i.Value.solver.GetRoot() == t && this._dirtyIks.ContainsKey(i.Value) == false).Value;
                        if (ik == null)
                            continue;

                        IKType type = (IKType)XmlConvert.ToInt32(node.Attributes["type"].Value);
                        IKData d;
                        switch (type)
                        {
                            case IKType.FABRIK:
                                d = new FABRIKData();
                                break;
                            case IKType.FABRIKRoot:
                                d = new FABRIKRootData();
                                break;
                            case IKType.AimIK:
                                d = new AimIKData();
                                break;
                            case IKType.CCDIK:
                                d = new CCDIKData();
                                break;
                            case IKType.FullBodyBipedIK:
                                d = new FullBodyBipedIKData();
                                break;
                            case IKType.LimbIK:
                                d = new LimbIKData();
                                break;
                            default:
                                continue;
                        }

                        if (node.Attributes["enabled"] != null)
                        {
                            bool enabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                            d.originalEnabled = ik.ik.enabled;
                            ik.ik.enabled = enabled;
                            changed = true;
                        }

                        if (node.Attributes["fixTransforms"] != null)
                        {
                            bool enabled = XmlConvert.ToBoolean(node.Attributes["fixTransforms"].Value);
                            d.originalFixTransforms = ik.ik.fixTransforms;
                            ik.ik.fixTransforms = enabled;
                            changed = true;
                        }

                        if (node.Attributes["weight"] != null)
                        {
                            float weight = XmlConvert.ToSingle(node.Attributes["weight"].Value);
                            d.originalWeight = ik.solver.GetIKPositionWeight();
                            ik.solver.SetIKPositionWeight(weight);
                            changed = true;
                        }

                        switch (ik.type)
                        {
                            case IKType.FABRIK:
                                {
                                    IKSolverFABRIK solver = (IKSolverFABRIK)ik.solver;
                                    FABRIKData data = (FABRIKData)d;
                                    if (node.Attributes["tolerance"] != null)
                                    {
                                        float tolerance = XmlConvert.ToSingle(node.Attributes["tolerance"].Value);
                                        data.originalTolerance = solver.tolerance;
                                        solver.tolerance = tolerance;
                                        changed = true;
                                    }
                                    if (node.Attributes["maxIterations"] != null)
                                    {
                                        int maxIterations = XmlConvert.ToInt32(node.Attributes["maxIterations"].Value);
                                        data.originalMaxIterations = solver.maxIterations;
                                        solver.maxIterations = maxIterations;
                                        changed = true;
                                    }
                                    break;
                                }
                            case IKType.FABRIKRoot:
                                {
                                    IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)ik.solver;
                                    FABRIKRootData data = (FABRIKRootData)d;
                                    if (node.Attributes["tolerance"] != null)
                                    {
                                        float tolerance = XmlConvert.ToSingle(node.Attributes["tolerance"].Value);
                                        data.originalRootPin = solver.rootPin;
                                        solver.rootPin = tolerance;
                                        changed = true;
                                    }
                                    if (node.Attributes["maxIterations"] != null)
                                    {
                                        int maxIterations = XmlConvert.ToInt32(node.Attributes["maxIterations"].Value);
                                        data.originalIterations = solver.iterations;
                                        solver.iterations = maxIterations;
                                        changed = true;
                                    }
                                    break;
                                }
                            case IKType.AimIK:
                                {
                                    IKSolverAim solver = (IKSolverAim)ik.solver;
                                    AimIKData data = (AimIKData)d;
                                    if (node.Attributes["tolerance"] != null)
                                    {
                                        float tolerance = XmlConvert.ToSingle(node.Attributes["tolerance"].Value);
                                        data.originalTolerance = solver.tolerance;
                                        solver.tolerance = tolerance;
                                        changed = true;
                                    }
                                    if (node.Attributes["maxIterations"] != null)
                                    {
                                        int maxIterations = XmlConvert.ToInt32(node.Attributes["maxIterations"].Value);
                                        data.originalMaxIterations = solver.maxIterations;
                                        solver.maxIterations = maxIterations;
                                        changed = true;
                                    }
                                    if (node.Attributes["axisX"] != null)
                                    {
                                        Vector3 axis = new Vector3(
                                                XmlConvert.ToSingle(node.Attributes["axisX"].Value),
                                                XmlConvert.ToSingle(node.Attributes["axisY"].Value),
                                                XmlConvert.ToSingle(node.Attributes["axisZ"].Value)
                                        );
                                        data.originalAxis = solver.axis;
                                        solver.axis = axis;
                                        changed = true;
                                    }
                                    if (node.Attributes["clampWeight"] != null)
                                    {
                                        float clampWeight = XmlConvert.ToSingle(node.Attributes["clampWeight"].Value);
                                        data.originalClampWeight = solver.clampWeight;
                                        solver.clampWeight = clampWeight;
                                        changed = true;
                                    }
                                    if (node.Attributes["clampSmoothing"] != null)
                                    {
                                        int clampSmoothing = XmlConvert.ToInt32(node.Attributes["clampSmoothing"].Value);
                                        data.originalClampSmoothing = solver.clampSmoothing;
                                        solver.clampSmoothing = clampSmoothing;
                                        changed = true;
                                    }
                                    if (node.Attributes["poleAxisX"] != null)
                                    {
                                        Vector3 poleAxis = new Vector3(
                                                XmlConvert.ToSingle(node.Attributes["poleAxisX"].Value),
                                                XmlConvert.ToSingle(node.Attributes["poleAxisY"].Value),
                                                XmlConvert.ToSingle(node.Attributes["poleAxisZ"].Value)
                                        );
                                        data.originalPoleAxis = solver.poleAxis;
                                        solver.poleAxis = poleAxis;
                                        changed = true;
                                    }
                                    if (node.Attributes["polePositionX"] != null)
                                    {
                                        Vector3 polePosition = new Vector3(
                                                XmlConvert.ToSingle(node.Attributes["polePositionX"].Value),
                                                XmlConvert.ToSingle(node.Attributes["polePositionY"].Value),
                                                XmlConvert.ToSingle(node.Attributes["polePositionZ"].Value)
                                        );
                                        data.originalPolePosition = solver.polePosition;
                                        solver.polePosition = polePosition;
                                        changed = true;
                                    }
                                    if (node.Attributes["poleWeight"] != null)
                                    {
                                        float poleWeight = XmlConvert.ToSingle(node.Attributes["poleWeight"].Value);
                                        data.originalPoleWeight = solver.poleWeight;
                                        solver.poleWeight = poleWeight;
                                        changed = true;
                                    }
                                    break;
                                }
                            case IKType.CCDIK:
                                {
                                    IKSolverCCD solver = (IKSolverCCD)ik.solver;
                                    CCDIKData data = (CCDIKData)d;
                                    if (node.Attributes["tolerance"] != null)
                                    {
                                        float tolerance = XmlConvert.ToSingle(node.Attributes["tolerance"].Value);
                                        data.originalTolerance = solver.tolerance;
                                        solver.tolerance = tolerance;
                                    }
                                    if (node.Attributes["maxIterations"] != null)
                                    {
                                        int maxIterations = XmlConvert.ToInt32(node.Attributes["maxIterations"].Value);
                                        data.originalMaxIterations = solver.maxIterations;
                                        solver.maxIterations = maxIterations;
                                        changed = true;
                                    }
                                    break;
                                }
                            case IKType.FullBodyBipedIK:
                                {
                                    IKSolverFullBodyBiped solver = (IKSolverFullBodyBiped)ik.solver;
                                    FullBodyBipedIKData data = (FullBodyBipedIKData)d;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("bodyEffector"), solver.bodyEffector, data.body) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("leftShoulderEffector"), solver.leftShoulderEffector, data.leftShoulder) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("leftHandEffector"), solver.leftHandEffector, data.leftHand) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("rightShoulderEffector"), solver.rightShoulderEffector, data.rightShoulder) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("rightHandEffector"), solver.rightHandEffector, data.rightHand) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("leftThighEffector"), solver.leftThighEffector, data.leftThigh) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("leftFootEffector"), solver.leftFootEffector, data.leftFoot) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("rightThighEffector"), solver.rightThighEffector, data.rightThigh) || changed;
                                    changed = this.LoadFullBodyBipedEffectorData(node.FindChildNode("rightFootEffector"), solver.rightFootEffector, data.rightFoot) || changed;

                                    changed = this.LoadFullBodyBipedConstraintBendData(node.FindChildNode("leftArmConstraint"), solver.leftArmChain.bendConstraint, data.leftArm) || changed;
                                    changed = this.LoadFullBodyBipedConstraintBendData(node.FindChildNode("rightArmConstraint"), solver.rightArmChain.bendConstraint, data.rightArm) || changed;
                                    changed = this.LoadFullBodyBipedConstraintBendData(node.FindChildNode("leftLegConstraint"), solver.leftLegChain.bendConstraint, data.leftLeg) || changed;
                                    changed = this.LoadFullBodyBipedConstraintBendData(node.FindChildNode("rightLegConstraint"), solver.rightLegChain.bendConstraint, data.rightLeg) || changed;
                                    break;
                                }
                            case IKType.LimbIK:
                                {
                                    IKSolverLimb solver = (IKSolverLimb)ik.solver;
                                    LimbIKData data = (LimbIKData)d;
                                    if (node.Attributes["rotationWeight"] != null)
                                    {
                                        float rotationWeight = XmlConvert.ToSingle(node.Attributes["rotationWeight"].Value);
                                        data.originalRotationWeight = solver.GetIKRotationWeight();
                                        solver.SetIKRotationWeight(rotationWeight);
                                        changed = true;
                                    }
                                    if (node.Attributes["bendModifierWeight"] != null)
                                    {
                                        float bendModifierWeight = XmlConvert.ToInt32(node.Attributes["bendModifierWeight"].Value);
                                        data.originalBendModifierWeight = solver.bendModifierWeight;
                                        solver.bendModifierWeight = bendModifierWeight;
                                        changed = true;
                                    }
                                    break;
                                }
                        }
                        this._dirtyIks.Add(ik, d);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load ik for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }

            return changed;
        }

        private bool LoadFullBodyBipedEffectorData(XmlNode node, IKEffector effector, FullBodyBipedIKData.EffectorData data)
        {
            if (node == null)
                return false;
            bool changed = false;
            if (node.Attributes["positionWeight"] != null)
            {
                float positionWeight = XmlConvert.ToSingle(node.Attributes["positionWeight"].Value);
                data.originalPositionWeight = effector.positionWeight;
                data.currentPositionWeight = positionWeight;
                changed = true;
            }
            if (node.Attributes["rotationWeight"] != null)
            {
                float rotationWeight = XmlConvert.ToSingle(node.Attributes["rotationWeight"].Value);
                data.originalRotationWeight = effector.rotationWeight;
                data.currentRotationWeight = rotationWeight;
                changed = true;
            }
            return changed;
        }

        private bool LoadFullBodyBipedConstraintBendData(XmlNode node, IKConstraintBend constraint, FullBodyBipedIKData.ConstraintBendData data)
        {
            if (node == null)
                return false;
            bool changed = false;
            if (node.Attributes["weight"] != null)
            {
                float weight = XmlConvert.ToSingle(node.Attributes["weight"].Value);
                data.originalWeight = constraint.weight;
                data.currentWeight = weight;
                changed = true;
            }
            return changed;
        }

        private void SetIKNotDirty(IKWrapper ik)
        {
            if (this._dirtyIks.TryGetValue(ik, out IKData d) == false)
                return;
            if (d.originalEnabled.hasValue)
            {
                ik.ik.enabled = d.originalEnabled;
                d.originalEnabled.Reset();
            }

            if (d.originalFixTransforms.hasValue)
            {
                ik.ik.fixTransforms = d.originalFixTransforms;
                d.originalFixTransforms.Reset();
            }

            if (d.originalWeight.hasValue)
            {
                ik.solver.SetIKPositionWeight(d.originalWeight);
                d.originalWeight.Reset();
            }

            switch (ik.type)
            {
                case IKType.FABRIK:
                    {
                        IKSolverFABRIK solver = (IKSolverFABRIK)ik.solver;
                        FABRIKData data = (FABRIKData)d;
                        if (data.originalTolerance.hasValue)
                        {
                            solver.tolerance = data.originalTolerance;
                            data.originalTolerance.Reset();
                        }
                        if (data.originalMaxIterations.hasValue)
                        {
                            solver.maxIterations = data.originalMaxIterations;
                            data.originalMaxIterations.Reset();
                        }
                        break;
                    }
                case IKType.FABRIKRoot:
                    {
                        IKSolverFABRIKRoot solver = (IKSolverFABRIKRoot)ik.solver;
                        FABRIKRootData data = (FABRIKRootData)d;
                        if (data.originalRootPin.hasValue)
                        {
                            solver.rootPin = data.originalRootPin;
                            data.originalRootPin.Reset();
                        }
                        if (data.originalIterations.hasValue)
                        {
                            solver.iterations = data.originalIterations;
                            data.originalIterations.Reset();
                        }
                        break;
                    }
                case IKType.AimIK:
                    {
                        IKSolverAim solver = (IKSolverAim)ik.solver;
                        AimIKData data = (AimIKData)d;
                        if (data.originalTolerance.hasValue)
                        {
                            solver.tolerance = data.originalTolerance;
                            data.originalTolerance.Reset();
                        }
                        if (data.originalMaxIterations.hasValue)
                        {
                            solver.maxIterations = data.originalMaxIterations;
                            data.originalMaxIterations.Reset();
                        }
                        if (data.originalAxis.hasValue)
                        {
                            solver.axis = data.originalAxis;
                            data.originalAxis.Reset();
                        }
                        if (data.originalClampWeight.hasValue)
                        {
                            solver.clampWeight = data.originalClampWeight;
                            data.originalClampWeight.Reset();
                        }
                        if (data.originalClampSmoothing.hasValue)
                        {
                            solver.clampSmoothing = data.originalClampSmoothing;
                            data.originalClampSmoothing.Reset();
                        }
                        if (data.originalPoleAxis.hasValue)
                        {
                            solver.poleAxis = data.originalPoleAxis;
                            data.originalPoleAxis.Reset();
                        }
                        if (data.originalPolePosition.hasValue)
                        {
                            solver.polePosition = data.originalPolePosition;
                            data.originalPolePosition.Reset();
                        }
                        if (data.originalPoleWeight.hasValue)
                        {
                            solver.poleWeight = data.originalPoleWeight;
                            data.originalPoleWeight.Reset();
                        }
                        break;
                    }
                case IKType.CCDIK:
                    {
                        IKSolverCCD solver = (IKSolverCCD)ik.solver;
                        CCDIKData data = (CCDIKData)d;
                        if (data.originalTolerance.hasValue)
                        {
                            solver.tolerance = data.originalTolerance;
                            data.originalTolerance.Reset();
                        }
                        if (data.originalMaxIterations.hasValue)
                        {
                            solver.maxIterations = data.originalMaxIterations;
                            data.originalMaxIterations.Reset();
                        }
                        break;
                    }
                case IKType.FullBodyBipedIK:
                    {
                        IKSolverFullBodyBiped solver = (IKSolverFullBodyBiped)ik.solver;
                        FullBodyBipedIKData data = (FullBodyBipedIKData)d;
                        this.SetFullBodyBipedEffectorNotDirty(solver.bodyEffector, data.body);
                        this.SetFullBodyBipedEffectorNotDirty(solver.leftShoulderEffector, data.leftShoulder);
                        this.SetFullBodyBipedEffectorNotDirty(solver.leftHandEffector, data.leftHand);
                        this.SetFullBodyBipedEffectorNotDirty(solver.rightShoulderEffector, data.rightShoulder);
                        this.SetFullBodyBipedEffectorNotDirty(solver.rightHandEffector, data.rightHand);
                        this.SetFullBodyBipedEffectorNotDirty(solver.leftThighEffector, data.leftThigh);
                        this.SetFullBodyBipedEffectorNotDirty(solver.leftFootEffector, data.leftFoot);
                        this.SetFullBodyBipedEffectorNotDirty(solver.rightThighEffector, data.rightThigh);
                        this.SetFullBodyBipedEffectorNotDirty(solver.rightFootEffector, data.rightFoot);

                        this.SetFullBodyBipedConstraintBendNotDirty(solver.leftArmChain.bendConstraint, data.leftArm);
                        this.SetFullBodyBipedConstraintBendNotDirty(solver.rightArmChain.bendConstraint, data.rightArm);
                        this.SetFullBodyBipedConstraintBendNotDirty(solver.leftLegChain.bendConstraint, data.leftLeg);
                        this.SetFullBodyBipedConstraintBendNotDirty(solver.rightLegChain.bendConstraint, data.rightLeg);
                        break;
                    }
                case IKType.LimbIK:
                    {
                        IKSolverLimb solver = (IKSolverLimb)ik.solver;
                        LimbIKData data = (LimbIKData)d;
                        if (data.originalRotationWeight.hasValue)
                        {
                            solver.SetIKRotationWeight(data.originalRotationWeight);
                            data.originalRotationWeight.Reset();
                        }
                        if (data.originalBendModifierWeight.hasValue)
                        {
                            solver.bendModifierWeight = data.originalBendModifierWeight;
                            data.originalBendModifierWeight.Reset();
                        }
                        break;
                    }
            }
            this._dirtyIks.Remove(ik);
        }

        private void SetFullBodyBipedEffectorNotDirty(IKEffector effector, FullBodyBipedIKData.EffectorData data)
        {
            if (data.originalPositionWeight.hasValue)
            {
                effector.positionWeight = data.originalPositionWeight;
                data.originalPositionWeight.Reset();
            }
            if (data.originalRotationWeight.hasValue)
            {
                effector.rotationWeight = data.originalRotationWeight;
                data.originalRotationWeight.Reset();
            }
        }

        private void SetFullBodyBipedConstraintBendNotDirty(IKConstraintBend constraint, FullBodyBipedIKData.ConstraintBendData data)
        {
            if (data.originalWeight.hasValue)
            {
                constraint.weight = data.originalWeight;
                data.originalWeight.Reset();
            }
        }

        private void ResetAll()
        {
            foreach (KeyValuePair<IKWrapper, IKData> pair in new Dictionary<IKWrapper, IKData>(this._dirtyIks))
                this.SetIKNotDirty(pair.Key);
        }
        #endregion

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            private class Parameter
            {
                public readonly IKEditor editor;
                public readonly IKWrapper ik;
                private readonly int _hashCode;

                private Parameter()
                {
                    unchecked
                    {
                        int hash = 17;
                        hash = hash * 31 + (this.editor != null ? this.editor.GetHashCode() : 0);
                        this._hashCode = hash * 31 + (this.ik != null ? this.ik.GetHashCode() : 0);
                    }
                }

                public Parameter(IKEditor editor, IKWrapper ik) : this()
                {
                    this.editor = editor;
                    this.ik = ik;
                }

                public override int GetHashCode()
                {
                    return this._hashCode;
                }
            }

            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "ikEnabled",
                        name: "IK Enabled",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            bool newEnabled = (bool)leftValue;
                            if (p.editor.GetIKEnabled(p.ik) != newEnabled)
                                p.editor.SetIKEnabled(p.ik, newEnabled);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetNotFullBodyBipedIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetIKEnabled(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                        );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "ikFixTransforms",
                        name: "IK Fix Transforms",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            bool newFixTransforms = (bool)leftValue;
                            if (p.editor.GetIKFixTransforms(p.ik) != newFixTransforms)
                                p.editor.SetIKFixTransforms(p.ik, newFixTransforms);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetNotFullBodyBipedIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetIKFixTransforms(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadBool("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                        );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "ikPositionWeight",
                        name: "IK Pos Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetIKPositionWeight(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTarget,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetIKPositionWeight(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                //FABRIK
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fabrikTolerance",
                        name: "IK Tolerance",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetFABRIKTolerance(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFABRIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetFABRIKTolerance(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fabrikMaxIterations",
                        name: "IK Max Iterations",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetFABRIKMaxIterations(((Parameter)parameter).ik, Mathf.RoundToInt(Mathf.LerpUnclamped((int)leftValue, (int)rightValue, factor))),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFABRIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetFABRIKMaxIterations(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                //FABRIKRoot
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fabrikRootTolerance",
                        name: "IK Tolerance",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetFABRIKRootRootPin(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFABRIKRoot,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetFABRIKRootRootPin(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fabrikRootMaxIterations",
                        name: "IK Max Iterations",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetFABRIKRootIterations(((Parameter)parameter).ik, Mathf.RoundToInt(Mathf.LerpUnclamped((int)leftValue, (int)rightValue, factor))),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFABRIKRoot,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetFABRIKRootIterations(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                //CCDIK
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "ccdikTolerance",
                        name: "IK Tolerance",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetCCDIKTolerance(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetCCDIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetCCDIKTolerance(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "ccdikMaxIterations",
                        name: "IK Max Iterations",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetCCDIKMaxIterations(((Parameter)parameter).ik, Mathf.RoundToInt(Mathf.LerpUnclamped((int)leftValue, (int)rightValue, factor))),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetCCDIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetCCDIKMaxIterations(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                //AimIK
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikTolerance",
                        name: "IK Tolerance",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKTolerance(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKTolerance(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikMaxIterations",
                        name: "IK Max Iterations",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKMaxIterations(((Parameter)parameter).ik, Mathf.RoundToInt(Mathf.LerpUnclamped((int)leftValue, (int)rightValue, factor))),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKMaxIterations(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikAxis",
                        name: "IK Axis",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKAxis(((Parameter)parameter).ik, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKAxis(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikClampWeight",
                        name: "IK Clamp Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKClampWeight(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKClampWeight(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikClampSmoothing",
                        name: "IK Clamp Smoothing",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKClampSmoothing(((Parameter)parameter).ik, Mathf.RoundToInt(Mathf.LerpUnclamped((int)leftValue, (int)rightValue, factor))),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKClampSmoothing(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadInt("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikPoleAxis",
                        name: "IK Pole Axis",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKPoleAxis(((Parameter)parameter).ik, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKPoleAxis(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikPoleAxis",
                        name: "IK Pole Position",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKPolePosition(((Parameter)parameter).ik, Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIKPoleTargetNull,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKPolePosition(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "aimikPoleWeight",
                        name: "IK Pole Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetAimIKPoleWeight(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetAimIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetAimIKPoleWeight(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                //FullBodyBipedIK
                GenerateEffectorInterpolables("R. Shoulder", e => ((IKSolverFullBodyBiped)e.ik.solver).rightShoulderEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).rightShoulder);
                GenerateEffectorInterpolables("L. Shoulder", e => ((IKSolverFullBodyBiped)e.ik.solver).leftShoulderEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).leftShoulder);
                GenerateEffectorInterpolables("R. Hand", e => ((IKSolverFullBodyBiped)e.ik.solver).rightHandEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).rightHand);
                GenerateEffectorInterpolables("L. Hand", e => ((IKSolverFullBodyBiped)e.ik.solver).leftHandEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).leftHand);
                GenerateEffectorInterpolables("Body", e => ((IKSolverFullBodyBiped)e.ik.solver).bodyEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).body);
                GenerateEffectorInterpolables("R. Thigh", e => ((IKSolverFullBodyBiped)e.ik.solver).rightThighEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).rightThigh);
                GenerateEffectorInterpolables("L. Thigh", e => ((IKSolverFullBodyBiped)e.ik.solver).leftThighEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).leftThigh);
                GenerateEffectorInterpolables("R. Foot", e => ((IKSolverFullBodyBiped)e.ik.solver).rightFootEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).rightFoot);
                GenerateEffectorInterpolables("L. Foot", e => ((IKSolverFullBodyBiped)e.ik.solver).leftFootEffector, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).leftFoot);
                GenerateConstraintBendInterpolables("R. Elbow", e => ((IKSolverFullBodyBiped)e.ik.solver).rightArmChain.bendConstraint, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).rightArm);
                GenerateConstraintBendInterpolables("L. Elbow", e => ((IKSolverFullBodyBiped)e.ik.solver).leftArmChain.bendConstraint, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).leftArm);
                GenerateConstraintBendInterpolables("R. Knee", e => ((IKSolverFullBodyBiped)e.ik.solver).rightLegChain.bendConstraint, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).rightLeg);
                GenerateConstraintBendInterpolables("L. Knee", e => ((IKSolverFullBodyBiped)e.ik.solver).leftLegChain.bendConstraint, e => ((FullBodyBipedIKData)e.editor.SetIKDirty(e.ik)).leftLeg);
                //LimbIK
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "limbikRotationWeight",
                        name: "IK Rot Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetLimbIKRotationWeight(((Parameter)parameter).ik, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetLimbIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetLimbIKRotationWeight(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "libmikBendModifierWeight",
                        name: "IK Bend Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Parameter)parameter).editor.SetLimbIKBendModifierWeight(((Parameter)parameter).ik, Mathf.RoundToInt(Mathf.LerpUnclamped((int)leftValue, (int)rightValue, factor))),
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetLimbIK,
                        getValue: (oci, parameter) => ((Parameter)parameter).editor.GetLimbIKBendModifierWeight(((Parameter)parameter).ik),
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
            }

            private static void GenerateEffectorInterpolables(string effectorName, Func<Parameter, IKEffector> getEffector, Func<Parameter, FullBodyBipedIKData.EffectorData> getEffectorData)
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fbbikEffectorPositionWeight" + effectorName,
                        name: "IK " + effectorName + " Pos Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            p.editor.SetEffectorPositionWeight(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), getEffector(p), getEffectorData(p));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFullBodyBipedIK,
                        getValue: (oci, parameter) =>
                        {
                            Parameter p = (Parameter)parameter;
                            return ((Parameter)parameter).editor.GetEffectorPositionWeight(getEffector(p));
                        },
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fbbikEffectorRotationWeight" + effectorName,
                        name: "IK " + effectorName + " Rot Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            p.editor.SetEffectorRotationWeight(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), getEffector(p), getEffectorData(p));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFullBodyBipedIK,
                        getValue: (oci, parameter) =>
                        {
                            Parameter p = (Parameter)parameter;
                            return ((Parameter)parameter).editor.GetEffectorRotationWeight(getEffector(p));
                        },
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
            }

            private static void GenerateConstraintBendInterpolables(string constraintName, Func<Parameter, IKConstraintBend> getConstraint, Func<Parameter, FullBodyBipedIKData.ConstraintBendData> getConstraintData)
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "fbbikConstraintBendWeight" + constraintName,
                        name: "IK " + constraintName + " Weight",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            Parameter p = (Parameter)parameter;
                            p.editor.SetConstraintBendWeight(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor), getConstraint(p), getConstraintData(p));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: IsCompatibleWithTargetFullBodyBipedIK,
                        getValue: (oci, parameter) =>
                        {
                            Parameter p = (Parameter)parameter;
                            return ((Parameter)parameter).editor.GetConstraintBendWeight(getConstraint(p));
                        },
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: GetParameter,
                        readParameterFromXml: ReadParameterFromXml,
                        writeParameterToXml: WriteParameterToXml,
                        checkIntegrity: CheckIntegrity
                );
            }

            private static object ReadParameterFromXml(ObjectCtrlInfo oci, XmlNode node)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                if (node.Attributes["parameter"] != null)
                {
                    string root = node.Attributes["parameter"].Value;
                    Transform t = controller._ikEditor._parent.transform.Find(root);
                    IKWrapper ik = controller._ikEditor._iks.FirstOrDefault(i => i.Value.solver.GetRoot() == t).Value;
                    return new Parameter(controller._ikEditor, ik);
                }
                return new Parameter(controller._ikEditor, controller._ikEditor._iks.FirstOrDefault().Value);
            }

            private static void WriteParameterToXml(ObjectCtrlInfo oci, XmlTextWriter writer, object o)
            {
                Parameter p = (Parameter)o;
                writer.WriteAttributeString("parameter", p.ik.solver.GetRoot().GetPathFrom(p.editor._parent.transform));
            }

            private static bool CheckIntegrity(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue)
            {
                if (parameter == null)
                    return false;
                Parameter p = (Parameter)parameter;
                return p.editor.shouldDisplay && p.ik.ik != null && p.ik.solver != null;
            }

            private static bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null;
            }

            private static bool IsCompatibleWithTargetNotFullBodyBipedIK(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type != IKType.FullBodyBipedIK;
            }

            private static bool IsCompatibleWithTargetFABRIK(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.FABRIK;
            }

            private static bool IsCompatibleWithTargetFABRIKRoot(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.FABRIKRoot;
            }

            private static bool IsCompatibleWithTargetAimIK(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.AimIK;
            }

            private static bool IsCompatibleWithTargetAimIKPoleTargetNull(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.AimIK && ((IKSolverAim)controller._ikEditor._ikTarget.solver).poleTarget == null;
            }

            private static bool IsCompatibleWithTargetCCDIK(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.CCDIK;
            }

            private static bool IsCompatibleWithTargetFullBodyBipedIK(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.FullBodyBipedIK;
            }

            private static bool IsCompatibleWithTargetLimbIK(ObjectCtrlInfo oci)
            {
                PoseController controller;
                return oci != null && oci.guideObject != null && oci.guideObject.transformTarget != null && (controller = oci.guideObject.transformTarget.GetComponent<PoseController>()) != null && controller._ikEditor.shouldDisplay && controller._ikEditor._ikTarget != null && controller._ikEditor._ikTarget.type == IKType.LimbIK;
            }

            private static object GetParameter(ObjectCtrlInfo oci)
            {
                PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                return new Parameter(controller._ikEditor, controller._ikEditor._ikTarget);
            }
        }
        #endregion
    }
}
