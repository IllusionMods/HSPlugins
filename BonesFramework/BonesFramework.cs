using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if IPA
using IllusionPlugin;
using Harmony;
#elif BEPINEX
using BepInEx;
using HarmonyLib;
#endif
#if AISHOUJO || HONEYSELECT2
using AIChara;
using BepInEx.Bootstrap;
#endif
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace BonesFramework
{
#if BEPINEX
    [BepInPlugin(_guid, _name, _version)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", BepInDependency.DependencyFlags.SoftDependency)]
#endif
    public class BonesFramework : GenericPlugin
#if IPA
        , IEnhancedPlugin
#endif
    {
        #region Private Types
        private class AdditionalObjectData
        {
            public GameObject parent;
            public readonly List<GameObject> objects = new List<GameObject>();
        }
        #endregion

        #region Private Variables
        private static BonesFramework _self;
        private static GameObject _currentLoadingObject;
        private static readonly HashSet<string> _currentAdditionalRootBones = new HashSet<string>();
        private static Transform _currentTransformParent;
        private static readonly Dictionary<GameObject, AdditionalObjectData> _currentAdditionalObjects = new Dictionary<GameObject, AdditionalObjectData>();
        private const string _name = "BonesFramework";
        private const string _version = "1.4.1";
        private const string _guid = "com.joan6694.illusionplugins.bonesframework";
#if AISHOUJO || HONEYSELECT2
        private static Type _uncensorSelectorType;
#endif
        #endregion

#if IPA
        public override string Name { get { return _name; } }
        public override string Version { get { return _version; } }
        public override string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_64", "StudioNEO_32", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
#endif

        #region Unity Methods
        protected override void Awake()
        {
            _self = this;
            base.Awake();

            UnityEngine.Debug.Log("BonesFramework: Trying to patch methods...");
            try
            {
                var harmony = HarmonyExtensions.CreateInstance(_guid);
#if HONEYSELECT
                harmony.Patch(typeof(CharBody).GetCoroutineMethod("LoadCharaFbxDataAsync"),
#elif AISHOUJO || HONEYSELECT2
                harmony.Patch(typeof(ChaControl).GetCoroutineMethod("LoadCharaFbxDataAsync"),
#endif
                        transpiler: new HarmonyMethod(typeof(BonesFramework), nameof(CharBody_LoadCharaFbxDataAsync_Transpiler), new[] { typeof(IEnumerable<CodeInstruction>) }));
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeights", new[] { typeof(GameObject), typeof(string), typeof(Transform) }),
                        new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Prefix)),
                        new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Postfix)));
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeightsLoop", new[] { typeof(Transform), typeof(Transform) }),
                        new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeightsLoop_Prefix)));

#if AISHOUJO || HONEYSELECT2
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeightsAndSetBounds", new[] { typeof(GameObject), typeof(string), typeof(Bounds), typeof(Transform) }),
                        new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Prefix)),
                        new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Postfix)));
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeightsAndSetBoundsLoop", new[] { typeof(Transform), typeof(Bounds), typeof(Transform) }),
                        new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeightsAndSetBoundsLoop_Prefix)));
                Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.uncensorselector", out PluginInfo info);
                if (info != null && info.Instance != null)
                {
                    _uncensorSelectorType = info.Instance.GetType();
                    Type uncensorSelectorControllerType = _uncensorSelectorType.GetNestedType("UncensorSelectorController", AccessTools.all);
                    if (uncensorSelectorControllerType != null)
                    {
                        UnityEngine.Debug.Log("BonesFramework: UncensorSelector found, trying to patch");
                        MethodInfo uncensorSelectorReloadCharacterBody = AccessTools.Method(uncensorSelectorControllerType, "ReloadCharacterBody");
                        if (uncensorSelectorReloadCharacterBody != null)
                        {
                            harmony.Patch(uncensorSelectorReloadCharacterBody, transpiler: new HarmonyMethod(typeof(BonesFramework), nameof(UncensorSelector_ReloadCharacterBody_Transpiler), new[] { typeof(IEnumerable<CodeInstruction>) }));
                            UnityEngine.Debug.Log("BonesFramework: UncensorSelector patched correctly");
                        }
                    }
                }
#endif
                UnityEngine.Debug.Log("BonesFramework: Patch successful!");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("BonesFramework: Couldn't patch properly:\n" + e);
            }
        }
        #endregion

        #region Private Methods
        private static void LoadAdditionalBonesForCurrent(string assetBundlePath, string assetName, string manifest)
        {
            TextAsset ta = CommonLib.LoadAsset<TextAsset>(assetBundlePath, "additional_bones", true, manifest);
            if (ta == null)
                return;
            UnityEngine.Debug.Log("BonesFramework: Loaded additional_bones TextAsset from " + assetBundlePath);

            string[] lines = ta.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] cells = line.Split('\t');
                if (!cells[0].Equals(assetName))
                    continue;
                UnityEngine.Debug.Log("BonesFramework: Found matching line for asset " + assetName + "\n" + line);

                _currentAdditionalObjects.Add(_currentLoadingObject, new AdditionalObjectData());

                EventTrigger eventTrigger = _currentLoadingObject.AddComponent<EventTrigger>();
                eventTrigger.onStart = (self2) => _self.ExecuteDelayed(() => ResetDynamicBones(self2.transform.parent.GetComponentsInChildren<DynamicBone>(true)));
                eventTrigger.onDestroy = DeleteBonesForObject;

                for (int i = 1; i < cells.Length; i++)
                    _currentAdditionalRootBones.Add(cells[i]);
                break;
            }
        }

        private static void DeleteBonesForObject(GameObject go)
        {
            AdditionalObjectData data;
            if (_currentAdditionalObjects.TryGetValue(go, out data) == false)
                return;
            DynamicBone[] dbs = null;
            if (data.parent != null)
                dbs = data.parent.GetComponentsInChildren<DynamicBone>(true);
            foreach (GameObject o in data.objects)
            {
                if (dbs != null)
                {
                    DynamicBoneCollider[] colliders = o.GetComponentsInChildren<DynamicBoneCollider>(true);
                    foreach (DynamicBoneCollider collider in colliders)
                    {
                        foreach (DynamicBone dynamicBone in dbs)
                        {
                            int index = dynamicBone.m_Colliders.FindIndex(c => c == collider);
                            if (index != -1)
                                dynamicBone.m_Colliders.RemoveAt(index);
                        }
                    }
                }
                GameObject.DestroyImmediate(o);
            }
            _currentAdditionalObjects.Remove(go);
            if (data.parent != null)
                _self.ExecuteDelayed(() => ResetDynamicBones(data.parent.transform.parent.GetComponentsInChildren<DynamicBone>(true)));

        }

        private static void ResetDynamicBones(DynamicBone[] dbs)
        {
            foreach (DynamicBone dynamicBone in dbs)
            {
                dynamicBone.InitTransforms();
                dynamicBone.SetupParticles();
            }
        }
        #endregion

        #region Patches
        private static IEnumerable<CodeInstruction> CharBody_LoadCharaFbxDataAsync_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            foreach (CodeInstruction inst in instructions)
            {
                yield return inst;
                if (set == false && inst.ToString().Contains("AddLoadAssetBundle")) //There's probably something better but idk m8
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
#if HONEYSELECT2
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
#endif
                    yield return new CodeInstruction(OpCodes.Call, typeof(BonesFramework).GetMethod(nameof(CharBody_LoadCharaFbxDataAsync_Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
            }
        }

        private static void CharBody_LoadCharaFbxDataAsync_Injected(object self
#if HONEYSELECT2
                                                                    , GameObject obj
                                                                    , string assetName

#endif
                )
        {
#if HONEYSELECT
            _currentLoadingObject = (GameObject)self.GetPrivate("<newObj>__6");
#elif AISHOUJO
            _currentLoadingObject = (GameObject)self.GetPrivate("$locvar2").GetPrivate("newObj");
#elif HONEYSELECT2
            _currentLoadingObject = obj;
#endif
            _currentAdditionalRootBones.Clear();
            if (_currentLoadingObject == null)
                return;
#if HONEYSELECT
            ListTypeFbx ltf = (ListTypeFbx)self.GetPrivate("<ltf>__3");
            string assetBundlePath = ltf.ABPath;
            string assetName = (string)self.GetPrivate("<assetName>__5");
            string manifest = ltf.Manifest;
#elif AISHOUJO
            string assetBundlePath = (string)self.GetPrivate("<assetBundleName>__0");
            string assetName = (string)self.GetPrivate("<assetName>__0");
            string manifest = (string)self.GetPrivate("<manifestName>__0");
#elif HONEYSELECT2
            string assetBundlePath = (string)self.GetPrivate("<assetBundleName>5__4");
            string manifest = (string)self.GetPrivate("<manifestName>5__3");
#endif
            LoadAdditionalBonesForCurrent(assetBundlePath, assetName, manifest);
        }

        private static void AssignedAnotherWeights_AssignedWeights_Prefix(GameObject obj)
        {
            _currentTransformParent = obj.transform.parent.Find("p_cf_anim");
            if (_currentTransformParent == null)
            {
                _currentTransformParent = obj.transform.parent.Find("p_cm_anim");
                if (_currentTransformParent == null)
                    _currentTransformParent = obj.transform.parent;
            }
        }

        private static void AssignedAnotherWeights_AssignedWeights_Postfix()
        {
            AdditionalObjectData data;
            if (_currentLoadingObject != null && _currentAdditionalObjects.TryGetValue(_currentLoadingObject, out data))
            {
                data.parent = _currentLoadingObject.transform.parent.gameObject;
                _self.ExecuteDelayed(() =>
                {
                    IEnumerable<DynamicBoneCollider> colliders = data.objects.SelectMany(go => go.GetComponentsInChildren<DynamicBoneCollider>(true));
                    foreach (DynamicBone bone in data.parent.GetComponentsInChildren<DynamicBone>(true))
                    {
                        if (bone.m_Colliders != null)
                        {
                            foreach (DynamicBoneCollider collider in colliders)
                                bone.m_Colliders.Add(collider);
                        }
                    }
                });
            }

            _currentLoadingObject = null;
        }

        private static bool AssignedAnotherWeights_AssignedWeightsLoop_Prefix(AssignedAnotherWeights __instance, Transform t, Transform rootBone)
        {
            SkinnedMeshRenderer component = t.GetComponent<SkinnedMeshRenderer>();
            if (component)
            {
                int num = component.bones.Length;
                Transform[] array = new Transform[num];
                GameObject gameObject = null;
                for (int i = 0; i < num; i++)
                {
                    Transform bone = component.bones[i];
                    if (__instance.dictBone.TryGetValue(bone.name, out gameObject))
                        array[i] = gameObject.transform;
                    else
                    {
                        // For AI, the bone is added anyway because Illusion decided to make their own pseudo BonesFramework, and if you look at the original code, it does the same thing (exceps more complicated because Illusion cannot produce optimized code).
#if HONEYSELECT
                        if (_currentAdditionalRootBones.Count != 0 && _currentAdditionalRootBones.Any(bone.IsChildOf))
#endif
                        {
                            array[i] = bone;
                        }
                    }
                }

                component.bones = array;
                if (rootBone)
                    component.rootBone = rootBone;
                else if (component.rootBone && __instance.dictBone.TryGetValue(component.rootBone.name, out gameObject))
                    component.rootBone = gameObject.transform;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Transform obj = t.GetChild(i);
                if (_currentAdditionalRootBones.Count != 0 && _currentAdditionalRootBones.Contains(obj.name))
                {
                    Transform parent = _currentTransformParent.FindDescendant(obj.parent.name);
                    Vector3 localPos = obj.localPosition;
                    Quaternion localRot = obj.localRotation;
                    Vector3 localScale = obj.localScale;
                    obj.SetParent(parent);
                    obj.localPosition = localPos;
                    obj.localRotation = localRot;
                    obj.localScale = localScale;
                    _currentAdditionalObjects[_currentLoadingObject].objects.Add(obj.gameObject);
                    --i;
                }
                else
                    AssignedAnotherWeights_AssignedWeightsLoop_Prefix(__instance, obj, rootBone);
            }

            return false;
        }

#if AISHOUJO || HONEYSELECT2
        private static bool AssignedAnotherWeights_AssignedWeightsAndSetBoundsLoop_Prefix(AssignedAnotherWeights __instance, Transform t, Bounds bounds, Transform rootBone)
        {
            SkinnedMeshRenderer renderer = t.GetComponent<SkinnedMeshRenderer>();
            if (renderer)
            {
                int length = renderer.bones.Length;
                Transform[] array = new Transform[length];
                GameObject gameObject = null;
                for (int i = 0; i < length; i++)
                {
                    Transform bone = renderer.bones[i];
                    if (__instance.dictBone.TryGetValue(bone.name, out gameObject))
                        array[i] = gameObject.transform;
                    else
                    {
#if HONEYSELECT
                        if (_currentAdditionalRootBones.Count != 0 && _currentAdditionalRootBones.Any(bone.IsChildOf))
#endif
                        {
                            array[i] = bone;
                        }
                    }
                }

                renderer.bones = array;
                renderer.localBounds = bounds;
                Cloth cloth = renderer.gameObject.GetComponent<Cloth>();
                if (rootBone && cloth == null)
                    renderer.rootBone = rootBone;
                else if (renderer.rootBone && __instance.dictBone.TryGetValue(renderer.rootBone.name, out gameObject))
                    renderer.rootBone = gameObject.transform;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Transform obj = t.GetChild(i);
                if (_currentAdditionalRootBones.Count != 0 && _currentAdditionalRootBones.Contains(obj.name))
                {
                    Transform parent = _currentTransformParent.FindDescendant(obj.parent.name);
                    Vector3 localPos = obj.localPosition;
                    Quaternion localRot = obj.localRotation;
                    Vector3 localScale = obj.localScale;
                    obj.SetParent(parent);
                    obj.localPosition = localPos;
                    obj.localRotation = localRot;
                    obj.localScale = localScale;
                    _currentAdditionalObjects[_currentLoadingObject].objects.Add(obj.gameObject);
                    --i;
                }
                else
                    AssignedAnotherWeights_AssignedWeightsAndSetBoundsLoop_Prefix(__instance, obj, bounds, rootBone);
            }

            return false;
        }

        private static IEnumerable<CodeInstruction> UncensorSelector_ReloadCharacterBody_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            foreach (CodeInstruction inst in instructions)
            {
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Call && inst.ToString().Contains("LoadAsset"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(BonesFramework).GetMethod(nameof(UncensorSelector_ReloadCharacterBody_Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
            }
        }

        private static GameObject UncensorSelector_ReloadCharacterBody_Injected(GameObject loadedObject, object __instance)
        {
            ChaControl chaControl = (ChaControl)__instance.GetPrivateProperty("ChaControl");
            if (chaControl == null)
                return loadedObject;
            _currentAdditionalRootBones.Clear();
            DeleteBonesForObject(chaControl.objBodyBone);
            DestroyImmediate(chaControl.objBodyBone.GetComponent<EventTrigger>());
            _currentLoadingObject = chaControl.objBodyBone;

            object bodyData = __instance.GetPrivateProperty("BodyData");
            string assetBundleName;
            string assetName;
            if (bodyData != null)
            {
                assetBundleName = (string)bodyData.GetPrivate("OOBase");
                assetName = (string)bodyData.GetPrivate("Asset");
            }
            else
            {
                assetBundleName = (string)_uncensorSelectorType.GetNestedType("Defaults", BindingFlags.Public | BindingFlags.Static).GetPrivate("OOBase");
                if (chaControl.sex == 0)
                    assetName = (string)_uncensorSelectorType.GetNestedType("Defaults", BindingFlags.Public | BindingFlags.Static).GetPrivate("AssetMale");
                else
                    assetName = (string)_uncensorSelectorType.GetNestedType("Defaults", BindingFlags.Public | BindingFlags.Static).GetPrivate("AssetFemale");
            }
            LoadAdditionalBonesForCurrent(assetBundleName, assetName, "");

            if (_currentAdditionalRootBones.Count != 0)
            {
                AdditionalObjectData additionalObjectData = _currentAdditionalObjects[_currentLoadingObject];
                RecurseTransforms(loadedObject.transform, bone =>
                {
                    if (_currentAdditionalRootBones.Contains(bone.name))
                    {
                        Transform parent = chaControl.objBodyBone.transform.FindDescendant(bone.parent.name);
                        Vector3 localPos = bone.localPosition;
                        Quaternion localRot = bone.localRotation;
                        Vector3 localScale = bone.localScale;
                        //string cachedName = bone.name;
                        //bone = Instantiate(bone);
                        //bone.name = cachedName;
                        bone.SetParent(parent);
                        bone.localPosition = localPos;
                        bone.localRotation = localRot;
                        bone.localScale = localScale;
                        additionalObjectData.objects.Add(bone.gameObject);
                        return true;
                    }
                    return false;
                });
                foreach (SkinnedMeshRenderer newRenderer in loadedObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    HashSet<Transform> newBones = new HashSet<Transform>();
                    SkinnedMeshRenderer oldRenderer = chaControl.objBodyBone.transform.parent.parent.FindDescendant(newRenderer.name)?.GetComponent<SkinnedMeshRenderer>();
                    if (oldRenderer == null)
                        continue;

                    foreach (Transform newRendererBone in newRenderer.bones)
                    {
                        foreach (GameObject additionalObject in additionalObjectData.objects)
                        {
                            if (newRendererBone.IsChildOf(additionalObject.transform))
                            {
                                //Transform oldRendererBone = chaControl.objBodyBone.transform.FindDescendant(newRendererBone.name);
                                //if (oldRendererBone != null && newBones.Contains(oldRendererBone) == false)
                                //    newBones.Add(oldRendererBone);
                                newBones.Add(newRendererBone);
                            }
                        }
                    }
                    foreach (Transform oldRendererBone in oldRenderer.bones)
                    {
                        if (oldRendererBone != null && newBones.Contains(oldRendererBone) == false)
                            newBones.Add(oldRendererBone);
                    }
                    oldRenderer.bones = newBones.ToArray();
                }
            }
            return loadedObject;
        }

        private static void RecurseTransforms(Transform t, Func<Transform, bool> onBone)
        {
            for (int i = 0; i < t.childCount; ++i)
            {
                Transform child = t.GetChild(i);
                if (onBone(child))
                    --i;
                else
                    RecurseTransforms(child, onBone);
            }
        }
#endif
        #endregion
    }
}