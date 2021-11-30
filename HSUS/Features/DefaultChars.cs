using System.Xml;
using ToolBox;
using ToolBox.Extensions;
#if HONEYSELECT
using System.IO;
using UnityEngine;
#elif KOIKATSU
using ChaCustom;
#elif AISHOUJO || HONEYSELECT2
using CharaCustom;
#endif

namespace HSUS.Features
{
    public class DefaultChars : IFeature
    {
        private string _defaultFemaleChar;
        private string _defaultMaleChar;

        public void Awake()
        {
        }

        public void LevelLoaded()
        {
#if !PLAYHOME
            if (HSUS._self._binary == Binary.Game &&
#if HONEYSELECT
                   HSUS._self._level == 21
#elif KOIKATSU
                   HSUS._self._level == 2
#elif AISHOUJO
                HSUS._self._level == 4
#elif HONEYSELECT2
                HSUS._self._level == 3
#endif
            )
            {

                HSUS._self.ExecuteDelayed(() =>
                {
#if HONEYSELECT
                if (string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                        LoadCustomDefault(Path.Combine(Path.Combine(Path.Combine(UserData.Path, "chara"), "female"), this._defaultFemaleChar).Replace("\\", "/"));
#elif KOIKATSU
                switch (CustomBase.Instance.modeSex)
                {
                    case 0:
                        if (string.IsNullOrEmpty(this._defaultMaleChar) == false)
                            LoadCustomDefault(UserData.Path + "chara/male/" + this._defaultMaleChar);
                        break;
                    case 1:
                        if (string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                            LoadCustomDefault(UserData.Path + "chara/female/" + this._defaultFemaleChar);
                        break;
                }
#elif AISHOUJO || HONEYSELECT2
                    switch (CustomBase.Instance.modeSex)
                    {
                        case 0:
                            if (string.IsNullOrEmpty(this._defaultMaleChar) == false)
                                LoadCustomDefault(UserData.Path + "chara/male/" + this._defaultMaleChar);
                            break;
                        case 1:
                            if (string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                                LoadCustomDefault(UserData.Path + "chara/female/" + this._defaultFemaleChar);
                            break;
                    }
#endif
                }, 2);
            }
#endif
        }


        public void LoadParams(XmlNode node)
        {
#if !PLAYHOME
            XmlNode femaleNode = node.FindChildNode("defaultFemaleChar");
            if (femaleNode != null && femaleNode.Attributes["path"] != null)
                this._defaultFemaleChar = femaleNode.Attributes["path"].Value;
            XmlNode maleNode = node.FindChildNode("defaultMaleChar");
            if (maleNode != null && maleNode.Attributes["path"] != null)
                this._defaultMaleChar = maleNode.Attributes["path"].Value;
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if !PLAYHOME
            writer.WriteStartElement("defaultFemaleChar");
            writer.WriteAttributeString("path", this._defaultFemaleChar);
            writer.WriteEndElement();

#if KOIKATSU || AISHOUJO
            writer.WriteStartElement("defaultMaleChar");
            writer.WriteAttributeString("path", this._defaultMaleChar);
            writer.WriteEndElement();
#endif
#endif
        }

#if HONEYSELECT
        private static void LoadCustomDefault(string path)
        {
            CustomControl customControl = Resources.FindObjectsOfTypeAll<CustomControl>()[0];
            int personality = customControl.chainfo.customInfo.personality;
            string name = customControl.chainfo.customInfo.name;
            bool isConcierge = customControl.chainfo.customInfo.isConcierge;
            bool flag = false;
            bool flag2 = false;
            if (customControl.modeCustom == 0)
            {
                customControl.chainfo.chaFile.Load(path);
                customControl.chainfo.chaFile.ChangeCoordinateType(customControl.chainfo.statusInfo.coordinateType);
                if (customControl.chainfo.chaFile.customInfo.isConcierge)
                {
                    flag = true;
                    flag2 = true;
                }
            }
            else
            {
                customControl.chainfo.chaFile.LoadBlockData(customControl.chainfo.customInfo, path);
                customControl.chainfo.chaFile.LoadBlockData(customControl.chainfo.chaFile.coordinateInfo, path);
                customControl.chainfo.chaFile.ChangeCoordinateType(customControl.chainfo.statusInfo.coordinateType);
                flag = true;
                flag2 = true;
            }
            customControl.chainfo.customInfo.isConcierge = isConcierge;
            if (customControl.chainfo.Sex == 0)
            {
                CharMale charMale = customControl.chainfo as CharMale;
                charMale.Reload();
                charMale.maleStatusInfo.visibleSon = false;
            }
            else
            {
                CharFemale charFemale = customControl.chainfo as CharFemale;
                charFemale.Reload();
                charFemale.UpdateBustSoftnessAndGravity();
            }
            if (flag)
            {
                customControl.chainfo.customInfo.personality = personality;
            }
            if (flag2)
            {
                customControl.chainfo.customInfo.name = name;
            }
            customControl.SetSameSetting();
            customControl.noChangeSubMenu = true;
            customControl.ChangeSwimTypeFromLoad();
            customControl.noChangeSubMenu = false;
            customControl.UpdateCharaName();
            customControl.UpdateAcsName();
        }

#elif KOIKATSU
        private static void LoadCustomDefault(string path)
        {
            ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            CustomBase.Instance.chaCtrl.chaFile.LoadFileLimited(path, chaCtrl.sex, true, true, true, true, true);
            chaCtrl.ChangeCoordinateType(true);
            chaCtrl.Reload(false, false, false, false);
            CustomBase.Instance.updateCustomUI = true;
            //CustomHistory.Instance.Add5(chaCtrl, chaCtrl.Reload, false, false, false, false);
        }
#elif AISHOUJO || HONEYSELECT2
        private static void LoadCustomDefault(string path)
        {
            Singleton<CustomBase>.Instance.chaCtrl.chaFile.LoadFileLimited(path, Singleton<CustomBase>.Instance.chaCtrl.sex);
            Singleton<CustomBase>.Instance.chaCtrl.ChangeNowCoordinate();
            Singleton<CustomBase>.Instance.chaCtrl.Reload();
            Singleton<CustomBase>.Instance.updateCustomUI = true;
            for (int i = 0; i < 20; i++)
            {
                Singleton<CustomBase>.Instance.ChangeAcsSlotName(i);
            }
            Singleton<CustomBase>.Instance.SetUpdateToggleSetting();
        }

#endif

    }
}