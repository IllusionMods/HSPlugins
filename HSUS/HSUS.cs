//#define BETA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using HSUS.Features;
#if IPA
using IllusionPlugin;
using Harmony;
#elif BEPINEX
using BepInEx;
using HarmonyLib;
#endif
#if HONEYSELECT
#elif PLAYHOME
using UnityEngine.SceneManagement;
#elif KOIKATSU
using ChaCustom;
using UnityEngine.SceneManagement;
#elif AISHOUJO || HONEYSELECT2
using UnityEngine.SceneManagement;
#endif
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HSUS
{
#if BEPINEX
    [BepInPlugin(_guid, _name, _version)]
#endif
    internal class HSUS : GenericPlugin
#if IPA
    , IEnhancedPlugin
#endif
    {
        internal const string _version = "1.9.0";
#if HONEYSELECT
        internal const string _name = "HSUS";
        internal const string _guid = "com.joan6694.illusionplugins.hsus";
#elif PLAYHOME
        internal const string _name = "PHUS";
        internal const string _guid = "com.joan6694.illusionplugins.phus";
#elif KOIKATSU
        internal const string _name = "KKUS";
        internal const string _guid = "com.joan6694.illusionplugins.kkus";
#elif AISHOUJO
        internal const string _name = "AIUS";
        internal const string _guid = "com.joan6694.illusionplugins.aius";
#elif HONEYSELECT2
        internal const string _name = "HS2US";
        internal const string _guid = "com.joan6694.illusionplugins.hs2us";
#endif
        private const string _config = "config.xml";

        #region Private Types
        internal delegate bool TranslationDelegate(ref string text);
        #endregion

        #region Private Variables
        private readonly List<IFeature> _features = new List<IFeature>();
        private readonly OptimizeCharaMaker _optimizeCharaMaker = new OptimizeCharaMaker();
        private readonly UIScale _uiScale = new UIScale();
        private readonly DeleteConfirmation _deleteConfirmation = new DeleteConfirmation();
        private readonly DefaultChars _defaultChars = new DefaultChars();
        private readonly ImproveNeoUI _improveNeoUi = new ImproveNeoUI();
        private readonly OptimizeNEO _optimizeNeo = new OptimizeNEO();
        private readonly GenericFK _genericFK = new GenericFK();
        private readonly DebugFeature _debugFeature = new DebugFeature();
        private readonly ImprovedTransformOperations _improvedTransformOperations = new ImprovedTransformOperations();
        private readonly AutoJointCorrection _autoJointCorrection = new AutoJointCorrection();
        private readonly EyesBlink _eyesBlink = new EyesBlink();
        private readonly CameraShortcuts _cameraShortcuts = new CameraShortcuts();
        private readonly AlternativeCenterToObjects _alternativeCenterToObjects = new AlternativeCenterToObjects();
        private readonly FingersFKCopyButtons _fingersFKCopyButtons = new FingersFKCopyButtons();
        private readonly AnimationOptionDisplay _animationOptionDisplay = new AnimationOptionDisplay();
        private readonly FKColors _fkColors = new FKColors();
        private readonly PostProcessing _postProcessing = new PostProcessing();
        private readonly AutomaticMemoryClean _automaticMemoryClean = new AutomaticMemoryClean();
        internal event Action _onUpdate;

        //Kept for old plugins interaction;
        private float _gameUIScale;

        private bool _vSyncEnabled = true;

        internal static HSUS _self;
        private string _assemblyLocation;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
#if HONEYSELECT
        internal TranslationDelegate _translationMethod;
        internal Sprite _searchBarBackground;
        internal Sprite _buttonBackground;
        internal readonly List<IEnumerator> _asyncMethods = new List<IEnumerator>();
#endif
#if IPA
        internal HarmonyInstance _harmonyInstance;
#elif BEPINEX
        internal Harmony _harmonyInstance;
#endif
        #endregion

        #region Public Accessors
#if IPA
        public override string Name { get { return _name; } }
        public override string Version
        {
            get
            {
                return _version
#if BETA
                       + "b"
#endif
                        ;
            }
        }
        public override string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
#endif
#if HONEYSELECT || KOIKATSU
        public bool optimizeCharaMaker { get { return OptimizeCharaMaker._optimizeCharaMaker; } }
#endif
        public static HSUS self { get { return _self; } }
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _self = this;

#if HONEYSELECT
            Type t = Type.GetType("UnityEngine.UI.Translation.TextTranslator,UnityEngine.UI.Translation");
            if (t != null)
            {
                MethodInfo info = t.GetMethod("Translate", BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                    this._translationMethod = (TranslationDelegate)Delegate.CreateDelegate(typeof(TranslationDelegate), info);
            }
#endif
            this._features.Add(this._optimizeCharaMaker);
            this._features.Add(this._uiScale);
            this._features.Add(this._deleteConfirmation);
            this._features.Add(this._defaultChars);
            this._features.Add(this._improveNeoUi);
            this._features.Add(this._optimizeNeo);
            this._features.Add(this._genericFK);
            this._features.Add(this._debugFeature);
            this._features.Add(this._improvedTransformOperations);
            this._features.Add(this._autoJointCorrection);
            this._features.Add(this._eyesBlink);
            this._features.Add(this._cameraShortcuts);
            this._features.Add(this._alternativeCenterToObjects);
            this._features.Add(this._fingersFKCopyButtons);
            this._features.Add(this._animationOptionDisplay);
            this._features.Add(this._fkColors);
            this._features.Add(this._postProcessing);
            this._features.Add(this._automaticMemoryClean);

            this._assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(Path.Combine(this._assemblyLocation, _name), _config);
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (IFeature feature in this._features)
                    feature.LoadParams(doc.DocumentElement);

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "vsync":
                            if (node.Attributes["enabled"] != null)
                                this._vSyncEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                            break;
                    }
                }
            }

            UIUtility.Init();

            this._gameUIScale = this._uiScale._gameUIScale;

            this._harmonyInstance = HarmonyExtensions.CreateInstance(_guid);
            this._harmonyInstance.PatchAllSafe();

            foreach (IFeature feature in this._features)
            {
                try
                {
                    feature.Awake();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(_name + ": Couldn't call Awake for feature " + feature + ":\n" + e);
                }
            }
        }

#if KOIKATSU || AISHOUJO
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode != LoadSceneMode.Single)
                this._uiScale.RefreshCanvases();
        }
#endif
        protected override void OnDestroy()
        {
            string folder = Path.Combine(this._assemblyLocation, _name);
            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, _config);
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(fileStream, Encoding.UTF8))
                {
                    xmlWriter.Formatting = Formatting.Indented;

                    xmlWriter.WriteStartElement("root");
                    xmlWriter.WriteAttributeString("version", _version
#if BETA
                                                              + "b"
#endif
                    );

                    foreach (IFeature feature in this._features)
                    {
                        feature.SaveParams(xmlWriter);
                    }

                    {
                        xmlWriter.WriteStartElement("vsync");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._vSyncEnabled));
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                }
            }
        }

        protected override void LevelLoaded(int level)
        {
            UIUtility.Init();
            foreach (IFeature feature in this._features)
            {
                try
                {
                    feature.LevelLoaded();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(_name + ": Couldn't call LevelLoaded for feature " + feature + ":\n" + e);
                }
            }

            this._uiScale.RefreshCanvases();
        }

        protected override void Update()
        {
            if (this._vSyncEnabled == false)
                QualitySettings.vSyncCount = 0;

            if (this._lastScreenWidth != Screen.width || this._lastScreenHeight != Screen.height)
                this.OnWindowResize();
            this._lastScreenWidth = Screen.width;
            this._lastScreenHeight = Screen.height;

#if HONEYSELECT
            if (this._asyncMethods.Count != 0)
            {
                IEnumerator method = this._asyncMethods[0];
                try
                {
                    if (method.MoveNext() == false)
                        this._asyncMethods.RemoveAt(0);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    this._asyncMethods.RemoveAt(0);
                }
            }
#endif
            if (this._onUpdate != null)
                this._onUpdate();
        }
        #endregion

        #region Private Methods
        private void OnWindowResize()
        {
            this.ExecuteDelayed(this._uiScale.Scale, 2);
        }
        #endregion
    }
}