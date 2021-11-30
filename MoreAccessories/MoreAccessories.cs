using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionPlugin;
using IllusionUtility.GetUtility;
using Manager;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoreAccessories
{
    public class MoreAccessories : GenericPlugin,
        IEnhancedPlugin
    {
        #region Private Types
        private class StudioSlotData
        {
            public RectTransform slot;
            public Text name;
            public Button onButton;
            public Button offButton;
        }

        private delegate bool TranslationDelegate(ref string text);
        #endregion

        #region Public Types
        public class CharAdditionalData
        {
            public List<GameObject> objAccessory = new List<GameObject>();
            public List<CharFileInfoClothes.Accessory> clothesInfoAccessory = new List<CharFileInfoClothes.Accessory>();
            public List<ListTypeFbx> infoAccessory = new List<ListTypeFbx>();
            public List<GameObject> objAcsMove = new List<GameObject>();
            public List<bool> showAccessory = new List<bool>();
            public Dictionary<int, List<GameObject>> charInfoDictTagObj = new Dictionary<int, List<GameObject>>();

            public Dictionary<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> rawAccessoriesInfos = new Dictionary<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>>();
        }

        public class MakerSlotData
        {
            public Button button;
            public Text text;
            public UI_TreeView treeView;

            public Toggle copyToggle;
            public Text copyText;
            public UI_OnMouseOverMessage copyOnMouseOver;

            public Toggle bulkColorToggle;
            public Text bulkColorText;
            public UI_OnMouseOverMessage bulkColorOnMouseOver;
        }
        #endregion

        #region Private Variables
        internal static MoreAccessories _self;
        internal Dictionary<CharFile, CharAdditionalData> _accessoriesByChar = new Dictionary<CharFile, CharAdditionalData>();
        internal CharAdditionalData _charaMakerAdditionalData;
        internal readonly SubMenuItem _smItem = new SubMenuItem();
        internal readonly List<MakerSlotData> _displayedMakerSlots = new List<MakerSlotData>();
        internal readonly Dictionary<string, string> _femaleMoreAttachPointsAliases = new Dictionary<string, string>()
        {
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top", "Skirt Top"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00", "Skirt Front 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01", "Skirt Front 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02", "Skirt Front 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02/cf_J_sk_00_03", "Skirt Front 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02/cf_J_sk_00_03/cf_J_sk_00_04", "Skirt Front 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02/cf_J_sk_00_03/cf_J_sk_00_04/cf_J_sk_00_05", "Skirt Front 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00", "Skirt Front Right 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01", "Skirt Front Right 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02", "Skirt Front Right 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02/cf_J_sk_01_03", "Skirt Front Right 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02/cf_J_sk_01_03/cf_J_sk_01_04", "Skirt Front Right 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02/cf_J_sk_01_03/cf_J_sk_01_04/cf_J_sk_01_05", "Skirt Front Right 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00", "Skirt Right 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01", "Skirt Right 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02", "Skirt Right 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02/cf_J_sk_02_03", "Skirt Right 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02/cf_J_sk_02_03/cf_J_sk_02_04", "Skirt Right 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02/cf_J_sk_02_03/cf_J_sk_02_04/cf_J_sk_02_05", "Skirt Right 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00", "Skirt Left 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01", "Skirt Left 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02", "Skirt Left 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02/cf_J_sk_06_03", "Skirt Left 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02/cf_J_sk_06_03/cf_J_sk_06_04", "Skirt Left 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02/cf_J_sk_06_03/cf_J_sk_06_04/cf_J_sk_06_05", "Skirt Left 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00", "Skirt Front Left 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01", "Skirt Front Left 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02", "Skirt Front Left 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02/cf_J_sk_07_03", "Skirt Front Left 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02/cf_J_sk_07_03/cf_J_sk_07_04", "Skirt Front Left 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02/cf_J_sk_07_03/cf_J_sk_07_04/cf_J_sk_07_05", "Skirt Front Left 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00", "Skirt Back Right 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01", "Skirt Back Right 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02", "Skirt Back Right 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02/cf_J_sk_03_03", "Skirt Back Right 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02/cf_J_sk_03_03/cf_J_sk_03_04", "Skirt Back Right 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02/cf_J_sk_03_03/cf_J_sk_03_04/cf_J_sk_03_05", "Skirt Back Right 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00", "Skirt Back 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01", "Skirt Back 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02", "Skirt Back 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02/cf_J_sk_04_03", "Skirt Back 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02/cf_J_sk_04_03/cf_J_sk_04_04", "Skirt Back 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02/cf_J_sk_04_03/cf_J_sk_04_04/cf_J_sk_04_05", "Skirt Back 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00", "Skirt Back Left 0"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01", "Skirt Back Left 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02", "Skirt Back Left 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02/cf_J_sk_05_03", "Skirt Back Left 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02/cf_J_sk_05_03/cf_J_sk_05_04", "Skirt Back Left 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02/cf_J_sk_05_03/cf_J_sk_05_04/cf_J_sk_05_05", "Skirt Back Left 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02", "Pelvis"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s/cf_J_Ana", "Anus"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s/cf_J_Kokan", "Pussy"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegKnee_dam_L", "Left Kneecap"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L", "Left Lower Leg 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L", "Left Lower Leg 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L/cf_J_Foot01_L", "Left Ankle 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L/cf_J_Foot01_L/cf_J_Foot02_L", "Left Heel"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L/cf_J_Foot01_L/cf_J_Foot02_L/cf_J_Toes01_L", "Left Toes"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp01_L", "Left Upper Leg 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp02_L", "Left Upper Leg 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp03_L", "Left Upper Leg 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegKnee_dam_R", "Right Kneecap"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R", "Right Lower Leg 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R", "Right Lower Leg 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R/cf_J_Foot01_R", "Right Ankle 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R/cf_J_Foot01_R/cf_J_Foot02_R", "Right Heel"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R/cf_J_Foot01_R/cf_J_Foot02_R/cf_J_Toes01_R", "Right Toes"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp01_R", "Right Upper Leg 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp02_R", "Right Upper Leg 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp03_R", "Right Upper Leg 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L/cf_J_SiriDam01_L/cf_J_Siri_L", "Left Buttcheek"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R/cf_J_SiriDam01_R/cf_J_Siri_R", "Right Buttcheek"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_LegUpDam_L", "Left Hip"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_LegUpDam_R", "Right Hip"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01", "Spine 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02", "Spine 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03", "Spine 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L", "Left Breast 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L", "Left Breast 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L", "Left Breast 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L/cf_J_Mune02_L/cf_J_Mune02_s_L/cf_J_Mune02_t_L", "Left Breast 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L/cf_J_Mune02_L/cf_J_Mune02_s_L/cf_J_Mune02_t_L/cf_J_Mune03_L/cf_J_Mune03_s_L/cf_J_Mune04_s_L", "Left Breast 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R", "Right Breast 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R", "Right Breast 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R", "Right Breast 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R/cf_J_Mune02_R/cf_J_Mune02_s_R/cf_J_Mune02_t_R", "Right Breast 4"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R/cf_J_Mune02_R/cf_J_Mune02_s_R/cf_J_Mune02_t_R/cf_J_Mune03_R/cf_J_Mune03_s_R/cf_J_Mune04_s_R", "Right Breast 5"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekLow_L", "Left Lower Cheek"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekLow_R", "Right Lower Cheek"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekUp_L", "Left Upper Cheek"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekUp_R", "Right Upper Cheek"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_Mouth_L", "Mouth Left"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_Mouth_R", "Mouth Right"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_MouthLow", "Mouth Bottom"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_Mouthup", "Mouth Top"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L", "Left Eye"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_EyePos_rz_L/cf_J_look_L/cf_J_eye_rs_L/cf_J_pupil_s_L", "Left Pupil"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R", "Right Eye"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_EyePos_rz_R/cf_J_look_R/cf_J_eye_rs_R/cf_J_pupil_s_R", "Right Pupil"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_L/cf_J_MayuMid_s_L", "Left Eyebrow Middle"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_L/cf_J_MayuTip_s_L", "Left Eyebrow Tip"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_R/cf_J_MayuMid_s_R", "Right Eyebrow Middle"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_R/cf_J_MayuTip_s_R", "Right Eyebrow Tip"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L", "Left Arm Up 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmElbo_dam_01_L", "Left Elbow"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L", "Left Arm Low 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_ArmLow02_dam_L", "Left Arm Low 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L", "Left Wrist 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Index01_L", "Left Index 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Index01_L/cf_J_Hand_Index02_L", "Left Index 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Index01_L/cf_J_Hand_Index02_L/cf_J_Hand_Index03_L", "Left Index 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Little01_L", "Left Pinky 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Little01_L/cf_J_Hand_Little02_L", "Left Pinky 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Little01_L/cf_J_Hand_Little02_L/cf_J_Hand_Little03_L", "Left Pinky 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Middle01_L", "Left Middle Finger 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Middle01_L/cf_J_Hand_Middle02_L", "Left Middle Finger 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Middle01_L/cf_J_Hand_Middle02_L/cf_J_Hand_Middle03_L", "Left Middle Finger 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Ring01_L", "Left Ring Finger 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Ring01_L/cf_J_Hand_Ring02_L", "Left Ring Finger 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Ring01_L/cf_J_Hand_Ring02_L/cf_J_Hand_Ring03_L", "Left Ring Finger 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Thumb01_L", "Left Thumb 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Thumb01_L/cf_J_Hand_Thumb02_L", "Left Thumb 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Thumb01_L/cf_J_Hand_Thumb02_L/cf_J_Hand_Thumb03_L", "Left Thumb 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmUp01_dam_L", "Left Arm Up Dam"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmUp03_dam_L", "Left Arm Up 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R", "Right Arm Up 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmElbo_dam_01_R", "Right Elbow"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R", "Right Arm Low 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_ArmLow02_dam_R", "Right Arm Low 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R", "Right Wrist 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Index01_R", "Right Index 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Index01_R/cf_J_Hand_Index02_R", "Right Index 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Index01_R/cf_J_Hand_Index02_R/cf_J_Hand_Index03_R", "Right Index 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Little01_R", "Right Pinky 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Little01_R/cf_J_Hand_Little02_R", "Right Pinky 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Little01_R/cf_J_Hand_Little02_R/cf_J_Hand_Little03_R", "Right Pinky 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Middle01_R", "Right Middle Finger 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Middle01_R/cf_J_Hand_Middle02_R", "Right Middle Finger 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Middle01_R/cf_J_Hand_Middle02_R/cf_J_Hand_Middle03_R", "Right Middle Finger 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Ring01_R", "Right Ring Finger 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Ring01_R/cf_J_Hand_Ring02_R", "Right Ring Finger 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Ring01_R/cf_J_Hand_Ring02_R/cf_J_Hand_Ring03_R", "Right Ring Finger 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Thumb01_R", "Right Thumb 1"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Thumb01_R/cf_J_Hand_Thumb02_R", "Right Thumb 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Thumb01_R/cf_J_Hand_Thumb02_R/cf_J_Hand_Thumb03_R", "Right Thumb 3"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmUp01_dam_R", "Right Arm Up Dam"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmUp03_dam_R", "Right Arm Up 2"},
            {"BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_SpineSk00_dam", "Collar Bone"}
        };
        internal List<string> _femaleMoreAttachPointsPaths = new List<string>();
        internal readonly Dictionary<string, string> _maleMoreAttachPointsAliases = new Dictionary<string, string>()
        {
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02", "Pelvis"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top", "Dick Base"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan100_00/cm_J_dan101_00/cm_J_dan109_00", "Dick Top"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan_f_top/cm_J_dan_f_L", "Left Testicle"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan_f_top/cm_J_dan_f_R", "Right Testicle"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kosi02_s/cm_J_Ana", "Anus"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegKnee_dam_L", "Left Kneecap"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L", "Left Lower Leg 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_Foot01_L", "Left Ankle 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_Foot01_L/cm_J_Foot02_L", "Left Heel"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_Foot01_L/cm_J_Foot02_L/cm_J_Toes01_L", "Left Toes"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_LegLow02_s_L", "Left Lower Leg 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_LegLow03_s_L", "Left Lower Leg 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp01_L", "Left Upper Leg 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp02_L", "Left Upper Leg 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp03_L", "Left Upper Leg 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegKnee_dam_R", "Right Kneecap"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R", "Right Lower Leg 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_Foot01_R", "Right Ankle 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_Foot01_R/cm_J_Foot02_R", "Right Heel"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_Foot01_R/cm_J_Foot02_R/cm_J_Toes01_R", "Right Toes"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_LegLow02_s_R", "Right Lower Leg 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_LegLow03_s_R", "Right Lower Leg 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp01_R", "Right Upper Leg 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp02_R", "Right Upper Leg 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp03_R", "Right Upper Leg 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_L", "Left Buttcheek"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_R", "Right Buttcheek"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_LegUpDam_L", "Left Hip"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_LegUpDam_R", "Right Hip"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01", "Spine 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02", "Spine 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03", "Spine 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekLow_L", "Left Lower Cheek"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekLow_R", "Right Lower Cheek"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekUp_L", "Left Upper Cheek"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekUp_R", "Right Upper Cheek"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_Mouth_L", "Mouth Left"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_Mouth_R", "Mouth Right"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_MouthLow", "Mouth Bottom"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_Mouthup", "Mouth Top"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L", "Left Eye"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L/cm_J_Eye_s_L/cm_J_EyePos_rz_L/cm_J_look_L/cm_J_eye_rs_L/cm_J_pupil_s_L", "Left Pupil"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R", "Right Eye"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R/cm_J_Eye_s_R/cm_J_EyePos_rz_R/cm_J_look_R/cm_J_eye_rs_R/cm_J_pupil_s_R", "Right Pupil"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_C", "Center Eyebrow"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_L/cm_J_MayuMid_s_L", "Left Eyebrow Middle"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_L/cm_J_MayuTip_s_L", "Left Eyebrow Tip"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_R/cm_J_MayuMid_s_R", "Right Eyebrow Middle"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_R/cm_J_MayuTip_s_R", "Right Eyebrow Tip"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L", "Left Arm Up 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmElbo_dam_02_L", "Left Elbow"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L", "Left Arm Low 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_ArmLow02_dam_L", "Left Arm Low 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L", "Left Wrist 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Index01_L", "Left Index 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Index01_L/cm_J_Hand_Index02_L", "Left Index 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Index01_L/cm_J_Hand_Index02_L/cm_J_Hand_Index03_L", "Left Index 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Little01_L", "Left Pinky 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Little01_L/cm_J_Hand_Little02_L", "Left Pinky 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Little01_L/cm_J_Hand_Little02_L/cm_J_Hand_Little03_L", "Left Pinky 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Middle01_L", "Left Middle Finger 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Middle01_L/cm_J_Hand_Middle02_L", "Left Middle Finger 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Middle01_L/cm_J_Hand_Middle02_L/cm_J_Hand_Middle03_L", "Left Middle Finger 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Ring01_L", "Left Ring Finger 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Ring01_L/cm_J_Hand_Ring02_L", "Left Ring Finger 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Ring01_L/cm_J_Hand_Ring02_L/cm_J_Hand_Ring03_L", "Left Ring Finger 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Thumb01_L", "Left Thumb 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Thumb01_L/cm_J_Hand_Thumb02_L", "Left Thumb 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Thumb01_L/cm_J_Hand_Thumb02_L/cm_J_Hand_Thumb03_L", "Left Thumb 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmUp02_dam_L", "Left Arm Up 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmUp03_dam_L", "Left Arm Up 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R", "Right Arm Up 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmElbo_dam_02_R", "Right Elbow"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R", "Right Arm Low 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_ArmLow02_dam_R", "Right Arm Low 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R", "Right Wrist 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Index01_R", "Right Index 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Index01_R/cm_J_Hand_Index02_R", "Right Index 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Index01_R/cm_J_Hand_Index02_R/cm_J_Hand_Index03_R", "Right Index 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Little01_R", "Right Pinky 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Little01_R/cm_J_Hand_Little02_R", "Right Pinky 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Little01_R/cm_J_Hand_Little02_R/cm_J_Hand_Little03_R", "Right Pinky 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Middle01_R", "Right Middle Finger 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Middle01_R/cm_J_Hand_Middle02_R", "Right Middle Finger 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Middle01_R/cm_J_Hand_Middle02_R/cm_J_Hand_Middle03_R", "Right Middle Finger 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Ring01_R", "Right Ring Finger 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Ring01_R/cm_J_Hand_Ring02_R", "Right Ring Finger 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Ring01_R/cm_J_Hand_Ring02_R/cm_J_Hand_Ring03_R", "Right Ring Finger 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Thumb01_R", "Right Thumb 1"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Thumb01_R/cm_J_Hand_Thumb02_R", "Right Thumb 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Thumb01_R/cm_J_Hand_Thumb02_R/cm_J_Hand_Thumb03_R", "Right Thumb 3"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmUp02_dam_R", "Right Arm Up 2"},
            {"BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmUp03_dam_R", "Right Arm Up 3"},
        };
        internal List<string> _maleMoreAttachPointsPaths = new List<string>();
        internal bool _loadAdditionalAccessories = true;
        private RectTransform _prefab;
        private SubMenuControl _smControl;
        internal SmMoreAccessories _smMoreAccessories;
        private CharInfo _charaMakerCharInfo;
        private MainMenuSelect _mainMenuSelect;
        private bool _ready = false;
        private RectTransform _addButtons;
        private Studio.OCIChar _selectedStudioCharacter;
        private readonly List<StudioSlotData> _displayedStudioSlots = new List<StudioSlotData>();
        private StudioSlotData _toggleAll;
        private TranslationDelegate _translationMethod;
        private ScrollRect _charaMakerCopyScrollView;
        private GameObject _charaMakerCopySlotTemplate;
        private LayoutElement _charaMakerBulkColorContainer;
        private GameObject _charaMakerBulkColorSlotTemplate;
        internal GuideObject _charaMakerGuideObject;
        private Camera _guideObjectCamera;
        #endregion

        #region Public Accessors
        public override string[] Filter { get { return new[] {"HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
        public override string Name { get { return "MoreAccessories"; } }
        public override string Version { get { return "1.3.1"; } }
        public CharInfo charaMakerCharInfo
        {
            get { return this._charaMakerCharInfo; }
            set
            {
                this._charaMakerCharInfo = value;
                CharAdditionalData additionalData;
                if (this._accessoriesByChar.TryGetValue(this._charaMakerCharInfo.chaFile, out additionalData) == false)
                {
                    additionalData = new CharAdditionalData();
                    this._accessoriesByChar.Add(this._charaMakerCharInfo.chaFile, additionalData);
                }

                this._charaMakerAdditionalData = additionalData;
            }
        }
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _self = this;

            switch (Process.GetCurrentProcess().ProcessName)
            {
                case "HoneySelect_32":
                case "HoneySelect_64":
                case "Honey Select Unlimited_32":
                case "Honey Select Unlimited_64":
                    this._binary = Binary.Game;
                    break;
                case "StudioNEO_32":
                case "StudioNEO_64":
                    this._binary = Binary.Studio;
                    break;
            }

            HSExtSave.HSExtSave.RegisterHandler("moreAccessories", this.OnCharaLoad, this.OnCharaSave, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, this.OnCoordLoad, this.OnCoordSave);

            UIUtility.Init();

            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //HSStudioNEOAddon_Patches.ManualPatch(harmony);
            Type t = Type.GetType("UnityEngine.UI.Translation.TextTranslator,UnityEngine.UI.Translation");
            if (t != null)
            {
                MethodInfo info = t.GetMethod("Translate", BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    this._translationMethod = (TranslationDelegate)Delegate.CreateDelegate(typeof(TranslationDelegate), info);
                }
            }
            this._femaleMoreAttachPointsPaths = this._femaleMoreAttachPointsAliases.Keys.ToList();
            this._maleMoreAttachPointsPaths = this._maleMoreAttachPointsAliases.Keys.ToList();
        }

        protected override void LevelLoaded(int level)
        {
            switch (this._binary)
            {
                case Binary.Game:
                    if (level == 21)
                        this.SpawnMakerGUI();
                    break;
                case Binary.Studio:
                    if (level == 3)
                        this.SpawnStudioGUI();
                    break;
            }
            this._ready = true;
        }

        protected override void Update()
        {
            if (this._binary == Binary.Studio && this._level == 3)
            {
                Studio.TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject != null)
                {
                    Studio.ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    {
                        Studio.OCIChar selected = info as Studio.OCIChar;
                        if (selected != this._selectedStudioCharacter)
                        {
                            this._selectedStudioCharacter = selected;
                            this.UpdateStudioUI();
                        }
                    }
                }
            }
        }

        protected override void LateUpdate()
        {
            if (this._binary == Binary.Game && this._level == 21)
                this._guideObjectCamera.fieldOfView = Camera.main.fieldOfView;
        }
        #endregion

        #region Private Methods
        private void SpawnMakerGUI()
        {
            UIUtility.SetCustomFont("mplus-1c-medium");
            if (Game.Instance.customSceneInfo.isFemale)
                this._prefab = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_Clothes/Accessory/AcsSlot10").transform as RectTransform;
            else
                this._prefab = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/MaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_Clothes/Accessory/AcsSlot10").transform as RectTransform;

            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.MoreAccessoriesResources);
            GameObject guideObjectPrefab = bundle.LoadAsset<GameObject>("M Root");
            this._charaMakerGuideObject = GameObject.Instantiate(guideObjectPrefab).AddComponent<GuideObject>();
            guideObjectPrefab.hideFlags |= HideFlags.HideInHierarchy;
            bundle.Unload(false);

            this._guideObjectCamera = new GameObject("GuideObjectCamera").AddComponent<Camera>();
            this._guideObjectCamera.transform.SetParent(Camera.main.transform);
            this._guideObjectCamera.transform.localPosition = Vector3.zero;
            this._guideObjectCamera.transform.localRotation = Quaternion.identity;
            this._guideObjectCamera.cullingMask = LayerMask.GetMask("Studio_ColSelect", "Studio_Col");
            this._guideObjectCamera.clearFlags = CameraClearFlags.Depth;
            this._guideObjectCamera.backgroundColor = Color.clear;
            this._guideObjectCamera.fieldOfView = Camera.main.fieldOfView;
            this._guideObjectCamera.depth = Camera.main.depth + 1;
            this._guideObjectCamera.nearClipPlane = Camera.main.nearClipPlane / 2f;            
            this._guideObjectCamera.renderingPath = RenderingPath.Forward;
            PhysicsRaycaster raycaster = this._guideObjectCamera.gameObject.AddComponent<PhysicsRaycaster>();
            raycaster.eventMask = LayerMask.GetMask("Studio_ColSelect");

            Dictionary<CharFile, CharAdditionalData> newDic = new Dictionary<CharFile, CharAdditionalData>();
            foreach (KeyValuePair<CharFile, CharAdditionalData> pair in this._accessoriesByChar)
            {
                if (pair.Key != null)
                    newDic.Add(pair.Key, pair.Value);
            }
            this._accessoriesByChar = newDic;
            this._displayedMakerSlots.Clear();

            foreach (SubMenuControl subMenuControl in Resources.FindObjectsOfTypeAll<SubMenuControl>())
            {
                this._smControl = subMenuControl;
                break;
            }

            foreach (SmAccessory smAccessory in Resources.FindObjectsOfTypeAll<SmAccessory>())
            {
                GameObject obj = GameObject.Instantiate(smAccessory.gameObject);
                obj.transform.SetParent(smAccessory.transform.parent);
                obj.transform.localScale = smAccessory.transform.localScale;
                obj.transform.localPosition = smAccessory.transform.localPosition;
                obj.transform.localRotation = smAccessory.transform.localRotation;
                (obj.transform as RectTransform).SetRect(smAccessory.transform as RectTransform);
                SmAccessory original = obj.GetComponent<SmAccessory>();
                this._smMoreAccessories = obj.AddComponent<SmMoreAccessories>();
                ReplaceEventsOf(this._smMoreAccessories, original);
                this._smMoreAccessories.LoadWith<SubMenuBase>(smAccessory);
                this._smMoreAccessories.PreInit(smAccessory);
                GameObject.Destroy(original);
                this._smItem.menuName = "MoreAccessories";
                this._smItem.objTop = obj;
                break;
            }

            Selectable template = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/TabMenu/Tab01").GetComponent<Selectable>();

            this._addButtons = UIUtility.CreateNewUIObject("AddAccessories", this._prefab.parent);
            this._addButtons.SetRect(this._prefab.anchorMin, this._prefab.anchorMax, this._prefab.offsetMin + new Vector2(0f, -this._prefab.rect.height * 1.2f), this._prefab.offsetMax + new Vector2(0f, -this._prefab.rect.height));
            this._addButtons.pivot = new Vector2(0.5f, 1f);
            this._addButtons.gameObject.AddComponent<UI_TreeView>();

            Button addButton = UIUtility.CreateButton("AddAccessoriesButton", this._addButtons, "+ Add accessory");
            addButton.transform.SetRect(Vector2.zero, new Vector2(0.70f, 1f), Vector2.zero, Vector2.zero);
            addButton.onClick.AddListener(this.AddSlot);
            addButton.colors = template.colors;
            ((Image)addButton.targetGraphic).sprite = ((Image)template.targetGraphic).sprite;
            Text text = addButton.GetComponentInChildren<Text>();
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = 200;
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));

            Button addTenButton = UIUtility.CreateButton("AddAccessoriesButton", this._addButtons, "Add 10");
            addTenButton.transform.SetRect(new Vector2(0.70f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            addTenButton.onClick.AddListener(this.AddTenSlots);
            addTenButton.colors = template.colors;
            ((Image)addTenButton.targetGraphic).sprite = ((Image)template.targetGraphic).sprite;
            text = addTenButton.GetComponentInChildren<Text>();
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = 200;
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));

            RectTransform container = (RectTransform)GameObject.Find($"CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu/SubItemTop/ClothesCopy_{(Game.Instance.customSceneInfo.isFemale ? "F" : "M")}/Menu").transform;
            this._charaMakerCopyScrollView = UIUtility.CreateScrollView("Toggles", container);
            this._charaMakerCopyScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerCopyScrollView.horizontal = false;
            this._charaMakerCopyScrollView.scrollSensitivity = 18f;
            if (this._charaMakerCopyScrollView.horizontalScrollbar != null)
                GameObject.Destroy(this._charaMakerCopyScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerCopyScrollView.verticalScrollbar != null)
                GameObject.Destroy(this._charaMakerCopyScrollView.verticalScrollbar.gameObject);
            GameObject.Destroy(this._charaMakerCopyScrollView.GetComponent<Image>());
            this._charaMakerCopyScrollView.transform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -183f), new Vector2(0f, -24f));
            this._charaMakerCopySlotTemplate = container.Find("accessory00").gameObject;
            _self._charaMakerCopyScrollView.content.offsetMin = new Vector2(0f, -158f);

            container = (RectTransform)GameObject.Find($"CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu/SubItemTop/ClothesColorCtrl_{(Game.Instance.customSceneInfo.isFemale ? "F" : "M")}/Menu/Top/ScrollView/ControlPanel/select").transform;
            this._charaMakerBulkColorContainer = UIUtility.CreateNewUIObject("Toggles", container).gameObject.AddComponent<LayoutElement>();
            VerticalLayoutGroup group = container.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            ContentSizeFitter contentSizeFitter = container.parent.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            container.parent.Find("Color01").gameObject.AddComponent<LayoutElement>().preferredHeight = 122;
            container.parent.Find("Color02").gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            container.parent.Find("TglReflectColor").gameObject.AddComponent<LayoutElement>().preferredHeight = 20;
            container.parent.Find("Panel").gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            Transform t = container.parent.Find("PanelComment");
            t.SetParent(container.parent.Find("Panel"));
            t.SetRect();

            group = container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            contentSizeFitter = container.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this._charaMakerBulkColorContainer.transform.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -182f), new Vector2(308f, 0f));
            this._charaMakerBulkColorContainer.preferredHeight = 182f;
            this._charaMakerBulkColorContainer.preferredWidth = 308;

            this._charaMakerBulkColorSlotTemplate = container.Find("accessory00").gameObject;


            while (container.GetChild(0).name.Equals("all_on") == false)
                container.GetChild(0).SetParent(this._charaMakerBulkColorContainer.transform);
            this._charaMakerBulkColorContainer.transform.SetAsFirstSibling();


            RectTransform buttonsContainer = UIUtility.CreateNewUIObject("Buttons", container);
            buttonsContainer.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -228f), new Vector2(304f, -184f));
            LayoutElement element = buttonsContainer.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 300f;
            element.preferredHeight = 44f;
            container.Find("all_on").SetParent(buttonsContainer);
            container.Find("all_off").SetParent(buttonsContainer);
            container.Find("clothes_on").SetParent(buttonsContainer);
            container.Find("accessory_on").SetParent(buttonsContainer);
        }
        
        public static void ReplaceEventsOf(object self, object obj)
        {
            foreach (Button b in Resources.FindObjectsOfTypeAll<Button>())
            {
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onClick.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onClick.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
            foreach (Slider b in Resources.FindObjectsOfTypeAll<Slider>())
            {
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
            foreach (InputField b in Resources.FindObjectsOfTypeAll<InputField>())
            {
                for (int i = 0; i < b.onEndEdit.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onEndEdit.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onEndEdit.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
                if (b.onValidateInput != null && ReferenceEquals(b.onValidateInput.Target, obj))
                {
                    b.onValidateInput.SetPrivate("_target", obj);
                }
            }
            foreach (Toggle b in Resources.FindObjectsOfTypeAll<Toggle>())
            {
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }

#if HONEYSELECT
            foreach (UI_OnEnableEvent b in Resources.FindObjectsOfTypeAll<UI_OnEnableEvent>())
            {
                for (int i = 0; i < b._event.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b._event.GetPersistentTarget(i), obj))
                    {
                        IList objects = b._event.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
#endif

            foreach (EventTrigger b in Resources.FindObjectsOfTypeAll<EventTrigger>())
            {
                foreach (EventTrigger.Entry et in b.triggers)
                {
                    for (int i = 0; i < et.callback.GetPersistentEventCount(); ++i)
                    {
                        if (ReferenceEquals(et.callback.GetPersistentTarget(i), obj))
                        {
                            IList objects = et.callback.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                            objects[i].SetPrivate("m_Target", self);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SmClothesCopy), "Init")]
        private class SmClothesCopy_Init_Patches
        {
            private static void Postfix(SmClothesCopy __instance)
            {
                while (__instance.objMenu.transform.GetChild(2).name.Equals("all_on") == false)
                    __instance.objMenu.transform.GetChild(2).SetParent(_self._charaMakerCopyScrollView.content);
                foreach (MakerSlotData slot in _self._displayedMakerSlots)
                {
                    slot.copyOnMouseOver.imgComment = __instance.imgComment;
                    slot.copyOnMouseOver.txtComment = __instance.txtComment;
                }
            }
        }

        [HarmonyPatch(typeof(SmClothesColorCtrl), "Init")]
        private class SmClothesColorCtrl_Init_Patches
        {
            private static void Postfix(SmClothesColorCtrl __instance, Toggle[] ___tglClothes, Text[] ___txtClothes, Image[] ___imgClothes, UI_OnMouseOverMessage[] ___ui_OverMsgClothes, Toggle[] ___tglHairAcs, Text[] ___txtHairAcs, Image[] ___imgHairAcs, Toggle[] ___tglAccessory, UI_OnMouseOverMessage[] ___ui_OverMsgAccessory)
            {
                if (__instance.objSelect)
                {
                    for (int j = 0; j < ___tglClothes.Length; j++)
                    {
                        Transform transform = _self._charaMakerBulkColorContainer.transform.FindChild("clothes" + j.ToString("00"));
                        if (transform)
                        {
                            ___tglClothes[j] = transform.GetComponent<Toggle>();
                            if (___tglClothes[j])
                            {
                                GameObject gameObject = transform.FindLoop("SubItem");
                                if (gameObject)
                                {
                                    ___txtClothes[j] = gameObject.GetComponent<Text>();
                                }
                                gameObject = transform.FindLoop("Checkmark");
                                if (gameObject)
                                {
                                    ___imgClothes[j] = gameObject.GetComponent<Image>();
                                }
                            }
                            ___ui_OverMsgClothes[j] = transform.gameObject.AddComponent<global::UI_OnMouseOverMessage>();
                            if (___ui_OverMsgClothes[j])
                            {
                                ___ui_OverMsgClothes[j].imgComment = __instance.imgComment;
                                ___ui_OverMsgClothes[j].txtComment = __instance.txtComment;
                            }
                        }
                    }
                    for (int k = 0; k < ___tglHairAcs.Length; k++)
                    {
                        Transform transform = _self._charaMakerBulkColorContainer.transform.FindChild("hairacs" + k.ToString("00"));
                        if (transform)
                        {
                            ___tglHairAcs[k] = transform.GetComponent<Toggle>();
                            if (___tglHairAcs[k])
                            {
                                GameObject gameObject = transform.FindLoop("SubItem");
                                if (gameObject)
                                {
                                    ___txtHairAcs[k] = gameObject.GetComponent<Text>();
                                }
                                gameObject = transform.FindLoop("Checkmark");
                                if (gameObject)
                                {
                                    ___imgHairAcs[k] = gameObject.GetComponent<Image>();
                                }
                            }
                        }
                    }
                    for (int l = 0; l < ___tglAccessory.Length; l++)
                    {
                        Transform transform = _self._charaMakerBulkColorContainer.transform.FindChild("accessory" + l.ToString("00"));
                        if (transform)
                        {
                            ___tglAccessory[l] = transform.GetComponent<Toggle>();
                            ___ui_OverMsgAccessory[l] = transform.gameObject.AddComponent<global::UI_OnMouseOverMessage>();
                            if (___ui_OverMsgAccessory[l])
                            {
                                ___ui_OverMsgAccessory[l].imgComment = __instance.imgComment;
                                ___ui_OverMsgAccessory[l].txtComment = __instance.txtComment;
                            }
                        }
                    }
                }
                foreach (MakerSlotData slot in _self._displayedMakerSlots)
                {
                    slot.bulkColorOnMouseOver.imgComment = __instance.imgComment;
                    slot.bulkColorOnMouseOver.txtComment = __instance.txtComment;
                }
            }
        }

        private void SpawnStudioGUI()
        {
            Transform accList = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot");
            this._prefab = accList.Find("Slot10") as RectTransform;

            Studio.MPCharCtrl ctrl = ((Studio.MPCharCtrl)Studio.Studio.Instance.rootButtonCtrl.GetPrivate("manipulate").GetPrivate("m_ManipulatePanelCtrl").GetPrivate("charaPanelInfo").GetPrivate("m_MPCharCtrl"));

            this._toggleAll = new StudioSlotData();
            this._toggleAll.slot = (RectTransform)GameObject.Instantiate(this._prefab.gameObject).transform;
            this._toggleAll.name = this._toggleAll.slot.GetComponentInChildren<Text>();
            this._toggleAll.onButton = this._toggleAll.slot.GetChild(1).GetComponent<Button>();
            this._toggleAll.offButton = this._toggleAll.slot.GetChild(2).GetComponent<Button>();
            this._toggleAll.name.text = "All";
            this._toggleAll.slot.SetParent(this._prefab.parent);
            this._toggleAll.slot.localPosition = Vector3.zero;
            this._toggleAll.slot.localScale = Vector3.one;
            this._toggleAll.onButton.onClick = new Button.ButtonClickedEvent();
            this._toggleAll.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.chaClothes.SetAccessoryStateAll(true);
                ctrl.UpdateInfo();
                this.UpdateStudioUI();
            });
            this._toggleAll.offButton.onClick = new Button.ButtonClickedEvent();
            this._toggleAll.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.chaClothes.SetAccessoryStateAll(false);
                ctrl.UpdateInfo();
                this.UpdateStudioUI();
            });
            this._toggleAll.slot.SetAsLastSibling();

        }

        internal void UpdateMakerGUI()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null || this._prefab == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
            {
                MakerSlotData sd;
                if (i < this._displayedMakerSlots.Count)
                {
                    sd = this._displayedMakerSlots[i];
                    sd.treeView.SetUnused(false);
                    sd.copyToggle.gameObject.SetActive(true);
                    sd.bulkColorToggle.gameObject.SetActive(true);
                }
                else
                {
                    sd = new MakerSlotData();
                    GameObject obj = GameObject.Instantiate(this._prefab.gameObject);
                    obj.transform.SetParent(this._prefab.parent);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localScale = this._prefab.localScale;
                    Transform selectRect = obj.transform.Find("MainSelectClothes");
                    if (selectRect != null)
                        GameObject.Destroy(selectRect.transform);
                    RectTransform rt = obj.transform as RectTransform;
                    rt.SetRect(this._prefab.anchorMin, this._prefab.anchorMax, this._prefab.offsetMin + new Vector2(0f, -this._prefab.rect.height), this._prefab.offsetMax + new Vector2(0f, -this._prefab.rect.height));
                    sd.button = obj.GetComponent<Button>();
                    sd.text = sd.button.GetComponentInChildren<Text>();
                    sd.treeView = sd.button.GetComponent<UI_TreeView>();
                    sd.button.onClick = new Button.ButtonClickedEvent();
                    string menuStr = "SM_MoreAccessories_" + i;
                    this._mainMenuSelect = GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MMSelectCtrlClothes").GetComponent<MainMenuSelect>();
                    sd.button.onClick.AddListener(() =>
                    {
                        this._smControl.ChangeSubMenu(menuStr);
                        this._mainMenuSelect.OnClick(rt);
                    });


                    int i2 = i + 1;

                    sd.copyToggle = GameObject.Instantiate(this._charaMakerCopySlotTemplate).GetComponent<Toggle>();
                    sd.copyText = sd.copyToggle.GetComponentInChildren<Text>();
                    sd.copyOnMouseOver = sd.copyToggle.GetComponent<UI_OnMouseOverMessage>();
                    if (sd.copyOnMouseOver == null)
                        sd.copyOnMouseOver = sd.copyToggle.gameObject.AddComponent<UI_OnMouseOverMessage>();
                    sd.copyToggle.transform.SetParent(this._charaMakerCopyScrollView.content);
                    ((RectTransform)sd.copyToggle.transform).anchoredPosition = new Vector2(4f + 104f * (i2 % 3), -140f - 20f * (i2 / 3));
                    sd.copyToggle.transform.localScale = this._charaMakerCopySlotTemplate.transform.localScale;
                    sd.copyToggle.gameObject.name = "accessory" + (i + 10);
                    sd.copyText.text = " Accessory " + (i + 11);

                    sd.bulkColorToggle = GameObject.Instantiate(this._charaMakerBulkColorSlotTemplate).GetComponent<Toggle>();
                    sd.bulkColorText = sd.bulkColorToggle.GetComponentInChildren<Text>();
                    sd.bulkColorOnMouseOver = sd.bulkColorToggle.GetComponent<UI_OnMouseOverMessage>();
                    if (sd.bulkColorOnMouseOver == null)
                        sd.bulkColorOnMouseOver = sd.bulkColorToggle.gameObject.AddComponent<UI_OnMouseOverMessage>();
                    sd.bulkColorToggle.transform.SetParent(this._charaMakerBulkColorContainer.transform);
                    ((RectTransform)sd.bulkColorToggle.transform).anchoredPosition = new Vector2(4f + 100f * (i2 % 3), -164f - 20f * (i2 / 3));
                    sd.bulkColorToggle.transform.localScale = this._charaMakerBulkColorSlotTemplate.transform.localScale;
                    sd.bulkColorToggle.gameObject.name = "accessory" + (i + 10);
                    sd.bulkColorText.text = " Accessory " + (i + 11);

                    this._displayedMakerSlots.Add(sd);
                }
            }
            this._charaMakerCopyScrollView.content.offsetMin = new Vector2(0f, -158f - 20f * (i / 3));
            this._charaMakerBulkColorContainer.preferredHeight = 184 + 20f * (i / 3);

            for (; i < this._displayedMakerSlots.Count; i++)
            {
                MakerSlotData sd = this._displayedMakerSlots[i];
                sd.treeView.SetUnused(true);
                sd.copyToggle.gameObject.SetActive(false);
                sd.bulkColorToggle.gameObject.SetActive(false);
            }
            this.CustomControl_UpdateAcsName();
            this._addButtons.SetAsLastSibling();
            this._prefab.parent.GetComponent<UI_TreeView>().UpdateView();
            this._smMoreAccessories.UpdateUI();
        }

        internal void UpdateStudioUI()
        {
            if (this._binary != Binary.Studio || this._selectedStudioCharacter == null || this._level != 3)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
            {
                StudioSlotData slot;
                CharFileInfoClothes.Accessory accessory = additionalData.clothesInfoAccessory[i];
                if (i < this._displayedStudioSlots.Count)
                {
                    slot = this._displayedStudioSlots[i];
                }
                else
                {
                    slot = new StudioSlotData();
                    slot.slot = (RectTransform)GameObject.Instantiate(this._prefab.gameObject).transform;
                    slot.name = slot.slot.GetComponentInChildren<Text>();
                    slot.onButton = slot.slot.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.GetChild(2).GetComponent<Button>();
                    slot.name.text = "Accessory " + (11 + i);
                    slot.slot.SetParent(this._prefab.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    int i1 = i;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessory[i1] = true;
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessory[i1] = false;
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    this._displayedStudioSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.onButton.interactable = accessory != null && accessory.type != -1;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.showAccessory[i] ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null && accessory.type != -1;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.showAccessory[i] ? Color.green : Color.white;
            }
            for (; i < this._displayedStudioSlots.Count; ++i)
                this._displayedStudioSlots[i].slot.gameObject.SetActive(false);
            this._toggleAll.slot.SetAsLastSibling();
        }

        internal void UIFallbackToCoordList()
        {
            this._smControl.ChangeSubMenu(SubMenuControl.SubMenuType.SM_ClothesLoad.ToString());
            this._smControl.ExecuteDelayed(() =>
            {
                if (Manager.Game.Instance.customSceneInfo.isFemale)
                    this._mainMenuSelect.OnClickScript(GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_System/SaveDelete") as RectTransform);
                else
                    this._mainMenuSelect.OnClickScript(GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/MaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_System/SaveDelete") as RectTransform);
            }, 2);
        }

        internal void CustomControl_UpdateAcsName()
        {
            for (int i = 0; i < this._charaMakerAdditionalData.clothesInfoAccessory.Count; ++i)
                this._displayedMakerSlots[i].text.text = this.CustomControl_GetAcsName(i, 14);
        }

        internal string CustomControl_GetAcsName(int slotNo, int limit, bool addType = false, bool addNo = true)
        {
            string str1 = string.Empty;
            if (null == this._charaMakerCharInfo)
            {
                Debug.LogWarning("まだ初期化されてない");
                return str1;
            }
            CharFileInfoClothes.Accessory accessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo];
            string str2;
            if (this._charaMakerCharInfo.Sex == 0)
            {
                if (accessory.type == -1)
                {
                    str2 = "None";
                }
                else
                {
                    Dictionary<int, ListTypeFbx> accessoryFbxList = this._charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type);
                    ListTypeFbx listTypeFbx = null;
                    str2 = accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx) ? listTypeFbx.Name : "None";
                }
            }
            else if (accessory.type == -1)
            {
                str2 = "None";
            }
            else
            {
                Dictionary<int, ListTypeFbx> accessoryFbxList = this._charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type);
                ListTypeFbx listTypeFbx = null;
                str2 = accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx) ? listTypeFbx.Name : "None";
            }
            if (this._translationMethod != null)
                this._translationMethod(ref str2);
            if (addNo)
                str2 = (slotNo + 11).ToString("00") + " " + str2;
            if (addType)
                str2 = CharDefine.AccessoryTypeName[accessory.type + 1] + ":" + str2;
            str1 = str2.Substring(0, Mathf.Min(limit, str2.Length));
            return str1;
        }

        internal void DuplicateCharacter(CharInfo source, CharInfo destination)
        {
            CharAdditionalData sourceAdditionalData;
            if (this._accessoriesByChar.TryGetValue(source.chaFile, out sourceAdditionalData) == false)
            {
                return;
            }
            CharAdditionalData destinationAdditionalData;
            if (this._accessoriesByChar.TryGetValue(destination.chaFile, out destinationAdditionalData) == false)
            {
                destinationAdditionalData = new CharAdditionalData();
                this._accessoriesByChar.Add(destination.chaFile, destinationAdditionalData);
            }

            foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> accessories in sourceAdditionalData.rawAccessoriesInfos)
            {
                List<CharFileInfoClothes.Accessory> accessories2;
                if (destinationAdditionalData.rawAccessoriesInfos.TryGetValue(accessories.Key, out accessories2))
                    accessories2.Clear();
                else
                {
                    accessories2 = new List<CharFileInfoClothes.Accessory>();
                    destinationAdditionalData.rawAccessoriesInfos.Add(accessories.Key, accessories2);
                }

                foreach (CharFileInfoClothes.Accessory accessory in accessories.Value)
                {
                    CharFileInfoClothes.Accessory newAccessory = new CharFileInfoClothes.Accessory();
                    newAccessory.Copy(accessory);
                    accessories2.Add(newAccessory);
                }
            }
            destinationAdditionalData.showAccessory.AddRange(sourceAdditionalData.showAccessory);
            while (destinationAdditionalData.infoAccessory.Count < destinationAdditionalData.clothesInfoAccessory.Count)
                destinationAdditionalData.infoAccessory.Add(null);
            while (destinationAdditionalData.objAccessory.Count < destinationAdditionalData.clothesInfoAccessory.Count)
                destinationAdditionalData.objAccessory.Add(null);
            while (destinationAdditionalData.objAcsMove.Count < destinationAdditionalData.clothesInfoAccessory.Count)
                destinationAdditionalData.objAcsMove.Add(null);
            CharBody_ChangeAccessory_Patches.Postfix(destination.chaBody, true);

            this.UpdateStudioUI();
        }

        private void AddSlot()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            additionalData.clothesInfoAccessory.Add(new CharFileInfoClothes.Accessory());
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this._charaMakerCharInfo.chaBody, additionalData, additionalData.clothesInfoAccessory.Count - 1, -1, -1, "", true);
            this.UpdateMakerGUI();
        }

        private void AddTenSlots()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            for (int i = 0; i < 10; i++)
                additionalData.clothesInfoAccessory.Add(new CharFileInfoClothes.Accessory());
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            for (int i = 0; i < 10; i++)
            {
                int idx = additionalData.clothesInfoAccessory.Count - 10 + i;
                CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this._charaMakerCharInfo.chaBody, additionalData, idx, -1, -1, "", true);
            }
            this.UpdateMakerGUI();
        }
        #endregion

        #region Saves
        private void OnCharaSave(CharFile charFile, XmlTextWriter writer)
        {
            this.OnCharaSaveGeneric(charFile, writer);
        }

        private void OnCharaSaveGeneric(CharFile charFile, XmlTextWriter writer, bool writeVisibility = false)
        {
            CharAdditionalData additionalData;
            if (!this._accessoriesByChar.TryGetValue(charFile, out additionalData))
                return;
            int maxCount = 0;
            foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> kvp in additionalData.rawAccessoriesInfos)
            {
                writer.WriteStartElement("accessorySet");
                writer.WriteAttributeString("type", XmlConvert.ToString((int)kvp.Key));
                foreach (CharFileInfoClothes.Accessory accessory in kvp.Value)
                {
                    writer.WriteStartElement("accessory");

                    if (accessory.type != -1)
                    {
                        writer.WriteAttributeString("type", XmlConvert.ToString(accessory.type));
                        writer.WriteAttributeString("id", XmlConvert.ToString(accessory.id));
                        writer.WriteAttributeString("parentKey", accessory.parentKey);
                        writer.WriteAttributeString("addPosX", XmlConvert.ToString(accessory.addPos.x));
                        writer.WriteAttributeString("addPosY", XmlConvert.ToString(accessory.addPos.y));
                        writer.WriteAttributeString("addPosZ", XmlConvert.ToString(accessory.addPos.z));
                        writer.WriteAttributeString("addRotX", XmlConvert.ToString(accessory.addRot.x));
                        writer.WriteAttributeString("addRotY", XmlConvert.ToString(accessory.addRot.y));
                        writer.WriteAttributeString("addRotZ", XmlConvert.ToString(accessory.addRot.z));
                        writer.WriteAttributeString("addSclX", XmlConvert.ToString(accessory.addScl.x));
                        writer.WriteAttributeString("addSclY", XmlConvert.ToString(accessory.addScl.y));
                        writer.WriteAttributeString("addSclZ", XmlConvert.ToString(accessory.addScl.z));

                        writer.WriteAttributeString("colorHSVDiffuseH", XmlConvert.ToString((double)accessory.color.hsvDiffuse.H));
                        writer.WriteAttributeString("colorHSVDiffuseS", XmlConvert.ToString((double)accessory.color.hsvDiffuse.S));
                        writer.WriteAttributeString("colorHSVDiffuseV", XmlConvert.ToString((double)accessory.color.hsvDiffuse.V));
                        writer.WriteAttributeString("colorAlpha", XmlConvert.ToString((double)accessory.color.alpha));
                        writer.WriteAttributeString("colorHSVSpecularH", XmlConvert.ToString((double)accessory.color.hsvSpecular.H));
                        writer.WriteAttributeString("colorHSVSpecularS", XmlConvert.ToString((double)accessory.color.hsvSpecular.S));
                        writer.WriteAttributeString("colorHSVSpecularV", XmlConvert.ToString((double)accessory.color.hsvSpecular.V));
                        writer.WriteAttributeString("colorSpecularIntensity", XmlConvert.ToString((double)accessory.color.specularIntensity));
                        writer.WriteAttributeString("colorSpecularSharpness", XmlConvert.ToString((double)accessory.color.specularSharpness));

                        writer.WriteAttributeString("color2HSVDiffuseH", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.H));
                        writer.WriteAttributeString("color2HSVDiffuseS", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.S));
                        writer.WriteAttributeString("color2HSVDiffuseV", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.V));
                        writer.WriteAttributeString("color2Alpha", XmlConvert.ToString((double)accessory.color2.alpha));
                        writer.WriteAttributeString("color2HSVSpecularH", XmlConvert.ToString((double)accessory.color2.hsvSpecular.H));
                        writer.WriteAttributeString("color2HSVSpecularS", XmlConvert.ToString((double)accessory.color2.hsvSpecular.S));
                        writer.WriteAttributeString("color2HSVSpecularV", XmlConvert.ToString((double)accessory.color2.hsvSpecular.V));
                        writer.WriteAttributeString("color2SpecularIntensity", XmlConvert.ToString((double)accessory.color2.specularIntensity));
                        writer.WriteAttributeString("color2SpecularSharpness", XmlConvert.ToString((double)accessory.color2.specularSharpness));
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                if (kvp.Value.Count > maxCount)
                    maxCount = kvp.Value.Count;
            }
            if (writeVisibility)
            {
                writer.WriteStartElement("visibility");
                for (int i = 0; i < maxCount && i < additionalData.showAccessory.Count; i++)
                {
                    writer.WriteStartElement("visible");
                    writer.WriteAttributeString("value", XmlConvert.ToString(additionalData.showAccessory[i]));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        internal void OnCharaLoad(CharFile charFile, XmlNode node)
        {
            this.OnCharaLoadGeneric(charFile, node);
        }

        private void OnCharaLoadGeneric(CharFile charFile, XmlNode node, bool readVisibility = false)
        {
            if (this._loadAdditionalAccessories == false)
                return;
            CharAdditionalData additionalData;
            if (this._accessoriesByChar.TryGetValue(charFile, out additionalData) == false)
            {
                additionalData = new CharAdditionalData();
                this._accessoriesByChar.Add(charFile, additionalData);
            }
            else if (node == null)
            {
                foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> pair in additionalData.rawAccessoriesInfos) // Useful only in the chara maker
                    pair.Value.Clear();
            }
            if (node != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "accessorySet":
                            CharDefine.CoordinateType type = (CharDefine.CoordinateType)XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                            List<CharFileInfoClothes.Accessory> accessories2;
                            if (additionalData.rawAccessoriesInfos.TryGetValue(type, out accessories2))
                                accessories2.Clear();
                            else
                            {
                                accessories2 = new List<CharFileInfoClothes.Accessory>();
                                additionalData.rawAccessoriesInfos.Add(type, accessories2);
                            }
                            foreach (XmlNode grandChildNode in childNode.ChildNodes)
                            {
                                CharFileInfoClothes.Accessory accessory;
                                if (grandChildNode.Attributes != null && grandChildNode.Attributes["type"] != null && XmlConvert.ToInt32(grandChildNode.Attributes["type"].Value) != -1)
                                    accessory = new CharFileInfoClothes.Accessory
                                    {
                                        type = XmlConvert.ToInt32(grandChildNode.Attributes["type"].Value),
                                        id = XmlConvert.ToInt32(grandChildNode.Attributes["id"].Value),
                                        parentKey = grandChildNode.Attributes["parentKey"].Value,
                                        addPos =
                                        {
                                            x = XmlConvert.ToSingle(grandChildNode.Attributes["addPosX"].Value),
                                            y = XmlConvert.ToSingle(grandChildNode.Attributes["addPosY"].Value),
                                            z = XmlConvert.ToSingle(grandChildNode.Attributes["addPosZ"].Value)
                                        },
                                        addRot =
                                        {
                                            x = XmlConvert.ToSingle(grandChildNode.Attributes["addRotX"].Value),
                                            y = XmlConvert.ToSingle(grandChildNode.Attributes["addRotY"].Value),
                                            z = XmlConvert.ToSingle(grandChildNode.Attributes["addRotZ"].Value)
                                        },
                                        addScl =
                                        {
                                            x = XmlConvert.ToSingle(grandChildNode.Attributes["addSclX"].Value),
                                            y = XmlConvert.ToSingle(grandChildNode.Attributes["addSclY"].Value),
                                            z = XmlConvert.ToSingle(grandChildNode.Attributes["addSclZ"].Value)
                                        },
                                        color = new HSColorSet
                                        {
                                            hsvDiffuse =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseV"].Value)
                                            },
                                            alpha = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorAlpha"].Value),
                                            hsvSpecular =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularV"].Value)
                                            },
                                            specularIntensity = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorSpecularIntensity"].Value),
                                            specularSharpness = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorSpecularSharpness"].Value)
                                        },
                                        color2 = new HSColorSet
                                        {
                                            hsvDiffuse =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseV"].Value)
                                            },
                                            alpha = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2Alpha"].Value),
                                            hsvSpecular =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularV"].Value)
                                            },
                                            specularIntensity = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2SpecularIntensity"].Value),
                                            specularSharpness = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2SpecularSharpness"].Value)
                                        }
                                    };
                                else
                                    accessory = new CharFileInfoClothes.Accessory();
                                accessories2.Add(accessory);
                            }
                            break;
                        case "visibility":
                            if (readVisibility == false)
                                break;
                            additionalData.showAccessory = new List<bool>();
                            foreach (XmlNode grandChildNode in childNode.ChildNodes)
                            {
                                switch (grandChildNode.Name)
                                {
                                    case "visible":
                                        additionalData.showAccessory.Add(grandChildNode.Attributes?["value"] == null || XmlConvert.ToBoolean(grandChildNode.Attributes["value"].Value));
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo == null || this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            this.UpdateMakerGUI();
            this.UpdateStudioUI();
        }

        private void OnSceneSave(string path, XmlTextWriter xmlWriter)
        {
            SortedDictionary<int, Studio.ObjectCtrlInfo> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> kvp in dic)
            {
                Studio.OCIChar ociChar = kvp.Value as Studio.OCIChar;
                if (ociChar != null)
                {
                    xmlWriter.WriteStartElement("characterInfo");
                    xmlWriter.WriteAttributeString("name", ociChar.charInfo.customInfo.name);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));
                    this.OnCharaSaveGeneric(ociChar.charInfo.chaFile, xmlWriter, true);
                    xmlWriter.WriteEndElement();
                }
            }
        }

        private void OnSceneLoad(string path, XmlNode n)
        {
            if (n == null)
                return;
            XmlNode node = n.CloneNode(true);
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, Studio.ObjectCtrlInfo>> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                int i = 0;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    Studio.OCIChar ociChar = null;
                    while (i < dic.Count && (ociChar = dic[i].Value as Studio.OCIChar) == null)
                        ++i;
                    if (i == dic.Count)
                        break;
                    this.OnCharaLoadGeneric(ociChar.charInfo.chaFile, childNode, true);
                    ociChar.charBody.ChangeAccessory();
                    ++i;
                }
            }, 3);
        }

        private void OnSceneImport(string path, XmlNode n)
        {
            if (n == null)
                return;
            XmlNode node = n.CloneNode(true);
            int max = -1;
            foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Key > max)
                    max = pair.Key;
            }
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, Studio.ObjectCtrlInfo>> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).Where(p => p.Key > max).ToList();

                int i = 0;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    Studio.OCIChar ociChar = null;
                    while (i < dic.Count && (ociChar = dic[i].Value as Studio.OCIChar) == null)
                        ++i;
                    if (i == dic.Count)
                        break;
                    this.OnCharaLoadGeneric(ociChar.charInfo.chaFile, childNode, true);
                    ociChar.charBody.ChangeAccessory();
                    ++i;
                }
            }, 3);
        }

        internal void OnCoordLoad(CharFileInfoClothes clothesinfo, XmlNode node)
        {
            CharAdditionalData additionalData = null;
            switch (this._binary)
            {
                case Binary.Game:

                    switch (this._level)
                    {
                        case 21:
                            additionalData = this._charaMakerAdditionalData;
                            break;
                        case 15:
                            if (HSceneClothChange_InitCharaList_Patches._isInitializing == false)
                                additionalData = this._accessoriesByChar[Singleton<Character>.Instance.GetFemale(Singleton<HSceneManager>.Instance.numFemaleClothCustom).femaleFile];
                            break;
                        default:
                            foreach (CharInfo charInfo in Resources.FindObjectsOfTypeAll<CharInfo>())
                            {
                                if (charInfo.clothesInfo == clothesinfo)
                                {
                                    additionalData = this._accessoriesByChar[charInfo.chaFile];
                                    break;
                                }
                            }
                            break;
                    }
                    break;
                case Binary.Studio:
                    if (this._selectedStudioCharacter != null && clothesinfo == this._selectedStudioCharacter.charInfo.clothesInfo)
                        additionalData = this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile];
                    break;
            }
            if (additionalData == null || this._loadAdditionalAccessories == false)
                return;
            List<CharFileInfoClothes.Accessory> accessories2 = additionalData.clothesInfoAccessory;
            accessories2.Clear();

            if (node != null)
            {
                node = node.FirstChild;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    CharFileInfoClothes.Accessory accessory;
                    if (childNode.Attributes != null && childNode.Attributes["type"] != null && XmlConvert.ToInt32(childNode.Attributes["type"].Value) != -1)
                        accessory = new CharFileInfoClothes.Accessory
                        {
                            type = XmlConvert.ToInt32(childNode.Attributes["type"].Value),
                            id = XmlConvert.ToInt32(childNode.Attributes["id"].Value),
                            parentKey = childNode.Attributes["parentKey"].Value,
                            addPos =
                            {
                                x = XmlConvert.ToSingle(childNode.Attributes["addPosX"].Value),
                                y = XmlConvert.ToSingle(childNode.Attributes["addPosY"].Value),
                                z = XmlConvert.ToSingle(childNode.Attributes["addPosZ"].Value)
                            },
                            addRot =
                            {
                                x = XmlConvert.ToSingle(childNode.Attributes["addRotX"].Value),
                                y = XmlConvert.ToSingle(childNode.Attributes["addRotY"].Value),
                                z = XmlConvert.ToSingle(childNode.Attributes["addRotZ"].Value)
                            },
                            addScl =
                            {
                                x = XmlConvert.ToSingle(childNode.Attributes["addSclX"].Value),
                                y = XmlConvert.ToSingle(childNode.Attributes["addSclY"].Value),
                                z = XmlConvert.ToSingle(childNode.Attributes["addSclZ"].Value)
                            },
                            color = new HSColorSet
                            {
                                hsvDiffuse =
                                {
                                    H = (float)XmlConvert.ToDouble(childNode.Attributes["colorHSVDiffuseH"].Value),
                                    S = (float)XmlConvert.ToDouble(childNode.Attributes["colorHSVDiffuseS"].Value),
                                    V = (float)XmlConvert.ToDouble(childNode.Attributes["colorHSVDiffuseV"].Value)
                                },
                                alpha = (float)XmlConvert.ToDouble(childNode.Attributes["colorAlpha"].Value),
                                hsvSpecular =
                                {
                                    H = (float)XmlConvert.ToDouble(childNode.Attributes["colorHSVSpecularH"].Value),
                                    S = (float)XmlConvert.ToDouble(childNode.Attributes["colorHSVSpecularS"].Value),
                                    V = (float)XmlConvert.ToDouble(childNode.Attributes["colorHSVSpecularV"].Value)
                                },
                                specularIntensity = (float)XmlConvert.ToDouble(childNode.Attributes["colorSpecularIntensity"].Value),
                                specularSharpness = (float)XmlConvert.ToDouble(childNode.Attributes["colorSpecularSharpness"].Value)
                            },
                            color2 = new HSColorSet
                            {
                                hsvDiffuse =
                                {
                                    H = (float)XmlConvert.ToDouble(childNode.Attributes["color2HSVDiffuseH"].Value),
                                    S = (float)XmlConvert.ToDouble(childNode.Attributes["color2HSVDiffuseS"].Value),
                                    V = (float)XmlConvert.ToDouble(childNode.Attributes["color2HSVDiffuseV"].Value)
                                },
                                alpha = (float)XmlConvert.ToDouble(childNode.Attributes["color2Alpha"].Value),
                                hsvSpecular =
                                {
                                    H = (float)XmlConvert.ToDouble(childNode.Attributes["color2HSVSpecularH"].Value),
                                    S = (float)XmlConvert.ToDouble(childNode.Attributes["color2HSVSpecularS"].Value),
                                    V = (float)XmlConvert.ToDouble(childNode.Attributes["color2HSVSpecularV"].Value)
                                },
                                specularIntensity = (float)XmlConvert.ToDouble(childNode.Attributes["color2SpecularIntensity"].Value),
                                specularSharpness = (float)XmlConvert.ToDouble(childNode.Attributes["color2SpecularSharpness"].Value)
                            }
                        };
                    else
                        accessory = new CharFileInfoClothes.Accessory();
                    accessories2.Add(accessory);
                }
            }
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo == null || this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            this.UpdateMakerGUI();
            this.UpdateStudioUI();
        }

        private void OnCoordSave(CharFileInfoClothes clothesinfo, XmlTextWriter writer)
        {
            CharAdditionalData additionalData = this._charaMakerAdditionalData;
            writer.WriteStartElement("accessorySet");
            foreach (CharFileInfoClothes.Accessory accessory in additionalData.clothesInfoAccessory)
            {
                writer.WriteStartElement("accessory");

                if (accessory.type != -1)
                {
                    writer.WriteAttributeString("type", XmlConvert.ToString(accessory.type));
                    writer.WriteAttributeString("id", XmlConvert.ToString(accessory.id));
                    writer.WriteAttributeString("parentKey", accessory.parentKey);
                    writer.WriteAttributeString("addPosX", XmlConvert.ToString(accessory.addPos.x));
                    writer.WriteAttributeString("addPosY", XmlConvert.ToString(accessory.addPos.y));
                    writer.WriteAttributeString("addPosZ", XmlConvert.ToString(accessory.addPos.z));
                    writer.WriteAttributeString("addRotX", XmlConvert.ToString(accessory.addRot.x));
                    writer.WriteAttributeString("addRotY", XmlConvert.ToString(accessory.addRot.y));
                    writer.WriteAttributeString("addRotZ", XmlConvert.ToString(accessory.addRot.z));
                    writer.WriteAttributeString("addSclX", XmlConvert.ToString(accessory.addScl.x));
                    writer.WriteAttributeString("addSclY", XmlConvert.ToString(accessory.addScl.y));
                    writer.WriteAttributeString("addSclZ", XmlConvert.ToString(accessory.addScl.z));

                    writer.WriteAttributeString("colorHSVDiffuseH", XmlConvert.ToString((double)accessory.color.hsvDiffuse.H));
                    writer.WriteAttributeString("colorHSVDiffuseS", XmlConvert.ToString((double)accessory.color.hsvDiffuse.S));
                    writer.WriteAttributeString("colorHSVDiffuseV", XmlConvert.ToString((double)accessory.color.hsvDiffuse.V));
                    writer.WriteAttributeString("colorAlpha", XmlConvert.ToString((double)accessory.color.alpha));
                    writer.WriteAttributeString("colorHSVSpecularH", XmlConvert.ToString((double)accessory.color.hsvSpecular.H));
                    writer.WriteAttributeString("colorHSVSpecularS", XmlConvert.ToString((double)accessory.color.hsvSpecular.S));
                    writer.WriteAttributeString("colorHSVSpecularV", XmlConvert.ToString((double)accessory.color.hsvSpecular.V));
                    writer.WriteAttributeString("colorSpecularIntensity", XmlConvert.ToString((double)accessory.color.specularIntensity));
                    writer.WriteAttributeString("colorSpecularSharpness", XmlConvert.ToString((double)accessory.color.specularSharpness));

                    writer.WriteAttributeString("color2HSVDiffuseH", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.H));
                    writer.WriteAttributeString("color2HSVDiffuseS", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.S));
                    writer.WriteAttributeString("color2HSVDiffuseV", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.V));
                    writer.WriteAttributeString("color2Alpha", XmlConvert.ToString((double)accessory.color2.alpha));
                    writer.WriteAttributeString("color2HSVSpecularH", XmlConvert.ToString((double)accessory.color2.hsvSpecular.H));
                    writer.WriteAttributeString("color2HSVSpecularS", XmlConvert.ToString((double)accessory.color2.hsvSpecular.S));
                    writer.WriteAttributeString("color2HSVSpecularV", XmlConvert.ToString((double)accessory.color2.hsvSpecular.V));
                    writer.WriteAttributeString("color2SpecularIntensity", XmlConvert.ToString((double)accessory.color2.specularIntensity));
                    writer.WriteAttributeString("color2SpecularSharpness", XmlConvert.ToString((double)accessory.color2.specularSharpness));
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        internal static XmlNode GetExtDataFromFile(string path, string extMarker)
        {
            byte[] bytes = File.ReadAllBytes(path);

            int index = bytes.LastIndexOf(Encoding.ASCII.GetBytes(extMarker));
            if (index == -1)
                return null;
            byte[] newBytes = new byte[bytes.Length - index];
            Array.Copy(bytes, index, newBytes, 0, newBytes.Length);
            using (MemoryStream stream = new MemoryStream(newBytes))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(stream);
                    XmlNode node = doc.FirstChild.FindChildNode("moreAccessories");
                    return node;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("MoreAccessories: Couldn't parse xml\n" + e);
                    return null;
                }
            }
        }
        #endregion
    }
}