using System.Xml;
using ToolBox.Extensions;
using System.Collections.Generic;
using Studio;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS.Features
{
    public class FingersFKCopyButtons : IFeature
    {
#if HONEYSELECT
        private bool _fingersFkCopyButtons = true;
#endif

        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
#if HONEYSELECT
            node = node.FindChildNode("fingersFkCopyButtons");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                this._fingersFkCopyButtons = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if HONEYSELECT
            writer.WriteStartElement("fingersFkCopyButtons");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._fingersFkCopyButtons));
            writer.WriteEndElement();
#endif
        }

        public void LevelLoaded()
        {
#if HONEYSELECT
            if (this._fingersFkCopyButtons && HSUS._self._binary == Binary.Studio && HSUS._self._level == 3)
            {
                HSUS._self.ExecuteDelayed(() =>
                {
                    RectTransform toggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Right Hand").transform as RectTransform;
                    Button b = UIUtility.CreateButton("Copy Right Fingers Button", toggle.parent, "From Anim");
                    RectTransform rt = (RectTransform)b.transform;
                    rt.SetRect(toggle.anchorMin, toggle.anchorMax, new Vector2(toggle.offsetMax.x + 4f, toggle.offsetMin.y), new Vector2(toggle.offsetMax.x + 64f, toggle.offsetMax.y));

                    GameObject go = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Right Hand Control View");
                    if (go != null)
                    {
                        rt.offsetMin += new Vector2(18, 0f);
                        rt = (RectTransform)go.transform;
                        rt.anchoredPosition -= new Vector2(11f, 0f);
                    }

                    b.onClick.AddListener(() =>
                    {
                        TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                        if (treeNodeObject == null)
                            return;
                        ObjectCtrlInfo info;
                        if (!Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                            return;
                        OCIChar selected = info as OCIChar;
                        if (selected == null)
                            return;
                        CopyToFKBoneOfGroup(selected.listBones, OIBoneInfo.BoneGroup.RightHand);
                    });
                    Text text = b.GetComponentInChildren<Text>();
                    text.rectTransform.SetRect();
                    text.color = Color.white;
                    Image image = b.GetComponent<Image>();
                    image.sprite = null;
                    image.color = new Color32(89, 88, 85, 255);
                    toggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Left Hand").transform as RectTransform;
                    b = UIUtility.CreateButton("Copy Left Fingers Button", toggle.parent, "From Anim");
                    rt = (RectTransform)b.transform;
                    b.transform.SetRect(toggle.anchorMin, toggle.anchorMax, new Vector2(toggle.offsetMax.x + 4f, toggle.offsetMin.y), new Vector2(toggle.offsetMax.x + 64f, toggle.offsetMax.y));
                    go = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Left Hand Control View");
                    if (go != null)
                    {
                        rt.offsetMin += new Vector2(18, 0f);
                        rt = (RectTransform)go.transform;
                        rt.anchoredPosition -= new Vector2(11f, 0f);
                    }

                    b.onClick.AddListener(() =>
                    {
                        TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                        if (treeNodeObject == null)
                            return;
                        ObjectCtrlInfo info;
                        if (!Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                            return;
                        OCIChar selected = info as OCIChar;
                        if (selected == null)
                            return;
                        this.CopyToFKBoneOfGroup(selected.listBones, OIBoneInfo.BoneGroup.LeftHand);
                    });
                    text = b.GetComponentInChildren<Text>();
                    text.rectTransform.SetRect();
                    text.color = Color.white;
                    image = b.GetComponent<Image>();
                    image.sprite = null;
                    image.color = new Color32(89, 88, 85, 255);
                });
            }
#endif
        }

#if HONEYSELECT
        private void CopyToFKBoneOfGroup(List<OCIChar.BoneInfo> listBones, OIBoneInfo.BoneGroup group)
        {
            List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
            foreach (OCIChar.BoneInfo bone in listBones)
            {
                if (bone.guideObject != null && bone.guideObject.transformTarget != null && bone.boneGroup == group)
                {
                    Vector3 oldValue = bone.guideObject.changeAmount.rot;
                    bone.guideObject.changeAmount.rot = bone.guideObject.transformTarget.localEulerAngles;
                    infos.Add(new GuideCommand.EqualsInfo()
                    {
                        dicKey = bone.guideObject.dicKey,
                        oldValue = oldValue,
                        newValue = bone.guideObject.changeAmount.rot
                    });
                }
            }
            UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos.ToArray()));
        }
#endif
    }
}