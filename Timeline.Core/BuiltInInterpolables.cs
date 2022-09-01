using Studio;
using System.Collections.Generic;
using ToolBox.Extensions;
using UnityEngine;
#if AISHOUJO || HONEYSELECT2
using AIChara;
#elif KOIKATSU || SUNSHINE
using Sideloader.AutoResolver;
using System.Linq;
#endif

namespace Timeline
{
    public static class BuiltInInterpolables
    {
        //TODO GLOBAL
        // Other plugins compatible

        // dark theme
        // loop the whole thing (with curves and stuff)
        // VideoExport events on the timeline

        // DONE

        public static void Populate()
        {
            Global();
            EnabledDisabled();
            Animation();
            TranslateRotationScale();

            CharacterClothingStates();
            CharacterJuice();
            CharacterStateMisc();
            CharacterNeck();
            CharacterEyesMouthHands();

            Item();

            Light();

            //Timeline. AddInterpolableModel(new InterpolableModel(
            //        owner: Timeline._ownerId,
            //        id: "debug",
            //        parameter: null,
            //        name: "Print Debug",
            //        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => UnityEngine.Debug.LogError(message: "Interpolating Before " + Time.frameCount + " " + factor),
            //        interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => UnityEngine.Debug.LogError(message: "Interpolating After " + Time.frameCount + " " + factor),
            //        isCompatibleWithTarget: (oci) => true,
            //        getValue: (oci, parameter) => null,
            //        readParameterFromXml: null,
            //        writeParameterToXml: null,
            //        readValueFromXml: null,
            //        writeValueToXml: null,
            //        useOciInHash: false
            //));
        }

        private static void Global()
        {
            Studio.CameraControl.CameraData globalCameraData = (Studio.CameraControl.CameraData)Studio.Studio.Instance.cameraCtrl.GetPrivate("cameraData");

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "cameraOPos",
                    parameter: null,
                    name: "Camera Origin Position",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => globalCameraData.pos = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => globalCameraData.pos,
                    readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                    useOciInHash: false
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "cameraORot",
                    parameter: null,
                    name: "Camera Origin Rotation",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => globalCameraData.rotate = Quaternion.SlerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor).eulerAngles,
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => Quaternion.Euler(globalCameraData.rotate),
                    readValueFromXml: (parameter, node) => node.ReadQuaternion("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Quaternion)o),
                    useOciInHash: false
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "cameraOZoom",
                    parameter: null,
                    name: "Camera Zoom",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => globalCameraData.distance = new Vector3(x: globalCameraData.distance.x, y: globalCameraData.distance.y, z: Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => globalCameraData.distance.z,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                    useOciInHash: false
            ));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "cameraFOV",
                    parameter: null,
                    name: "Camera FOV",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => Studio.Studio.Instance.cameraCtrl.fieldOfView = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => Studio.Studio.Instance.cameraCtrl.fieldOfView,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                    useOciInHash: false
            ));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "cameraPos",
                    parameter: null,
                    name: "Camera Position",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor);
                        UpdateCameraData(Studio.Studio.Instance.cameraCtrl.mainCmaera.transform);
                    },
                    interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor),
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position,
                    readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                    useOciInHash: false
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "cameraRot",
                    parameter: null,
                    name: "Camera Rotation",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.rotation = Quaternion.SlerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor);
                        UpdateCameraData(Studio.Studio.Instance.cameraCtrl.mainCmaera.transform);
                    },
                    interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.rotation = Quaternion.SlerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor),
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.rotation,
                    readValueFromXml: (parameter, node) => node.ReadQuaternion("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Quaternion)o),
                    useOciInHash: false
            ));

            void UpdateCameraData(Transform cameraTransform)
            {
                globalCameraData.rotate = cameraTransform.localRotation.eulerAngles;
                globalCameraData.pos = -(cameraTransform.localRotation * globalCameraData.distance - cameraTransform.localPosition);

            }

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "timeScale",
                    parameter: null,
                    name: "Time Scale",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => Time.timeScale = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor),
                    interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => Time.timeScale = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor),
                    isCompatibleWithTarget: (oci) => true,
                    getValue: (oci, parameter) => Time.timeScale,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o),
                    useOciInHash: false
            ));
        }

        private static void EnabledDisabled()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "objectEnabled",
                    parameter: null,
                    name: "Enabled",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        bool value = (bool)leftValue;
                        if (oci.treeNodeObject.visible != value)
                            oci.treeNodeObject.visible = value;
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci != null && oci is OCILight == false,
                    getValue: (oci, parameter) => oci.treeNodeObject.visible,
                    readValueFromXml: (parameter, node) => node.ReadBool("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o)
            ));
        }

        private static void TranslateRotationScale()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "guideObjectPos",
                    name: "Selected GuideObject Pos",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((GuideObject)parameter).changeAmount.pos = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor),
                    interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => ((GuideObject)parameter).changeAmount.pos = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor),
                    isCompatibleWithTarget: oci => oci != null,
                    getValue: (oci, parameter) => ((GuideObject)parameter).changeAmount.pos,
                    readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                    getParameter: oci => GuideObjectManager.Instance.selectObject,
                    readParameterFromXml: (oci, node) =>
                    {
                        Transform t = oci.guideObject.transformTarget.Find(node.Attributes["guideObjectPath"].Value);
                        if (t == null)
                            return null;
                        GuideObject guideObject;
                        Timeline._self._allGuideObjects.TryGetValue(t, out guideObject);
                        return guideObject;
                    },
                    writeParameterToXml: (oci, writer, o) => writer.WriteAttributeString("guideObjectPath", ((GuideObject)o).transformTarget.GetPathFrom(oci.guideObject.transformTarget)),
                    checkIntegrity: (oci, parameter, leftValue, rightValue) => parameter != null,
                    getFinalName: (name, oci, parameter) => $"GO Position ({((GuideObject)parameter).transformTarget.name})"
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "guideObjectRot",
                    name: "Selected GuideObject Rot",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((GuideObject)parameter).changeAmount.rot = Quaternion.SlerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor).eulerAngles,
                    interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => ((GuideObject)parameter).changeAmount.rot = Quaternion.SlerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor).eulerAngles,
                    isCompatibleWithTarget: (oci) => oci != null,
                    getValue: (oci, parameter) => Quaternion.Euler(((GuideObject)parameter).changeAmount.rot),
                    readValueFromXml: (parameter, node) => node.ReadQuaternion("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Quaternion)o),
                    getParameter: oci => GuideObjectManager.Instance.selectObject,
                    readParameterFromXml: (oci, node) =>
                    {
                        Transform t = oci.guideObject.transformTarget.Find(node.Attributes["guideObjectPath"].Value);
                        if (t == null)
                            return null;
                        GuideObject guideObject;
                        Timeline._self._allGuideObjects.TryGetValue(t, out guideObject);
                        return guideObject;
                    },
                    writeParameterToXml: (oci, writer, o) => writer.WriteAttributeString("guideObjectPath", ((GuideObject)o).transformTarget.GetPathFrom(oci.guideObject.transformTarget)),
                    checkIntegrity: (oci, parameter, leftValue, rightValue) => parameter != null,
                    getFinalName: (name, oci, parameter) => $"GO Rotation ({((GuideObject)parameter).transformTarget.name})"
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "guideObjectScale",
                    name: "Selected GuideObject Scl",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((GuideObject)parameter).changeAmount.scale = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor),
                    interpolateAfter: (oci, parameter, leftValue, rightValue, factor) => ((GuideObject)parameter).changeAmount.scale = Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor),
                    isCompatibleWithTarget: (oci) => oci != null,
                    getValue: (oci, parameter) => ((GuideObject)parameter).changeAmount.scale,
                    readValueFromXml: (parameter, node) => node.ReadVector3("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector3)o),
                    getParameter: oci => GuideObjectManager.Instance.selectObject,
                    readParameterFromXml: (oci, node) =>
                    {
                        Transform t = oci.guideObject.transformTarget.Find(node.Attributes["guideObjectPath"].Value);
                        if (t == null)
                            return null;
                        GuideObject guideObject;
                        Timeline._self._allGuideObjects.TryGetValue(t, out guideObject);
                        return guideObject;
                    },
                    writeParameterToXml: (oci, writer, o) => writer.WriteAttributeString("guideObjectPath", ((GuideObject)o).transformTarget.GetPathFrom(oci.guideObject.transformTarget)),
                    checkIntegrity: (oci, parameter, leftValue, rightValue) => parameter != null,
                    getFinalName: (name, oci, parameter) => $"GO Scale ({((GuideObject)parameter).transformTarget.name})"
            ));
        }

        private static void Animation()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "charAnimation",
                    parameter: null,
                    name: "Animation",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        OCIChar chara = (OCIChar)oci;
                        OICharInfo.AnimeInfo info = (OICharInfo.AnimeInfo)leftValue;
                        if (chara.oiCharInfo.animeInfo.group != info.group || chara.oiCharInfo.animeInfo.category != info.category || chara.oiCharInfo.animeInfo.no != info.no)
                            chara.LoadAnime(info.group, info.category, info.no);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) =>
                    {
                        OICharInfo.AnimeInfo info = ((OCIChar)oci).oiCharInfo.animeInfo;
                        return new OICharInfo.AnimeInfo() { group = info.group, category = info.category, no = info.no };
                    },
#if KOIKATSU || SUNSHINE
                    readValueFromXml: (parameter, node) =>
                    {
                        string nodeGUID = node.Attributes?["GUID"]?.InnerText;
                        int nodeGr = node.ReadInt("valueGroup");
                        int nodeCa = node.ReadInt("valueCategory");
                        int nodeNo = node.ReadInt("valueNo");                        
                        StudioResolveInfo resolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.Slot == nodeNo && x.GUID == nodeGUID && x.Group == nodeGr && x.Category == nodeCa);
                        return nodeNo >= UniversalAutoResolver.BaseSlotID && resolveInfo == null
                            ? new OICharInfo.AnimeInfo() { group = 0, category = 0, no = 0 } // This prevents some of the potential LoadAnime spam
                            : new OICharInfo.AnimeInfo() { group = nodeGr, category = nodeCa, no = resolveInfo != null ? resolveInfo.LocalSlot : nodeNo };
                    },
                    writeValueToXml: (parameter, writer, o) =>
                    {
                        OICharInfo.AnimeInfo info = (OICharInfo.AnimeInfo)o;
                        StudioResolveInfo resolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.LocalSlot == info.no && x.Group == info.group && x.Category == info.category);
                        writer.WriteAttributeString("GUID", info.no >= UniversalAutoResolver.BaseSlotID && resolveInfo != null ? resolveInfo.GUID : "");
                        writer.WriteValue("valueGroup", info.group);
                        writer.WriteValue("valueCategory", info.category);
                        writer.WriteValue("valueNo", info.no >= UniversalAutoResolver.BaseSlotID && resolveInfo != null ? resolveInfo.Slot : info.no);
                    }
#else               // AI&HS2 Studio use original ID(management number) for animation zipmods by default
                    readValueFromXml: (parameter, node) => new OICharInfo.AnimeInfo() { category = node.ReadInt("valueCategory"), group = node.ReadInt("valueGroup"), no = node.ReadInt("valueNo") },
                    writeValueToXml: (parameter, writer, o) =>
                    {
                        OICharInfo.AnimeInfo info = (OICharInfo.AnimeInfo)o;
                        writer.WriteValue("valueCategory", info.category);
                        writer.WriteValue("valueGroup", info.group);
                        writer.WriteValue("valueNo", info.no);
                    }
#endif
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "objectAnimationSpeed",
                    parameter: null,
                    name: "Animation Speed",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => oci.animeSpeed = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci != null,
                    getValue: (oci, parameter) => oci.animeSpeed,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "charAnimationPattern",
                    parameter: null,
                    name: "Animation Pattern",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).animePattern = Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).animePattern,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemAnimationTime",
                    parameter: null,
                    name: "Animation Time",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        Animator animator = ((OCIItem)oci).animator;
                        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                        animator.Play(info.shortNameHash, 0, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) =>
                    {
                        OCIItem item = oci as OCIItem;
                        return item != null && item.isAnime;
                    },
                    getValue: (oci, parameter) => ((OCIItem)oci).animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)
            ));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "charAnimationTime",
                    parameter: null,
                    name: "Animation Time",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        Animator animator = ((OCIChar)oci).charAnimeCtrl.animator;
                        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                        animator.Play(info.shortNameHash, 0, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
                        //animator.Update(0f);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).charAnimeCtrl.animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)
            ));
        }
#if HONEYSELECT
        
        private static void CharacterClothingStates()
        {
            Dictionary<CharDefine.ClothesStateKindFemale, string> femaleClothes = new Dictionary<CharDefine.ClothesStateKindFemale, string>()
            {
                {CharDefine.ClothesStateKindFemale.top, "Top"},
                {CharDefine.ClothesStateKindFemale.bot, "Bottom"},
                {CharDefine.ClothesStateKindFemale.bra, "Bra"},
                {CharDefine.ClothesStateKindFemale.shorts, "Panties"},
                {CharDefine.ClothesStateKindFemale.swimsuitTop, "Upper Swimsuit"},
                {CharDefine.ClothesStateKindFemale.swimsuitBot, "Lower Swimsuit"},
                {CharDefine.ClothesStateKindFemale.swimClothesTop, "Swim Top"},
                {CharDefine.ClothesStateKindFemale.swimClothesBot, "Swim Bottom"},
                {CharDefine.ClothesStateKindFemale.gloves, "Gloves"},
                {CharDefine.ClothesStateKindFemale.panst, "Pantyhose"},
                {CharDefine.ClothesStateKindFemale.socks, "Socks"},
                {CharDefine.ClothesStateKindFemale.shoes, "Shoes"},
            };
            foreach (KeyValuePair<CharDefine.ClothesStateKindFemale, string> pair in femaleClothes)
            {
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: "femaleClothes",
                        parameter: (int)pair.Key,
                        name: $"{pair.Value} State",
                        interpolateBefore: InterpolateClothes,
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                        getValue: GetClothesValue,
                        readValueFromXml: (parameter, node) => node.ReadByte("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o),
                        readParameterFromXml: (oci, node) => node.ReadInt("parameter"),
                        writeParameterToXml: (oci, writer, o) => writer.WriteValue("parameter", (int)o),
                        getFinalName: (n, oci, parameter) => $"{femaleClothes[(CharDefine.ClothesStateKindFemale)(int)parameter]} State"
                        ));
            }

            Dictionary<CharDefine.ClothesStateKindMale, string> maleClothes = new Dictionary<CharDefine.ClothesStateKindMale, string>()
            {
                {CharDefine.ClothesStateKindMale.clothes, "Clothes"},
                {CharDefine.ClothesStateKindMale.shoes, "Shoes"},
            };

            foreach (KeyValuePair<CharDefine.ClothesStateKindMale, string> pair in maleClothes)
            {
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: "maleClothes",
                        parameter: (int)pair.Key,
                        name: $"{pair.Value} State",
                        interpolateBefore: InterpolateClothes,
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCICharMale,
                        getValue: GetClothesValue,
                        readValueFromXml: (parameter, node) => node.ReadByte("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o),
                        readParameterFromXml: (oci, node) => node.ReadInt("parameter"),
                        writeParameterToXml: (oci, writer, o) => writer.WriteValue("parameter", (int)o),
                        getFinalName: (n, oci, parameter) => $"{maleClothes[(CharDefine.ClothesStateKindMale)(int)parameter]} State"

                ));
            }
        }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        private static void CharacterClothingStates()
        {
#if KOIKATSU
            Dictionary<ChaFileDefine.ClothesKind, string> clothes = new Dictionary<ChaFileDefine.ClothesKind, string>()
            {
                {ChaFileDefine.ClothesKind.top, "Top"},
                {ChaFileDefine.ClothesKind.bot, "Bottom"},
                {ChaFileDefine.ClothesKind.bra, "Bra"},
                {ChaFileDefine.ClothesKind.shorts, "Panties"},
                {ChaFileDefine.ClothesKind.gloves, "Gloves"},
                {ChaFileDefine.ClothesKind.panst, "Pantyhose"},
                {ChaFileDefine.ClothesKind.socks, "Legwear"},
                {ChaFileDefine.ClothesKind.shoes_inner, "Shoes Inside"},
                {ChaFileDefine.ClothesKind.shoes_outer, "Shoes Outside"},
            };
#else
            Dictionary<ChaFileDefine.ClothesKind, string> clothes = new Dictionary<ChaFileDefine.ClothesKind, string>()
            {
                {ChaFileDefine.ClothesKind.top, "Top"},
                {ChaFileDefine.ClothesKind.bot, "Bottom"},
                {ChaFileDefine.ClothesKind.inner_t, "Bra"},
                {ChaFileDefine.ClothesKind.inner_b, "Panties"},
                {ChaFileDefine.ClothesKind.gloves, "Gloves"},
                {ChaFileDefine.ClothesKind.panst, "Pantyhose"},
                {ChaFileDefine.ClothesKind.socks, "Legwear"},
                {ChaFileDefine.ClothesKind.shoes, "Shoes"},
            };
#endif
            foreach (KeyValuePair<ChaFileDefine.ClothesKind, string> pair in clothes)
            {
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: "charClothes",
                        parameter: (int)pair.Key,
                        name: $"{pair.Value} State",
                        interpolateBefore: InterpolateClothes,
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIChar,
                        getValue: GetClothesValue,
                        readValueFromXml: (parameter, node) => node.ReadByte("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o),
                        readParameterFromXml: (oci, node) => node.ReadInt("parameter"),
                        writeParameterToXml: (oci, writer, o) => writer.WriteValue("parameter", (int)o),
                        getFinalName: (n, oci, parameter) => $"{clothes[(ChaFileDefine.ClothesKind)(int)parameter]} State"
                        ));
            }
        }
#endif

        private static void InterpolateClothes(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue, float factor)
        {
            int index = (int)parameter;
            byte value = (byte)leftValue;
            if ((byte)GetClothesValue(oci, parameter) != value)
                ((OCIChar)oci).SetClothesState(index, value);
        }

        private static object GetClothesValue(ObjectCtrlInfo oci, object parameter)
        {
#if HONEYSELECT
            return ((OCIChar)oci).charFileInfoStatus.clothesState[(int)parameter];
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            return ((OCIChar)oci).charFileStatus.clothesState[(int)parameter];
#endif
        }

#if HONEYSELECT
        private static void CharacterJuice()
        {
            Dictionary<CharDefine.SiruObjKind, string> juice = new Dictionary<CharDefine.SiruObjKind, string>()
            {
                {CharDefine.SiruObjKind.top, "Top"},
                {CharDefine.SiruObjKind.bot, "Bottom"},
                {CharDefine.SiruObjKind.bra, "Bra"},
                {CharDefine.SiruObjKind.shorts, "Panties"},
                {CharDefine.SiruObjKind.swim, "Swimsuit"},
            };

            foreach (KeyValuePair<CharDefine.SiruObjKind, string> pair in juice)
            {
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: "juice",
                        parameter: (int)pair.Key,
                        name: $"{pair.Value} Juice State",
                        interpolateBefore: InterpolateJuice,
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                        getValue: GetJuiceValue,
                        readValueFromXml: (parameter, node) => node.ReadByte("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o),
                        readParameterFromXml: (oci, node) => node.ReadInt("parameter"),
                        writeParameterToXml: (oci, writer, o) => writer.WriteValue("parameter", (int)o)
                ));
            }
        }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        private static void CharacterJuice()
        {
#if KOIKATSU
            Dictionary<ChaFileDefine.SiruParts, string> juice = new Dictionary<ChaFileDefine.SiruParts, string>()
            {
                {ChaFileDefine.SiruParts.SiruKao, "Face"},
                {ChaFileDefine.SiruParts.SiruFrontUp, "Chest"},
                {ChaFileDefine.SiruParts.SiruFrontDown, "Stomach"},
                {ChaFileDefine.SiruParts.SiruBackUp, "Back"},
                {ChaFileDefine.SiruParts.SiruBackDown, "Butt"},
            };
#else
            Dictionary<ChaFileDefine.SiruParts, string> juice = new Dictionary<ChaFileDefine.SiruParts, string>()
            {
                {ChaFileDefine.SiruParts.SiruKao, "Face"},
                {ChaFileDefine.SiruParts.SiruFrontTop, "Chest"},
                {ChaFileDefine.SiruParts.SiruFrontBot, "Stomach"},
                {ChaFileDefine.SiruParts.SiruBackTop, "Back"},
                {ChaFileDefine.SiruParts.SiruBackBot, "Butt"},
            };
#endif

            foreach (KeyValuePair<ChaFileDefine.SiruParts, string> pair in juice)
            {
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: "juice",
                        parameter: (int)pair.Key,
                        name: $"{pair.Value} Juice State",
                        interpolateBefore: InterpolateJuice,
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIChar,
                        getValue: GetJuiceValue,
                        readValueFromXml: (parameter, node) => node.ReadByte("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o),
                        readParameterFromXml: (oci, node) => node.ReadInt("parameter"),
                        writeParameterToXml: (oci, writer, o) => writer.WriteValue("parameter", (int)o)
                ));
            }
        }
#endif

        private static void InterpolateJuice(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue, float factor)
        {
#if HONEYSELECT
            CharDefine.SiruParts index = (CharDefine.SiruParts)(int)parameter;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            ChaFileDefine.SiruParts index = (ChaFileDefine.SiruParts)(int)parameter;
#endif
            byte value = (byte)leftValue;
            if ((byte)GetJuiceValue(oci, parameter) != value)
                ((OCIChar)oci).SetSiruFlags(index, value);
        }

        private static object GetJuiceValue(ObjectCtrlInfo oci, object parameter)
        {
#if HONEYSELECT
            return ((OCIChar)oci).GetSiruFlags((CharDefine.SiruParts)(int)parameter);
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            return ((OCIChar)oci).GetSiruFlags((ChaFileDefine.SiruParts)(int)parameter);
#endif
        }

#if HONEYSELECT
        private static void CharacterStateMisc()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "femaleTears",
                    parameter: null,
                    name: "Tears",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        byte value = (byte)leftValue;
                        if (((OCIChar)oci).GetTearsLv() != value)
                            ((OCIChar)oci).SetTearsLv(_state: value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                    getValue: (oci, parameter) => ((OCIChar)oci).GetTearsLv(),
                    readValueFromXml: (parameter, node) => node.ReadByte("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "femaleBlush",
                    parameter: null,
                    name: "Blush",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).SetHohoAkaRate(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                    getValue: (oci, parameter) => ((OCIChar)oci).GetHohoAkaRate(),
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "femaleNipples",
                    parameter: null,
                    name: "Nipples",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).SetNipStand(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                    getValue: (oci, parameter) => ((OCIChar)oci).oiCharInfo.nipple,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "femaleSkinShine",
                    parameter: null,
                    name: "Skin Shine",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).SetTuyaRate(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                    getValue: (oci, parameter) => ((OCIChar)oci).oiCharInfo.skinRate,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

        }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        private static void CharacterStateMisc()
        {
#if KOIKATSU
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "tears",
                    parameter: null,
                    name: "Tears",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        byte value = (byte)leftValue;
                        if (((OCIChar)oci).GetTearsLv() != value)
                            ((OCIChar)oci).SetTearsLv(value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).GetTearsLv(),
                    readValueFromXml: (parameter, node) => node.ReadByte("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (byte)o)));
#endif

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "blush",
                    parameter: null,
                    name: "Blush",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).SetHohoAkaRate(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).GetHohoAkaRate(),
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "femaleNipples",
                    parameter: null,
                    name: "Nipples",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).SetNipStand(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCICharFemale,
                    getValue: (oci, parameter) => ((OCIChar)oci).oiCharInfo.nipple,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
        }
#endif

        private static void CharacterNeck()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterNeck",
                    parameter: null,
                    name: "Neck Direction",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        int value = (int)leftValue;
#if HONEYSELECT
                        if (((OCIChar)oci).charFileInfoStatus.neckLookPtn != value)
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                        if (((OCIChar)oci).charFileStatus.neckLookPtn != value)
#endif
                            ((OCIChar)oci).ChangeLookNeckPtn(value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
#if HONEYSELECT
                    getValue: (oci, parameter) => ((OCIChar)oci).charFileInfoStatus.neckLookPtn,
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                    getValue: (oci, parameter) => ((OCIChar)oci).charFileStatus.neckLookPtn,
#endif
                    readValueFromXml: (parameter, node) => node.ReadInt("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o)));
        }

        private static void CharacterEyesMouthHands()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterEyes",
                    parameter: null,
                    name: "Eyes",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        int value = (int)leftValue;
                        if (((OCIChar)oci).charInfo.GetEyesPtn() != value)
                            ((OCIChar)oci).charInfo.ChangeEyesPtn(value, false);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).charInfo.GetEyesPtn(),
                    readValueFromXml: (parameter, node) => node.ReadInt("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterEyesOpen",
                    parameter: null,
                    name: "Eyes Open",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).ChangeEyesOpen(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).charInfo.GetEyesOpenMax(),
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
#if KOIKATSU
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterEyebrows",
                    parameter: null,
                    name: "Eyebrows",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        int value = (int)leftValue;
                        if (((OCIChar)oci).charInfo.GetEyebrowPtn() != value)
                            ((OCIChar)oci).charInfo.ChangeEyebrowPtn(value, false);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).charInfo.GetEyebrowPtn(),
                    readValueFromXml: (parameter, node) => node.ReadInt("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterEyebrowsOpen",
                    parameter: null,
                    name: "Eyebrows Open",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).charInfo.ChangeEyebrowOpenMax(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).charInfo.GetEyebrowOpenMax(),
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
#endif
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterMouth",
                    parameter: null,
                    name: "Mouth",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        int value = (int)leftValue;
                        if (((OCIChar)oci).charInfo.GetMouthPtn() != value)
                            ((OCIChar)oci).charInfo.ChangeMouthPtn(value, false);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).charInfo.GetMouthPtn(),
                    readValueFromXml: (parameter, node) => node.ReadInt("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterMouthOpen",
                    parameter: null,
                    name: "Mouth Open",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIChar)oci).ChangeMouthOpen(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).oiCharInfo.mouthOpen,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterLeftHand",
                    parameter: null,
                    name: "Left Hand",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        int value = (int)leftValue;
                        if (((OCIChar)oci).oiCharInfo.handPtn[0] != value)
                            ((OCIChar)oci).ChangeHandAnime(0, value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).oiCharInfo.handPtn[0],
                    readValueFromXml: (parameter, node) => node.ReadInt("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "characterRightHand",
                    parameter: null,
                    name: "Right Hand",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        int value = (int)leftValue;
                        if (((OCIChar)oci).oiCharInfo.handPtn[1] != value)
                            ((OCIChar)oci).ChangeHandAnime(1, value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIChar,
                    getValue: (oci, parameter) => ((OCIChar)oci).oiCharInfo.handPtn[1],
                    readValueFromXml: (parameter, node) => node.ReadInt("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (int)o)));
        }

#if HONEYSELECT
        private static void Item()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemColor",
                    parameter: null,
                    name: "Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color.rgbaDiffuse,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemSpecColor",
                    parameter: null,
                    name: "Specular Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetGloss(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color.rgbSpecular,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemMetallic",
                    parameter: null,
                    name: "Metallic",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetIntensity(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color.specularIntensity,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemSmoothness",
                    parameter: null,
                    name: "Smoothness",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetSharpness(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color.specularSharpness,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemColor2",
                    parameter: null,
                    name: "Color 2",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor2(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem && ((OCIItem)oci).isColor2,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color2.rgbaDiffuse,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemSpecColor2",
                    parameter: null,
                    name: "Specular Color 2",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetGloss2(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem && ((OCIItem)oci).isColor2,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color2.rgbSpecular,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemSmoothness2",
                    parameter: null,
                    name: "Smoothness 2",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetSharpness2(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem && ((OCIItem)oci).isColor2,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color2.specularSharpness,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
        }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
        private static void Item()
        {
            //TODO PanelComponent fields

            for (int index = 0; index < 3; ++index)
            {
                int i = index;
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: $"itemColor{i + 1}",
                        parameter: null,
                        name: $"Color {i + 1}",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor), i),
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIItem it && it.useColor[i],
#if KOIKATSU
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color[i],
#else
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.colors[i].mainColor,
#endif
                        readValueFromXml: (parameter, node) => node.ReadColor("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));

                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: $"itemPatternColor{i + 1}",
                        parameter: null,
                        name: $"Pattern Color {i + 1}",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor), i + 3),
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIItem it && it.useColor[i] && it.usePattern[i],
#if KOIKATSU
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color[i + 3],
#else
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.colors[i + 3].mainColor,
#endif
                        readValueFromXml: (parameter, node) => node.ReadColor("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: $"itemPatternUV{i + 1}",
                        parameter: null,
                        name: $"Pattern UV {i + 1}",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            OCIItem item = (OCIItem)oci;
#if KOIKATSU
                            item.itemInfo.pattern[i].uv = Vector4.LerpUnclamped((Vector4)leftValue, (Vector4)rightValue, factor);
#else
                            item.itemInfo.colors[i].pattern.uv = Vector4.LerpUnclamped((Vector4)leftValue, (Vector4)rightValue, factor);
#endif
                            item.UpdateColor();
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIItem it && it.useColor[i] && it.usePattern[i],
#if KOIKATSU
                            getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.pattern[i].uv,
#else
                            getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.colors[i].pattern.uv,
#endif
                        readValueFromXml: (parameter, node) => node.ReadVector4("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector4)o)));
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: $"itemPatternRotation{i + 1}",
                        parameter: null,
                        name: $"Pattern Rot {i + 1}",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetPatternRot(i, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIItem it && it.useColor[i] && it.usePattern[i],
#if KOIKATSU
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.pattern[i].rot,
#else
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.colors[i].pattern.rot,
#endif
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
                Timeline.AddInterpolableModel(new InterpolableModel(
                        owner: Timeline._ownerId,
                        id: $"itemPatternClamp{i + 1}",
                        parameter: null,
                        name: $"Pattern Tiling {i + 1}",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            OCIItem item = (OCIItem)oci;
#if KOIKATSU
                            if (item.itemInfo.pattern[i].clamp != (bool)leftValue)
#else
                        if (item.itemInfo.colors[i].pattern.clamp != (bool)leftValue)
#endif
                                item.SetPatternClamp(i, (bool)leftValue);
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: (oci) => oci is OCIItem it && it.useColor[i] && it.usePattern[i],
#if KOIKATSU
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.pattern[i].rot,
#else
                        getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.colors[i].pattern.rot,
#endif
                        readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                        writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
            }

            //TODO: Fix the rest
#if KOIKATSU
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemColor4",
                    parameter: null,
                    name: "Color 4",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor), 7),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.useColor4,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color[7],
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));


            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemShadowColor",
                    parameter: null,
                    name: "Shadow Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor), 6),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkShadow,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color[6],
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemAlpha",
                    parameter: null,
                    name: "Alpha",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetAlpha(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkAlpha,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.alpha,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemEmissionColor",
                    parameter: null,
                    name: "Emission Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetEmissionColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkEmission && it.checkEmissionColor,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.emissionColor,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemEmissionPower",
                    parameter: null,
                    name: "Emission Power",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetEmissionPower(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkEmission && it.checkEmissionPower,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.emissionPower,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemLightCancel",
                    parameter: null,
                    name: "Light Cancel",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetLightCancel(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkLightCancel,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.lightCancel,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemLineColor",
                    parameter: null,
                    name: "Line Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetLineColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkLine,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.lineColor,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemLineWidth",
                    parameter: null,
                    name: "Line Width",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetLineWidth(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkLine,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.lineWidth,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemPanelColor",
                    parameter: null,
                    name: "Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor), 0),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkPanel,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.color[0],
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));


            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemPanelPatternUV",
                    parameter: null,
                    name: "Pattern UV",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        OCIItem item = (OCIItem)oci;
                        item.itemInfo.pattern[0].uv = Vector4.LerpUnclamped((Vector4)leftValue, (Vector4)rightValue, factor);
                        item.UpdateColor();
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkPanel,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.pattern[0].uv,
                    readValueFromXml: (parameter, node) => node.ReadVector4("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Vector4)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemPanelPatternRotation",
                    parameter: null,
                    name: "Pattern Rot",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCIItem)oci).SetPatternRot(0, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkPanel,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.pattern[0].rot,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "itemPanelPatternClamp",
                    parameter: null,
                    name: "Pattern Tiling",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        OCIItem item = (OCIItem)oci;
                        if (item.itemInfo.pattern[0].clamp != (bool)leftValue)
                            item.SetPatternClamp(0, (bool)leftValue);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCIItem it && it.checkPanel,
                    getValue: (oci, parameter) => ((OCIItem)oci).itemInfo.pattern[0].rot,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
#endif
        }
#endif

        private static void Light()
        {
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "lightColor",
                    parameter: null,
                    name: "Color",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCILight)oci).SetColor(Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCILight,
                    getValue: (oci, parameter) => ((OCILight)oci).lightInfo.color,
                    readValueFromXml: (parameter, node) => node.ReadColor("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (Color)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "lightOnOff",
                    parameter: null,
                    name: "On/Off",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        bool value = (bool)leftValue;
                        if (((OCILight)oci).light.enabled != value)
                            ((OCILight)oci).SetEnable(value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCILight,
                    getValue: (oci, parameter) => ((OCILight)oci).light.enabled,
                    readValueFromXml: (parameter, node) => node.ReadBool("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "lightShadows",
                    parameter: null,
                    name: "Shadows",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    {
                        bool value = (bool)leftValue;
                        if (((OCILight)oci).light.shadows != LightShadows.None != value)
                            ((OCILight)oci).SetShadow(value);
                    },
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCILight,
                    getValue: (oci, parameter) => ((OCILight)oci).light.shadows != LightShadows.None,
                    readValueFromXml: (parameter, node) => node.ReadBool("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (bool)o)));
            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "lightStrength",
                    parameter: null,
                    name: "Strength",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCILight)oci).SetIntensity(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCILight,
                    getValue: (oci, parameter) => ((OCILight)oci).light.intensity,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "lightRange",
                    parameter: null,
                    name: "Range",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCILight)oci).SetRange(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCILight && (((OCILight)oci).lightType == LightType.Point || ((OCILight)oci).lightType == LightType.Spot),
                    getValue: (oci, parameter) => ((OCILight)oci).light.range,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));

            Timeline.AddInterpolableModel(new InterpolableModel(
                    owner: Timeline._ownerId,
                    id: "lightSpotAngle",
                    parameter: null,
                    name: "Spot Angle",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((OCILight)oci).SetSpotAngle(Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: (oci) => oci is OCILight && ((OCILight)oci).lightType == LightType.Spot,
                    getValue: (oci, parameter) => ((OCILight)oci).light.spotAngle,
                    readValueFromXml: (parameter, node) => node.ReadFloat("value"),
                    writeValueToXml: (parameter, writer, o) => writer.WriteValue("value", (float)o)));
        }
    }
}
