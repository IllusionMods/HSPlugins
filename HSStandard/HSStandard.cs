using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using IllusionPlugin;
using Studio;
using UnityEngine;
using Resources = HSStandard.Properties.Resources;

namespace HSStandard
{
    public class HSStandard : IEnhancedPlugin
    {
        public const string versionNum = "1.0.2";

        #region Private Types
        private class SwapDetails
        {
            public Dictionary<Renderer, HashSet<int>> materialsToIgnore = null;
        }
        #endregion

        #region Private Variables
        private static Material _depthTextureOnly;
        private static Material _addAlpha;
        private static Material _standardIgnoreProjector;
        private static Material _hsStandard;
        private static Material _hsStandardSSS;
        private static Material _hsStandardFade;
        private static Material _hsStandardTransparent;
        private static Material _hsStandardCutout;
        private static Material _hsStandardTwoSided;
        private static Material _hsStandardTwoSidedFade;
        private static Material _hsStandardTwoSidedCutout;
        private static Material _hsStandardAnisotropic;
        private static Material _hsStandardAnisotropicTwoSided;
        private static Material _hsStandardAnisotropicTransparent;
        private static Material _hsStandardAnisotropicTransparentBlend;
        private static Material _hsStandardAnisotropicTransparentTwoSided;
        private static Material _hsStandardTwoColorsAnimated;
        private static Material _hsStandardTwoColorsCutout;
        private static Material _hsStandardTwoColorsFade;
        private static Material _hsStandardTwoLayers;
        private static Material _hsStandardTwoLayersCutout;
        private static Material _hsStandardTwoLayersTwoSided;
        private static Material _hsStandardTwoLayersTwoSidedCutout;
        private static Material _hsStandardTwoLayersTwoSidedFade;
        private static readonly List<KeyValuePair<GameObject, SwapDetails>> _toSwap = new List<KeyValuePair<GameObject, SwapDetails>>();
        private static bool _ignoreSwap;
        //private static bool _forceHair;
        private static bool _isHair;
        private static bool _isSkin;
        private static bool _isBodyStuff;
        private static bool _isClothes;
        private static bool _isVanillaMap;

        private static bool _replaceHair = true;
        private static bool _replaceSkin = true;
        private static bool _replaceBodyStuff = true;
        private static bool _replaceClothes = true;
        private static bool _replaceSingleColorClothes = true;
        private static bool _replaceTwoColorsClothes = true;
        private static bool _replaceVanillaMaps = false;
        private static bool _replaceOther = true;
        private static bool _replaceSingleColorOther = true;
        private static bool _replaceTwoColorsOther = true;
        private static bool _fixHairDOF = false;
        private static bool _dedicatedSkinShader = true;
        private static bool _dedicatedHairShader = true;

        private static FieldInfo _moreAccCharAdditionalDataObjAccessory;
        #endregion

        #region Public Accessors
        public string Name { get { return "HSStandard"; }}
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(Resources.HSStandardResources);
            _depthTextureOnly = bundle.LoadAsset<Material>("DepthTextureOnly");
            _addAlpha = bundle.LoadAsset<Material>("AddAlpha");
            _standardIgnoreProjector = bundle.LoadAsset<Material>("StandardIgnoreProjector");
            _hsStandard = bundle.LoadAsset<Material>("HSStandard");
            _hsStandardSSS = bundle.LoadAsset<Material>("HSStandardSSS");
            _hsStandardTwoSided = bundle.LoadAsset<Material>("HSStandardTwoSided");
            _hsStandardFade = bundle.LoadAsset<Material>("HSStandardFade");
            _hsStandardTransparent = bundle.LoadAsset<Material>("HSStandardTransparent");
            _hsStandardCutout = bundle.LoadAsset<Material>("HSStandardCutout");
            _hsStandardTwoSidedFade = bundle.LoadAsset<Material>("HSStandardTwoSidedFade");
            _hsStandardTwoSidedCutout = bundle.LoadAsset<Material>("HSStandardTwoSidedCutout");
            _hsStandardAnisotropic = bundle.LoadAsset<Material>("HSStandardAnisotropic");
            _hsStandardAnisotropicTwoSided = bundle.LoadAsset<Material>("HSStandardAnisotropicTwoSided");
            _hsStandardAnisotropicTransparent = bundle.LoadAsset<Material>("HSStandardAnisotropicTransparent");
            _hsStandardAnisotropicTransparentBlend = bundle.LoadAsset<Material>("HSStandardAnisotropicTransparentBlend");
            _hsStandardAnisotropicTransparentTwoSided = bundle.LoadAsset<Material>("HSStandardAnisotropicTransparentTwoSided");
            _hsStandardTwoColorsAnimated = bundle.LoadAsset<Material>("HSStandardTwoColorsAnimated");
            _hsStandardTwoColorsCutout = bundle.LoadAsset<Material>("HSStandardTwoColorsCutout");
            _hsStandardTwoColorsFade = bundle.LoadAsset<Material>("HSStandardTwoColorsFade");
            _hsStandardTwoLayers = bundle.LoadAsset<Material>("HSStandardTwoLayers");
            _hsStandardTwoLayersCutout = bundle.LoadAsset<Material>("HSStandardTwoLayersCutout");
            _hsStandardTwoLayersTwoSided = bundle.LoadAsset<Material>("HSStandardTwoLayersTwoSided");
            _hsStandardTwoLayersTwoSidedCutout = bundle.LoadAsset<Material>("HSStandardTwoLayersTwoSidedCutout");
            _hsStandardTwoLayersTwoSidedFade = bundle.LoadAsset<Material>("HSStandardTwoLayersTwoSidedFade");
            bundle.Unload(false);

            _replaceHair = ModPrefs.GetBool("HSStandard", "replaceHair", true, true);
            _replaceSkin = ModPrefs.GetBool("HSStandard", "replaceSkin", true, true);
            _replaceBodyStuff = ModPrefs.GetBool("HSStandard", "replaceBodyStuff", true, true);
            _replaceClothes = ModPrefs.GetBool("HSStandard", "replaceClothes", true, true);
            _replaceSingleColorClothes = ModPrefs.GetBool("HSStandard", "replaceSingleColorClothes", true, true);
            _replaceTwoColorsClothes = ModPrefs.GetBool("HSStandard", "replaceTwoColorsClothes", true, true);
            _replaceVanillaMaps = ModPrefs.GetBool("HSStandard", "replaceVanillaMaps", true, true);
            _replaceOther = ModPrefs.GetBool("HSStandard", "replaceOther", true, true);
            _replaceSingleColorOther = ModPrefs.GetBool("HSStandard", "replaceSingleColorOther", true, true);
            _replaceTwoColorsOther = ModPrefs.GetBool("HSStandard", "replaceTwoColorsOther", true, true);
            _fixHairDOF = ModPrefs.GetBool("HSStandard", "fixHairDOF", false, true);
            _dedicatedSkinShader = ModPrefs.GetBool("HSStandard", "dedicatedSkinShader", false, true);
            _dedicatedHairShader = ModPrefs.GetBool("HSStandard", "dedicatedHairShader", true, true);


            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.hsstandard");

            if (_replaceSkin && _dedicatedSkinShader)
            {
                harmony.Patch(AccessTools.Method(typeof(CustomTextureControl), "RebuildTextureAndSetMaterial"), null, new HarmonyMethod(typeof(CustomTextureContrl_RebuildTextureAndSetMaterial_Patches), nameof(CustomTextureContrl_RebuildTextureAndSetMaterial_Patches.Postfix)));
            }

            harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "UpdateSiru", new []{ typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(UpdateSiruPrefix)));

            harmony.Patch(AccessTools.Method(typeof(AssetBundleManager), "LoadAsset", new []{typeof(string), typeof(string), typeof(Type), typeof(string)}), null, new HarmonyMethod(typeof(HSStandard), nameof(LoadAssetPostfix)));
            harmony.Patch(typeof(CharCustom).GetMethod("ChangeMaterial", BindingFlags.Public | BindingFlags.Instance), null, null, new HarmonyMethod(typeof(HSStandard), nameof(ChangeMaterialTranspiler)));

            //IsHair and IsClothes modifiers
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesBot", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair))); 
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesBra", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesGloves", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesPanst", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesShoes", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesShorts", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesSocks", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesSwimBot", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesSwimsuit", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesSwimTop", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharFemaleBody).GetMethod("ChangeClothesTop", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));

            harmony.Patch(typeof(CharMaleBody).GetMethod("ChangeClothesBase", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));
            harmony.Patch(typeof(CharMaleBody).GetMethod("ChangeClothesShoes", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsClothesIsHair)));

            //IsHair Modifiers
            harmony.Patch(AccessTools.Method(typeof(AddObjectItem), "Load", new []{ typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsHair)));
            harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "ChangeHead", new[] { typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuffIsHair)));
            harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "ChangeHead", new[] { typeof(int), typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuffIsHair)));
            harmony.Patch(AccessTools.Method(typeof(CharMaleBody), "ChangeHead", new[] { typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuffIsHair)));
            harmony.Patch(AccessTools.Method(typeof(CharMaleBody), "ChangeHead", new[] { typeof(int), typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrueIsHairFalse)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuffIsHair)));

            ////ForceHair Modifiers
            //harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "ChangeHair", new[] { typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetForceHairTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetForceHair)));
            //harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "ChangeHair", new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetForceHairTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetForceHair)));
            //harmony.Patch(AccessTools.Method(typeof(CharMaleBody), "ChangeHair", new[] { typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetForceHairTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetForceHair)));
            //harmony.Patch(AccessTools.Method(typeof(CharMaleBody), "ChangeHair", new[] { typeof(int), typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetForceHairTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetForceHair)));

            //IsClothes Modifiers
            harmony.Patch(AccessTools.Method(typeof(CharBody), "ChangeAccessory", new []{ typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrue)), new HarmonyMethod(typeof(HSStandard), nameof(OnChangeAccessoryPostfix)));
            harmony.Patch(AccessTools.Method(typeof(CharBody), "ChangeAccessory", new []{ typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool) }), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrue)), new HarmonyMethod(typeof(HSStandard), nameof(OnChangeAccessoryPostfixSingle)));

            Type moreAccessoriesPatch = Type.GetType("MoreAccessories.CharBody_ChangeAccessory_Patches,MoreAccessories");
            if (moreAccessoriesPatch != null)
            {
                harmony.Patch(moreAccessoriesPatch.GetMethod("ChangeAccessoryAsync", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic), new HarmonyMethod(typeof(HSStandard), nameof(SetIsClothesTrue)), new HarmonyMethod(typeof(HSStandard), nameof(OnChangeAccessoryPostfixMoreAccessories)));
                _moreAccCharAdditionalDataObjAccessory = Type.GetType("MoreAccessories.MoreAccessories+CharAdditionalData,MoreAccessories").GetField("objAccessory", BindingFlags.Public | BindingFlags.Instance);
            }

            //IsSkin Modifiers
            harmony.Patch(typeof(CustomTextureControl).GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsSkinTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsSkin)));

            //IsBodyStuff Modifiers
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeEyebrow", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeEyeHi", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeEyeL", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeEyelashes", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeEyeR", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeNip", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharFemaleCustom).GetMethod("ChangeUnderHair", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));

            harmony.Patch(typeof(CharMaleCustom).GetMethod("ChangeBeard", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharMaleCustom).GetMethod("ChangeEyebrow", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharMaleCustom).GetMethod("ChangeEyeL", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));
            harmony.Patch(typeof(CharMaleCustom).GetMethod("ChangeEyeR", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsBodyStuffTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsBodyStuff)));

            //IsVanillaMap Modifiers
            harmony.Patch(typeof(Manager.Map).GetMethod("LoadMap", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), new HarmonyMethod(typeof(HSStandard), nameof(SetIsVanillaMapTrue)), new HarmonyMethod(typeof(HSStandard), nameof(ResetIsVanillaMap)));

            if (_fixHairDOF && _replaceHair)
            {
                //DepthTextureOnly
                harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "ChangeHair", new[] { typeof(bool) }), null, new HarmonyMethod(typeof(HSStandard), nameof(AddDepthTextureOnlyMaterialToFemaleHair)));
                harmony.Patch(AccessTools.Method(typeof(CharFemaleBody), "ChangeHair", new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) }), null, new HarmonyMethod(typeof(HSStandard), nameof(AddDepthTextureOnlyMaterialToFemaleHair)));
                harmony.Patch(AccessTools.Method(typeof(CharMaleBody), "ChangeHair", new[] { typeof(bool) }), null, new HarmonyMethod(typeof(HSStandard), nameof(AddDepthTextureOnlyMaterialToMaleHair)));
                harmony.Patch(AccessTools.Method(typeof(CharMaleBody), "ChangeHair", new[] { typeof(int), typeof(bool) }), null, new HarmonyMethod(typeof(HSStandard), nameof(AddDepthTextureOnlyMaterialToMaleHair)));
            }
        }

        public void OnLevelWasInitialized(int level) { }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
            _ignoreSwap = Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt);
            if (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
            {
                foreach (KeyValuePair<string, AssetBundleManager.BundlePack> pair in AssetBundleManager.ManifestBundlePack)
                {
                    foreach (KeyValuePair<string, LoadedAssetBundle> bundle in new Dictionary<string, LoadedAssetBundle>(pair.Value.LoadedAssetBundles))
                    {
                        AssetBundleManager.UnloadAssetBundle(bundle.Key, true, pair.Key);
                    }
                }
            }
            _isHair = true;
            //_forceHair = false;
            _isSkin = false;
            _isBodyStuff = false;
            _isClothes = false;
            _isVanillaMap = false;
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate()
        {
            if (_toSwap.Count != 0)
            {
                foreach (KeyValuePair<GameObject, SwapDetails> pair in _toSwap)
                {                    
                    int i = 0;
                    foreach (Renderer renderer in pair.Key.GetComponentsInChildren<Renderer>(true))
                    {
                        HashSet<int> toIgnoreMatList = null;
                        if (pair.Value.materialsToIgnore != null)
                            pair.Value.materialsToIgnore.TryGetValue(renderer, out toIgnoreMatList);

                        Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
                        for (int j = 0; j < renderer.sharedMaterials.Length; j++)
                        {
                            if (toIgnoreMatList == null || toIgnoreMatList.Contains(j) == false)
                            {
                                Material material = renderer.materials[j];
                                if (material != null)
                                    newMaterials[j] = SwapShader(material, out bool _);
                            }
                            else
                                newMaterials[j] = renderer.materials[j];
                        }
                        renderer.materials = newMaterials;
                        ++i;
                    }
                }
                _toSwap.Clear();
            }
        }

        public void OnApplicationQuit()
        {
        }
        #endregion

        #region Private Methods
        private static void LoadAssetPostfix(ref AssetBundleLoadAssetOperation __result)
        {
            if (__result == null)
                return;
            __result = new DummyAssetBundleLoadAssetOperation(__result);
        }

        internal static T HandleAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (_ignoreSwap)
                return asset;
            Type type = typeof(T);
            if (type == typeof(Material))
                asset = SwapShader(asset as Material, out bool _, false) as T;
            else if (type == typeof(GameObject))
            {
                GameObject go = asset as GameObject;
                if (go != null)
                {
                    SwapDetails details = new SwapDetails();
                    int i = 0;
                    foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer is ParticleSystemRenderer)
                            continue;
                        Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
                        for (int j = 0; j < renderer.sharedMaterials.Length; j++)
                        {
                            Material material = renderer.materials[j];
                            if (material != null)
                            {
                                bool replaced;
                                newMaterials[j] = SwapShader(material, out replaced);
                                if (replaced == false)
                                {
                                    if (details.materialsToIgnore == null)
                                        details.materialsToIgnore = new Dictionary<Renderer, HashSet<int>>();
                                    HashSet<int> toIgnoreMatList;
                                    if (details.materialsToIgnore.TryGetValue(renderer, out toIgnoreMatList) == false)
                                    {
                                        toIgnoreMatList = new HashSet<int>();
                                        details.materialsToIgnore.Add(renderer, toIgnoreMatList);
                                    }
                                    toIgnoreMatList.Add(j);
                                }
                            }
                        }
                        renderer.materials = newMaterials;
                        ++i;
                        SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                        if (skinnedMeshRenderer != null)
                            skinnedMeshRenderer.updateWhenOffscreen = true;
                    }
                    _toSwap.Add(new KeyValuePair<GameObject, SwapDetails>(go, details));
                }
            }
            return asset;
        }

        private static Material SwapShader(Material mat, out bool replaced, bool destroy = true)
        {
            if ( /*_forceHair ||*/ _isHair && mat.GetInt("_HairEffect") == 1)
            {
                if (_replaceHair == false)
                {
                    replaced = false;
                    return mat;
                }
            }
            else if (_isSkin)
            {
                if (_replaceSkin == false)
                {
                    replaced = false;
                    return mat;
                }
            }
            else if (_isBodyStuff)
            {
                if (_replaceBodyStuff == false)
                {
                    replaced = false;
                    return mat;
                }
            }
            else if (_isClothes)
            {
                if (_replaceClothes == false)
                {
                    replaced = false;
                    return mat;
                }
                switch (mat.shader.name)
                {
                    case "Shader Forge/PBRsp":
                    case "Shader Forge/PBRsp_alpha":
                    case "Shader Forge/PBRsp_alpha_blend":
                    case "Shader Forge/PBRsp_texture_alpha":
                    case "Standard_culloff":
                    case "Shader Forge/PBRsp_culloff":
                    case "Shader Forge/PBRsp_alpha_culloff":
                    case "Shader Forge/PBRsp_texture_alpha_culloff":
                    case "Shader Forge/PBRsp_2layer":
                    case "Shader Forge/PBRsp_2layer_cutout":
                    case "Shader Forge/PBRsp_2layer_culloff":
                    case "Shader Forge/PBRsp_2layer_cutout_culloff":
                    case "Shader Forge/PBRsp_2layer_alpha_culloff":
                        if (_replaceSingleColorClothes == false)
                        {
                            replaced = false;
                            return mat;
                        }
                        break;

                    case "Shader Forge/PBRsp_3mask_alpha":
                    case "Shader Forge/PBRsp_3mask":
                    case "Shader Forge/PBRsp_3mask_uv":
                        if (_replaceTwoColorsClothes == false)
                        {
                            replaced = false;
                            return mat;
                        }
                        break;
                }
            }
            else if (_isVanillaMap)
            {
                if (_replaceVanillaMaps == false)
                {
                    replaced = false;
                    return mat;
                }
            }
            else
            {
                if (_replaceOther == false)
                {
                    replaced = false;
                    return mat;
                }
                switch (mat.shader.name)
                {
                    case "Shader Forge/PBRsp":
                    case "Shader Forge/PBRsp_alpha":
                    case "Shader Forge/PBRsp_alpha_blend":
                    case "Shader Forge/PBRsp_texture_alpha":
                    case "Standard_culloff":
                    case "Shader Forge/PBRsp_culloff":
                    case "Shader Forge/PBRsp_alpha_culloff":
                    case "Shader Forge/PBRsp_texture_alpha_culloff":
                    case "Shader Forge/PBRsp_2layer":
                    case "Shader Forge/PBRsp_2layer_cutout":
                    case "Shader Forge/PBRsp_2layer_culloff":
                    case "Shader Forge/PBRsp_2layer_cutout_culloff":
                    case "Shader Forge/PBRsp_2layer_alpha_culloff":
                        if (_replaceSingleColorOther == false)
                        {
                            replaced = false;
                            return mat;
                        }
                        break;

                    case "Shader Forge/PBRsp_3mask_alpha":
                    case "Shader Forge/PBRsp_3mask":
                    case "Shader Forge/PBRsp_3mask_uv":
                        if (_replaceTwoColorsOther == false)
                        {
                            replaced = false;
                            return mat;
                        }
                        break;
                }

            }

            Material newMaterial = null;
            switch (mat.shader.name)
            {
                case "Shader Forge/PBRsp": //Opaque
                    if ((mat.GetInt("_HairEffect") == 0 || _isHair == false) /* && _forceHair == false*/)
                    {
                        newMaterial = _isSkin && _dedicatedSkinShader ? new Material(_hsStandardSSS) : new Material(_hsStandard);
                        SwapPropertiesClassic(mat, newMaterial);
                        SetMaterialKeywords(newMaterial);
                    }
                    else
                    {
                        if (_dedicatedHairShader)
                        {
                            newMaterial = new Material(_hsStandardAnisotropic);
                            SwapPropertiesAnisotropic(mat, newMaterial);
                            SetMaterialKeywordsAnisotropic(newMaterial);
                        }
                        else
                        {
                            newMaterial = new Material(_hsStandard);
                            SwapPropertiesClassic(mat, newMaterial);
                            SetMaterialKeywords(newMaterial);
                        }
                    }
                    break;
                case "Shader Forge/PBRsp_alpha": //Fade (but with alphatest as well)
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                    {
                        newMaterial = new Material(_hsStandardAnisotropicTransparent);

                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                        SwapPropertiesAnisotropic(mat, newMaterial);
                        SetMaterialKeywordsAnisotropic(newMaterial);
                    }
                    else
                    {
                        newMaterial = new Material(_hsStandardFade);
                        SwapPropertiesClassic(mat, newMaterial);

                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                        SetMaterialKeywords(newMaterial);
                    }
                    break;

                case "Shader Forge/PBRsp_alpha_blend": //custom edit of PBRsp_alpha
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                    {
                        newMaterial = new Material(_hsStandardAnisotropicTransparentBlend);

                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        //newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);

                        SwapPropertiesAnisotropic(mat, newMaterial);
                        SetMaterialKeywordsAnisotropic(newMaterial);
                    }
                    else
                    {
                        newMaterial = new Material(_hsStandardFade);
                        SwapPropertiesClassic(mat, newMaterial);

                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);

                        SetMaterialKeywords(newMaterial);
                    }
                    break;

                case "Shader Forge/PBRsp_texture_alpha": //Fade
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                    {
                        newMaterial = new Material(_hsStandardAnisotropicTransparent);

                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                        SwapPropertiesAnisotropic(mat, newMaterial);
                        SetMaterialKeywordsAnisotropic(newMaterial);
                    }
                    else
                    {
                        newMaterial = new Material(_hsStandardFade);
                        SwapPropertiesClassic(mat, newMaterial);

                        newMaterial.SetFloat("_Cutoff", 0f);
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1); //Maybe set that to zero if more problems arise

                        newMaterial.SetTexture("_BumpMap", mat.GetTexture("_SpecGlossMap"));
                        newMaterial.SetTextureOffset("_BumpMap", mat.GetTextureOffset("_SpecGlossMap"));
                        newMaterial.SetTextureScale("_BumpMap", mat.GetTextureScale("_SpecGlossMap"));

                        newMaterial.SetFloat("_NormalStrength", 0.20f);

                        SetMaterialKeywords(newMaterial);
                    }
                    break;

                case "Shader Forge/PBRsp_texture_alpha_glass": //Transparent
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                        break;
                        newMaterial = new Material(_hsStandardTransparent);
                        SwapPropertiesClassic(mat, newMaterial);

                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                        SetMaterialKeywords(newMaterial);
                    break;

                case "Standard_culloff": //Just a test, might remove in the future
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                            {
                                newMaterial = new Material(_hsStandardAnisotropicTwoSided);
                                SwapPropertiesAnisotropic(mat, newMaterial);
                                SetMaterialKeywordsAnisotropic(newMaterial);
                            }
                            else
                            {
                                newMaterial = new Material(_hsStandardTwoSided);
                                SwapPropertiesClassic(mat, newMaterial);
                                SetMaterialKeywords(newMaterial);
                            }
                            break;
                        case "TransparentCutout":
                            if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                                break;
                            newMaterial = new Material(_hsStandardTwoSidedCutout);
                            SwapPropertiesClassic(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.GetFloat("_Cutoff"), 0f, 0.95f));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "Transparent":
                            if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                                break;
                            newMaterial = new Material(_hsStandardTwoSidedFade);
                            SwapPropertiesClassic(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                            newMaterial.SetInt("_ZWrite", 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;

                case "Shader Forge/PBRsp_culloff": //Opaque
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                    {
                        newMaterial = new Material(_hsStandardAnisotropicTwoSided);
                        SwapPropertiesAnisotropic(mat, newMaterial);
                        SetMaterialKeywordsAnisotropic(newMaterial);
                    }
                    else
                    {
                        newMaterial = new Material(_hsStandardTwoSided);
                        SwapPropertiesClassic(mat, newMaterial);
                        SetMaterialKeywords(newMaterial);
                    }
                    break;

                case "Shader Forge/PBRsp_alpha_culloff": //Fade or Cutout it seems
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                    {
                        newMaterial = new Material(_hsStandardAnisotropicTransparentTwoSided);
                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                        SwapPropertiesAnisotropic(mat, newMaterial);
                        SetMaterialKeywordsAnisotropic(newMaterial);
                    }
                    else
                    {
                        newMaterial = new Material(_hsStandardTwoSidedFade);
                        SwapPropertiesClassic(mat, newMaterial);
                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.GetFloat("_Cutoff"), 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                        SetMaterialKeywords(newMaterial);
                    }
                    break;
                case "Shader Forge/PBRsp_texture_alpha_culloff": //Fade
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                    {
                        newMaterial = new Material(_hsStandardAnisotropicTransparentTwoSided);
                        newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                        newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                        SwapPropertiesAnisotropic(mat, newMaterial);
                        SetMaterialKeywordsAnisotropic(newMaterial);
                    }
                    else
                    {
                        newMaterial = new Material(_hsStandardTwoSidedFade);
                        SwapPropertiesClassic(mat, newMaterial);
                        newMaterial.SetFloat("_Cutoff", 0f);
                        newMaterial.SetInt("_ZWrite", 0); //Important for eyelashes
                        SetMaterialKeywords(newMaterial);
                    }
                    break;

                case "Shader Forge/PBRsp_3mask_uv": //Opaque
                    newMaterial = new Material(_hsStandardTwoColorsAnimated);
                    SwapPropertiesTwoColorsAnimated(mat, newMaterial);
                    newMaterial.SetFloat("_Cutoff", 0f);
                    newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);
                    SetMaterialKeywords(newMaterial);
                    break;

                case "Shader Forge/PBRsp_3mask_alpha": //Cutout
                    newMaterial = new Material(_hsStandardTwoColorsFade);
                    SwapPropertiesTwoColors(mat, newMaterial);
                    newMaterial.SetFloat("_Cutoff", 0f);
                    newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);
                    SetMaterialKeywords(newMaterial);
                    break;

                case "Shader Forge/PBRsp_3mask": //Cutout
                    newMaterial = new Material(_hsStandardTwoColorsCutout);
                    SwapPropertiesTwoColors(mat, newMaterial);
                    newMaterial.SetFloat("_Cutoff", 0.5f);
                    newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                    SetMaterialKeywords(newMaterial);
                    break;

                case "Shader Forge/PBRsp_2layer": //Opaque
                case "Shader Forge/PBRsp_2layer_cutout": //Cutout
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                        break;
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandardTwoLayers);
                            SwapPropertiesTwoLayers(mat, newMaterial);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoLayersCutout);
                            SwapPropertiesTwoLayers(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.GetFloat("_Cutoff"), 0f, 0.95f));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;

                case "Shader Forge/PBRsp_2layer_culloff": //Opaque
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                        break;
                    newMaterial = new Material(_hsStandardTwoLayersTwoSided);
                    SwapPropertiesTwoLayers(mat, newMaterial);
                    SetMaterialKeywords(newMaterial);
                    break;

                case "Shader Forge/PBRsp_2layer_alpha_culloff": //Transparent
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                        break;
                    switch (mat.GetTag("RenderType", false))
                    {
                        //default: //Opaque
                        //    newMaterial = new Material(_hsStandardTwoLayersTwoSided);
                        //    SwapPropertiesTwoLayers(mat, newMaterial);
                        //    SetMaterialKeywords(newMaterial);
                        //    break;
                        case "Transparent":
                            newMaterial = new Material(_hsStandardTwoLayersTwoSidedFade);
                            SwapPropertiesTwoLayers(mat, newMaterial);

                            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoLayersTwoSidedCutout);
                            SwapPropertiesTwoLayers(mat, newMaterial);

                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;
                case "Shader Forge/PBRsp_2layer_cutout_culloff": //Opaque
                    if (mat.GetInt("_HairEffect") == 1 && _isHair && _dedicatedHairShader)
                        break;

                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            //newMaterial = new Material(_hsStandardTwoLayersTwoSided); //Was problematic apparently, fuck you illusion
                            newMaterial = new Material(_hsStandardTwoLayersTwoSidedCutout);
                            SwapPropertiesTwoLayers(mat, newMaterial);

                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SetMaterialKeywords(newMaterial);

                            break;
                        case "Transparent":
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoLayersTwoSidedCutout);
                            SwapPropertiesTwoLayers(mat, newMaterial);

                            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SetMaterialKeywords(newMaterial);

                            break;
                    }
                    break;


                //Handling shader swap multiple times

                case "HSStandard/PBRsp":
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandard);
                            SwapPropertiesClassic(mat, newMaterial);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "Transparent":
                            newMaterial = new Material(_hsStandardFade);
                            SwapPropertiesClassic(mat, newMaterial);

                            newMaterial.SetFloat("_Cutoff", mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f);
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardCutout);
                            SwapPropertiesClassic(mat, newMaterial);

                            newMaterial.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);

                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;
                case "HSStandard/PBRsp SSS":
                    newMaterial = new Material(_hsStandardSSS);
                    SwapPropertiesSSS(mat, newMaterial);
                    SetMaterialKeywords(newMaterial);
                    break;

                case "HSStandard/PBRsp Anisotropic":
                case "HSStandard/PBRsp Anisotropic Hair":
                case "HSStandard/PBRsp Anisotropic Cutout":
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandardAnisotropic);
                            SwapPropertiesAnisotropic(mat, newMaterial);
                            SetMaterialKeywordsAnisotropic(newMaterial);
                            break;
                        case "TransparentCutout":
                        case "Transparent":
                            newMaterial = new Material(_hsStandardAnisotropicTransparent);

                            newMaterial.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SwapPropertiesAnisotropic(mat, newMaterial);
                            SetMaterialKeywordsAnisotropic(newMaterial);
                            break;
                    }
                    break;

                case "HSStandard/PBRsp Anisotropic Hair Blend":
                    newMaterial = new Material(_hsStandardAnisotropicTransparentBlend);

                    newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f, 0f, 0.95f));
                    //newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);

                    SwapPropertiesAnisotropic(mat, newMaterial);
                    SetMaterialKeywordsAnisotropic(newMaterial);
                    break;

                //
                case "HSStandard/PBRsp (Two Sided)":
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandardTwoSided);
                            SwapPropertiesClassic(mat, newMaterial);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoSidedCutout);
                            SwapPropertiesClassic(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "Transparent":
                            newMaterial = new Material(_hsStandardTwoSidedFade);
                            SwapPropertiesClassic(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f);
                            newMaterial.SetInt("_ZWrite", 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;
                case "HSStandard/PBRsp Anisotropic (Two Sided)":
                case "HSStandard/PBRsp Anisotropic Hair (Two Sided)":
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandardAnisotropicTwoSided);
                            SwapPropertiesAnisotropic(mat, newMaterial);
                            SetMaterialKeywordsAnisotropic(newMaterial);
                            break;
                        case "Transparent":
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardAnisotropicTransparentTwoSided);

                            newMaterial.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SwapPropertiesAnisotropic(mat, newMaterial);
                            SetMaterialKeywordsAnisotropic(newMaterial);
                            break;
                    }
                    break;
                //
                case "HSStandard/PBRsp Two Colors":
                    switch (mat.GetTag("RenderType", false))
                    {
                        case "Transparent":
                            newMaterial = new Material(_hsStandardTwoColorsFade);
                            SwapPropertiesTwoColors(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", 0f);
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoColorsCutout);
                            SwapPropertiesTwoColors(mat, newMaterial);
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;
                //
                case "HSStandard/PBRsp Two Colors Animated":
                            newMaterial = new Material(_hsStandardTwoColorsAnimated);
                            SwapPropertiesTwoColorsAnimated(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", 0f);
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);
                            SetMaterialKeywords(newMaterial);
                    break;
                //
                case "HSStandard/PBRsp Two Layers":
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandardTwoLayers);
                            SwapPropertiesTwoLayers(mat, newMaterial);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoLayersCutout);
                            SwapPropertiesTwoLayers(mat, newMaterial);
                            newMaterial.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);
                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;
                //
                case "HSStandard/PBRsp Two Layers (Two Sided)":
                    switch (mat.GetTag("RenderType", false))
                    {
                        default: //Opaque
                            newMaterial = new Material(_hsStandardTwoLayersTwoSided);
                            SwapPropertiesTwoLayers(mat, newMaterial);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "Transparent":
                            newMaterial = new Material(_hsStandardTwoLayersTwoSidedFade);
                            SwapPropertiesTwoLayers(mat, newMaterial);

                            newMaterial.SetFloat("_Cutoff", mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.01f);
                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 0);
                            SetMaterialKeywords(newMaterial);
                            break;
                        case "TransparentCutout":
                            newMaterial = new Material(_hsStandardTwoLayersTwoSidedCutout);
                            SwapPropertiesTwoLayers(mat, newMaterial);

                            newMaterial.SetInt("_ZWrite", mat.HasProperty("_ZWrite") ? mat.GetInt("_ZWrite") : 1);

                            SetMaterialKeywords(newMaterial);
                            break;
                    }
                    break;
            }
            if (newMaterial != null)
            {
                newMaterial.name = mat.name.Replace(" (Instance)", "");
                if (destroy)
                    UnityEngine.Object.Destroy(mat);
                mat = newMaterial;
            }
            replaced = true; //Setting this to true anyway because if it was ignored, it was ignore by normal means and not blacklist and could be potentially replaced later during the second pass.
            return mat;
        }

        private static void SwapCommonProperties(Material mat, Material newMaterial)
        {
            newMaterial.SetColor("_Color", mat.GetColor("_Color"));
            newMaterial.SetColor("_SpecColor", mat.GetColor("_SpecColor"));

            newMaterial.SetTexture("_MainTex", mat.GetTexture("_MainTex"));
            newMaterial.SetTextureOffset("_MainTex", mat.GetTextureOffset("_MainTex"));
            newMaterial.SetTextureScale("_MainTex", mat.GetTextureScale("_MainTex"));

            newMaterial.SetTexture("_BumpMap", mat.GetTexture("_BumpMap"));
            newMaterial.SetTextureOffset("_BumpMap", mat.GetTextureOffset("_BumpMap"));
            newMaterial.SetTextureScale("_BumpMap", mat.GetTextureScale("_BumpMap"));

            newMaterial.SetFloat("_NormalStrength", mat.HasProperty("_NormalStrength") ? mat.GetFloat("_NormalStrength") : 1);

            newMaterial.SetTexture("_SpecGlossMap", mat.GetTexture("_SpecGlossMap"));
            newMaterial.SetTextureOffset("_SpecGlossMap", mat.GetTextureOffset("_SpecGlossMap"));
            newMaterial.SetTextureScale("_SpecGlossMap", mat.GetTextureScale("_SpecGlossMap"));

            newMaterial.SetTexture("_OcclusionMap", mat.GetTexture("_OcclusionMap"));
            newMaterial.SetTextureOffset("_OcclusionMap", mat.GetTextureOffset("_OcclusionMap"));
            newMaterial.SetTextureScale("_OcclusionMap", mat.GetTextureScale("_OcclusionMap"));

            newMaterial.SetFloat("_Metallic", mat.GetFloat("_Metallic"));
            newMaterial.SetFloat("_Smoothness", mat.GetFloat("_Smoothness"));
            newMaterial.SetFloat("_OcclusionStrength", Mathf.Clamp01(mat.GetFloat("_OcclusionStrength")));

            newMaterial.renderQueue = mat.renderQueue;
            newMaterial.globalIlluminationFlags = mat.globalIlluminationFlags;
        }

        private static void SwapPropertiesClassic(Material mat, Material newMaterial)
        {
            //newMaterial.CopyPropertiesFromMaterial(mat);
            
            SwapCommonProperties(mat, newMaterial);

            newMaterial.SetTexture("_BlendNormalMap", mat.GetTexture("_BlendNormalMap"));
            newMaterial.SetTextureOffset("_BlendNormalMap", mat.GetTextureOffset("_BlendNormalMap"));
            newMaterial.SetTextureScale("_BlendNormalMap", mat.GetTextureScale("_BlendNormalMap"));

            newMaterial.SetTexture("_DetailNormalMap", mat.GetTexture("_DetailNormalMap"));
            newMaterial.SetTextureOffset("_DetailNormalMap", mat.GetTextureOffset("_DetailNormalMap"));
            newMaterial.SetTextureScale("_DetailNormalMap", mat.GetTextureScale("_DetailNormalMap"));

            newMaterial.SetTexture("_DetailMask", mat.GetTexture("_DetailMask"));
            newMaterial.SetTextureOffset("_DetailMask", mat.GetTextureOffset("_DetailMask"));
            newMaterial.SetTextureScale("_DetailMask", mat.GetTextureScale("_DetailMask"));

            newMaterial.SetFloat("_BlendNormalMapScale", mat.GetFloat("_BlendNormalMapScale"));
            newMaterial.SetFloat("_DetailNormalMapScale", mat.GetFloat("_DetailNormalMapScale"));

            newMaterial.SetTexture("_Emission", null);
            newMaterial.SetColor("_EmissionColor", Color.black);
        }

        private static void SwapPropertiesSSS(Material mat, Material newMaterial)
        {
            SwapPropertiesClassic(mat, newMaterial);
            newMaterial.SetFloat("_ThicknessAttenuation", mat.GetFloat("_ThicknessAttenuation"));
            newMaterial.SetFloat("_TranslucencyDistortion", mat.GetFloat("_TranslucencyDistortion"));
            newMaterial.SetFloat("_TranslucencyPower", mat.GetFloat("_TranslucencyPower"));
            newMaterial.SetFloat("_TranslucencyScale", mat.GetFloat("_TranslucencyScale"));
            newMaterial.SetFloat("_TranslucencyAmbient", mat.GetFloat("_TranslucencyAmbient"));
        }

        private static void SwapPropertiesAnisotropic(Material mat, Material newMaterial)
        {
            //newMaterial.CopyPropertiesFromMaterial(mat);

            SwapCommonProperties(mat, newMaterial);

            Texture normalMap = mat.GetTexture("_BumpMap");
            Texture detailNormal = mat.GetTexture("_DetailNormal");
            if (normalMap == null && detailNormal != null)
            {
                newMaterial.SetTexture("_BumpMap", detailNormal);
                newMaterial.SetTextureOffset("_BumpMap", mat.GetTextureOffset("_DetailNormal"));
                newMaterial.SetTextureScale("_BumpMap", mat.GetTextureScale("_DetailNormal"));
                newMaterial.SetFloat("_NormalStrength", mat.HasProperty("_DetailNormalMapScale") ? mat.GetFloat("_DetailNormalMapScale") : 1);

                newMaterial.SetTexture("_Tangent", detailNormal);
                newMaterial.SetTextureOffset("_Tangent", mat.GetTextureOffset("_DetailNormal"));
                newMaterial.SetTextureScale("_Tangent", mat.GetTextureScale("_DetailNormal"));
            }
            else
            {
                newMaterial.SetTexture("_Tangent", normalMap);
                newMaterial.SetTextureOffset("_Tangent", mat.GetTextureOffset("_BumpMap"));
                newMaterial.SetTextureScale("_Tangent", mat.GetTextureScale("_BumpMap"));

                newMaterial.SetTexture("_DetailNormalMap", detailNormal);
                newMaterial.SetTextureOffset("_DetailNormalMap", mat.GetTextureOffset("_DetailNormal"));
                newMaterial.SetTextureScale("_DetailNormalMap", mat.GetTextureScale("_DetailNormal"));
                newMaterial.SetFloat("_DetailNormalMapScale", mat.HasProperty("_DetailNormalMapScale") ? mat.GetFloat("_DetailNormalMapScale") : 1);
            }

            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f, 0f, 0.95f));

            newMaterial.SetInt("_HairEffect", 1);
        }

        private static void SwapPropertiesTwoColors(Material mat, Material newMaterial)
        {
            //newMaterial.CopyPropertiesFromMaterial(mat);

            SwapCommonProperties(mat, newMaterial);

            newMaterial.SetColor("_Color_2", mat.GetColor("_Color_2"));
            newMaterial.SetColor("_Color_3", mat.GetColor("_Color_3"));
            newMaterial.SetColor("_Color_4", mat.GetColor("_Color_4"));


            newMaterial.SetColor("_SpecColor_2", mat.GetColor("_SpecColor_2"));
            newMaterial.SetColor("_SpecColor_3", mat.GetColor("_SpecColor_3"));
            newMaterial.SetColor("_SpecColor_4", mat.GetColor("_SpecColor_4"));

            newMaterial.SetTexture("_DetailNormalMap", mat.GetTexture("_DetailNormalMap"));
            newMaterial.SetTextureOffset("_DetailNormalMap", mat.GetTextureOffset("_DetailNormalMap"));
            newMaterial.SetTextureScale("_DetailNormalMap", mat.GetTextureScale("_DetailNormalMap"));

            newMaterial.SetTexture("_DetailNormalMap_2", mat.GetTexture("_DetailNormalMap_2"));
            newMaterial.SetTextureOffset("_DetailNormalMap_2", mat.GetTextureOffset("_DetailNormalMap_2"));
            newMaterial.SetTextureScale("_DetailNormalMap_2", mat.GetTextureScale("_DetailNormalMap_2"));

            newMaterial.SetTexture("_DetailNormalMap_3", mat.GetTexture("_DetailNormalMap_3"));
            newMaterial.SetTextureOffset("_DetailNormalMap_3", mat.GetTextureOffset("_DetailNormalMap_3"));
            newMaterial.SetTextureScale("_DetailNormalMap_3", mat.GetTextureScale("_DetailNormalMap_3"));

            newMaterial.SetTexture("_DetailNormalMap_4", mat.GetTexture("_DetailNormalMap_4"));
            newMaterial.SetTextureOffset("_DetailNormalMap_4", mat.GetTextureOffset("_DetailNormalMap_4"));
            newMaterial.SetTextureScale("_DetailNormalMap_4", mat.GetTextureScale("_DetailNormalMap_4"));

            newMaterial.SetTexture("_DetailMask", mat.GetTexture("_DetailMask"));
            newMaterial.SetTextureOffset("_DetailMask", mat.GetTextureOffset("_DetailMask"));
            newMaterial.SetTextureScale("_DetailMask", mat.GetTextureScale("_DetailMask"));

            newMaterial.SetTexture("_Colormask", mat.GetTexture("_Colormask"));
            newMaterial.SetTextureOffset("_Colormask", mat.GetTextureOffset("_Colormask"));
            newMaterial.SetTextureScale("_Colormask", mat.GetTextureScale("_Colormask"));

            newMaterial.SetFloat("_DetailNormalMapScale", mat.GetFloat("_DetailNormalMapScale"));
            newMaterial.SetFloat("_DetailNormalMapScale_2", mat.GetFloat("_DetailNormalMapScale_2"));
            newMaterial.SetFloat("_DetailNormalMapScale_3", mat.GetFloat("_DetailNormalMapScale_3"));
            newMaterial.SetFloat("_DetailNormalMapScale_4", mat.GetFloat("_DetailNormalMapScale_4"));

            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.GetFloat("_Cutoff"), 0.01f, 0.95f));

            newMaterial.SetTexture("_Emission", null);
            newMaterial.SetColor("_EmissionColor", Color.black);
        }

        private static void SwapPropertiesTwoColorsAnimated(Material mat, Material newMaterial)
        {
            SwapCommonProperties(mat, newMaterial);

            newMaterial.SetColor("_Color_3", mat.GetColor("_Color_3"));
            newMaterial.SetColor("_Color_4", mat.GetColor("_Color_4"));

            newMaterial.SetColor("_SpecColor_3", mat.GetColor("_SpecColor_3"));
            newMaterial.SetColor("_SpecColor_4", mat.GetColor("_SpecColor_4"));

            newMaterial.SetTexture("_DetailNormalMap", mat.GetTexture("_DetailNormalMap"));
            newMaterial.SetTextureOffset("_DetailNormalMap", mat.GetTextureOffset("_DetailNormalMap"));
            newMaterial.SetTextureScale("_DetailNormalMap", mat.GetTextureScale("_DetailNormalMap"));

            newMaterial.SetTexture("_DetailNormalMap_3", mat.GetTexture("_DetailNormalMap_3"));
            newMaterial.SetTextureOffset("_DetailNormalMap_3", mat.GetTextureOffset("_DetailNormalMap_3"));
            newMaterial.SetTextureScale("_DetailNormalMap_3", mat.GetTextureScale("_DetailNormalMap_3"));

            newMaterial.SetTexture("_DetailMask", mat.GetTexture("_DetailMask"));
            newMaterial.SetTextureOffset("_DetailMask", mat.GetTextureOffset("_DetailMask"));
            newMaterial.SetTextureScale("_DetailMask", mat.GetTextureScale("_DetailMask"));

            newMaterial.SetTexture("_Colormask", mat.GetTexture("_Colormask"));
            newMaterial.SetTextureOffset("_Colormask", mat.GetTextureOffset("_Colormask"));
            newMaterial.SetTextureScale("_Colormask", mat.GetTextureScale("_Colormask"));

            newMaterial.SetTexture("_EffectMap", mat.GetTexture("_EffectMap"));
            newMaterial.SetTextureOffset("_EffectMap", mat.GetTextureOffset("_EffectMap"));
            newMaterial.SetTextureScale("_EffectMap", mat.GetTextureScale("_EffectMap"));

            newMaterial.SetFloat("_DetailNormalMapScale", mat.GetFloat("_DetailNormalMapScale"));
            newMaterial.SetFloat("_DetailNormalMapScale_3", mat.GetFloat("_DetailNormalMapScale_3"));

            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.GetFloat("_Cutoff"), 0.01f, 0.95f));

            newMaterial.SetFloat("_RimPower", mat.GetFloat("_RimPower"));
            newMaterial.SetFloat("_Refraction", mat.GetFloat("_Refraction"));
            newMaterial.SetFloat("_EffectContrast", mat.GetFloat("_EffectContrast"));
            newMaterial.SetFloat("_Effect2Power", mat.GetFloat("_Effect2Power"));
            newMaterial.SetVector("_UVScroll", mat.GetVector("_UVScroll"));

            newMaterial.SetTexture("_Emission", null);
            newMaterial.SetColor("_EmissionColor", Color.white);
        }

        private static void SwapPropertiesTwoLayers(Material mat, Material newMaterial)
        {
            //newMaterial.CopyPropertiesFromMaterial(mat);

            SwapCommonProperties(mat, newMaterial);

            newMaterial.SetColor("_Color_2", mat.GetColor("_Color_2"));

            newMaterial.SetColor("_SpecColor_2", mat.GetColor("_SpecColor_2"));

            newMaterial.SetTexture("_OverTex", mat.GetTexture("_OverTex"));
            newMaterial.SetTextureOffset("_OverTex", mat.GetTextureOffset("_OverTex"));
            newMaterial.SetTextureScale("_OverTex", mat.GetTextureScale("_OverTex"));

            newMaterial.SetTexture("_BumpMap2", mat.GetTexture("_BumpMap2"));
            newMaterial.SetTextureOffset("_BumpMap2", mat.GetTextureOffset("_BumpMap2"));
            newMaterial.SetTextureScale("_BumpMap2", mat.GetTextureScale("_BumpMap2"));

            newMaterial.SetFloat("_NormalStrength2", mat.HasProperty("_NormalStrength2") ? mat.GetFloat("_NormalStrength2") : 1);

            newMaterial.SetTexture("_SpecGlossMap2", mat.GetTexture("_SpecGlossMap2"));
            newMaterial.SetTextureOffset("_SpecGlossMap2", mat.GetTextureOffset("_SpecGlossMap2"));
            newMaterial.SetTextureScale("_SpecGlossMap2", mat.GetTextureScale("_SpecGlossMap2"));

            newMaterial.SetTexture("_BlendNormalMap", mat.GetTexture("_BlendNormalMap"));
            newMaterial.SetTextureOffset("_BlendNormalMap", mat.GetTextureOffset("_BlendNormalMap"));
            newMaterial.SetTextureScale("_BlendNormalMap", mat.GetTextureScale("_BlendNormalMap"));

            newMaterial.SetTexture("_DetailNormalMap", mat.GetTexture("_DetailNormalMap"));
            newMaterial.SetTextureOffset("_DetailNormalMap", mat.GetTextureOffset("_DetailNormalMap"));
            newMaterial.SetTextureScale("_DetailNormalMap", mat.GetTextureScale("_DetailNormalMap"));

            newMaterial.SetTexture("_DetailMask", mat.GetTexture("_DetailMask"));
            newMaterial.SetTextureOffset("_DetailMask", mat.GetTextureOffset("_DetailMask"));
            newMaterial.SetTextureScale("_DetailMask", mat.GetTextureScale("_DetailMask"));

            newMaterial.SetFloat("_BlendNormalMapScale", mat.GetFloat("_BlendNormalMapScale"));
            newMaterial.SetFloat("_DetailNormalMapScale", mat.GetFloat("_DetailNormalMapScale"));
            newMaterial.SetFloat("_DetailMask2", mat.GetFloat("_DetailMask2"));
            newMaterial.SetFloat("_Cutoff", Mathf.Clamp(mat.GetFloat("_Cutoff"), 0f, 0.95f));

            newMaterial.SetTexture("_Emission", null);
            newMaterial.SetColor("_EmissionColor", Color.black);
        }

        private static void SetMaterialKeywords(Material material)
        {
            SetKeyword(material, "_NORMALMAP", CheckTexture(material, "_BumpMap") || CheckTexture(material, "_BumpMap2") || CheckTexture(material, "_BlendNormalMap") || CheckTexture(material, "_DetailNormalMap") || CheckTexture(material, "_DetailNormalMap_2") || CheckTexture(material, "_DetailNormalMap_3") || CheckTexture(material, "_DetailNormalMap_4"));
            SetKeyword(material, "_SPECGLOSSMAP", CheckTexture(material, "_SpecGlossMap") || CheckTexture(material, "_SpecGlossMap2"));
            SetKeyword(material, "_PARALLAXMAP", CheckTexture(material, "_Transmission"));
            SetKeyword(material, "_DETAIL_MULX2", CheckTexture(material, "_DetailNormalMap") || CheckTexture(material, "_DetailNormalMap_2") || CheckTexture(material, "_DetailNormalMap_3") || CheckTexture(material, "_DetailNormalMap_4"));

            bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black);
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            // Setup lightmap emissive flags
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (!shouldEmissionBeEnabled)
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }

        private static void SetMaterialKeywordsAnisotropic(Material material)
        {
            SetKeyword(material, "OCCLUSION_ON", CheckTexture(material, "_OcclusionMap"));
            SetKeyword(material, "SPECULAR_ON", true);
            SetKeyword(material, "NORMALMAP", CheckTexture(material, "_BumpMap"));
            SetKeyword(material, "FUZZ_ON", CheckTexture(material, "_FuzzTex"));
            SetKeyword(material, "DETAILNORMAL_ON", CheckTexture(material, "_DetailNormal"));

            SetMaterialKeywords(material);
        }

        private static bool CheckTexture(Material mat, string property)
        {
            return mat.HasProperty(property) && mat.GetTexture(property) != null;
        }

        private static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        private static bool ShouldEmissionBeEnabled(Color color)
        {
            return color.maxColorComponent > (0.1f / 255.0f);
        }

        private static IEnumerable<CodeInstruction> ChangeMaterialTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Stloc_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Call, typeof(HSStandard).GetMethod(nameof(ChangeMaterialTranspilerInjected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
            }
        }

        private static void ChangeMaterialTranspilerInjected(Material material, CharReference.TagObjKey key)
        {
            if (material != null)
                switch (key)
                {
                    case CharReference.TagObjKey.ObjEyeHi:
                        material.renderQueue += 10;
                        break;
                    case CharReference.TagObjKey.ObjNip:
                        material.renderQueue -= 1;
                        material.SetFloat("_Cutoff", 0f);
                        break;
                    case CharReference.TagObjKey.ObjUnderHair:
                        material.SetFloat("_Cutoff", 0f);
                        material.SetInt("_ZWrite", 0); //Might remove in the future if more problem arise
                        material.DisableKeyword("_NORMALMAP");
                        break;
                    case CharReference.TagObjKey.ObjEyebrow:
                        material.SetInt("_ZWrite", 0); //This might be replaced by disabling ZWrite on PBRsp_texture_alpha altoghether, but this might break other things, I don't know, we'll see I guess.
                        material.renderQueue += 11;
                        break;
                }
        }

        private static void SetIsHairFalse()
        {
            _isHair = false;
        }

        private static void ResetIsHair()
        {
            _isHair = true;
        }

        //private static void SetForceHairTrue()
        //{
        //    _forceHair = true;
        //}

        //private static void ResetForceHair()
        //{
        //    _forceHair = false;
        //}

        private static void SetIsSkinTrue()
        {
            _isSkin = true;
        }

        private static void ResetIsSkin()
        {
            _isSkin = false;
        }

        private static void SetIsBodyStuffTrue()
        {
            _isBodyStuff = true;
        }

        private static void ResetIsBodyStuff()
        {
            _isBodyStuff = false;
        }

        private static void SetIsClothesTrue()
        {
            _isClothes = true;
        }

        private static void ResetIsClothes()
        {
            _isClothes = false;
        }

        private static void SetIsClothesTrueIsHairFalse()
        {
            SetIsClothesTrue();
            SetIsHairFalse();
        }

        private static void ResetIsClothesIsHair()
        {
            ResetIsClothes();
            ResetIsHair();
        }

        private static void SetIsBodyStuffTrueIsHairFalse()
        {
            SetIsBodyStuffTrue();
            SetIsHairFalse();
        }

        private static void ResetIsBodyStuffIsHair()
        {
            ResetIsBodyStuff();
            ResetIsHair();
        }

        private static void SetIsVanillaMapTrue()
        {
            _isVanillaMap = true;
        }

        private static void ResetIsVanillaMap()
        {
            _isVanillaMap = false;
        }

        private static void OnChangeAccessoryPostfixSingle(CharBody __instance, int _slotNo)
        {
            if (_ignoreSwap == false)
            {
                GameObject accessoryObj = __instance.objAccessory[_slotNo];
                if (accessoryObj != null)
                {
                    foreach (Renderer renderer in accessoryObj.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer.CompareTag("ObjHairAcs") && renderer.sharedMaterials.Length == 1)
                        {
                            AddDepthTextureOnlyMaterial(renderer);
                        }
                    }
                }
            }
            ResetIsClothes();
        }

        private static void OnChangeAccessoryPostfix(CharBody __instance)
        {
            if (_ignoreSwap == false)
            {
                for (int i = 0; i < 10; i++)
                {
                    GameObject accessoryObj = __instance.objAccessory[i];
                    if (accessoryObj != null)
                    {
                        foreach (Renderer renderer in accessoryObj.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer.CompareTag("ObjHairAcs") && renderer.sharedMaterials.Length == 1)
                                AddDepthTextureOnlyMaterial(renderer);
                        }
                    }
                }
            }
            ResetIsClothes();
        }

        private static void OnChangeAccessoryPostfixMoreAccessories(object data, int _slotNo)
        {
            if (_ignoreSwap == false)
            {
                List<GameObject> objAccessory = (List<GameObject>)_moreAccCharAdditionalDataObjAccessory.GetValue(data);
                if (objAccessory != null)
                {
                    GameObject accessoryObj = objAccessory[_slotNo];
                    if (accessoryObj != null)
                    {
                        foreach (Renderer renderer in accessoryObj.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer.CompareTag("ObjHairAcs") && renderer.sharedMaterials.Length == 1)
                            {
                                AddDepthTextureOnlyMaterial(renderer);
                            }
                        }
                    }
                }
            }
            ResetIsClothes();
        }

        private static void AddDepthTextureOnlyMaterialToFemaleHair(CharFemaleBody __instance)
        {
            if (_ignoreSwap)
                return;
            foreach (GameObject gameObject in __instance.objHair)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Any(r => r.materials.Any(m => m.shader.name.EndsWith("blend", StringComparison.OrdinalIgnoreCase))))
                    continue;
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterials.Length == 1)
                    {
                        AddDepthTextureOnlyMaterial(renderer);
                    }
                }
            }
        }

        private static void AddDepthTextureOnlyMaterialToMaleHair(CharMaleBody __instance)
        {
            if (_ignoreSwap)
                return;
            foreach (GameObject gameObject in __instance.objHair)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Any(r => r.materials.Any(m => m.shader.name.EndsWith("blend", StringComparison.OrdinalIgnoreCase))))
                    continue;
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterials.Length == 1)
                    {
                        AddDepthTextureOnlyMaterial(renderer);
                    }
                }
            }
        }

        private static void AddDepthTextureOnlyMaterial(Renderer renderer)
        {
            Material mat = renderer.materials[0];
            if (mat.GetTag("RenderType", false).StartsWith("Transparent"))
            {
                Material[] newMaterials = new Material[2];
                newMaterials[0] = mat;
                Material depthOnlyMat = new Material(_depthTextureOnly);
                depthOnlyMat.SetTexture("_MainTex", mat.GetTexture("_MainTex"));
                newMaterials[1] = depthOnlyMat;
                renderer.materials = newMaterials;
            }
        }

        private static class CustomTextureContrl_RebuildTextureAndSetMaterial_Patches
        {
            public static void Postfix(Texture ___texMain, RenderTexture ___createTex)
            {
                if (_ignoreSwap)
                    return;


                RenderTexture temp = RenderTexture.GetTemporary(___createTex.width, ___createTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                bool sRGBWrite = GL.sRGBWrite;
                GL.sRGBWrite = true;

                RenderTexture active = RenderTexture.active;
                RenderTexture.active = null;

                Graphics.Blit(Texture2D.whiteTexture, temp);
                _addAlpha.SetTexture("_AlphaTex", ___texMain);
                Graphics.Blit(___createTex, temp, _addAlpha);
                Graphics.Blit(temp, ___createTex);

                RenderTexture.ReleaseTemporary(temp);
                RenderTexture.active = active;

                GL.sRGBWrite = sRGBWrite;
            }
        }

        private static bool UpdateSiruPrefix(CharFemaleBody __instance, bool forceChange, CharFileInfoStatusFemale ___femaleStatusInfo)
        {
            if (_ignoreSwap)
                return true;
            if (null != __instance.customMatFace)
            {
                const int faceIndex = 0;
                if (forceChange || ___femaleStatusInfo.siruLv[faceIndex] != __instance.siruNewLv[faceIndex])
                {
                    ___femaleStatusInfo.siruLv[faceIndex] = __instance.siruNewLv[faceIndex];
                    int newLength = 3;
                    if (__instance.siruNewLv[faceIndex] == 0)
                        newLength = 2;
                    Material[] newMaterials = new Material[newLength];
                    newMaterials[0] = __instance.customMatFace;
                    newMaterials[1] = SwapShaderIgnoreProjector(__instance.matHohoAka);
                    List<GameObject> faceSkinObjects = __instance.chaInfo.GetTagInfo(CharReference.TagObjKey.ObjSkinFace);
                    Material[] materials = faceSkinObjects[0].GetComponent<Renderer>().materials;
                    if (materials.Length >= 2)
                        newMaterials[1] = materials[1];
                    newMaterials[1].SetOverrideTag("IgnoreProjector", "True");
                    if (__instance.siruNewLv[faceIndex] != 0)
                    {
                        string key = CharDefine.SiruParts.SiruKao + __instance.siruNewLv[faceIndex].ToString("00");
                        Material material = null;
                        if (Singleton<Manager.Character>.Instance.dictSiruMaterial.TryGetValue(key, out material))
                            newMaterials[2] = SwapShaderIgnoreProjector(material);
                    }
                    foreach (GameObject gameObject in faceSkinObjects)
                    {
                        if (null != gameObject)
                            gameObject.GetComponent<Renderer>().materials = newMaterials;
                    }
                }
            }
            if (null != __instance.customMatBody)
            {
                CharDefine.SiruParts[] siruParts =
                {
                        CharDefine.SiruParts.SiruFrontUp,
                        CharDefine.SiruParts.SiruFrontDown,
                        CharDefine.SiruParts.SiruBackUp,
                        CharDefine.SiruParts.SiruBackDown
                    };
                List<string> list = new List<string>();
                bool flag = false;
                for (int j = 0; j < siruParts.Length; j++)
                {
                    if (forceChange || ___femaleStatusInfo.siruLv[(int)siruParts[j]] != __instance.siruNewLv[(int)siruParts[j]])
                        flag = true;
                    if (__instance.siruNewLv[(int)siruParts[j]] != 0)
                    {
                        string item = siruParts[j] + __instance.siruNewLv[(int)siruParts[j]].ToString("00");
                        list.Add(item);
                    }
                    ___femaleStatusInfo.siruLv[(int)siruParts[j]] = __instance.siruNewLv[(int)siruParts[j]];
                }
                if (flag)
                {
                    int num4 = 1 + list.Count;
                    Material[] newMaterials = new Material[num4];
                    newMaterials[0] = __instance.customMatBody;
                    for (int k = 0; k < list.Count; k++)
                    {
                        Material material;
                        if (Singleton<Manager.Character>.Instance.dictSiruMaterial.TryGetValue(list[k], out material))
                        {
                            material.SetOverrideTag("IgnoreProjector", "True");
                            material = SwapShaderIgnoreProjector(material);
                            material.renderQueue -= 1;
                            newMaterials[1 + k] = material;
                        }
                    }
                    List<GameObject> bodySkinObjects = __instance.chaInfo.GetTagInfo(CharReference.TagObjKey.ObjSkinBody);
                    foreach (GameObject gameObject in bodySkinObjects)
                    {
                        if (gameObject != null)
                            gameObject.GetComponent<Renderer>().materials = newMaterials;
                    }
                }
            }

            return false;
        }

        private static Material SwapShaderIgnoreProjector(Material source)
        {
            switch (source.shader.name)
            {
                case "Standard":
                    Material newMaterial = new Material(_standardIgnoreProjector);
                    newMaterial.name = source.name;
                    newMaterial.renderQueue = source.renderQueue;
                    newMaterial.CopyPropertiesFromMaterial(source);
                    return newMaterial;
            }
            return source;
        }

        #endregion
    }

    internal class DummyAssetBundleLoadAssetOperation : AssetBundleLoadAssetOperation
    {
        private readonly AssetBundleLoadAssetOperation _original;

        public DummyAssetBundleLoadAssetOperation(AssetBundleLoadAssetOperation original)
        {
            this._original = original;
        }

        public override bool Update()
        {
            return this._original.Update();
        }

        public override bool IsDone()
        {
            return this._original.IsDone();
        }

        public override bool IsEmpty()
        {
            return this._original.IsEmpty();
        }

        public override T GetAsset<T>()
        {
            T asset = this._original.GetAsset<T>();
            return HSStandard.HandleAsset(asset);
        }

        public override T[] GetAllAssets<T>()
        {
            T[] assets = this._original.GetAllAssets<T>();
            for (int i = 0; i < assets.Length; i++)
                assets[i] = HSStandard.HandleAsset(assets[i]);
            return assets;
        }
    }
}
