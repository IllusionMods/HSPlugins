using System;
using System.Collections;
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
            get { return this._optimizeIK; }
            set
            {
                this._optimizeIK = value;
                if (this._body != null)
                {
                    if (value)
                    {
                        this._body.solver.spineStiffness = 0f;
                        this._body.solver.pullBodyVertical = 0f;
                    }
                    else
                    {
                        this._body.solver.spineStiffness = this._cachedSpineStiffness;
                        this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
                    }
                }
            }
        }
        public bool crotchJointCorrection { get; set; }
        public bool leftFootJointCorrection { get; set; }
        public bool rightFootJointCorrection { get; set; }
        public override bool isDraggingDynamicBone { get { return base.isDraggingDynamicBone || this._boobsEditor != null && this._boobsEditor.isDraggingDynamicBone; } }
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
            this._body = this._target.ociChar.finalIK;
#endif
#if HONEYSELECT
            if (this._target.isFemale)
#endif
            {
                this._boobsEditor = new BoobsEditor(this, this._target);
                this._modules.Add(this._boobsEditor);
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
            this._siriDamL = this.transform.FindDescendant("cf_d_siri_L");
            this._siriDamR = this.transform.FindDescendant("cf_d_siri_R");
            this._kosi =     this.transform.FindDescendant("cf_s_waist02");
#elif AISHOUJO || HONEYSELECT2
            this._siriDamL = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L");
            this._siriDamR = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R");
            this._kosi =     this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s");
#endif
            this._leftFoot2 = this._body.solver.leftLegMapping.bone3.GetChild(0);
            this._leftFoot2OriginalRotation = this._leftFoot2.localRotation;
#if HONEYSELECT || PLAYHOME
            this._leftFoot2ParentOriginalRotation = 357.7f;
#elif KOIKATSU
            this._leftFoot2ParentOriginalRotation = 358f;
#elif AISHOUJO || HONEYSELECT2
            this._leftFoot2ParentOriginalRotation = 357.62f;
#endif

            this._rightFoot2 = this._body.solver.rightLegMapping.bone3.GetChild(0);
            this._rightFoot2OriginalRotation = this._rightFoot2.localRotation;
#if HONEYSELECT || PLAYHOME
            this._rightFoot2ParentOriginalRotation = 357.7f;
#elif KOIKATSU
            this._rightFoot2ParentOriginalRotation = 358f;
#elif AISHOUJO || HONEYSELECT2
            this._rightFoot2ParentOriginalRotation = 357.62f;
#endif

            this._siriDamLOriginalRotation = this._siriDamL.localRotation;
            this._siriDamROriginalRotation = this._siriDamR.localRotation;
            this._kosiOriginalRotation = this._kosi.localRotation;

            IKSolver_Patches.onPostUpdate += this.IKSolverOnPostUpdate;
            IKExecutionOrder_Patches.onPostLateUpdate += this.IKExecutionOrderOnPostLateUpdate;
            FKCtrl_Patches.onPreLateUpdate += this.FKCtrlOnPreLateUpdate;
#if HONEYSELECT
            CharBody_Patches.onPostLateUpdate += this.CharBodyOnPostLateUpdate;
#elif PLAYHOME
            Expression_Patches.onPostLateUpdate += this.ExpressionOnPostLateUpdate;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            Character_Patches.onPostLateUpdate += this.CharacterOnPostLateUpdate;
#endif
            OCIChar_ChangeChara_Patches.onChangeChara += this.OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile += this.OnLoadClothesFile;
#if HONEYSELECT || KOIKATSU
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo += this.OnCoordinateReplaced;
#endif

            this.crotchJointCorrection = MainWindow._self.crotchCorrectionByDefault;
            this.leftFootJointCorrection = MainWindow._self.anklesCorrectionByDefault;
            this.rightFootJointCorrection = MainWindow._self.anklesCorrectionByDefault;
        }

        protected override void Start()
        {
            base.Start();
            this._cachedSpineStiffness = this._body.solver.spineStiffness;
            this._cachedPullBodyVertical = this._body.solver.pullBodyVertical;

            if (this._optimizeIK)
            {
                this._body.solver.spineStiffness = 0f;
                this._body.solver.pullBodyVertical = 0f;
            }
            else
            {
                this._body.solver.spineStiffness = this._cachedSpineStiffness;
                this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (this._target.ikEnabled == false)
            {
                if (this._scheduleNextIKPostUpdate != null)
                {
                    Action tempAction = this._scheduleNextIKPostUpdate;
                    this._scheduleNextIKPostUpdate = null;
                    tempAction();
                }

                this.InitJointCorrection();
            }
        }

        protected override void OnDestroy()
        {
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            IKSolver_Patches.onPostUpdate -= this.IKSolverOnPostUpdate;
            IKExecutionOrder_Patches.onPostLateUpdate -= this.IKExecutionOrderOnPostLateUpdate;
            FKCtrl_Patches.onPreLateUpdate -= this.FKCtrlOnPreLateUpdate;
#if HONEYSELECT
            CharBody_Patches.onPostLateUpdate -= this.CharBodyOnPostLateUpdate;
#elif PLAYHOME
            Expression_Patches.onPostLateUpdate -= this.ExpressionOnPostLateUpdate;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            Character_Patches.onPostLateUpdate -= this.CharacterOnPostLateUpdate;
#endif
            OCIChar_ChangeChara_Patches.onChangeChara -= this.OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile -= this.OnLoadClothesFile;
#if HONEYSELECT || KOIKATSU
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo -= this.OnCoordinateReplaced;
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
                this.optimizeIK = other2.optimizeIK;
                this.crotchJointCorrection = other2.crotchJointCorrection;
                this.leftFootJointCorrection = other2.leftFootJointCorrection;
                this.rightFootJointCorrection = other2.rightFootJointCorrection;
                if (this._target.isFemale)
                    this._boobsEditor.LoadFrom(other2._boobsEditor);
            }
        }



        public bool IsPartEnabled(FullBodyBipedEffector part)
        {
            return this._target.ikEnabled && this._target.ociChar.listIKTarget[_effectorToIndex[part]].active;
        }

        public bool IsPartEnabled(FullBodyBipedChain part)
        {
            return this._target.ikEnabled && this._target.ociChar.listIKTarget[_chainToIndex[part]].active;
        }

        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        {
            OCIChar.IKInfo info = this._target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (this._target.ikEnabled && info.active)
            {
                if (this._currentDragType != DragType.None)
                {
                    if (this._oldRotValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldRotValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.rot);
                    info.guideObject.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            OCIChar.IKInfo info = this._target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (!this._target.ikEnabled || info.active == false)
                return Quaternion.identity;
            return info.guideObject.transformTarget.localRotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition, bool world = true)
        {
            OCIChar.IKInfo info = this._target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (this._target.ikEnabled && info.active)
            {
                if (this._currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldPosValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.pos);
                    info.guideObject.changeAmount.pos = world ? info.guideObject.transformTarget.parent.InverseTransformPoint(targetPosition) : targetPosition;
                }
            }
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type, bool world = true)
        {
            OCIChar.IKInfo info = this._target.ociChar.listIKTarget[_effectorToIndex[type]];
            if (!this._target.ikEnabled || info.active == false)
                return Vector3.zero;
            return world ? info.guideObject.transformTarget.position : info.guideObject.transformTarget.localPosition;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 targetPosition, bool world = true)
        {
            OCIChar.IKInfo info = this._target.ociChar.listIKTarget[_chainToIndex[type]];
            if (this._target.ikEnabled && info.active)
            {
                if (this._currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldPosValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.pos);
                    info.guideObject.changeAmount.pos = world ? info.guideObject.transformTarget.parent.InverseTransformPoint(targetPosition) : targetPosition;
                }
            }
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type, bool world = true)
        {
            OCIChar.IKInfo info = this._target.ociChar.listIKTarget[_chainToIndex[type]];
            if (!this._target.ikEnabled || info.active == false)
                return Vector3.zero;
            return world ? info.guideObject.transformTarget.position : info.guideObject.transformTarget.localPosition;
        }

        public void CopyLimbToTwin(FullBodyBipedChain ikLimb, OIBoneInfo.BoneGroup fkLimb)
        {
            this._scheduleNextIKPostUpdate = this.CopyLimbToTwinInternal;
            this._nextIKCopy = ikLimb;
            this._nextFKCopy = fkLimb;
        }

        public void CopyHandToTwin(OIBoneInfo.BoneGroup fkLimb)
        {
            this._scheduleNextIKPostUpdate = this.CopyHandToTwinInternal;
            this._nextFKCopy = fkLimb;
        }

        public void SwapPose()
        {
            this._scheduleNextIKPostUpdate = this.SwapPoseInternal;
        }
        #endregion

        #region Private Methods
        private void IKSolverOnPostUpdate(IKSolver solver)
        {
            if (this.enabled == false || this._body.solver != solver)
                return;
            if (this._scheduleNextIKPostUpdate != null)
            {
                Action tempAction = this._scheduleNextIKPostUpdate;
                this._scheduleNextIKPostUpdate = null;
                tempAction();
            }
            this.InitJointCorrection();
            foreach (AdvancedModeModule module in this._modules)
                module.IKSolverOnPostUpdate();
        }

        private void IKExecutionOrderOnPostLateUpdate()
        {
            if (this.enabled == false)
                return;
            foreach (AdvancedModeModule module in this._modules)
                module.IKExecutionOrderOnPostLateUpdate();
        }

        private void FKCtrlOnPreLateUpdate(FKCtrl ctrl)
        {
            if (this.enabled == false || this._target.ociChar.fkCtrl != ctrl)
                return;
            foreach (AdvancedModeModule module in this._modules)
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
            this.ApplyJointCorrection();
        }
#endif
        private void OnCharacterReplaced(OCIChar chara)
        {
            if (this._target.ociChar != chara)
                return;
            this._target.RefreshFKBones();
            foreach (AdvancedModeModule module in this._modules)
                module.OnCharacterReplaced();
        }
        private void OnLoadClothesFile(OCIChar chara)
        {
            if (this._target.ociChar != chara)
                return;
            this._target.RefreshFKBones();
            foreach (AdvancedModeModule module in this._modules)
                module.OnLoadClothesFile();
        }

#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        private void OnCoordinateReplaced(OCIChar chara, CharDefine.CoordinateType type, bool force)
#elif KOIKATSU
        private void OnCoordinateReplaced(OCIChar chara, ChaFileDefine.CoordinateType type, bool force)
#endif
        {
            if (this._target.ociChar != chara)
                return;
            this._target.RefreshFKBones();
            foreach (AdvancedModeModule module in this._modules)
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
            this.StartDrag(DragType.Both);
            this._lockDrag = true;
            HashSet<OIBoneInfo.BoneGroup> fkTwinLimb = new HashSet<OIBoneInfo.BoneGroup>();
            switch (this._nextFKCopy)
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

            this._additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            foreach (OCIChar.BoneInfo bone in this._target.ociChar.listBones)
            {
                Transform twinBoneTransform = null;
                Transform boneTransform = bone.guideObject.transformTarget;
                if (fkTwinLimb.Contains(bone.boneGroup) == false)
                    continue;
                twinBoneTransform = this._bonesEditor.GetTwinBone(boneTransform);
                if (twinBoneTransform == null)
                    twinBoneTransform = boneTransform;

                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && this._target.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
                {
                    if (twinBoneTransform == boneTransform)
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = rot.eulerAngles;

                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
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
                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                }
            }

            FullBodyBipedChain limb = this._nextIKCopy;
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
                    effectorSrcRealBone = this._body.references.leftHand;
                    effectorDestRealBone = this._body.references.rightHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = FullBodyBipedEffector.LeftFoot;
                    effectorSrcRealBone = this._body.references.leftFoot;
                    effectorDestRealBone = this._body.references.rightFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorSrcRealBone = this._body.references.rightHand;
                    effectorDestRealBone = this._body.references.leftHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = FullBodyBipedEffector.RightFoot;
                    effectorSrcRealBone = this._body.references.rightFoot;
                    effectorDestRealBone = this._body.references.leftFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorSrcRealBone = null;
                    effectorDestRealBone = null;
                    root = null;
                    break;
            }
            bendGoalSrc = limb;
            bendGoalDest = this.GetTwinChain(limb);
            effectorDest = this.GetTwinEffector(effectorSrc);

            Vector3 localPos = root.InverseTransformPoint(this._target.ociChar.listIKTarget[_effectorToIndex[effectorSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);
            localPos = root.InverseTransformPoint(this._target.ociChar.listIKTarget[_chainToIndex[bendGoalSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);


            this.SetBoneTargetPosition(effectorDest, effectorPosition);
            this.SetBendGoalPosition(bendGoalDest, bendGoalPosition);
            this.SetBoneTargetRotation(effectorDest, this.GetBoneTargetRotation(effectorDest));

            this._scheduleNextIKPostUpdate = () =>
            {
                Quaternion rot = effectorSrcRealBone.localRotation;
                rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                effectorDestRealBone.localRotation = rot; //Setting real bone local rotation
                OCIChar.IKInfo effectorDestInfo = this._target.ociChar.listIKTarget[_effectorToIndex[effectorDest]];
                effectorDestInfo.guideObject.transformTarget.rotation = effectorDestRealBone.rotation; //Using real bone rotation to set IK target rotation;
                this.SetBoneTargetRotation(effectorDest, effectorDestInfo.guideObject.transformTarget.localRotation); //Setting again the IK target with its own local rotation through normal means so it isn't ignored by neo while saving
                this._lockDrag = false;
                this.StopDrag();
            };
        }

        private void CopyHandToTwinInternal()
        {
            this.StartDrag(DragType.Both);
            this._lockDrag = true;
            HashSet<OIBoneInfo.BoneGroup> fkTwinLimb = new HashSet<OIBoneInfo.BoneGroup>();
            switch (this._nextFKCopy)
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
            this._additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            foreach (OCIChar.BoneInfo bone in this._target.ociChar.listBones)
            {
                Transform twinBoneTransform = null;
                Transform boneTransform = bone.guideObject.transformTarget;
                if (fkTwinLimb.Contains(bone.boneGroup) == false)
                    continue;
                twinBoneTransform = this._bonesEditor.GetTwinBone(boneTransform);
                if (twinBoneTransform == null)
                    twinBoneTransform = boneTransform;

                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && this._target.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
                {
                    if (twinBoneTransform == boneTransform)
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = rot.eulerAngles;

                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
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
                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                }
            }

            this._lockDrag = false;
            this.StopDrag();
        }

        private void SwapPoseInternal()
        {
            this.StartDrag(DragType.Both);
            this._lockDrag = true;

            this._additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            HashSet<Transform> done = new HashSet<Transform>();
            foreach (OCIChar.BoneInfo bone in this._target.ociChar.listBones)
            {
                Transform twinBoneTransform = null;
                Transform boneTransform = bone.guideObject.transformTarget;
                switch (bone.boneGroup)
                {
                    case OIBoneInfo.BoneGroup.Hair:
                        continue;

                    case OIBoneInfo.BoneGroup.Skirt:
                        twinBoneTransform = this.GetSkirtTwinBone(boneTransform);
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
                        twinBoneTransform = this._bonesEditor.GetTwinBone(boneTransform);
                        if (twinBoneTransform == null)
                            twinBoneTransform = boneTransform;
                        break;
                }
                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && this._target.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
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

                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
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

                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot

                        });
                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
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
                        FullBodyBipedEffector twin = this.GetTwinEffector(pair.Key);
                        Vector3 position = this._target.ociChar.listIKTarget[pair.Value].guideObject.transformTarget.localPosition;
                        position.x *= -1f;
                        Vector3 twinPosition = this._target.ociChar.listIKTarget[_effectorToIndex[twin]].guideObject.transformTarget.localPosition;
                        twinPosition.x *= -1f;
                        this.SetBoneTargetPosition(pair.Key, twinPosition, false);
                        this.SetBoneTargetPosition(twin, position, false);
                        break;
                }
            }

            foreach (KeyValuePair<FullBodyBipedChain, int> pair in _chainToIndex)
            {
                switch (pair.Key)
                {
                    case FullBodyBipedChain.LeftArm:
                    case FullBodyBipedChain.LeftLeg:
                        FullBodyBipedChain twin = this.GetTwinChain(pair.Key);
                        Vector3 position = this._target.ociChar.listIKTarget[pair.Value].guideObject.transformTarget.localPosition;
                        position.x *= -1f;
                        Vector3 twinPosition = this._target.ociChar.listIKTarget[_chainToIndex[twin]].guideObject.transformTarget.localPosition;
                        twinPosition.x *= -1f;
                        this.SetBendGoalPosition(pair.Key, twinPosition, false);
                        this.SetBendGoalPosition(twin, position, false);
                        break;
                }
            }

            this._scheduleNextIKPostUpdate = () =>
            {
                foreach (KeyValuePair<FullBodyBipedEffector, int> pair in _effectorToIndex)
                {
                    switch (pair.Key)
                    {
                        case FullBodyBipedEffector.LeftHand:
                        case FullBodyBipedEffector.LeftFoot:
                            FullBodyBipedEffector twin = this.GetTwinEffector(pair.Key);
                            Quaternion rot = this._target.ociChar.listIKTarget[pair.Value].guideObject.transformTarget.localRotation;
                            rot = new Quaternion(-rot.x, rot.y, rot.z, -rot.w);
                            Quaternion twinRot = this._target.ociChar.listIKTarget[_effectorToIndex[twin]].guideObject.transformTarget.localRotation;
                            twinRot = new Quaternion(-twinRot.x, twinRot.y, twinRot.z, -twinRot.w);
                            this.SetBoneTargetRotation(pair.Key, twinRot);
                            this.SetBoneTargetRotation(twin, rot);
                            break;
                    }
                }

                this._lockDrag = false;
                this.StopDrag();
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
            return this.transform.FindDescendant(newName);
        }

        private void InitJointCorrection()
        {
            if (this.crotchJointCorrection)
            {
                this._siriDamLRotation = Quaternion.Lerp(Quaternion.identity, this._body.solver.leftLegMapping.bone1.localRotation, 0.4f);
                this._siriDamRRotation = Quaternion.Lerp(Quaternion.identity, this._body.solver.rightLegMapping.bone1.localRotation, 0.4f);
                this._kosiRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.localRotation, this._body.solver.rightLegMapping.bone1.localRotation, 0.5f), 0.25f);
            }

            if (this.leftFootJointCorrection)
                this._leftFoot2Rotation = Mathf.LerpAngle(0f, this._leftFoot2.parent.localRotation.eulerAngles.x - this._leftFoot2ParentOriginalRotation, 0.9f);

            if (this.rightFootJointCorrection)
                this._rightFoot2Rotation = Mathf.LerpAngle(0f, this._rightFoot2.parent.localRotation.eulerAngles.x - this._rightFoot2ParentOriginalRotation, 0.9f);
        }

        private void ApplyJointCorrection()
        {
            if (this.crotchJointCorrection)
            {
                this._siriDamL.localRotation = this._siriDamLRotation;
                this._siriDamR.localRotation = this._siriDamRRotation;
                this._kosi.localRotation = this._kosiRotation;
            }
            else if (this._lastCrotchJointCorrection)
            {
                this._siriDamL.localRotation = this._siriDamLOriginalRotation;
                this._siriDamR.localRotation = this._siriDamROriginalRotation;
                this._kosi.localRotation = this._kosiOriginalRotation;
            }

            if (this.leftFootJointCorrection)
                this._leftFoot2.localRotation = Quaternion.AngleAxis(this._leftFoot2Rotation, Vector3.right);
            else if (this._lastLeftFootJointCorrection)
                this._leftFoot2.localRotation = this._leftFoot2OriginalRotation;

            if (this.rightFootJointCorrection)
                this._rightFoot2.localRotation = Quaternion.AngleAxis(this._rightFoot2Rotation, Vector3.right);
            else if (this._lastrightFootJointCorrection)
                this._rightFoot2.localRotation = this._rightFoot2OriginalRotation;

            this._lastCrotchJointCorrection = this.crotchJointCorrection;
            this._lastLeftFootJointCorrection = this.leftFootJointCorrection;
            this._lastrightFootJointCorrection = this.rightFootJointCorrection;
        }
        #endregion

        #region Saves


        public override void SaveXml(XmlTextWriter xmlWriter)
        {
            if (this.optimizeIK == false)
            {
                xmlWriter.WriteAttributeString("optimizeIK", XmlConvert.ToString(this.optimizeIK));
            }
            xmlWriter.WriteAttributeString("crotchCorrection", XmlConvert.ToString(this.crotchJointCorrection));
            xmlWriter.WriteAttributeString("leftAnkleCorrection", XmlConvert.ToString(this.leftFootJointCorrection));
            xmlWriter.WriteAttributeString("rightAnkleCorrection", XmlConvert.ToString(this.rightFootJointCorrection));
            base.SaveXml(xmlWriter);
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            this.optimizeIK = xmlNode.Attributes?["optimizeIK"] == null || XmlConvert.ToBoolean(xmlNode.Attributes["optimizeIK"].Value);
            this.crotchJointCorrection = xmlNode.Attributes?["crotchCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["crotchCorrection"].Value);
            this.leftFootJointCorrection = xmlNode.Attributes?["leftAnkleCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["leftAnkleCorrection"].Value);
            this.rightFootJointCorrection = xmlNode.Attributes?["rightAnkleCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["rightAnkleCorrection"].Value);
            return base.LoadXml(xmlNode);
        }
        #endregion
    }
}