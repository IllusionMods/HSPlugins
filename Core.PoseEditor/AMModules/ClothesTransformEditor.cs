using Illusion.Extensions;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ToolBox.Extensions;
using UnityEngine;
using Vectrosity;
namespace HSPE.AMModules
{
    public class ClothesTransformEditor : AdvancedModeModule
    {
#if HONEYSELECT || KOIKATSU || PLAYHOME
        private const float _cubeSize = 0.012f;
#else
        private const float _cubeSize = 0.12f;
#endif
        private readonly GenericOCITarget _target;
        enum ChoiceType : int
        {
            Top = 0,
            Bottom,
            InnerTop,
            InnerBottom,
            Panst,
            TopHalf,
            BottomHalf,
            InnerTopHalf,
            InnerBottomHalf,
            PanstHalf,
            Gloves,
            Socks,
            Shoes,
            Count
        }

        private static readonly string[] _clothesKeys = { "ct_clothesTop", "ct_clothesBot", "ct_inner_t", "ct_inner_b", "ct_panst", "ct_gloves", "ct_socks", "ct_shoes" };
        private static readonly string[] _choiceKeyString = { "ct_clothesTop", "ct_clothesBot", "ct_inner_t", "ct_inner_b", "ct_panst",
                                                                "ct_clothesTop_half", "ct_clothesBot_half", "ct_inner_t_half", "ct_inner_b_half", "ct_panst_half",
                                                                 "ct_gloves", "ct_socks", "ct_shoes"};
        private static readonly Dictionary<string, ChoiceType> _ChoiceKey = new Dictionary<string, ChoiceType>
        {
            { "ct_clothesTop", ChoiceType.Top },
            { "ct_clothesBot", ChoiceType.Bottom },
            { "ct_inner_t", ChoiceType.InnerTop },
            { "ct_inner_b", ChoiceType.InnerBottom },
            { "ct_panst", ChoiceType.Panst },
            { "ct_clothesTop_half", ChoiceType.TopHalf },
            { "ct_clothesBot_half", ChoiceType.BottomHalf },
            { "ct_inner_t_half", ChoiceType.InnerTopHalf },
            { "ct_inner_b_half", ChoiceType.InnerBottomHalf },
            { "ct_panst_half", ChoiceType.PanstHalf },
            { "ct_gloves", ChoiceType.Gloves },
            { "ct_socks", ChoiceType.Socks },
            { "ct_shoes", ChoiceType.Shoes }
        };

        class ClothesTransfer
        {
            public GameObject transfer = new GameObject();
        }

        class ClothesTransferList
        {
            public GameObject[] clotheSlot = new GameObject[(int)ChoiceType.Count];
            public List<ClothesTransfer> transfers = new List<ClothesTransfer>();

            public void RemoveTransfer(ClothesTransfer transfer)
            {
                for (int i = 0; i < transfers.Count; i++)
                {
                    if (transfers[i] == transfer)
                    {
                        transfers.RemoveAt(i);
                        break;
                    }
                }

                for (int i = 0; i < (int)ChoiceType.Count; i++)
                {
                    if (clotheSlot[i] == transfer.transfer)
                    {
                        clotheSlot[i] = null;
                    }
                }

                transfer.transfer.transform.SetParent(null);
                GameObject.Destroy(transfer.transfer);
            }

            public void RemoveAllTransfers()
            {
                for (int i = 0; i < (int)ChoiceType.Count; i++)
                {
                    clotheSlot[i] = null;
                }

                foreach (var transfer in transfers)
                {
                    transfer.transfer.transform.SetParent(null);
                    GameObject.Destroy(transfer.transfer);
                }

                transfers.Clear();
            }
        }

        private bool _renderersChanged = false;
        private Transform[] _clothesKeyTransforms = new Transform[_clothesKeys.Length];
        private List<SkinnedMeshRenderer>[] _clothesRenderers = new List<SkinnedMeshRenderer>[(int)ChoiceType.Count];
        private Dictionary<Transform, ClothesTransferList> _clothesTransferLists = new Dictionary<Transform, ClothesTransferList>();
        private Dictionary<string, ClothesTransferList> _clothesTransferListsByStr = new Dictionary<string, ClothesTransferList>();
        private Transform _currSearchStartTargetTransform = null;

        private static readonly string _cloneToken = "_CTClone_";
        //private static readonly string _targetTransStartToken = "cf_J_";
        //private static readonly string _targetTransEndToken = "_s";
        //private static readonly string[] _notInTheRuleNodes = { "cf_J_Kokan", "cf_J_Ana", "cf_J_Foot", "cf_J_Toes",
        //                                                    "cf_J_Hand_Index","cf_J_Hand_Little","cf_J_Hand_Middle","cf_J_Hand_Ring","cf_J_Hand_Thumb" };

        private List<Transform> _targetTransforms = new List<Transform>();

        private Transform _currTargetTransform = null;
        private ClothesTransferList _currTargetTransferList = null;
        private ClothesTransfer _currTargetTransfer = null;
        private Vector2 _targetTransformListScroll;
        private Vector2 _clothesTransferScroll;
        private Vector2 _choiceSelectScroll;
        private static string _search = "";

        private static readonly List<VectorLine> _cubeDebugLines = new List<VectorLine>();
        private bool _showGizmos = false;

        public ClothesTransformEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            if (target.type != GenericOCITarget.Type.Character)
            {
                return;
            }

            _target = target;
            _parent.onLateUpdate += LateUpdate;
            _parent.onDisable += OnDisable;


            if (_cubeDebugLines.Count == 0)
            {
                Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * _cubeSize,
                    topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * _cubeSize,
                    bottomLeftForward = (Vector3.down + Vector3.left + Vector3.forward) * _cubeSize,
                    bottomRightForward = (Vector3.down + Vector3.right + Vector3.forward) * _cubeSize,
                    topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * _cubeSize,
                    topRightBack = (Vector3.up + Vector3.right + Vector3.back) * _cubeSize,
                    bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * _cubeSize,
                    bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * _cubeSize;
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftForward, topRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightForward, bottomRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightForward, bottomLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftForward, topLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topRightBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, bottomRightBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomLeftBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, topLeftBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, topRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, bottomLeftForward));

                VectorLine l = VectorLine.SetLine(_redColor, Vector3.zero, Vector3.right * _cubeSize * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);
                l = VectorLine.SetLine(_greenColor, Vector3.zero, Vector3.up * _cubeSize * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);
                l = VectorLine.SetLine(_blueColor, Vector3.zero, Vector3.forward * _cubeSize * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);

                foreach (VectorLine line in _cubeDebugLines)
                {
                    line.lineWidth = 2f;
                    line.active = true;
                }
            }

            for (int i = 0; i < (int)ChoiceType.Count; i++)
            {
                _clothesRenderers[i] = new List<SkinnedMeshRenderer>();
            }

            FindTargetTransforms();
        }

        private void LateUpdate()
        {
            if(_renderersChanged == false)
            {
                if (_currSearchStartTargetTransform != null)
                {
                    for (int index = 0; index < _clothesKeys.Length; index++)
                    {
                        if(_clothesKeyTransforms[index] == null)
                        {
                            _renderersChanged = true;
                            break;
                        }
                    }

                    if (_renderersChanged)
                    {
                        MainWindow._self.ExecuteDelayed(() =>
                        {
                            RefreshClothesRenderers();
                            ChangeClothesRenderersBone();

                            _renderersChanged = false;
                        }, 5);
                    }
                }
            }
        }

        private void OnDisable()
        {
            DeleteAllTransfers();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _parent.onLateUpdate -= LateUpdate;
            _parent.onDisable -= OnDisable;
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            if (_target == null) return 0;

            int written = 0;
            if (_clothesTransferListsByStr.Count != 0)
            {
                xmlWriter.WriteStartElement("clothesTransferLists");
                foreach (var kvp in _clothesTransferListsByStr)
                {
                    if (kvp.Value == null || kvp.Value.transfers.Count == 0)
                        continue;
                    string n = kvp.Key;
                    xmlWriter.WriteStartElement("TargetTransformName");
                    xmlWriter.WriteAttributeString("name", n);

                    for (int i = 0; i < (int)ChoiceType.Count; i++)
                    {
                        if (kvp.Value.clotheSlot[i] != null)
                        {
                            xmlWriter.WriteAttributeString(_choiceKeyString[i], kvp.Value.clotheSlot[i].name);
                        }
                    }

                    foreach (var transfer in kvp.Value.transfers)
                    {
                        xmlWriter.WriteStartElement("Transfer");
                        xmlWriter.WriteAttributeString("name", transfer.transfer.name);
                        Transform currTransform = transfer.transfer.transform;

                        xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(currTransform.localPosition.x));
                        xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(currTransform.localPosition.y));
                        xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(currTransform.localPosition.z));

                        xmlWriter.WriteAttributeString("rotW", XmlConvert.ToString(currTransform.localRotation.w));
                        xmlWriter.WriteAttributeString("rotX", XmlConvert.ToString(currTransform.localRotation.x));
                        xmlWriter.WriteAttributeString("rotY", XmlConvert.ToString(currTransform.localRotation.y));
                        xmlWriter.WriteAttributeString("rotZ", XmlConvert.ToString(currTransform.localRotation.z));

                        xmlWriter.WriteAttributeString("scaleX", XmlConvert.ToString(currTransform.localScale.x));
                        xmlWriter.WriteAttributeString("scaleY", XmlConvert.ToString(currTransform.localScale.y));
                        xmlWriter.WriteAttributeString("scaleZ", XmlConvert.ToString(currTransform.localScale.z));

                        ++written;
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }

            return written;
        }
        public override bool LoadXml(XmlNode xmlNode)
        {
            if (_target == null) return false;

            bool changed = false;
            DeleteAllTransfers();
            XmlNode objects = xmlNode.FindChildNode("clothesTransferLists");


            if (objects != null)
            {
                Dictionary<string, Transform> transforms = new Dictionary<string, Transform>();

                foreach (var kpv in _targetTransforms)
                {
                    transforms.Add(kpv.name, kpv);
                }

                foreach (XmlNode transferList in objects.ChildNodes)
                {
                    if (transferList.Name != "TargetTransformName")
                        continue;
                    string TargetTransformName = transferList.Attributes["name"].Value;
                    Dictionary<string, List<ChoiceType>> slotedName = new Dictionary<string, List<ChoiceType>>();
                    for (int i = 0; i < (int)ChoiceType.Count; i++)
                    {
                        string currChoice = _choiceKeyString[i];
                        if (transferList.Attributes[currChoice] != null)
                        {
                            string transferName = transferList.Attributes[currChoice].Value;

                            List<ChoiceType> choiceList = null;
                            if (!slotedName.TryGetValue(transferName, out choiceList))
                            {
                                choiceList = new List<ChoiceType>();
                                slotedName.Add(transferList.Attributes[currChoice].Value, choiceList);
                            }

                            choiceList.Add(_ChoiceKey[currChoice]);
                        }
                    }

                    foreach (XmlNode transfer in transferList.ChildNodes)
                    {
                        try
                        {
                            if (transfer.Name != "Transfer")
                                continue;

                            string transferName = transfer.Attributes["name"].Value;
                            var result = CreateTransfer(transforms[TargetTransformName], transferName);

                            result.Value.transfer.transform.localPosition = new Vector3(
                                XmlConvert.ToSingle(transfer.Attributes["posX"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["posY"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["posZ"].Value));

                            result.Value.transfer.transform.localRotation = new Quaternion(
                                XmlConvert.ToSingle(transfer.Attributes["rotX"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["rotY"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["rotZ"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["rotW"].Value));

                            result.Value.transfer.transform.localScale = new Vector3(
                                XmlConvert.ToSingle(transfer.Attributes["scaleX"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["scaleY"].Value),
                                XmlConvert.ToSingle(transfer.Attributes["scaleZ"].Value));

                            List<ChoiceType> choiceList = null;
                            if (slotedName.TryGetValue(transferName, out choiceList))
                            {
                                foreach (var choice in choiceList)
                                {
                                    result.Key.clotheSlot[(int)choice] = result.Value.transfer;
                                }
                            }


                            changed = true;
                        }
                        catch (Exception e)
                        {
                            HSPE.Logger.LogError("Couldn't load clothesTransfer " + _parent.name + " " + transfer.OuterXml + "\n" + e);
                        }
                    }
                }
            }

            _renderersChanged = true;
            MainWindow._self.ExecuteDelayed(() =>
            {
                RefreshClothesRenderers();
                ChangeClothesRenderersBone();

                _renderersChanged = false;
            }, 2);

            return changed;
        }

        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.ClothesTransformEditor; } }
        public override string displayName { get { return "Clothes"; } }
        public override bool shouldDisplay
        {
            get
            {
                return _target != null;
            }
        }

        private KeyValuePair<ClothesTransferList, ClothesTransfer> CreateTransfer(Transform targetTransform, string name = null)
        {
            ClothesTransferList transferList = null;
            if (_clothesTransferLists.TryGetValue(targetTransform, out transferList) == false)
            {
                transferList = new ClothesTransferList();
                _clothesTransferLists.Add(targetTransform, transferList);
                _clothesTransferListsByStr.Add(targetTransform.name, transferList);
            }

            ClothesTransfer transfer = new ClothesTransfer();

            if (name == null)
            {
                string lastIndex = "0";

                if (transferList.transfers.Count > 0)
                {
                    lastIndex = (int.Parse(transferList.transfers.Last().transfer.name.Split('_').Last()) + 1).ToString();
                }

                transfer.transfer.name = targetTransform.name + _cloneToken + lastIndex;
            }
            else
            {
                transfer.transfer.name = name;
            }

            transfer.transfer.transform.SetParent(targetTransform);
            transfer.transfer.transform.localPosition = Vector3.zero;
            transfer.transfer.transform.localRotation = Quaternion.identity;
            transfer.transfer.transform.localScale = Vector3.one;
            transferList.transfers.Add(transfer);

            return new KeyValuePair<ClothesTransferList, ClothesTransfer>(transferList, transfer);
        }
        private void DeleteAllTransfers()
        {
            if (_clothesTransferLists.Count == 0 && _clothesTransferListsByStr.Count == 0)
            {
                return;
            }

            Dictionary<SkinnedMeshRenderer, Transform[]> createdBones = new Dictionary<SkinnedMeshRenderer, Transform[]>();

            for (int i = 0; i < (int)ChoiceType.Count; i++)
            {
                foreach (SkinnedMeshRenderer renderer in _clothesRenderers[i])
                {
                    if (renderer != null)
                    {
                        for (int j = 0; j < renderer.bones.Length; j++)
                        {
                            if (renderer.bones[j] != null)
                            {
                                if (renderer.bones[j].name.Contains(_cloneToken))
                                {
                                    if (createdBones.ContainsKey(renderer) == false)
                                    {
                                        createdBones.Add(renderer, new Transform[renderer.bones.Length]);
                                        createdBones[renderer] = renderer.bones;
                                    }

                                    createdBones[renderer][j] = renderer.bones[j].parent;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var createdBone in createdBones)
            {
                createdBone.Key.bones = createdBone.Value;
            }

            foreach (var transferList in _clothesTransferLists)
            {
                transferList.Value.RemoveAllTransfers();
            }

            _clothesTransferLists.Clear();
            _clothesTransferListsByStr.Clear();
            _currTargetTransferList = null;
            _currTargetTransfer = null;
        }


        private void DeleteTransferList(Transform targetTransform, ClothesTransferList transferList)
        {
            if (targetTransform != null && transferList != null)
            {
                ChangeClothesRenderersBone(targetTransform, null);

                transferList.RemoveAllTransfers();
                _clothesTransferLists.Remove(targetTransform);
                _clothesTransferListsByStr.Remove(targetTransform.name);
            }

            _currTargetTransferList = null;
            _currTargetTransfer = null;
        }

        public override void GUILogic()
        {
            _showGizmos = true;

            GUILayout.BeginHorizontal();
            {
                {
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    {
                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("Create Clothes transform") && _currTargetTransform != null)
                        {
                            var result = CreateTransfer(_currTargetTransform);

                            _currTargetTransferList = result.Key;
                            _currTargetTransfer = result.Value;
                        }

                        if (GUILayout.Button("Refresh Clothes Renderer"))
                        {
                            RefreshClothesRenderers();
                            ChangeClothesRenderersBone();
                        }
                        GUILayout.EndHorizontal();
                    }

                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                        _search = GUILayout.TextField(_search);
                        if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            _search = "";

                        GUILayout.EndHorizontal();
                    }

                    {
                        GUILayout.BeginHorizontal();

                        _targetTransformListScroll = GUILayout.BeginScrollView(_targetTransformListScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

                        Color backColor = GUI.color;
                        foreach (Transform currTransform in _targetTransforms)
                        {
                            if (currTransform.name.ToLower().Contains(_search.ToLower()))
                            {
                                if (currTransform == _currTargetTransform)
                                {
                                    GUI.color = Color.cyan;
                                }

                                if (GUILayout.Button(currTransform.name, GUILayout.ExpandWidth(false)))
                                {
                                    _currTargetTransform = currTransform;
                                    _currTargetTransfer = null;

                                    if (_clothesTransferLists.TryGetValue(_currTargetTransform, out _currTargetTransferList) == false)
                                    {
                                        _currTargetTransferList = null;
                                    }
                                }

                                GUI.color = backColor;
                            }
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndScrollView();
                    }


                    GUILayout.EndVertical();
                }

                {
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    GUILayout.BeginHorizontal();

                    _clothesTransferScroll = GUILayout.BeginScrollView(_clothesTransferScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

                    {
                        GUILayout.BeginHorizontal();
                        Color back = GUI.color;
                        GUI.color = Color.red;

                        if (GUILayout.Button("Delete CT clone"))
                        {
                            if (_currTargetTransfer != null)
                            {
                                ClothesTransferList listResult = null;
                                if (_clothesTransferLists.TryGetValue(_currTargetTransform, out listResult))
                                {
                                    for (int i = 0; i < (int)ChoiceType.Count; i++)
                                    {
                                        if (listResult.clotheSlot[i] == _currTargetTransfer.transfer)
                                        {
                                            ChangeClothesRenderersBone(_currTargetTransform, (ChoiceType)i, null);
                                        }
                                    }

                                    listResult.RemoveTransfer(_currTargetTransfer);
                                    _currTargetTransfer = null;
                                }
                            }
                            else
                            {
                                if (_currTargetTransferList != null)
                                {
                                    DeleteTransferList(_currTargetTransform, _currTargetTransferList);

                                    _currTargetTransfer = null;
                                    _currTargetTransferList = null;
                                }
                            }

                        }

                        GUI.color = back;
                        GUILayout.EndHorizontal();
                    }

                    Color backColor = GUI.color;
                    foreach (var transferList in _clothesTransferLists)
                    {
                        if (transferList.Value == _currTargetTransferList)
                        {
                            GUI.color = Color.cyan;
                        }

                        if (GUILayout.Button(transferList.Key.name, GUILayout.ExpandWidth(false)))
                        {
                            _currTargetTransform = transferList.Key;
                            _currTargetTransferList = transferList.Value;
                            _currTargetTransfer = null;
                        }

                        GUI.color = backColor;

                        if (transferList.Value == _currTargetTransferList)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20.0f);
                            GUILayout.BeginVertical();

                            foreach (var transfer in transferList.Value.transfers)
                            {
                                if (transfer == _currTargetTransfer)
                                {
                                    GUI.color = Color.cyan;
                                }
                                if (GUILayout.Button(transfer.transfer.name, GUILayout.ExpandWidth(false)))
                                {
                                    _currTargetTransfer = transfer;
                                }
                                GUI.color = backColor;
                            }

                            GUILayout.EndVertical();
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

                {
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    GUILayout.BeginHorizontal();
                    _choiceSelectScroll = GUILayout.BeginScrollView(_choiceSelectScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

                    if (_currTargetTransferList != null && _currTargetTransfer != null)
                    {
                        for (int i = 0; i < (int)ChoiceType.Count; i++)
                        {
                            if (_currTargetTransferList.clotheSlot[i] == null)
                            {
                                if (GUILayout.Button(_choiceKeyString[i], GUILayout.ExpandWidth(false)))
                                {
                                    _currTargetTransferList.clotheSlot[i] = _currTargetTransfer.transfer;
                                    ChangeClothesRenderersBone(_currTargetTransfer.transfer.transform.parent, (ChoiceType)i, _currTargetTransfer.transfer);
                                }

                            }
                            else if (_currTargetTransferList.clotheSlot[i] == _currTargetTransfer.transfer)
                            {
                                Color backColor = GUI.color;
                                GUI.color = Color.magenta;

                                if (GUILayout.Button(_choiceKeyString[i], GUILayout.ExpandWidth(false)))
                                {
                                    _currTargetTransferList.clotheSlot[i] = _currTargetTransfer.transfer;
                                    ChangeClothesRenderersBone(_currTargetTransfer.transfer.transform.parent, (ChoiceType)i, null);
                                }

                                GUI.color = backColor;
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Transfer non selected", GUILayout.ExpandWidth(false));
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

            }
            GUILayout.EndHorizontal();
        }

        private KeyValuePair<Transform, Transform> GetDefHalfClothesTransform(Transform clotheKeyTransform)
        {
#if AISHOUJO || HONEYSELECT2
            AIChara.CmpClothes clotheComp = clotheKeyTransform.GetComponent<AIChara.CmpClothes>();

            if (clotheComp != null)
            {
                if (clotheComp.objBotDef != null)
                {
                    if(clotheComp.objBotHalf != null)
                    {
                        return new KeyValuePair<Transform, Transform>(clotheComp.objBotDef.transform, clotheComp.objBotHalf.transform);
                    }
                    else
                    {
                        return new KeyValuePair<Transform, Transform>(clotheComp.objBotDef.transform, null);
                    }
                }

                if(clotheComp.objTopDef != null)
                {
                    if(clotheComp.objTopHalf != null)
                    {
                        return new KeyValuePair<Transform, Transform>(clotheComp.objTopDef.transform, clotheComp.objTopHalf.transform);
                    }
                    else
                    {
                        return new KeyValuePair<Transform, Transform>(clotheComp.objTopDef.transform, null);
                    }
                }
            }
#else
#endif
            return new KeyValuePair<Transform, Transform>(null, null);
        }

        private SkinnedMeshRenderer GetBodyRenderer()
        {
            SkinnedMeshRenderer bodyRenderer = null;
#if AISHOUJO || HONEYSELECT2
            List<Transform> transformStack = new List<Transform>();

            transformStack.Add(_parent.gameObject.transform);

            while (transformStack.Count != 0)
            {
                Transform currTransform = transformStack.Pop();

                if (currTransform.Find("p_cf_body_00"))
                {
                    Transform bodyTransform = currTransform.Find("p_cf_body_00");
                    AIChara.CmpBody bodyCmp = bodyTransform.GetComponent<AIChara.CmpBody>();

                    if (bodyCmp != null)
                    {
                        if (bodyCmp.targetCustom != null && bodyCmp.targetCustom.rendBody != null)
                        {
                            bodyRenderer = bodyCmp.targetCustom.rendBody.transform.GetComponent<SkinnedMeshRenderer>();
                        }
                        else
                        {
                            if (bodyCmp.targetEtc != null && bodyCmp.targetEtc.objBody != null)
                            {
                                bodyRenderer = bodyCmp.targetEtc.objBody.GetComponent<SkinnedMeshRenderer>();
                            }
                        }
                    }

                    break;
                }
                else if(currTransform.Find("p_cm_body_00"))
                {
                    Transform bodyTransform = currTransform.Find("p_cm_body_00");
                    AIChara.CmpBody bodyCmp = bodyTransform.GetComponent<AIChara.CmpBody>();

                    if (bodyCmp != null)
                    {
                        if (bodyCmp.targetCustom != null && bodyCmp.targetCustom.rendBody != null)
                        {
                            bodyRenderer = bodyCmp.targetCustom.rendBody.transform.GetComponent<SkinnedMeshRenderer>();
                        }
                        else
                        {
                            if (bodyCmp.targetEtc != null && bodyCmp.targetEtc.objBody != null)
                            {
                                bodyRenderer = bodyCmp.targetEtc.objBody.GetComponent<SkinnedMeshRenderer>();
                            }
                        }
                    }

                    break;
                }

                for (int i = 0; i < currTransform.childCount; i++)
                {
                    transformStack.Add(currTransform.GetChild(i));
                }
            }
#endif
            return bodyRenderer;
        }

        private void FindTargetTransforms()
        {
            Dictionary<string, Transform> bodyTransforms = new Dictionary<string, Transform>();

            SkinnedMeshRenderer bodyRenderer = null;

            bodyRenderer = GetBodyRenderer();
            if (bodyRenderer == null) return;

            _currTargetTransform = null;
            _currTargetTransferList = null;
            _currTargetTransfer = null;
            _targetTransforms.Clear();
            _clothesTransferLists.Clear();

            foreach (Transform bone in bodyRenderer.bones)
            {
                Transform sameNameTransform = null;

                if (!bodyTransforms.TryGetValue(bone.name, out sameNameTransform))
                {
                    bodyTransforms.Add(bone.name, bone);
                    _targetTransforms.Add(bone);
                }
                else
                {
                    HSPE.Logger.LogError("this body has same name bones :" + sameNameTransform.name);
                }
            }

            if (_clothesTransferListsByStr.Count > 0)
            {
                foreach (var transformNames in _clothesTransferListsByStr)
                {
                    Transform transform = null;

                    if (bodyTransforms.TryGetValue(transformNames.Key, out transform))
                    {
                        _clothesTransferLists.Add(transform, transformNames.Value);
                    }
                }

                _clothesTransferListsByStr.Clear();

                foreach (var clothesTransferList in _clothesTransferLists)
                {
                    _clothesTransferListsByStr.Add(clothesTransferList.Key.name, clothesTransferList.Value);
                }
            }
        }

        private void RefreshClothesRenderer(int index, Transform targetClothesTransform)
        {
            _clothesRenderers[index].Clear();

            _clothesKeyTransforms[index] = targetClothesTransform;

            if (targetClothesTransform != null)
            {
                ChoiceType currChoice = ChoiceType.Count;
                _ChoiceKey.TryGetValue(targetClothesTransform.name, out currChoice);
                if (currChoice < ChoiceType.Gloves)
                {
                    var allRenderers = targetClothesTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var clotheTransform = GetDefHalfClothesTransform(targetClothesTransform);

                    foreach (var currRenderer in allRenderers)
                    {
                        Transform currTransform = currRenderer.transform;

                        while (currTransform != null)
                        {
                            if (currTransform == clotheTransform.Key)
                            {
                                _clothesRenderers[(int)currChoice].Add(currRenderer);
                                break;
                            }
                            else if (currTransform == clotheTransform.Value)
                            {
                                _clothesRenderers[(int)_ChoiceKey[_clothesKeys[index] + "_half"]].Add(currRenderer);
                                break;
                            }

                            if (currTransform.transform == targetClothesTransform)
                            {
                                _clothesRenderers[(int)currChoice].Add(currRenderer);
                                _clothesRenderers[(int)_ChoiceKey[_clothesKeys[index] + "_half"]].Add(currRenderer);
                                break;
                            }
                            else
                            {
                                currTransform = currTransform.parent;
                            }
                        }
                    }
                }
            }
        }


        private void RefreshClothesRenderers()
        {
            for (int i = 0; i < (int)ChoiceType.Count; i++)
            {
                _clothesRenderers[i].Clear();
            }
            _currSearchStartTargetTransform = _parent.gameObject.transform.Find("BodyTop");

            if (_currSearchStartTargetTransform == null)
            {
                return;
            }

            for (int i = 0; i < _clothesKeys.Length; i++)
            {
                _clothesKeyTransforms[i] = _currSearchStartTargetTransform.Find(_clothesKeys[i]);
            }

            for (int i = 0; i < _clothesKeys.Length; i++)
            {
                if (_clothesKeyTransforms[i] != null)
                {
                    ChoiceType currChoice = ChoiceType.Count;
                    _ChoiceKey.TryGetValue(_clothesKeyTransforms[i].name, out currChoice);
                    if (currChoice < ChoiceType.Gloves)
                    {
                        var allRenderers = _clothesKeyTransforms[i].GetComponentsInChildren<SkinnedMeshRenderer>();
                        var clotheTransform = GetDefHalfClothesTransform(_clothesKeyTransforms[i]);

                        foreach (var currRenderer in allRenderers)
                        {
                            Transform currTransform = currRenderer.transform;

                            while (currTransform != null)
                            {
                                if (currTransform == clotheTransform.Key)
                                {
                                    _clothesRenderers[(int)currChoice].Add(currRenderer);
                                    break;
                                }
                                else if (currTransform == clotheTransform.Value)
                                {
                                    _clothesRenderers[(int)_ChoiceKey[_clothesKeys[i] + "_half"]].Add(currRenderer);
                                    break;
                                }

                                if (currTransform.transform == _clothesKeyTransforms[i])
                                {
                                    _clothesRenderers[(int)currChoice].Add(currRenderer);
                                    _clothesRenderers[(int)_ChoiceKey[_clothesKeys[i] + "_half"]].Add(currRenderer);
                                    break;
                                }
                                else
                                {
                                    currTransform = currTransform.parent;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (currChoice != ChoiceType.Count)
                        {
                            var renderers = _clothesKeyTransforms[i].GetComponentsInChildren<SkinnedMeshRenderer>();

                            if (renderers.Length > 0)
                            {
                                _clothesRenderers[(int)currChoice].AddRange(renderers);
                            }
                        }
                    }
                }
            }
        }

        private void ChangeClothesRenderersBone()
        {
            if (_clothesTransferLists.Count == 0 && _clothesTransferListsByStr.Count == 0)
            {
                return;
            }

            Dictionary<SkinnedMeshRenderer, Transform[]> createdBones = new Dictionary<SkinnedMeshRenderer, Transform[]>();

            foreach (var transferList in _clothesTransferLists)
            {
                for (int choice = 0; choice < (int)ChoiceType.Count; choice++)
                {
                    GameObject currTransfer = transferList.Value.clotheSlot[choice];

                    if (currTransfer != null)
                    {
                        foreach (SkinnedMeshRenderer renderer in _clothesRenderers[choice])
                        {
                            for (int i = 0; i < renderer.bones.Length; i++)
                            {
                                if (renderer.bones[i] == transferList.Key)
                                {
                                    if (createdBones.ContainsKey(renderer) == false)
                                    {
                                        createdBones.Add(renderer, new Transform[renderer.bones.Length]);
                                        createdBones[renderer] = renderer.bones;
                                    }

                                    createdBones[renderer][i] = currTransfer.transform;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var createdBone in createdBones)
            {
                createdBone.Key.bones = createdBone.Value;
            }
        }


        private void ChangeClothesRenderersBone(Transform origin, ChoiceType choice, GameObject transfer)
        {
            if (_clothesTransferLists.Count == 0 && _clothesTransferListsByStr.Count == 0)
            {
                return;
            }

            Dictionary<SkinnedMeshRenderer, Transform[]> createdBones = new Dictionary<SkinnedMeshRenderer, Transform[]>();

            GameObject prevObject = _clothesTransferLists[origin].clotheSlot[(int)choice];

            if (prevObject == null)
            {
                prevObject = origin.gameObject;
            }

            foreach (SkinnedMeshRenderer renderer in _clothesRenderers[(int)choice])
            {
                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    if (renderer.bones[i] == origin || renderer.bones[i] == prevObject.transform)
                    {
                        if (createdBones.ContainsKey(renderer) == false)
                        {
                            createdBones.Add(renderer, new Transform[renderer.bones.Length]);
                            createdBones[renderer] = renderer.bones;
                        }

                        if (transfer == null)
                        {
                            createdBones[renderer][i] = origin;
                        }
                        else
                        {
                            createdBones[renderer][i] = transfer.transform;
                        }
                    }
                }
            }

            _clothesTransferLists[origin].clotheSlot[(int)choice] = transfer;

            foreach (var createdBone in createdBones)
            {
                createdBone.Key.bones = createdBone.Value;
            }
        }

        private void ChangeClothesRenderersBone(Transform origin, GameObject transfer)
        {
            if (_clothesTransferLists.Count == 0 && _clothesTransferListsByStr.Count == 0)
            {
                return;
            }

            Dictionary<SkinnedMeshRenderer, Transform[]> createdBones = new Dictionary<SkinnedMeshRenderer, Transform[]>();

            var TransferList = _clothesTransferLists[origin];

            for (int choice = 0; choice < (int)ChoiceType.Count; choice++)
            {
                GameObject prevObject = _clothesTransferLists[origin].clotheSlot[(int)choice];

                if (prevObject == null)
                {
                    prevObject = origin.gameObject;
                }

                foreach (SkinnedMeshRenderer renderer in _clothesRenderers[(int)choice])
                {
                    for (int i = 0; i < renderer.bones.Length; i++)
                    {
                        if (renderer.bones[i] == origin || renderer.bones[i] == prevObject.transform)
                        {
                            if (createdBones.ContainsKey(renderer) == false)
                            {
                                createdBones.Add(renderer, new Transform[renderer.bones.Length]);
                                createdBones[renderer] = renderer.bones;
                            }

                            if (transfer == null)
                            {
                                createdBones[renderer][i] = origin;
                            }
                            else
                            {
                                createdBones[renderer][i] = transfer.transform;
                            }
                        }
                    }
                }

                TransferList.clotheSlot[(int)choice] = transfer;
            }

            foreach (var createdBone in createdBones)
            {
                createdBone.Key.bones = createdBone.Value;
            }
        }

        public override void OnCharacterReplaced()
        {
            _currTargetTransform = null;
            _currTargetTransferList = null;
            _currTargetTransfer = null;
            _targetTransforms.Clear();
            _clothesTransferLists.Clear();

            _renderersChanged = true;
            MainWindow._self.ExecuteDelayed(() =>
            {
                FindTargetTransforms();
                RefreshClothesRenderers();
                ChangeClothesRenderersBone();

                _renderersChanged = false;
            }, 5);
        }
#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
        }
#endif

        public override void OnLoadClothesFile()
        {
        }


        public override void UpdateGizmos()
        {
            Transform gizmoTarget = null;

            if (_currTargetTransform != null)
            {
                gizmoTarget = _currTargetTransform;
            }

            if (_currTargetTransfer != null)
            {
                gizmoTarget = _currTargetTransfer.transfer.transform;
            }

            if (_showGizmos == false)
            {
                foreach (VectorLine line in _cubeDebugLines)
                    line.active = _showGizmos;

                return;
            }
            else
            {
                foreach (VectorLine line in _cubeDebugLines)
                    line.active = _showGizmos;

                _showGizmos = false;
            }

            if (gizmoTarget != null)
            {
                Vector3 topLeftForward = gizmoTarget.transform.position + (gizmoTarget.up + -gizmoTarget.right + gizmoTarget.forward) * _cubeSize,
                        topRightForward = gizmoTarget.transform.position + (gizmoTarget.up + gizmoTarget.right + gizmoTarget.forward) * _cubeSize,
                        bottomLeftForward = gizmoTarget.transform.position + (-gizmoTarget.up + -gizmoTarget.right + gizmoTarget.forward) * _cubeSize,
                        bottomRightForward = gizmoTarget.transform.position + (-gizmoTarget.up + gizmoTarget.right + gizmoTarget.forward) * _cubeSize,
                        topLeftBack = gizmoTarget.transform.position + (gizmoTarget.up + -gizmoTarget.right + -gizmoTarget.forward) * _cubeSize,
                        topRightBack = gizmoTarget.transform.position + (gizmoTarget.up + gizmoTarget.right + -gizmoTarget.forward) * _cubeSize,
                        bottomLeftBack = gizmoTarget.transform.position + (-gizmoTarget.up + -gizmoTarget.right + -gizmoTarget.forward) * _cubeSize,
                        bottomRightBack = gizmoTarget.transform.position + (-gizmoTarget.up + gizmoTarget.right + -gizmoTarget.forward) * _cubeSize;
                int i = 0;
                _cubeDebugLines[i++].SetPoints(topLeftForward, topRightForward);
                _cubeDebugLines[i++].SetPoints(topRightForward, bottomRightForward);
                _cubeDebugLines[i++].SetPoints(bottomRightForward, bottomLeftForward);
                _cubeDebugLines[i++].SetPoints(bottomLeftForward, topLeftForward);
                _cubeDebugLines[i++].SetPoints(topLeftBack, topRightBack);
                _cubeDebugLines[i++].SetPoints(topRightBack, bottomRightBack);
                _cubeDebugLines[i++].SetPoints(bottomRightBack, bottomLeftBack);
                _cubeDebugLines[i++].SetPoints(bottomLeftBack, topLeftBack);
                _cubeDebugLines[i++].SetPoints(topLeftBack, topLeftForward);
                _cubeDebugLines[i++].SetPoints(topRightBack, topRightForward);
                _cubeDebugLines[i++].SetPoints(bottomRightBack, bottomRightForward);
                _cubeDebugLines[i++].SetPoints(bottomLeftBack, bottomLeftForward);

                _cubeDebugLines[i++].SetPoints(gizmoTarget.transform.position, gizmoTarget.transform.position + gizmoTarget.right * _cubeSize * 2);
                _cubeDebugLines[i++].SetPoints(gizmoTarget.transform.position, gizmoTarget.transform.position + gizmoTarget.up * _cubeSize * 2);
                _cubeDebugLines[i++].SetPoints(gizmoTarget.transform.position, gizmoTarget.transform.position + gizmoTarget.forward * _cubeSize * 2);

                foreach (VectorLine line in _cubeDebugLines)
                    line.Draw();
            }
        }
    }
}