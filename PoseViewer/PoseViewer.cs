using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using IllusionPlugin;
using IllusionUtility.SetUtility;
using Studio;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PoseViewer
{
    public class PoseViewer : IEnhancedPlugin
    {
        #region IPA
        public const string versionNum = "1.0.0";

        public string Name { get { return "PoseViewer"; } }
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }
        #endregion

        #region Private Variables
        private static PoseViewer _self;
        private PauseRegistrationList _original;
        private Camera _poseCamera;
        private OCICharFemale _femaleMannequin;
        private OCICharMale _maleMannequin;
        private Light _light;
        private float _cachedAmbientIntensity;
        private AmbientMode _cachedAmbientMode;
        private Color _cachedAmbientColor;
        private RawImage _previewImage;
        #endregion

        #region Unity Methods
        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnApplicationStart()
        {
            _self = this;
        }

        public void OnApplicationQuit()
        {

        }

        public void OnLevelWasLoaded(int level)
        {
            if (level == 3)
                this.ExecuteDelayed(this.Init, 10);
        }

        public void OnUpdate()
        {
        }
        #endregion

        #region Private Methods
        private void Init()
        {
            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
                camera.cullingMask &= ~(1 << 31);
            this._poseCamera = new GameObject("PoseCamera", typeof(Camera)).GetComponent<Camera>();
            this._poseCamera.cullingMask = 1 << 31;
            this._poseCamera.depth = -10;
            this._poseCamera.fieldOfView = 23;
            this._poseCamera.nearClipPlane = 0.08f;
            this._poseCamera.renderingPath = RenderingPath.Forward;
            this._poseCamera.gameObject.SetActive(false);

            this._light = new GameObject("Light", typeof(Light)).GetComponent<Light>();
            this._light.transform.SetParent(this._poseCamera.transform);
            this._light.transform.localPosition = Vector3.zero;
            this._light.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);
            this._light.cullingMask = 1 << 31;
            this._light.color = Color.white;
            this._light.intensity = 1f;
            this._light.shadows = LightShadows.None;
            this._light.type = LightType.Directional;

            this.SpawnGUI();
            var harmonyInstance = HarmonyExtensions.CreateInstance("com.joan6694.illusionplugins.poseviewer");
            harmonyInstance.PatchAllSafe();
        }

        private void RecurseGameObject(GameObject obj, Action<GameObject> action)
        {
            action(obj);
            foreach (Transform t in obj.transform)
                this.RecurseGameObject(t.gameObject, action);
        }


        private void SpawnGUI()
        {
            this._original = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/06_Pause").GetComponent<PauseRegistrationList>();

            Image img = this._original.GetComponent<Image>();
            //Color[] newImg = img.sprite.texture.GetPixels((int)img.sprite.textureRect.x, (int)img.sprite.textureRect.y, (int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
            //Texture2D tex = new Texture2D((int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
            //tex.SetPixels(newImg);
            //tex.Apply();
            img.sprite = Sprite.Create(img.sprite.texture, img.sprite.textureRect, img.sprite.pivot, img.sprite.pixelsPerUnit, 0, SpriteMeshType.Tight, new Vector4(101, 33, 20, 74));
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            ((RectTransform)this._original.transform).offsetMin += new Vector2(0f, -130);
            ((RectTransform)this._original.transform.Find("Button Delete").transform).anchoredPosition += new Vector2(0f, -132f);
            ((RectTransform)this._original.transform.Find("Scroll View").transform).offsetMin += new Vector2(0f, -132f);
            ((RectTransform)this._original.transform.Find("Scroll View/Scrollbar Vertical").transform).offsetMin += new Vector2(0f, -132f);
            this._previewImage = UIUtility.CreateRawImage("Preview", this._original.transform.transform);
            this._previewImage.rectTransform.SetRect(1f, 1f, 1f, 1f, 0f, -352f * 0.7f, 252f * 0.7f, 0f);
            this._previewImage.rectTransform.anchoredPosition += new Vector2(6f, 0f);
            this._previewImage.gameObject.SetActive(false);
        }

        private void LoadPreview(string path)
        {
            Studio.Studio.Instance.StartCoroutine(this.GetImage(path, t =>
            {
                if (this._previewImage.texture != null)
                {
                    Object.Destroy(this._previewImage.texture);
                    this._previewImage.texture = null;
                }
                this._previewImage.texture = t;
            }));

        }

        private IEnumerator GetImage(string path, Action<Texture2D> action)
        {
            if (File.Exists(path + ".png"))
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.ARGB32, true, true);
                tex.LoadImage(File.ReadAllBytes(path + ".png"));
                tex.Apply(true);
                action(tex);
            }
            else
                yield return this.GenerateSingleImage(path, action);
        }

        private void OnPreGeneration()
        {
            this._poseCamera.gameObject.SetActive(true);
            if (this._original.ociChar.charInfo.Sex == 0)
            {
                this._poseCamera.transform.SetPosition(0f, 0.92f, 4.89f);
                this._poseCamera.transform.SetRotation(0f, 180f, 0f);
            }
            else
            {
                this._poseCamera.transform.SetPosition(0f, 0.87f, 4.7f);
                this._poseCamera.transform.SetRotation(0f, 180f, 0f);
            }

            if (this._original.ociChar.charInfo.Sex == 0)
            {
                if (this._maleMannequin == null)
                    this.CreateMaleMannequin();
                this._maleMannequin.charInfo.gameObject.SetActive(true);
            }
            else
            {
                if (this._femaleMannequin == null)
                    this.CreateFemaleMannequin();
                this._femaleMannequin.charInfo.gameObject.SetActive(true);
            }

            foreach (Light light in Resources.FindObjectsOfTypeAll<Light>())
            {
                if (light != this._light)
                    light.cullingMask = light.cullingMask & ~(1 << 31);
            }
            this._cachedAmbientIntensity = RenderSettings.ambientIntensity;
            this._cachedAmbientMode = RenderSettings.ambientMode;
            this._cachedAmbientColor = RenderSettings.ambientSkyColor;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = Color.white;
        }

        private void OnPostGeneration()
        {
            this._poseCamera.gameObject.SetActive(false);
            if (this._maleMannequin != null)
                this._maleMannequin.charInfo.gameObject.SetActive(false);
            if (this._femaleMannequin != null)
                this._femaleMannequin.charInfo.gameObject.SetActive(false);
            RenderSettings.ambientIntensity = this._cachedAmbientIntensity;
            RenderSettings.ambientMode = this._cachedAmbientMode;
            RenderSettings.ambientSkyColor = this._cachedAmbientColor;
        }

        private IEnumerator GenerateSingleImage(string path, Action<Texture2D> onGenerated)
        {
            this.OnPreGeneration();

            if (this._original.ociChar.charInfo.Sex == 0)
                PauseCtrl.Load(this._maleMannequin, path);
            else
                PauseCtrl.Load(this._femaleMannequin, path);

            yield return new WaitForEndOfFrame();

            byte[] pngData = null;
            Texture2D screenshot = this.TakeScreenshot(ref pngData);
            File.WriteAllBytes(path + ".png", screenshot.EncodeToPNG());
            onGenerated(screenshot);
            this.OnPostGeneration();
        }

        public virtual Texture2D TakeScreenshot(ref byte[] pngData, int createW = 252, int createH = 352)
        {
            if (createW == 0 || createH == 0)
            {
                createW = 252;
                createH = 352;
            }
            Vector2 screenSize = ScreenInfo.GetScreenSize();
            float screenRate = ScreenInfo.GetScreenRate();
            float screenCorrectY = ScreenInfo.GetScreenCorrectY();
            float num = 720f * screenRate / screenSize.y;
            int num2 = 504;
            int num3 = 704;
            int num4 = num2;
            int num5 = num3;
            RenderTexture temporary = RenderTexture.GetTemporary((int)(1280f / num), (int)(720f / num), 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
            {
                Camera coordinateCamera = this._poseCamera;
                RenderTexture targetTexture = coordinateCamera.targetTexture;
                Rect rect = coordinateCamera.rect;
                coordinateCamera.targetTexture = temporary;
                coordinateCamera.Render();
                coordinateCamera.targetTexture = targetTexture;
                coordinateCamera.rect = rect;
            }
            Texture2D texture2D = new Texture2D(num4, num5, TextureFormat.RGB24, false, true);
            RenderTexture.active = temporary;
            float x = 388f + (1280f / num - 1280f) * 0.5f;
            float y = 8f + screenCorrectY / screenRate;
            texture2D.ReadPixels(new Rect(x, y, (float)num4, (float)num5), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temporary);
            if (num4 != createW || num5 != createH)
            {
                TextureScale.Bilinear(texture2D, createW, createH);
            }
            return texture2D;
        }

        private void CreateFemaleMannequin()
        {
            int num = -1;
            GameObject gameObject = new GameObject("mannequinFemale");
            CharFemale charFemale = gameObject.AddComponent<CharFemale>();
            charFemale.Initialize(gameObject, -1, num);
            OICharInfo info = new OICharInfo(charFemale.femaleFile, -1);

            this._femaleMannequin = new OCICharFemale();
            charFemale.Load(true);
            charFemale.InitializeExpression(true);
            this._femaleMannequin.charInfo = charFemale;
            this._femaleMannequin.charBody = charFemale.femaleBody;
            this._femaleMannequin.charReference = charFemale;
            this._femaleMannequin.animeIKCtrl = charFemale.femaleBody.animIKCtrl;
            this._femaleMannequin.objectInfo = info;
            this._femaleMannequin.optionItemCtrl = charFemale.gameObject.AddComponent<OptionItemCtrl>();
            this._femaleMannequin.optionItemCtrl.animator = charFemale.animBody;
            this._femaleMannequin.optionItemCtrl.oiCharInfo = info;
            this._femaleMannequin.charAnimeCtrl = charFemale.gameObject.AddComponent<CharAnimeCtrl>();
            this._femaleMannequin.charAnimeCtrl.animator = charFemale.animBody;
            this._femaleMannequin.charAnimeCtrl.oiCharInfo = info;
            AddObjectAssist.InitHandAnime(this._femaleMannequin);
            if (info.animeInfo.group == 0 && info.animeInfo.category == 2 && info.animeInfo.no == 11)
            {
                int group = info.animeInfo.group;
                int category = info.animeInfo.category;
                int no = info.animeInfo.no;
                float animeNormalizedTime = info.animeNormalizedTime;
                this._femaleMannequin.LoadAnime(0, 1, 0, 0f);
                charFemale.animBody.Update(0f);
                info.animeInfo.group = group;
                info.animeInfo.category = category;
                info.animeInfo.no = no;
                info.animeNormalizedTime = animeNormalizedTime;
            }

            AddObjectAssist.InitBone(this._femaleMannequin, charFemale.femaleBody.transform, Singleton<Info>.Instance.dicFemaleBoneInfo);
            AddObjectAssist.InitIKTarget(this._femaleMannequin, true);
            AddObjectAssist.InitLookAt(this._femaleMannequin);
            this._femaleMannequin.voiceCtrl.ociChar = this._femaleMannequin;
            this._femaleMannequin.InitKinematic(charFemale.gameObject, charFemale.femaleBody.animIKCtrl, charFemale.femaleBody.neckLookCtrl, AddObjectFemale.GetHairDynamic(charFemale.femaleBody), AddObjectFemale.GetSkirtDynamic(charFemale.femaleBody));

            this._femaleMannequin.LoadAnime(info.animeInfo.group, info.animeInfo.category, info.animeInfo.no, info.animeNormalizedTime);
            for (int l = 0; l < 5; l++)
            {
                this._femaleMannequin.ActiveIK((OIBoneInfo.BoneGroup)(1 << l), info.activeIK[l], false);
            }
            this._femaleMannequin.ActiveKinematicMode(OICharInfo.KinematicMode.IK, info.enableIK, true);
            for (int i = 0; i < FKCtrl.parts.Length; i++)
            {
                OIBoneInfo.BoneGroup boneGroup = FKCtrl.parts[i];
                this._femaleMannequin.ActiveFK(boneGroup, this._femaleMannequin.oiCharInfo.activeFK[i], this._femaleMannequin.oiCharInfo.activeFK[i]);
            }
            this._femaleMannequin.ActiveKinematicMode(OICharInfo.KinematicMode.FK, info.enableFK, true);
            for (int j = 0; j < info.expression.Length; j++)
            {
                this._femaleMannequin.charInfo.EnableExpressionCategory(j, info.expression[j]);
            }
            this._femaleMannequin.animeSpeed = this._femaleMannequin.animeSpeed;
            CharFileInfoStatusFemale femaleStatusInfo = charFemale.femaleStatusInfo;
            byte[] siruLv = femaleStatusInfo.siruLv;
            for (int k = 0; k < siruLv.Length; k++)
            {
                siruLv[k] = 0;
            }
            AddObjectAssist.UpdateState(this._femaleMannequin);

            this._femaleMannequin.oiCharInfo.charFile.LoadBlockData(this._femaleMannequin.charInfo.customInfo as CharFileInfoCustomFemale, "custom/presets_f_00.unity3d", "cf_mannequin");
            ((CharFemaleBody)this._femaleMannequin.charBody).LoadMannequinHead();
            this._femaleMannequin.charBody.mannequinMode = true;
            this._femaleMannequin.charInfo.Reload(true, true, true);
            this._femaleMannequin.SetClothesStateAll(2);
            this._femaleMannequin.charBody.ForceUpdate();
            this._femaleMannequin.charInfo.chaCustom.SetBaseMaterial(CharReference.TagObjKey.ObjSkinBody, CommonLib.LoadAsset<Material>("chara/cf_m_base.unity3d", "cf_m_body_mannequin", true, string.Empty));
            this.RecurseGameObject(gameObject, (g) => g.layer = 31);
            this._femaleMannequin.charInfo.gameObject.SetActive(false);

            foreach (OCIChar.IKInfo ikInfo in this._femaleMannequin.listIKTarget)
                GuideObjectManager.Instance.Delete(ikInfo.guideObject, false);
            foreach (OCIChar.BoneInfo boneInfo in this._femaleMannequin.listBones)
                GuideObjectManager.Instance.Delete(boneInfo.guideObject, false);
        }

        private void CreateMaleMannequin()
        {
            int num = -2;
            GameObject gameObject = new GameObject("mannequinMale");
            CharMale charMale = gameObject.AddComponent<CharMale>();
            charMale.Initialize(gameObject, -1, num);
            OICharInfo info = new OICharInfo(charMale.maleFile, -2);

            this._maleMannequin = new OCICharMale();
            charMale.Load(true);
            charMale.InitializeExpression(true);
            this._maleMannequin.charInfo = charMale;
            this._maleMannequin.charBody = charMale.maleBody;
            this._maleMannequin.charReference = charMale;
            this._maleMannequin.animeIKCtrl = charMale.maleBody.animIKCtrl;
            this._maleMannequin.objectInfo = info;
            this._maleMannequin.optionItemCtrl = charMale.gameObject.AddComponent<OptionItemCtrl>();
            this._maleMannequin.optionItemCtrl.animator = charMale.animBody;
            this._maleMannequin.optionItemCtrl.oiCharInfo = info;
            this._maleMannequin.charAnimeCtrl = charMale.gameObject.AddComponent<CharAnimeCtrl>();
            this._maleMannequin.charAnimeCtrl.animator = charMale.animBody;
            this._maleMannequin.charAnimeCtrl.oiCharInfo = info;
            AddObjectAssist.InitHandAnime(this._maleMannequin);
            int group = info.animeInfo.group;
            int category = info.animeInfo.category;
            int no = info.animeInfo.no;
            float animeNormalizedTime = info.animeNormalizedTime;
            this._maleMannequin.LoadAnime(0, 0, 1, 0f);
            charMale.animBody.Update(0f);
            info.animeInfo.group = group;
            info.animeInfo.category = category;
            info.animeInfo.no = no;
            info.animeNormalizedTime = animeNormalizedTime;
            AddObjectAssist.InitBone(this._maleMannequin, charMale.maleBody.transform, Singleton<Info>.Instance.dicMaleBoneInfo);
            AddObjectAssist.InitIKTarget(this._maleMannequin, true);
            AddObjectAssist.InitLookAt(this._maleMannequin);
            this._maleMannequin.voiceCtrl.ociChar = this._maleMannequin;
            List<DynamicBone> list = new List<DynamicBone>();
            foreach (GameObject go in charMale.maleBody.objHair)
                list.AddRange(go.GetComponents<DynamicBone>());
            this._maleMannequin.InitKinematic(charMale.gameObject, charMale.maleBody.animIKCtrl, charMale.maleBody.neckLookCtrl, (from v in list
                                                                                                                 where v != null
                                                                                                                 select v).ToArray<DynamicBone>(), null);
            this._maleMannequin.LoadAnime(info.animeInfo.group, info.animeInfo.category, info.animeInfo.no, info.animeNormalizedTime);
            this._maleMannequin.ActiveKinematicMode(OICharInfo.KinematicMode.IK, info.enableIK, true);
            for (int j = 0; j < 5; j++)
            {
                this._maleMannequin.ActiveIK((OIBoneInfo.BoneGroup)(1 << j), info.activeIK[j], false);
            }
            for (int i = 0; i < FKCtrl.parts.Length; i++)
            {
                OIBoneInfo.BoneGroup boneGroup = FKCtrl.parts[i];
                this._maleMannequin.ActiveFK(boneGroup, this._maleMannequin.oiCharInfo.activeFK[i], this._maleMannequin.oiCharInfo.activeFK[i]);
            }
            this._maleMannequin.ActiveKinematicMode(OICharInfo.KinematicMode.FK, info.enableFK, true);
            for (int k = 0; k < info.expression.Length; k++)
            {
                this._maleMannequin.charInfo.EnableExpressionCategory(k, info.expression[k]);
            }
            this._maleMannequin.animeSpeed = this._maleMannequin.animeSpeed;
            this._maleMannequin.SetVisibleSon(info.visibleSon);
            this._maleMannequin.SetVisibleSimple(info.visibleSimple);
            this._maleMannequin.SetSimpleColor(info.simpleColor.rgbaDiffuse);
            AddObjectAssist.UpdateState(this._maleMannequin);

            this._maleMannequin.oiCharInfo.charFile.LoadBlockData(this._maleMannequin.charInfo.customInfo as CharFileInfoCustomMale, "custom/presets_m_00.unity3d", "cm_mannequin");
            ((CharMaleBody)this._maleMannequin.charBody).LoadMannequinHead();
            this._maleMannequin.charBody.mannequinMode = true;
            this._maleMannequin.charInfo.Reload(true, true, true);
            this._maleMannequin.SetClothesStateAll(2);
            this._maleMannequin.charBody.ForceUpdate();
            this._maleMannequin.charInfo.chaCustom.SetBaseMaterial(CharReference.TagObjKey.ObjSkinBody, CommonLib.LoadAsset<Material>("chara/cm_m_base.unity3d", "cm_m_body_mannequin", true, string.Empty));
            this.RecurseGameObject(gameObject, (g) => g.layer = 31);
            this._maleMannequin.charInfo.gameObject.SetActive(false);

            foreach (OCIChar.IKInfo ikInfo in this._maleMannequin.listIKTarget)
                GuideObjectManager.Instance.Delete(ikInfo.guideObject, false);
            foreach (OCIChar.BoneInfo boneInfo in this._maleMannequin.listBones)
                GuideObjectManager.Instance.Delete(boneInfo.guideObject, false);
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(PauseRegistrationList), "OnClickSelect", typeof(int))]
        private static class PauseResistrationList_OnClickSelect_Patches
        {
            private static void Postfix(PauseRegistrationList __instance, int _no, Dictionary<int, StudioNode> ___dicNode, List<string> ___listPath)
            {
                _self._previewImage.gameObject.SetActive(_no != -1);
                if (_no != -1)
                    _self.LoadPreview(___listPath[_no]);
            }
        }

        [HarmonyPatch(typeof(PauseCtrl), "Save", typeof(OCIChar), typeof(string))]
        private static class PauseResistrationList_OnClickSave_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                foreach (CodeInstruction inst in instructionsList)
                {
                    if (set == false && inst.opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(PauseResistrationList_OnClickSave_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        set = true;
                    }
                    yield return inst;
                }
            }

            private static void Injected(string path)
            {
                Studio.Studio.Instance.StartCoroutine(_self.GenerateSingleImage(path, Object.Destroy));
            }
        }

        [HarmonyPatch(typeof(PauseRegistrationList), "OnSelectDeleteYes")]
        private static class PauseResistrationList_OnClickDelete_Patches
        {
            private static void Prefix(int ___select, List<string> ___listPath)
            {
                if (File.Exists(___listPath[___select] + ".png"))
                    File.Delete(___listPath[___select] + ".png");
            }
        }

        [HarmonyPatch(typeof(PauseCtrl.FileInfo), "Apply", typeof(OCIChar))]
        private static class PauseCtrl_Apply_Patches
        {
            private static bool Prefix(PauseCtrl.FileInfo __instance, OCIChar _char)
            {
                _char.LoadAnime(__instance.group, __instance.category, __instance.no, __instance.normalizedTime);
                for (int i = 0; i < __instance.activeIK.Length; i++)
                    _char.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), __instance.activeIK[i], false);
                _char.ActiveKinematicMode(OICharInfo.KinematicMode.IK, __instance.enableIK, true);
                foreach (KeyValuePair<int, ChangeAmount> keyValuePair in __instance.dicIK)
                    _char.oiCharInfo.ikTarget[keyValuePair.Key].changeAmount.Copy(keyValuePair.Value, true, true, true);
                for (int j = 0; j < __instance.activeFK.Length; j++)
                    _char.ActiveFK(FKCtrl.parts[j], __instance.activeFK[j], false);
                _char.ActiveKinematicMode(OICharInfo.KinematicMode.FK, __instance.enableFK, true);
                foreach (KeyValuePair<int, ChangeAmount> keyValuePair2 in __instance.dicFK)
                {
                    if (_char.oiCharInfo.bones.TryGetValue(keyValuePair2.Key, out OIBoneInfo info))
                        info.changeAmount.Copy(keyValuePair2.Value, true, true, true);
                }
                for (int k = 0; k < __instance.expression.Length; k++)
                    _char.EnableExpressionCategory(k, __instance.expression[k]);
                return false;
            }
        }
        #endregion
    }
}
