using Studio;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT
using IllusionUtility.SetUtility;
#elif KOIKATSU
#endif

namespace HSUS.Features
{
    public class ImprovedTransformOperations : IFeature
    {
        public void Awake()
        {
        }

        public void LevelLoaded()
        {
#if !PLAYHOME
            GameObject canvasGuideInput = GameObject.Find("StudioScene/Canvas Guide Input");
            if (HSUS.ImprovedTransformOperations.Value && HSUS._self.binary == Binary.Studio && canvasGuideInput != null)
                canvasGuideInput.AddComponent<TransformOperations>();
#endif
        }


#if !PLAYHOME
        [HarmonyPatch(typeof(GuideInput), "OnEndEditScale", new[] { typeof(int) })]
        private static class GuideInput_OnEndEditScale_Patches
        {
            private static readonly HarmonyExtensions.Replacement[] _replacements = {
                new HarmonyExtensions.Replacement
                {
                    pattern = new[]
                    {
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Max", new []{typeof(float), typeof(float)})),
                    },
                    replacer = new[]
                    {
                        new CodeInstruction(OpCodes.Call, typeof(GuideInput_OnEndEditScale_Patches).GetMethod(nameof(DummyMax), BindingFlags.NonPublic | BindingFlags.Static)),
                    }
                }
            };

            private static bool Prepare()
            {
                return HSUS._self.binary == Binary.Studio && HSUS.ImprovedTransformOperations.Value;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
            }

            private static float DummyMax(float first, float second)
            {
                return first;
            }
        }

        [HarmonyPatch(typeof(GuideScale), "OnDrag", new[] { typeof(PointerEventData) })]
        private static class GuideScale_OnDrag_Patches
        {

            private static readonly HarmonyExtensions.Replacement[] _replacements = {
#if HONEYSELECT || KOIKATSU
                new HarmonyExtensions.Replacement
                {
                    pattern = new[]
                    {
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Max", new []{typeof(float), typeof(float)})),
                    },
                    replacer = new[]
                    {
                        new CodeInstruction(OpCodes.Call, typeof(GuideScale_OnDrag_Patches).GetMethod(nameof(DummyMax), BindingFlags.NonPublic | BindingFlags.Static)),
                    }
                }
#elif AISHOUJO || HONEYSELECT2
                new HarmonyExtensions.Replacement
                {
                    pattern = new[]
                    {
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Clamp", new []{typeof(float), typeof(float), typeof(float)})),
                    },
                    replacer = new[]
                    {
                        new CodeInstruction(OpCodes.Call, typeof(GuideScale_OnDrag_Patches).GetMethod(nameof(DummyClamp), BindingFlags.NonPublic | BindingFlags.Static)),
                    }
                }
#endif
            };

            private static bool Prepare()
            {
                return HSUS._self.binary == Binary.Studio && HSUS.ImprovedTransformOperations.Value;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
            }

#if HONEYSELECT || KOIKATSU
            private static float DummyMax(float first, float second)
            {
                return first;
            }
#elif AISHOUJO || HONEYSELECT2
            private static float DummyClamp(float value, float min, float max)
            {
                return value;
            }
#endif
        }
#endif

#if HONEYSELECT
        [HarmonyPatch(typeof(CharClothes), "SetAccessoryScl", new[] { typeof(int), typeof(float), typeof(bool), typeof(int) })]
        public class CharClothes_SetAccessoryScl_Patches
        {
            public static bool Prepare()
            {
                return _improvedTransformOperations;
            }

            public static bool Prefix(CharClothes __instance, ref bool __result, int slotNo, float value, bool _add, int flags, CharInfo ___chaInfo, CharFileInfoClothes ___clothesInfo)
            {
                if (!MathfEx.RangeEqualOn(0, slotNo, 9))
                {
                    __result = false;
                    return false;
                }
                GameObject gameObject = ___chaInfo.chaBody.objAcsMove[slotNo];
                if (null == gameObject)
                {
                    __result = false;
                    return false;
                }
                if ((flags & 1) != 0)
                {
                    float num = (!_add ? 0f : ___clothesInfo.accessory[slotNo].addScl.x) + value;
                    ___clothesInfo.accessory[slotNo].addScl.x = num;
                }
                if ((flags & 2) != 0)
                {
                    float num2 = (!_add ? 0f : ___clothesInfo.accessory[slotNo].addScl.y) + value;
                    ___clothesInfo.accessory[slotNo].addScl.y = num2;
                }
                if ((flags & 4) != 0)
                {
                    float num3 = (!_add ? 0f : ___clothesInfo.accessory[slotNo].addScl.z) + value;
                    ___clothesInfo.accessory[slotNo].addScl.z = num3;
                }
                gameObject.transform.SetLocalScale(___clothesInfo.accessory[slotNo].addScl.x, ___clothesInfo.accessory[slotNo].addScl.y, ___clothesInfo.accessory[slotNo].addScl.z);
                __result = true;
                return false;
            }
        }

#endif
#if HONEYSELECT
        [HarmonyPatch(typeof(TreeNodeCtrl), "CopyChangeAmount")]
        internal static class TreeNodeCtrl_CopyChangeAmount_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio && _improvedTransformOperations;
            }

            private static bool Prefix(TreeNodeCtrl __instance)
            {
                TreeNodeObject[] selectNodes = __instance.selectNodes;
                ObjectCtrlInfo objectCtrlInfo = null;
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[0], out objectCtrlInfo))
                {
                    return false;
                }
                List<TreeNodeCommand.MoveCopyInfo> list = new List<TreeNodeCommand.MoveCopyInfo>();
                for (int i = 1; i < selectNodes.Length; i++)
                {
                    ObjectCtrlInfo objectCtrlInfo2 = null;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[i], out objectCtrlInfo2))
                    {
                        list.Add(new TreeNodeCommand.MoveCopyInfo(objectCtrlInfo2.objectInfo.dicKey, objectCtrlInfo2.objectInfo.changeAmount, objectCtrlInfo.objectInfo.changeAmount));
                    }
                }
                UndoRedoManager.Instance.Do(new TreeNodeCommand.MoveCopyCommand(list.ToArray()));
                return false;
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickCopy")]
        internal static class WorkspaceCtrl_OnClickCopy_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio && _improvedTransformOperations;
            }

            private static void Postfix(WorkspaceCtrl __instance)
            {
                foreach (Button button in (Button[])__instance.GetType().GetProperty("buttons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance, null))
                    button.interactable = true;
            }
        }
#endif

#if !PLAYHOME
        public class TransformOperations : MonoBehaviour
        {
            private class TransformData
            {
                public Vector3 position;
                public Vector3 rotation;
                public Vector3 scale;
            }

            private Button _copyTransform;
            private Button _pasteTransform;
            private Button _resetTransform;
            private int _lastObjectCount = 0;
            private HashSet<GuideObject> _hashSelectObject;
            private bool _clipboardEmpty = true;
            private Vector3 _savedPosition;
            private Vector3 _savedRotation;
            private Vector3 _savedScale;

            private bool _draggingXPos = false;
            private bool _draggingYPos = false;
            private bool _draggingZPos = false;
            private bool _draggingXRot = false;
            private bool _draggingYRot = false;
            private bool _draggingZRot = false;
            private bool _draggingXScale = false;
            private bool _draggingYScale = false;
            private bool _draggingZScale = false;
            private readonly Dictionary<GuideObject, TransformData> _originalTransforms = new Dictionary<GuideObject, TransformData>();

            private void Awake()
            {
                RectTransform guideInput = transform.Find("Guide Input") as RectTransform;
                Image container = UIUtility.CreatePanel("Additional Operations", transform);
                container.rectTransform.SetRect(Vector2.zero, Vector2.zero, guideInput.offsetMin + new Vector2(4 + guideInput.rect.width, -1f), guideInput.offsetMin + new Vector2(5 + guideInput.rect.width + 105, guideInput.rect.height + 1f));
                container.color = new Color32(59, 58, 56, 167);
                container.sprite = UIUtility.resources.inputField;

#if HONEYSELECT
                for (int i = 0; i < this.transform.childCount; i++)
                {
                    RectTransform rt = this.transform.GetChild(i) as RectTransform;
                    if (rt != guideInput && rt != container.rectTransform)
                        rt.anchoredPosition += new Vector2(105, 0f);
                }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                ((RectTransform)guideInput.Find("Button Move")).anchoredPosition += new Vector2(100f, 0f);
                ((RectTransform)guideInput.Find("Button Rotation")).anchoredPosition += new Vector2(100f, 0f);
                ((RectTransform)guideInput.Find("Button Scale")).anchoredPosition += new Vector2(100f, 0f);
#endif
                Sprite background = null;
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    switch (sprite.name)
                    {
                        case "sp_sn_12_00_01":
                            background = sprite;
                            goto DOUBLEBREAK;
                    }
                }
            DOUBLEBREAK:
                _copyTransform = UIUtility.CreateButton("Copy Transform", container.transform, "Copy Transform");
                _copyTransform.transform.SetRect(new Vector2(0f, 0.666f), Vector2.one, new Vector2(5f, 1f), new Vector2(-5f, -5f));
                ((Image)_copyTransform.targetGraphic).sprite = background;
                Text t = _copyTransform.GetComponentInChildren<Text>();
                t.color = Color.white;
                t.alignByGeometry = true;
                t.rectTransform.SetRect();
                _copyTransform.onClick.AddListener(CopyTransform);

                _pasteTransform = UIUtility.CreateButton("Paste Transform", container.transform, "Paste Transform");
                _pasteTransform.transform.SetRect(new Vector2(0f, 0.333f), new Vector2(1f, 0.666f), new Vector2(5f, 2f), new Vector2(-5f, -2f));
                ((Image)_pasteTransform.targetGraphic).sprite = background;
                t = _pasteTransform.GetComponentInChildren<Text>();
                t.color = Color.white;
                t.alignByGeometry = true;
                t.rectTransform.SetRect();
                _pasteTransform.onClick.AddListener(PasteTransform);

                _resetTransform = UIUtility.CreateButton("Reset Transform", container.transform, "Reset Transform");
                _resetTransform.transform.SetRect(Vector2.zero, new Vector2(1f, 0.333f), new Vector2(5f, 5f), new Vector2(-5f, -1f));
                ((Image)_resetTransform.targetGraphic).sprite = background;
                ((Image)_resetTransform.targetGraphic).color = Color.Lerp(Color.red, Color.white, 0.3f);
                t = _resetTransform.GetComponentInChildren<Text>();
                t.color = Color.white;
                t.alignByGeometry = true;
                t.rectTransform.SetRect();
                _resetTransform.onClick.AddListener(ResetTransform);

                _hashSelectObject = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");

#if HONEYSELECT
                RectTransform posX = (RectTransform)guideInput.Find("Pos/InputField X");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform posX = (RectTransform)guideInput.Find("Pos/TextMeshPro - InputField X");
#endif
                RawImage posXDrag = UIUtility.CreateRawImage("DragX", posX.parent);
                posXDrag.color = new Color32(0, 0, 0, 1);
                posXDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(posX.offsetMin.x - 24, posX.offsetMin.y), new Vector2(posX.offsetMin.x, posX.offsetMax.y));
                posXDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingXPos = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform posY = (RectTransform)guideInput.Find("Pos/InputField Y");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform posY = (RectTransform)guideInput.Find("Pos/TextMeshPro - InputField Y");
#endif
                RawImage posYDrag = UIUtility.CreateRawImage("DragY", posY.parent);
                posYDrag.color = new Color32(0, 0, 0, 1);
                posYDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(posY.offsetMin.x - 24, posY.offsetMin.y), new Vector2(posY.offsetMin.x, posY.offsetMax.y));
                posYDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingYPos = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform posZ = (RectTransform)guideInput.Find("Pos/InputField Z");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform posZ = (RectTransform)guideInput.Find("Pos/TextMeshPro - InputField Z");
#endif
                RawImage posZDrag = UIUtility.CreateRawImage("DragZ", posZ.parent);
                posZDrag.color = new Color32(0, 0, 0, 1);
                posZDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(posZ.offsetMin.x - 24, posZ.offsetMin.y), new Vector2(posZ.offsetMin.x, posZ.offsetMax.y));
                posZDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingZPos = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform rotX = (RectTransform)guideInput.Find("Rot/InputField X");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform rotX = (RectTransform)guideInput.Find("Rot/TextMeshPro - InputField X");
#endif
                RawImage rotXDrag = UIUtility.CreateRawImage("DragX", rotX.parent);
                rotXDrag.color = new Color32(0, 0, 0, 1);
                rotXDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(rotX.offsetMin.x - 24, rotX.offsetMin.y), new Vector2(rotX.offsetMin.x, rotX.offsetMax.y));
                rotXDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingXRot = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform rotY = (RectTransform)guideInput.Find("Rot/InputField Y");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform rotY = (RectTransform)guideInput.Find("Rot/TextMeshPro - InputField Y");
#endif
                RawImage rotYDrag = UIUtility.CreateRawImage("DragY", rotY.parent);
                rotYDrag.color = new Color32(0, 0, 0, 1);
                rotYDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(rotY.offsetMin.x - 24, rotY.offsetMin.y), new Vector2(rotY.offsetMin.x, rotY.offsetMax.y));
                rotYDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingYRot = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform rotZ = (RectTransform)guideInput.Find("Rot/InputField Z");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform rotZ = (RectTransform)guideInput.Find("Rot/TextMeshPro - InputField Z");
#endif
                RawImage rotZDrag = UIUtility.CreateRawImage("DragZ", rotZ.parent);
                rotZDrag.color = new Color32(0, 0, 0, 1);
                rotZDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(rotZ.offsetMin.x - 24, rotZ.offsetMin.y), new Vector2(rotZ.offsetMin.x, rotZ.offsetMax.y));
                rotZDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingZRot = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform scaleX = (RectTransform)guideInput.Find("Scl/InputField X");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform scaleX = (RectTransform)guideInput.Find("Scl/TextMeshPro - InputField X");
#endif
                RawImage scaleXDrag = UIUtility.CreateRawImage("DragX", scaleX.parent);
                scaleXDrag.color = new Color32(0, 0, 0, 1);
                scaleXDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(scaleX.offsetMin.x - 24, scaleX.offsetMin.y), new Vector2(scaleX.offsetMin.x, scaleX.offsetMax.y));
                scaleXDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingXScale = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform scaleY = (RectTransform)guideInput.Find("Scl/InputField Y");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform scaleY = (RectTransform)guideInput.Find("Scl/TextMeshPro - InputField Y");
#endif
                RawImage scaleYDrag = UIUtility.CreateRawImage("DragY", scaleY.parent);
                scaleYDrag.color = new Color32(0, 0, 0, 1);
                scaleYDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(scaleY.offsetMin.x - 24, scaleY.offsetMin.y), new Vector2(scaleY.offsetMin.x, scaleY.offsetMax.y));
                scaleYDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingYScale = true;
                    InitOriginalTransforms();
                };

#if HONEYSELECT
                RectTransform scaleZ = (RectTransform)guideInput.Find("Scl/InputField Z");
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                RectTransform scaleZ = (RectTransform)guideInput.Find("Scl/TextMeshPro - InputField Z");
#endif
                RawImage scaleZDrag = UIUtility.CreateRawImage("DragZ", scaleZ.parent);
                scaleZDrag.color = new Color32(0, 0, 0, 1);
                scaleZDrag.rectTransform.SetRect(new Vector2(0, 1), new Vector2(0, 1), new Vector2(scaleZ.offsetMin.x - 24, scaleZ.offsetMin.y), new Vector2(scaleZ.offsetMin.x, scaleZ.offsetMax.y));
                scaleZDrag.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (e) =>
                {
                    _draggingZScale = true;
                    InitOriginalTransforms();
                };
            }

            private void Update()
            {
                if (_lastObjectCount != _hashSelectObject.Count)
                    UpdateButtonsVisibility();
                _lastObjectCount = _hashSelectObject.Count;

                if (Input.GetMouseButtonUp(0))
                {
                    if (_draggingXPos || _draggingYPos || _draggingZPos ||
                        _draggingXRot || _draggingYRot || _draggingZRot ||
                        _draggingXScale || _draggingYScale || _draggingZScale)
                    {
                        List<GuideCommand.EqualsInfo> moveChangeAmountInfo = new List<GuideCommand.EqualsInfo>();
                        List<GuideCommand.EqualsInfo> rotateChangeAmountInfo = new List<GuideCommand.EqualsInfo>();
                        List<GuideCommand.EqualsInfo> scaleChangeAmountInfo = new List<GuideCommand.EqualsInfo>();

                        foreach (GuideObject guideObject in _hashSelectObject)
                        {
                            TransformData data = _originalTransforms[guideObject];
                            if (guideObject.enablePos)
                            {
                                moveChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                                {
                                    dicKey = guideObject.dicKey,
                                    oldValue = data.position,
                                    newValue = guideObject.changeAmount.pos
                                });
                            }
                            if (guideObject.enableRot)
                            {
                                rotateChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                                {
                                    dicKey = guideObject.dicKey,
                                    oldValue = data.rotation,
                                    newValue = guideObject.changeAmount.rot
                                });
                            }
                            if (guideObject.enableScale)
                            {
                                scaleChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                                {
                                    dicKey = guideObject.dicKey,
                                    oldValue = data.scale,
                                    newValue = guideObject.changeAmount.scale
                                });
                            }
                        }
                        UndoRedoManager.Instance.Push(new TransformEqualsCommand(moveChangeAmountInfo.ToArray(), rotateChangeAmountInfo.ToArray(), scaleChangeAmountInfo.ToArray()));
                    }
                    _draggingXPos = false;
                    _draggingYPos = false;
                    _draggingZPos = false;
                    _draggingXRot = false;
                    _draggingYRot = false;
                    _draggingZRot = false;
                    _draggingXScale = false;
                    _draggingYScale = false;
                    _draggingZScale = false;
                }

                if (_draggingXPos || _draggingYPos || _draggingZPos ||
                    _draggingXRot || _draggingYRot || _draggingZRot ||
                    _draggingXScale || _draggingYScale || _draggingZScale)
                {
                    float delta = Input.GetAxisRaw("Mouse X") * (Input.GetKey(KeyCode.LeftShift) ? 4f : 1f) / (Input.GetKey(KeyCode.LeftControl) ? 8f : 1f)
#if HONEYSELECT || KOIKATSU
                                                                                                            / 10f
#endif
                            ;

                    foreach (GuideObject guideObject in _hashSelectObject)
                    {
                        if (guideObject.enablePos)
                        {
                            if (_draggingXPos)
                                guideObject.changeAmount.pos += new Vector3(delta, 0f, 0f);
                            else if (_draggingYPos)
                                guideObject.changeAmount.pos += new Vector3(0f, delta, 0f);
                            else if (_draggingZPos)
                                guideObject.changeAmount.pos += new Vector3(0f, 0f, delta);
                        }
                        if (guideObject.enableRot)
                        {

                            if (_draggingXRot)
                                guideObject.changeAmount.rot = (Quaternion.Euler(guideObject.changeAmount.rot) * Quaternion.AngleAxis(delta * 20f, Vector3.right)).eulerAngles;
                            else if (_draggingYRot)
                                guideObject.changeAmount.rot = (Quaternion.Euler(guideObject.changeAmount.rot) * Quaternion.AngleAxis(delta * 20f, Vector3.up)).eulerAngles;
                            else if (_draggingZRot)
                                guideObject.changeAmount.rot = (Quaternion.Euler(guideObject.changeAmount.rot) * Quaternion.AngleAxis(delta * 20f, Vector3.forward)).eulerAngles;
                        }
                        if (guideObject.enableScale)
                        {
                            if (_draggingXScale)
                                guideObject.changeAmount.scale += new Vector3(delta, 0f, 0f);
                            else if (_draggingYScale)
                                guideObject.changeAmount.scale += new Vector3(0f, delta, 0f);
                            else if (_draggingZScale)
                                guideObject.changeAmount.scale += new Vector3(0f, 0f, delta);
                        }
                    }
                }

                if (HSUS._self.binary == Binary.Studio)
                    if (HSUS.CopyTransformHotkey.Value.IsDown())
                        CopyTransform();
                    else if (HSUS.PasteTransformHotkey.Value.IsDown())
                        PasteTransform();
                    else if (HSUS.PasteTransformPositionOnlyHotkey.Value.IsDown())
                        PasteTransform(true, false, false);
                    else if (HSUS.PasteTransformRotationOnlyHotkey.Value.IsDown())
                        PasteTransform(false, true, false);
                    else if (HSUS.PasteTransformScaleOnlyHotkey.Value.IsDown())
                        PasteTransform(false, false, true);
                    else if (HSUS.ResetTransformHotkey.Value.IsDown())
                        ResetTransform();
            }

            private void UpdateButtonsVisibility()
            {
                _copyTransform.interactable = _hashSelectObject.Count == 1;
                _pasteTransform.interactable = _hashSelectObject.Count > 0 && _clipboardEmpty == false;
                _resetTransform.interactable = _hashSelectObject.Count > 0;
            }

            private void CopyTransform()
            {
                if (_hashSelectObject.Count == 1)
                {
                    GuideObject source = _hashSelectObject.First();
                    _savedPosition = source.changeAmount.pos;
                    _savedRotation = source.changeAmount.rot;
                    _savedScale = source.changeAmount.scale;
                    _clipboardEmpty = false;
                    UpdateButtonsVisibility();
                }
                else if (_hashSelectObject.Count > 1)
                    HSUS.Logger.LogMessage("Please select only 1 object when copying to prevent ambiguity");
            }

            // Without this the compiler will complain
            // `cannot convert from 'method group' to 'UnityAction'`
            // when adding this function to a listener
            private void PasteTransform()
            {
                PasteTransform(true, true, true);
            }

            private void PasteTransform(bool pastePos, bool pasteRot, bool pasteScale)
            {
                if (_clipboardEmpty)
                    return;
                SetValues(
                    pastePos ? _savedPosition : (Vector3?)null,
                    pasteRot ? _savedRotation : (Vector3?)null,
                    pasteScale ? _savedScale: (Vector3?)null
                );
            }

            private void ResetTransform()
            {
                SetValues(Vector3.zero, Vector3.zero, Vector3.one);
            }

            private void SetValues(Vector3? pos, Vector3? rot, Vector3? scale)
            {
                List<GuideCommand.EqualsInfo> moveChangeAmountInfo = new List<GuideCommand.EqualsInfo>();
                List<GuideCommand.EqualsInfo> rotateChangeAmountInfo = new List<GuideCommand.EqualsInfo>();
                List<GuideCommand.EqualsInfo> scaleChangeAmountInfo = new List<GuideCommand.EqualsInfo>();

                foreach (GuideObject guideObject in _hashSelectObject)
                {
                    if (guideObject.enablePos && pos != null)
                    {
                        Vector3 oldPosValue = guideObject.changeAmount.pos;
                        guideObject.changeAmount.pos = (Vector3)pos;
                        moveChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = oldPosValue,
                            newValue = guideObject.changeAmount.pos
                        });
                    }
                    if (guideObject.enableRot && rot != null)
                    {
                        Vector3 oldRotValue = guideObject.changeAmount.rot;
                        guideObject.changeAmount.rot = (Vector3)rot;
                        rotateChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = guideObject.changeAmount.rot
                        });
                    }
                    if (guideObject.enableScale && scale != null)
                    {
                        Vector3 oldScaleValue = guideObject.changeAmount.scale;
                        guideObject.changeAmount.scale = (Vector3)scale;
                        scaleChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = oldScaleValue,
                            newValue = guideObject.changeAmount.scale
                        });
                    }
                }
                UndoRedoManager.Instance.Push(new TransformEqualsCommand(moveChangeAmountInfo.ToArray(), rotateChangeAmountInfo.ToArray(), scaleChangeAmountInfo.ToArray()));
            }

            private void InitOriginalTransforms()
            {
                _originalTransforms.Clear();
                foreach (GuideObject guideObject in _hashSelectObject)
                    _originalTransforms.Add(guideObject, new TransformData() { position = guideObject.changeAmount.pos, rotation = guideObject.changeAmount.rot, scale = guideObject.changeAmount.scale });
            }
        }

        public class TransformEqualsCommand : Studio.ICommand
        {
            private readonly GuideCommand.EqualsInfo[] _moveChangeAmountInfo;
            private readonly GuideCommand.EqualsInfo[] _rotateChangeAmountInfo;
            private readonly GuideCommand.EqualsInfo[] _scaleChangeAmountInfo;

            public TransformEqualsCommand(GuideCommand.EqualsInfo[] moveChangeAmountInfo, GuideCommand.EqualsInfo[] rotateChangeAmountInfo, GuideCommand.EqualsInfo[] scaleChangeAmountInfo)
            {
                _moveChangeAmountInfo = moveChangeAmountInfo;
                _rotateChangeAmountInfo = rotateChangeAmountInfo;
                _scaleChangeAmountInfo = scaleChangeAmountInfo;
            }

            public void Do()
            {
                foreach (GuideCommand.EqualsInfo info in _moveChangeAmountInfo)
                {
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.pos = info.newValue;
                }

                foreach (GuideCommand.EqualsInfo info in _rotateChangeAmountInfo)
                {
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.rot = info.newValue;
                }

                foreach (GuideCommand.EqualsInfo info in _scaleChangeAmountInfo)
                {
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.scale = info.newValue;
                }

            }

            public void Redo()
            {
                Do();
            }

            public void Undo()
            {
                foreach (GuideCommand.EqualsInfo info in _moveChangeAmountInfo)
                {
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.pos = info.oldValue;
                }
                foreach (GuideCommand.EqualsInfo info in _rotateChangeAmountInfo)
                {
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.rot = info.oldValue;
                }
                foreach (GuideCommand.EqualsInfo info in _scaleChangeAmountInfo)
                {
                    ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                    if (changeAmount != null)
                        changeAmount.scale = info.oldValue;
                }
            }
        }
#endif
    }
}
