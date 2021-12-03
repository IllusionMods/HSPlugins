#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
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
        public void Awake()
        {
        }

        public void LevelLoaded()
        {
#if !PLAYHOME
            if (HSUS._self.binary == Binary.Game &&
#if HONEYSELECT
                HSUS._self._level == 21
#elif SUNSHINE
                HSUS._self.level == 3
#elif KOIKATSU
                HSUS._self.level == 2
#elif AISHOUJO
                HSUS._self.level == 4
#elif HONEYSELECT2
                HSUS._self.level == 3
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
                            if (string.IsNullOrEmpty(HSUS.DefaultMaleChar.Value) == false)
                                LoadCustomDefault(UserData.Path + "chara/male/" + HSUS.DefaultMaleChar.Value);
                            break;
                        case 1:
                            if (string.IsNullOrEmpty(HSUS.DefaultFemaleChar.Value) == false)
                                LoadCustomDefault(UserData.Path + "chara/female/" + HSUS.DefaultFemaleChar.Value);
                            break;
                    }
#elif AISHOUJO || HONEYSELECT2
                    switch (CustomBase.Instance.modeSex)
                    {
                        case 0:
                            if (string.IsNullOrEmpty(HSUS.DefaultMaleChar.Value) == false)
                                LoadCustomDefault(UserData.Path + "chara/male/" + HSUS.DefaultMaleChar.Value);
                            break;
                        case 1:
                            if (string.IsNullOrEmpty(HSUS.DefaultFemaleChar.Value) == false)
                                LoadCustomDefault(UserData.Path + "chara/female/" + HSUS.DefaultFemaleChar.Value);
                            break;
                    }
#endif
                }, 2);
            }
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
#endif