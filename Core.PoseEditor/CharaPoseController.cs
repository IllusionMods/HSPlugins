using System;
using System.Collections.Generic;
using System.Xml;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using HSPE.AMModules;
using RootMotion.FinalIK;
using Studio;
using ToolBox.Extensions;
using UnityEngine;
#if KOIKATSU || AISHOUJO || HONEYSELECT2
using Manager;
#endif

namespace HSPE
{
    public class CharaPoseController : PoseController
    {
        #region Constants
        private static readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>()
        {
            { FullBodyBipedEffector.Body, 0 },
            { FullBodyBipedEffector.LeftShoulder, 1 },
            { FullBodyBipedEffector.LeftHand, 3 },
            { FullBodyBipedEffector.RightShoulder, 4 },
            { FullBodyBipedEffector.RightHand, 6 },
            { FullBodyBipedEffector.LeftThigh, 7 },
            { FullBodyBipedEffector.LeftFoot, 9 },
            { FullBodyBipedEffector.RightThigh, 10 },
            { FullBodyBipedEffector.RightFoot, 12 }
        };
        private static readonly Dictionary<FullBodyBipedChain, int> _chainToIndex = new Dictionary<FullBodyBipedChain, int>()
        {
            { FullBodyBipedChain.LeftArm, 2 },
            { FullBodyBipedChain.RightArm, 5 },
            { FullBodyBipedChain.LeftLeg, 8 },
            { FullBodyBipedChain.RightLeg, 11 }
        };

        public static readonly HashSet<FullBodyBipedEffector> nonRotatableEffectors = new HashSet<FullBodyBipedEffector>()
        {
            FullBodyBipedEffector.Body,
            FullBodyBipedEffector.LeftShoulder,
            FullBodyBipedEffector.LeftThigh,
            FullBodyBipedEffector.RightShoulder,
            FullBodyBipedEffector.RightThigh
        };

        internal static readonly HashSet<PoseController> _charaPoseControllers = new HashSet<PoseController>();
        #endregion

        #region Patches
#if HONEYSELECT
        [HarmonyPatch(typeof(CharBody), "LateUpdate")]
        private class CharBody_Patches
        {
            public static event Action<CharBody> onPreLateUpdate;
            public static event Action<CharBody> onPostLateUpdate;

            public static void Prefix(CharBody __instance)
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate(__instance);

            }
            public static void Postfix(CharBody __instance)
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate(__instance);
            }
        }
#elif PLAYHOME
        [HarmonyPatch(typeof(Expression), "LateUpdate")]
        private class Expression_Patches
        {
            public static event Action<Expression> onPreLateUpdate;
            public static event Action<Expression> onPostLateUpdate;

            public static void Prefix(Expression __instance)
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate(__instance);

            }
            public static void Postfix(Expression __instance)
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate(__instance);
            }
        }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        [HarmonyPatch(typeof(Character), "LateUpdate")]
        private class Character_Patches
        {
            public static event Action onPreLateUpdate;
            public static event Action onPostLateUpdate;

            public static void Prefix()
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate();

            }
            public static void Postfix()
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate();
            }
        }
#endif
        [HarmonyPatch(typeof(IKExecutionOrder), "LateUpdate")]
        private class IKExecutionOrder_Patches
        {
            public static event Action onPostLateUpdate;
            public static void Postfix()
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate();
            }
        }

        [HarmonyPatch(typeof(IKSolver), "Update")]
        private class IKSolver_Patches
        {
            public static event Action<IKSolver> onPostUpdate;
            [HarmonyBefore("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(IKSolver __instance)
            {
                if (onPostUpdate != null)
                    onPostUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
        private class FKCtrl_Patches
        {
            public static event Action<FKCtrl> onPreLateUpdate;
            public static event Action<FKCtrl> onPostLateUpdate;
            public static void Prefix(FKCtrl __instance)
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate(__instance);
            }
            public static void Postfix(FKCtrl __instance)
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate(__instance);
            }
        }
        #endregion

        #region Private Variables
        private FullBodyBipedIK _body;
        private float _cachedSpineStiffness;
        private float _cachedPullBodyVertical;
        private bool _optimizeIK = true;

        private Transform _siriDamL;
        private Transform _siriDamR;
        private Transform _kosi;
        private Quaternion _siriDamLOriginalRotation;
        private Quaternion _siriDamROriginalRotation;
        private Quaternion _kosiOriginalRotation;
        private Quaternion _siriDamLRotation;
        private Quaternion _siriDamRRotation;
        private Quaternion _kosiRotation;
#if KOIKATSU
        private Transform _ana;
        private Quaternion _anaOriginalRotation;
        private Quaternion _anaOriginalRotationOffset;
        private Vector3 _anaOriginalPosition;
        private Vector3 _anaOriginalPositionOffset;
        private Quaternion _anaRotation;
        private Vector3 _anaPosition;
#endif
        private bool _lastCrotchJointCorrection = false;

        private Transform _leftFoot2;
        private Quaternion _leftFoot2OriginalRotation;
        private float _leftFoot2ParentOriginalRotation;
        private float _leftFoot2Rotation;
        private bool _lastLeftFootJointCorrection = false;

        private Transform _rightFoot2;
        private Quaternion _rightFoot2OriginalRotation;
        private float _rightFoot2ParentOriginalRotation;
        private float _rightFoot2Rotation;
        private bool _lastrightFootJointCorrection = false;

        internal BoobsEditor _boobsEditor;
        private Action _scheduleNextIKPostUpdate = null;
        private FullBodyBipedChain _nextIKCopy;
        private OIBoneInfo.BoneGroup _nextFKCopy;
        #endregion

        #region Public Accessors
        public bool optimizeIK
        {
            get { return _optimizeIK; }
            set
            {
                _optimizeIK = value;
                if (_body != null)
                {
                    if (value)
                    {
                        _body.solver.spineStiffness = 0f;
                        _body.solver.pullBodyVertical = 0f;
                    }
                    else
                    {
                        _body.solver.spineStiffness = _cachedSpineStiffness;
                        _body.solver.pullBodyVertical = _cachedPullBodyVertical;
                    }
                }
            }
        }
        public bool crotchJointCorrection { get; set; }
        public bool leftFootJointCorrection { get; set; }
        public bool rightFootJointCorrection { get; set; }
        public override bool isDraggingDynamicBone { get { return base.isDraggingDynamicBone || _boobsEditor != null && _boobsEditor.isDraggingDynamicBone; } }
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _charaPoseControllers.Add(this);
#if HONEYSELECT
            this._body = this._target.ociChar.animeIKCtrl.IK;
#elif PLAYHOME
            this._body = this._target.ociChar.fullBodyIK;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            _body = _target.ociChar.finalIK;
#endif
#if HONEYSELECT
            if (this._target.isFemale)
#endif
            {
                _boobsEditor = new BoobsEditor(this, _target);
                _modules.Add(_boobsEditor);
            }
#if HONEYSELECT
            if (this._target.isFemale)
            {
                this._siriDamL = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L");
                this._siriDamR = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R");
                this._kosi = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s");
            }
            else
            {
                this._siriDamL = this.transform.Find("BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_L");
                this._siriDamR = this.transform.Find("BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_R");
                this._kosi = this.transform.Find("BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kosi02_s");
            }
#elif PLAYHOME
            if (this._target.isFemale)
            {
                this._siriDamL = this.transform.Find("p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L");
                this._siriDamR = this.transform.Find("p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R");
                this._kosi =     this.transform.Find("p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s");
            }
            else
            {
                this._siriDamL = this.transform.Find("p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_L");
                this._siriDamR = this.transform.Find("p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_R");
                this._kosi =     this.transform.Find("p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kosi02_s");
            }
#elif KOIKATSU
            _siriDamL = transform.FindDescendant("cf_d_siri_L");
            _siriDamR = transform.FindDescendant("cf_d_siri_R");
            _kosi = transform.FindDescendant("cf_s_waist02");
            _ana = transform.FindDescendant("cf_d_ana");
#elif AISHOUJO || HONEYSELECT2
            this._siriDamL = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L");
            this._siriDamR = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R");
            this._kosi =     this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s");
#endif
            _leftFoot2 = _body.solver.leftLegMapping.bone3.GetChild(0);
            _leftFoot2OriginalRotation = _leftFoot2.localRotation;
#if HONEYSELECT || PLAYHOME
            this._leftFoot2ParentOriginalRotation = 357.7f;
#elif KOIKATSU
            _leftFoot2ParentOriginalRotation = 358f;
#elif AISHOUJO || HONEYSELECT2
            this._leftFoot2ParentOriginalRotation = 357.62f;
#endif

            _rightFoot2 = _body.solver.rightLegMapping.bone3.GetChild(0);
            _rightFoot2OriginalRotation = _rightFoot2.localRotation;
#if HONEYSELECT || PLAYHOME
            this._rightFoot2ParentOriginalRotation = 357.7f;
#elif KOIKATSU
            _rightFoot2ParentOriginalRotation = 358f;
#elif AISHOUJO || HONEYSELECT2
            this._rightFoot2ParentOriginalRotation = 357.62f;
#endif

            _siriDamLOriginalRotation = _siriDamL.localRotation;
            _siriDamROriginalRotation = _siriDamR.localRotation;
            _kosiOriginalRotation = _kosi.localRotation;

#if KOIKATSU
            _anaOriginalRotation = _ana.localRotation;
            _anaOriginalPosition = _ana.localPosition;
            _anaOriginalRotationOffset = Quaternion.Inverse(_kosi.localRotation) * _ana.localRotation;
            _anaOriginalPositionOffset = _ana.localPosition - _kosi.localPosition;
#endif

            IKSolver_Patches.onPostUpdate += IKSolverOnPostUpdate;
            IKExecutionOrder_Patches.onPostLateUpdate += IKExecutionOrderOnPostLateUpdate;
            FKCtrl_Patches.onPreLateUpdate += FKCtrlOnPreLateUpdate;
#if HONEYSELECT
            CharBody_Patches.onPostLateUpdate += this.CharBodyOnPostLateUpdate;
#elif PLAYHOME
            Expression_Patches.onPostLateUpdate += this.ExpressionOnPostLateUpdate;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            Character_Patches.onPostLateUpdate += CharacterOnPostLateUpdate;
#endif
            OCIChar_ChangeChara_Patches.onChangeChara += OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile += OnLoadClothesFile;
#if HONEYSELECT || KOIKATSU
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo += OnCoordinateReplaced;
#endif

            crotchJointCorrection = MainWindow._self.crotchCorrectionByDefault;
            leftFootJointCorrection = MainWindow._self.anklesCorrectionByDefault;
            rightFootJointCorrection = MainWindow._self.anklesCorrectionByDefault;
        }

        protected override void Start()
        {
            base.Start();
            _cachedSpineStiffness = _body.solver.spineStiffness;
            _cachedPullBodyVertical = _body.solver.pullBodyVertical;

            if (_optimizeIK)
            {
                _body.solver.spineStiffness = 0f;
                _body.solver.pullBodyVertical = 0f;
            }
            else
            {
                _body.solver.spineStiffness = _cachedSpineStiffness;
                _body.solver.pullBodyVertical = _cachedPullBodyVertical;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (_target.ikEnabled == false)
            {
                if (_scheduleNextIKPostUpdate != null)
                {
                    Action tempAction = _scheduleNextIKPostUpdate;
                    _scheduleNextIKPostUpdate = null;
                    tempAction();
                }

                InitJointCorrection();
            }
        }

        protected override void OnDestroy()
        {
            _body.solver.spineStiffness = _cachedSpineStiffness;
            _body.solver.pullBodyVertical = _cachedPullBodyVertical;
            IKSolver_Patches.onPostUpdate -= IKSolverOnPostUpdate;
            IKExecutionOrder_Patches.onPostLateUpdate -= IKExecutionOrderOnPostLateUpdate;
            FKCtrl_Patches.onPreLateUpdate -= FKCtrlOnPreLateUpdate;
#if HONEYSELECT
            CharBody_Patches.onPostLateUpdate -= this.CharBodyOnPostLateUpdate;
#elif PLAYHOME
            Expression_Patches.onPostLateUpdate -= this.ExpressionOnPostLateUpdate;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            Character_Patches.onPostLateUpdate -= CharacterOnPostLateUpdate;
#endif
            OCIChar_ChangeChara_Patches.onChangeChara -= OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile -= OnLoadClothesFile;
#if HONEYSELECT || KOIKATSU
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo -= OnCoordinateReplaced;
#endif
            base.OnDestroy();
            _charaPoseControllers.Remove(this);
        }
        #endregion

        #region Public Methods
        public override void LoadFrom(PoseController other)
        {
            base.LoadFrom(other);
            if (other == null)
                return;
            CharaPoseController other2 = other as CharaPoseController;
            if (other2 != null)
            {
                optimizeIK = other2.optimizeIK;
                crotchJointCorrection = other2.crotchJointCorrection;
                leftFootJointCorrection = other2.leftFootJointCorrection;
                rightFootJointCorrection = other2.rightFootJointCorrection;
                if (_target.isFemale)
                    _boobsEditor.LoadFrom(other2._boobsEditor);
            }
        }



        public bool IsPartEnabled(FullBodyBipedEffector part)
        {
            return _target.ikEnabled && _target.ociChar.listIKTarget[_effectorToIndex[part]].active;
        }

        public bool IsPartEnabled(FullBodyBipedChain part)
        {
            return _target.ikEnabled && _target.ociChar.listIKTarget[_chainToIndex[part]].active;
        }

        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        {
            OCIChar.IKInfo info = _target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (_target.ikEnabled && info.active)
            {
                if (_currentDragType != DragType.None)
                {
                    if (_oldRotValues.ContainsKey(info.guideObject.dicKey) == false)
                        _oldRotValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.rot);
                    info.guideObject.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            OCIChar.IKInfo info = _target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (!_target.ikEnabled || info.active == false)
                return Quaternion.identity;
            return info.guideObject.transformTarget.localRotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition, bool world = true)
        {
            OCIChar.IKInfo info = _target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (_target.ikEnabled && info.active)
            {
                if (_currentDragType != DragType.None)
                {
                    if (_oldPosValues.ContainsKey(info.guideObject.dicKey) == false)
                        _oldPosValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.pos);
                    info.guideObject.changeAmount.pos = world ? info.guideObject.transformTarget.parent.InverseTransformPoint(targetPosition) : targetPosition;
                }
            }
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type, bool world = true)
        {
            OCIChar.IKInfo info = _target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (!_target.ikEnabled || info.active == false)
                return Vector3.zero;
            return world ? info.guideObject.transformTarget.position : info.guideObject.transformTarget.localPosition;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 targetPosition, bool world = true)
        {
            OCIChar.IKInfo info = _target.ociChar.listIKTarget[_chainToIndex[type]];
            if (_target.ikEnabled && info.active)
            {
                if (_currentDragType != DragType.None)
                {
                    if (_oldPosValues.ContainsKey(info.guideObject.dicKey) == false)
                        _oldPosValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.pos);
                    info.guideObject.changeAmount.pos = world ? info.guideObject.transformTarget.parent.InverseTransformPoint(targetPosition) : targetPosition;
                }
            }
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type, bool world = true)
        {
            OCIChar.IKInfo info = _target.ociChar.listIKTarget[_chainToIndex[type]];
            if (!_target.ikEnabled || info.active == false)
                return Vector3.zero;
            return world ? info.guideObject.transformTarget.position : info.guideObject.transformTarget.localPosition;
        }

        public void CopyLimbToTwin(FullBodyBipedChain ikLimb, OIBoneInfo.BoneGroup fkLimb)
        {
            _scheduleNextIKPostUpdate = CopyLimbToTwinInternal;
            _nextIKCopy = ikLimb;
            _nextFKCopy = fkLimb;
        }

        public void CopyHandToTwin(OIBoneInfo.BoneGroup fkLimb)
        {
            _scheduleNextIKPostUpdate = CopyHandToTwinInternal;
            _nextFKCopy = fkLimb;
        }

        public void SwapPose()
        {
            _scheduleNextIKPostUpdate = SwapPoseInternal;
        }
        #endregion

        #region Private Methods
        private void IKSolverOnPostUpdate(IKSolver solver)
        {
            if (enabled == false || _body.solver != solver)
                return;
            if (_scheduleNextIKPostUpdate != null)
            {
                Action tempAction = _scheduleNextIKPostUpdate;
                _scheduleNextIKPostUpdate = null;
                tempAction();
            }
            InitJointCorrection();
            foreach (AdvancedModeModule module in _modules)
                module.IKSolverOnPostUpdate();
        }

        private void IKExecutionOrderOnPostLateUpdate()
        {
            if (enabled == false)
                return;
            foreach (AdvancedModeModule module in _modules)
                module.IKExecutionOrderOnPostLateUpdate();
        }

        private void FKCtrlOnPreLateUpdate(FKCtrl ctrl)
        {
            if (enabled == false || _target.ociChar.fkCtrl != ctrl)
                return;
            foreach (AdvancedModeModule module in _modules)
                module.FKCtrlOnPreLateUpdate();
        }

#if HONEYSELECT
        private void CharBodyOnPostLateUpdate(CharBody human)
        {
            if (this._target.ociChar.charBody != human)
                return;
            this.ApplyJointCorrection();
        }
#elif PLAYHOME
        private void ExpressionOnPostLateUpdate(Expression expression)
        {
            if (this._target.ociChar.charInfo.expression != expression)
                return;
            this.ApplyJointCorrection();
        }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        private void CharacterOnPostLateUpdate()
        {
            ApplyJointCorrection();
        }
#endif
        private void OnCharacterReplaced(OCIChar chara)
        {
            if (_target.ociChar != chara)
                return;
            _target.RefreshFKBones();
            foreach (AdvancedModeModule module in _modules)
                module.OnCharacterReplaced();
        }
        private void OnLoadClothesFile(OCIChar chara)
        {
            if (_target.ociChar != chara)
                return;
            _target.RefreshFKBones();
            foreach (AdvancedModeModule module in _modules)
                module.OnLoadClothesFile();
        }

#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        private void OnCoordinateReplaced(OCIChar chara, CharDefine.CoordinateType type, bool force)
#elif KOIKATSU
        private void OnCoordinateReplaced(OCIChar chara, ChaFileDefine.CoordinateType type, bool force)
#endif
        {
            if (_target.ociChar != chara)
                return;
            _target.RefreshFKBones();
            foreach (AdvancedModeModule module in _modules)
                module.OnCoordinateReplaced(type, force);
        }
#endif

        private FullBodyBipedEffector GetTwinEffector(FullBodyBipedEffector effector)
        {
            switch (effector)
            {
                case FullBodyBipedEffector.LeftFoot:
                    return FullBodyBipedEffector.RightFoot;
                case FullBodyBipedEffector.RightFoot:
                    return FullBodyBipedEffector.LeftFoot;
                case FullBodyBipedEffector.LeftHand:
                    return FullBodyBipedEffector.RightHand;
                case FullBodyBipedEffector.RightHand:
                    return FullBodyBipedEffector.LeftHand;
                case FullBodyBipedEffector.LeftThigh:
                    return FullBodyBipedEffector.RightThigh;
                case FullBodyBipedEffector.RightThigh:
                    return FullBodyBipedEffector.LeftThigh;
                case FullBodyBipedEffector.LeftShoulder:
                    return FullBodyBipedEffector.RightShoulder;
                case FullBodyBipedEffector.RightShoulder:
                    return FullBodyBipedEffector.LeftShoulder;
            }
            return effector;
        }

        private FullBodyBipedChain GetTwinChain(FullBodyBipedChain chain)
        {
            switch (chain)
            {
                case FullBodyBipedChain.LeftArm:
                    return FullBodyBipedChain.RightArm;
                case FullBodyBipedChain.RightArm:
                    return FullBodyBipedChain.LeftArm;
                case FullBodyBipedChain.LeftLeg:
                    return FullBodyBipedChain.RightLeg;
                case FullBodyBipedChain.RightLeg:
                    return FullBodyBipedChain.LeftLeg;
            }
            return chain;
        }

        private void CopyLimbToTwinInternal()
        {
            StartDrag(DragType.Both);
            _lockDrag = true;
            HashSet<OIBoneInfo.BoneGroup> fkTwinLimb = new HashSet<OIBoneInfo.BoneGroup>();
            switch (_nextFKCopy)
            {
                case OIBoneInfo.BoneGroup.RightLeg:
                    fkTwinLimb.Add(OIBoneInfo.BoneGroup.LeftLeg);
                    fkTwinLimb.Add((OIBoneInfo.BoneGroup)5);
                    break;
                case OIBoneInfo.BoneGroup.LeftLeg:
                    fkTwinLimb.Add(OIBoneInfo.BoneGroup.RightLeg);
                    fkTwinLimb.Add((OIBoneInfo.BoneGroup)3);
                    break;
                case OIBoneInfo.BoneGroup.RightArm:
                    fkTwinLimb.Add(OIBoneInfo.BoneGroup.LeftArm);
                    fkTwinLimb.Add((OIBoneInfo.BoneGroup)17);
                    break;
                case OIBoneInfo.BoneGroup.LeftArm:
                    fkTwinLimb.Add(OIBoneInfo.BoneGroup.RightArm);
                    fkTwinLimb.Add((OIBoneInfo.BoneGroup)9);
                    break;
            }

            _additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            foreach (OCIChar.BoneInfo bone in _target.ociChar.listBones)
            {
                Transform twinBoneTransform = null;
                Transform boneTransform = bone.guideObject.transformTarget;
                if (fkTwinLimb.Contains(bone.boneGroup) == false)
                    continue;
                twinBoneTransform = _bonesEditor.GetTwinBone(boneTransform);
                if (twinBoneTransform == null)
                    twinBoneTransform = boneTransform;

                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && _target.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
                {
                    if (twinBoneTransform == boneTransform)
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = rot.eulerAngles;

                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                    else
                    {
                        Quaternion twinRot = Quaternion.Euler(twinBone.guideObject.changeAmount.rot);
                        twinRot = new Quaternion(twinRot.x, -twinRot.y, -twinRot.z, twinRot.w);
                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;
                        bone.guideObject.changeAmount.rot = twinRot.eulerAngles;
                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                }
            }

            FullBodyBipedChain limb = _nextIKCopy;
            FullBodyBipedEffector effectorSrc;
            FullBodyBipedChain bendGoalSrc;
            FullBodyBipedEffector effectorDest;
            FullBodyBipedChain bendGoalDest;
            Transform effectorSrcRealBone;
            Transform effectorDestRealBone;
            Transform root;
            switch (limb)
            {
                case FullBodyBipedChain.LeftArm:
                    effectorSrc = FullBodyBipedEffector.LeftHand;
                    effectorSrcRealBone = _body.references.leftHand;
                    effectorDestRealBone = _body.references.rightHand;
                    root = _body.solver.spineMapping.spineBones[_body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = FullBodyBipedEffector.LeftFoot;
                    effectorSrcRealBone = _body.references.leftFoot;
                    effectorDestRealBone = _body.references.rightFoot;
                    root = _body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorSrcRealBone = _body.references.rightHand;
                    effectorDestRealBone = _body.references.leftHand;
                    root = _body.solver.spineMapping.spineBones[_body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = FullBodyBipedEffector.RightFoot;
                    effectorSrcRealBone = _body.references.rightFoot;
                    effectorDestRealBone = _body.references.leftFoot;
                    root = _body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorSrcRealBone = null;
                    effectorDestRealBone = null;
                    root = null;
                    break;
            }
            bendGoalSrc = limb;
            bendGoalDest = GetTwinChain(limb);
            effectorDest = GetTwinEffector(effectorSrc);

            Vector3 localPos = root.InverseTransformPoint(_target.ociChar.listIKTarget[_effectorToIndex[effectorSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);
            localPos = root.InverseTransformPoint(_target.ociChar.listIKTarget[_chainToIndex[bendGoalSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);


            SetBoneTargetPosition(effectorDest, effectorPosition);
            SetBendGoalPosition(bendGoalDest, bendGoalPosition);
            SetBoneTargetRotation(effectorDest, GetBoneTargetRotation(effectorDest));

            _scheduleNextIKPostUpdate = () =>
            {
                Quaternion rot = effectorSrcRealBone.localRotation;
                rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                effectorDestRealBone.localRotation = rot; //Setting real bone local rotation
                OCIChar.IKInfo effectorDestInfo = _target.ociChar.listIKTarget[_effectorToIndex[effectorDest]];
                effectorDestInfo.guideObject.transformTarget.rotation = effectorDestRealBone.rotation; //Using real bone rotation to set IK target rotation;
                SetBoneTargetRotation(effectorDest, effectorDestInfo.guideObject.transformTarget.localRotation); //Setting again the IK target with its own local rotation through normal means so it isn't ignored by neo while saving
                _lockDrag = false;
                StopDrag();
            };
        }

        private void CopyHandToTwinInternal()
        {
            StartDrag(DragType.Both);
            _lockDrag = true;
            HashSet<OIBoneInfo.BoneGroup> fkTwinLimb = new HashSet<OIBoneInfo.BoneGroup>();
            switch (_nextFKCopy)
            {
                case OIBoneInfo.BoneGroup.RightHand:
                    fkTwinLimb.Add(OIBoneInfo.BoneGroup.LeftHand);
                    fkTwinLimb.Add((OIBoneInfo.BoneGroup)65);
                    break;
                case OIBoneInfo.BoneGroup.LeftHand:
                    fkTwinLimb.Add(OIBoneInfo.BoneGroup.RightHand);
                    fkTwinLimb.Add((OIBoneInfo.BoneGroup)33);
                    break;
            }

            //TODO Delete that disgusting duplicated code
            _additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            foreach (OCIChar.BoneInfo bone in _target.ociChar.listBones)
            {
                Transform boneTransform = bone.guideObject.transformTarget;
                if (fkTwinLimb.Contains(bone.boneGroup) == false)
                    continue;
                Transform twinBoneTransform = _bonesEditor.GetTwinBone(boneTransform);
                if (twinBoneTransform == null)
                    twinBoneTransform = boneTransform;

                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && _target.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
                {
                    if (twinBoneTransform == boneTransform)
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = rot.eulerAngles;

                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                    else
                    {
                        Quaternion twinRot = Quaternion.Euler(twinBone.guideObject.changeAmount.rot);
                        twinRot = new Quaternion(twinRot.x, -twinRot.y, -twinRot.z, twinRot.w);
                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;
                        bone.guideObject.changeAmount.rot = twinRot.eulerAngles;

#if KOIKATSU
                        FixTwinFingerBone(bone);
#endif

                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                }
            }

            _lockDrag = false;
            StopDrag();
        }

#if KOIKATSU
        private void FixTwinFingerBone( OCIChar.BoneInfo bone )
        {
            //Could be simplified to a single if/else, but it's left verbose for clarity
            if (bone.boneID == 22 || bone.boneID == 25 || bone.boneID == 28 || bone.boneID == 31 || bone.boneID == 34) //Right hand first joint
                bone.guideObject.changeAmount.rot = new Vector3(-bone.guideObject.changeAmount.rot.x, 180 + bone.guideObject.changeAmount.rot.y, 180 - bone.guideObject.changeAmount.rot.z);

            else if (bone.boneID == 23 || bone.boneID == 26 || bone.boneID == 29 || bone.boneID == 32 || bone.boneID == 35) //Right hand second joint
                bone.guideObject.changeAmount.rot = new Vector3(bone.guideObject.changeAmount.rot.x, -bone.guideObject.changeAmount.rot.y, -bone.guideObject.changeAmount.rot.z);

            else if (bone.boneID == 24 || bone.boneID == 27 || bone.boneID == 30 || bone.boneID == 33 || bone.boneID == 36) //Right hand third joint
                bone.guideObject.changeAmount.rot = new Vector3(bone.guideObject.changeAmount.rot.x, -bone.guideObject.changeAmount.rot.y, -bone.guideObject.changeAmount.rot.z);


            else if (bone.boneID == 37 || bone.boneID == 40 || bone.boneID == 43 || bone.boneID == 46 || bone.boneID == 49) //Left hand first joint
                bone.guideObject.changeAmount.rot = new Vector3(-bone.guideObject.changeAmount.rot.x, 180 + bone.guideObject.changeAmount.rot.y, 180 - bone.guideObject.changeAmount.rot.z);

            else if (bone.boneID == 38 || bone.boneID == 41 || bone.boneID == 44 || bone.boneID == 47 || bone.boneID == 50) //Left hand second joint
                bone.guideObject.changeAmount.rot = new Vector3(bone.guideObject.changeAmount.rot.x, -bone.guideObject.changeAmount.rot.y, -bone.guideObject.changeAmount.rot.z);

            else if (bone.boneID == 39 || bone.boneID == 42 || bone.boneID == 45 || bone.boneID == 48 || bone.boneID == 51) //Left hand third joint
                bone.guideObject.changeAmount.rot = new Vector3(bone.guideObject.changeAmount.rot.x, -bone.guideObject.changeAmount.rot.y, -bone.guideObject.changeAmount.rot.z);
        }
#endif

        private void SwapPoseInternal()
        {
            StartDrag(DragType.Both);
            _lockDrag = true;

            _additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            HashSet<Transform> done = new HashSet<Transform>();
            foreach (OCIChar.BoneInfo bone in _target.ociChar.listBones)
            {
                Transform twinBoneTransform = null;
                Transform boneTransform = bone.guideObject.transformTarget;
                switch (bone.boneGroup)
                {
                    case OIBoneInfo.BoneGroup.Hair:
                        continue;

                    case OIBoneInfo.BoneGroup.Skirt:
                        twinBoneTransform = GetSkirtTwinBone(boneTransform);
                        break;

                    case OIBoneInfo.BoneGroup.Neck:
                    case OIBoneInfo.BoneGroup.Body:
                    case OIBoneInfo.BoneGroup.Breast:
                    case OIBoneInfo.BoneGroup.RightLeg:
                    case OIBoneInfo.BoneGroup.LeftLeg:
                    case OIBoneInfo.BoneGroup.RightArm:
                    case OIBoneInfo.BoneGroup.LeftArm:
                    case OIBoneInfo.BoneGroup.RightHand:
                    case OIBoneInfo.BoneGroup.LeftHand:
                    default:
                        twinBoneTransform = _bonesEditor.GetTwinBone(boneTransform);
                        if (twinBoneTransform == null)
                            twinBoneTransform = boneTransform;
                        break;
                }
                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && _target.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
                {
                    if (done.Contains(boneTransform) || done.Contains(twinBoneTransform))
                        continue;
                    done.Add(boneTransform);
                    if (twinBoneTransform != boneTransform)
                        done.Add(twinBoneTransform);

                    if (twinBoneTransform == boneTransform)
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = rot.eulerAngles;

                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                    else
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                        Quaternion twinRot = Quaternion.Euler(twinBone.guideObject.changeAmount.rot);
                        twinRot = new Quaternion(twinRot.x, -twinRot.y, -twinRot.z, twinRot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;
                        Vector3 oldTwinRotValue = twinBone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = twinRot.eulerAngles;
                        twinBone.guideObject.changeAmount.rot = rot.eulerAngles;

#if KOIKATSU
                        FixTwinFingerBone(bone);
                        FixTwinFingerBone(twinBone);
#endif

                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot

                        });
                        _additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = twinBone.guideObject.dicKey,
                            oldValue = oldTwinRotValue,
                            newValue = twinBone.guideObject.changeAmount.rot
                        });
                    }
                }
            }

            foreach (KeyValuePair<FullBodyBipedEffector, int> pair in _effectorToIndex)
            {
                switch (pair.Key)
                {
                    case FullBodyBipedEffector.Body:
                    case FullBodyBipedEffector.LeftShoulder:
                    case FullBodyBipedEffector.LeftHand:
                    case FullBodyBipedEffector.LeftThigh:
                    case FullBodyBipedEffector.LeftFoot:
                        FullBodyBipedEffector twin = GetTwinEffector(pair.Key);
                        Vector3 position = _target.ociChar.listIKTarget[pair.Value].guideObject.transformTarget.localPosition;
                        position.x *= -1f;
                        Vector3 twinPosition = _target.ociChar.listIKTarget[_effectorToIndex[twin]].guideObject.transformTarget.localPosition;
                        twinPosition.x *= -1f;
                        SetBoneTargetPosition(pair.Key, twinPosition, false);
                        SetBoneTargetPosition(twin, position, false);
                        break;
                }
            }

            foreach (KeyValuePair<FullBodyBipedChain, int> pair in _chainToIndex)
            {
                switch (pair.Key)
                {
                    case FullBodyBipedChain.LeftArm:
                    case FullBodyBipedChain.LeftLeg:
                        FullBodyBipedChain twin = GetTwinChain(pair.Key);
                        Vector3 position = _target.ociChar.listIKTarget[pair.Value].guideObject.transformTarget.localPosition;
                        position.x *= -1f;
                        Vector3 twinPosition = _target.ociChar.listIKTarget[_chainToIndex[twin]].guideObject.transformTarget.localPosition;
                        twinPosition.x *= -1f;
                        SetBendGoalPosition(pair.Key, twinPosition, false);
                        SetBendGoalPosition(twin, position, false);
                        break;
                }
            }

#if (KOIKATSU && SUNSHINE) || HONEYSELECT2
            var cha = _target.ociChar.charInfo;
            bool handShapeEnable0 = cha.GetEnableShapeHand(0);
            bool handShapeEnable1 = cha.GetEnableShapeHand(1);
            int hand00 = cha.GetShapeHandIndex(0, 0);
            int hand01 = cha.GetShapeHandIndex(0, 1);
            int hand10 = cha.GetShapeHandIndex(1, 0);
            int hand11 = cha.GetShapeHandIndex(1, 1);
            float blend0 = cha.GetShapeHandBlendValue(0);
            float blend1 = cha.GetShapeHandBlendValue(1);
            
            _target.ociChar.ChangeHandAnime(0, hand10);
            _target.ociChar.ChangeHandAnime(1, hand00);
            cha.SetEnableShapeHand(0, handShapeEnable1);
            cha.SetEnableShapeHand(1, handShapeEnable0);
            cha.SetShapeHandIndex(0, hand10, hand11);
            cha.SetShapeHandBlend(0, blend1);
            cha.SetShapeHandIndex(1, hand00, hand01);
            cha.SetShapeHandBlend(1, blend0);
#endif

            var fkCtrl = _target.ociChar.fkCtrl;
            if (fkCtrl.isActiveAndEnabled)
            {
                bool leftHandFK = false;
                bool rightHandFK = false;

                foreach (var bone in fkCtrl.listBones)
                {
                    if (bone.gameObject.name.Contains("Toes"))
                        continue;

                    if (bone.group == OIBoneInfo.BoneGroup.LeftHand && bone.enable)
                        leftHandFK = true;

                    if (bone.group == OIBoneInfo.BoneGroup.RightHand && bone.enable)
                        rightHandFK = true;
                }

                if (leftHandFK ^ rightHandFK)
                {
                    _target.ociChar.ActiveFK(OIBoneInfo.BoneGroup.LeftHand, rightHandFK);
                    _target.ociChar.ActiveFK(OIBoneInfo.BoneGroup.RightHand, leftHandFK);
                }
            }

            _scheduleNextIKPostUpdate = () =>
            {
                foreach (KeyValuePair<FullBodyBipedEffector, int> pair in _effectorToIndex)
                {
                    switch (pair.Key)
                    {
                        case FullBodyBipedEffector.LeftHand:
                        case FullBodyBipedEffector.LeftFoot:
                            FullBodyBipedEffector twin = GetTwinEffector(pair.Key);
                            Quaternion rot = _target.ociChar.listIKTarget[pair.Value].guideObject.transformTarget.localRotation;
                            rot = new Quaternion(-rot.x, rot.y, rot.z, -rot.w);
                            Quaternion twinRot = _target.ociChar.listIKTarget[_effectorToIndex[twin]].guideObject.transformTarget.localRotation;
                            twinRot = new Quaternion(-twinRot.x, twinRot.y, twinRot.z, -twinRot.w);
                            SetBoneTargetRotation(pair.Key, twinRot);
                            SetBoneTargetRotation(twin, rot);
                            break;
                    }
                }

                _lockDrag = false;
                StopDrag();
            };
        }

        private Transform GetSkirtTwinBone(Transform bone)
        {
            int id = int.Parse(bone.name.Substring(bone.name.Length - 5, 2));
            string newName = "";
            switch (id)
            {
                case 00:
                case 04:
                    return bone;

                case 01:
                    newName = bone.name.Replace("sk_01", "sk_07");
                    break;
                case 07:
                    newName = bone.name.Replace("sk_07", "sk_01");
                    break;
                case 02:
                    newName = bone.name.Replace("sk_02", "sk_06");
                    break;
                case 06:
                    newName = bone.name.Replace("sk_06", "sk_02");
                    break;
                case 03:
                    newName = bone.name.Replace("sk_03", "sk_05");
                    break;
                case 05:
                    newName = bone.name.Replace("sk_05", "sk_03");
                    break;
            }
            return transform.FindDescendant(newName);
        }

        private void InitJointCorrection()
        {
            if (crotchJointCorrection)
            {
                _siriDamLRotation = Quaternion.Lerp(Quaternion.identity, _body.solver.leftLegMapping.bone1.localRotation, 0.4f);
                _siriDamRRotation = Quaternion.Lerp(Quaternion.identity, _body.solver.rightLegMapping.bone1.localRotation, 0.4f);
                _kosiRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Lerp(_body.solver.leftLegMapping.bone1.localRotation, _body.solver.rightLegMapping.bone1.localRotation, 0.5f), 0.25f);
                
#if KOIKATSU                
                _anaRotation = _kosiRotation * _anaOriginalRotationOffset;
                _anaPosition = _kosiRotation * _anaOriginalPositionOffset;
#endif
            }

            if (leftFootJointCorrection)
                _leftFoot2Rotation = Mathf.LerpAngle(0f, _leftFoot2.parent.localRotation.eulerAngles.x - _leftFoot2ParentOriginalRotation, 0.9f);

            if (rightFootJointCorrection)
                _rightFoot2Rotation = Mathf.LerpAngle(0f, _rightFoot2.parent.localRotation.eulerAngles.x - _rightFoot2ParentOriginalRotation, 0.9f);
        }

        private void ApplyJointCorrection()
        {
            if (crotchJointCorrection)
            {
                _siriDamL.localRotation = _siriDamLRotation;
                _siriDamR.localRotation = _siriDamRRotation;
                _kosi.localRotation = _kosiRotation;
#if KOIKATSU
                _ana.localRotation = _anaRotation;
                _ana.localPosition = _anaPosition;
#endif
            }
            else if (_lastCrotchJointCorrection)
            {
                _siriDamL.localRotation = _siriDamLOriginalRotation;
                _siriDamR.localRotation = _siriDamROriginalRotation;
                _kosi.localRotation = _kosiOriginalRotation;
#if KOIKATSU
                _ana.localRotation = _anaOriginalRotation;
                _ana.localPosition = _anaOriginalPosition;
#endif
            }

            if (leftFootJointCorrection)
                _leftFoot2.localRotation = Quaternion.AngleAxis(_leftFoot2Rotation, Vector3.right);
            else if (_lastLeftFootJointCorrection)
                _leftFoot2.localRotation = _leftFoot2OriginalRotation;

            if (rightFootJointCorrection)
                _rightFoot2.localRotation = Quaternion.AngleAxis(_rightFoot2Rotation, Vector3.right);
            else if (_lastrightFootJointCorrection)
                _rightFoot2.localRotation = _rightFoot2OriginalRotation;

            _lastCrotchJointCorrection = crotchJointCorrection;
            _lastLeftFootJointCorrection = leftFootJointCorrection;
            _lastrightFootJointCorrection = rightFootJointCorrection;
        }
        #endregion

        #region Saves


        public override void SaveXml(XmlTextWriter xmlWriter)
        {
            if (optimizeIK == false)
            {
                xmlWriter.WriteAttributeString("optimizeIK", XmlConvert.ToString(optimizeIK));
            }
            xmlWriter.WriteAttributeString("crotchCorrection", XmlConvert.ToString(crotchJointCorrection));
            xmlWriter.WriteAttributeString("leftAnkleCorrection", XmlConvert.ToString(leftFootJointCorrection));
            xmlWriter.WriteAttributeString("rightAnkleCorrection", XmlConvert.ToString(rightFootJointCorrection));
            base.SaveXml(xmlWriter);
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            optimizeIK = xmlNode.Attributes?["optimizeIK"] == null || XmlConvert.ToBoolean(xmlNode.Attributes["optimizeIK"].Value);
            crotchJointCorrection = xmlNode.Attributes?["crotchCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["crotchCorrection"].Value);
            leftFootJointCorrection = xmlNode.Attributes?["leftAnkleCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["leftAnkleCorrection"].Value);
            rightFootJointCorrection = xmlNode.Attributes?["rightAnkleCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["rightAnkleCorrection"].Value);
            return base.LoadXml(xmlNode);
        }
        #endregion
    }
}