#if !BEPINEX
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT
using Config;
using Studio;
using ILSetUtility.TimeUtility;
using kleberswf.tools.miniprofiler;
#endif

namespace HSUS.Features
{
    public class DebugFeature : IFeature
    {
#if HONEYSELECT
        internal static bool _miniProfilerEnabled = true;
        internal static bool _miniProfilerStartCollapsed = true;
#endif

        public void Awake()
        {
        }

        //        public void LoadParams(XmlNode node)
        //        {
        //            node = node.FindChildNode("debug");
        //            if (node == null)
        //                return;
        //            if (node.Attributes["enabled"] != null)
        //                _debugEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
        //            if (node.Attributes["value"] != null)
        //            {
        //                string value = node.Attributes["value"].Value;
        //                if (Enum.IsDefined(typeof(KeyCode), value))
        //                    _debugShortcut = (KeyCode)Enum.Parse(typeof(KeyCode), value);
        //            }
        //#if HONEYSELECT
        //            if (node.Attributes["miniProfilerEnabled"] != null)
        //                _miniProfilerEnabled = XmlConvert.ToBoolean(node.Attributes["miniProfilerEnabled"].Value);
        //            if (node.Attributes["miniProfilerStartCollapsed"] != null)
        //                _miniProfilerStartCollapsed = XmlConvert.ToBoolean(node.Attributes["miniProfilerStartCollapsed"].Value);
        //#endif
        //        }

        //        public void SaveParams(XmlTextWriter writer)
        //        {
        //            writer.WriteStartElement("debug");
        //            writer.WriteAttributeString("enabled", XmlConvert.ToString(_debugEnabled));
        //            writer.WriteAttributeString("value", _debugShortcut.ToString());
        //#if HONEYSELECT
        //            writer.WriteAttributeString("miniProfilerEnabled", XmlConvert.ToString(_miniProfilerEnabled));
        //            writer.WriteAttributeString("miniProfilerStartCollapsed", XmlConvert.ToString(_miniProfilerStartCollapsed));
        //#endif
        //            writer.WriteEndElement();
        //        }

        public void LevelLoaded()
        {
            if (HSUS.Debug.Value && HSUS._self.gameObject.GetComponent<DebugConsole>() == null)
                HSUS._self.gameObject.AddComponent<DebugConsole>();
        }

#if HONEYSELECT
        [HarmonyPatch(typeof(TimeUtility), "Update")]
        internal static class TimeUtility_Update_Patches
        {
            private static bool Prepare()
            {
                return _debugEnabled && _miniProfilerEnabled;
            }

            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            private static bool Prefix()
            {
                if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.Delete))
                {
                    DebugSystem debugStatus = Manager.Config.DebugStatus;
                    debugStatus.FPS = !debugStatus.FPS;
                    Singleton<Manager.Config>.Instance.Save();
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(TimeUtility), "OnGUI")]
        internal static class TimeUtility_OnGUI_Patches
        {
            private static bool Prepare()
            {
                return _debugEnabled && _miniProfilerEnabled;
            }

            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            private static bool Prefix()
            {
                return false;
            }
        }
#endif
    }

    public class DebugConsole : MonoBehaviour
    {
#region Types
        private struct ObjectPair
        {
            public readonly object parent;
            public readonly object child;
            private readonly int _hashCode;

            public ObjectPair(object inParent, object inChild)
            {
                parent = inParent;
                child = inChild;
                _hashCode = -157375006;
                _hashCode = _hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(parent);
                _hashCode = _hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(child);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        private class FunctionTextWriter : TextWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }

            private readonly TextWriter _old;
            private readonly StringBuilder _buffer = new StringBuilder();

            public FunctionTextWriter(TextWriter old)
            {
                _old = old;
            }

            public override void Write(char value)
            {
                if (value == '\n')
                {
                    _lastLogs.AddLast(new KeyValuePair<LogType, string>(LogType.Log, DateTime.Now.ToString("[HH:mm:ss] ") + _buffer.ToString()));
                    if (_lastLogs.Count == 1001)
                        _lastLogs.RemoveFirst();
                    _scroll3.y += 999999;
                    _buffer.Length = 0;
                    _buffer.Capacity = 0;
                }
                else
                    _buffer.Append(value);
                _old.Write(value);
            }
        }
#endregion

#region Private Variables
        private Transform _target;
        private readonly HashSet<GameObject> _openedGameObjects = new HashSet<GameObject>();
        private Vector2 _scroll;
        private Vector2 _scroll2;
        private static Vector2 _scroll3;
        private static readonly LinkedList<KeyValuePair<LogType, string>> _lastLogs = new LinkedList<KeyValuePair<LogType, string>>();
        private static bool _debug;
        private Rect _rect = new Rect(480f, 270f, 960, 540f);
        private const int _uniqueId = ('H' << 24) | ('S' << 16) | ('U' << 8) | 'S';
        private static readonly Process _process;
        private static readonly byte _bits;
        private readonly HashSet<ObjectPair> _openedObjects = new HashSet<ObjectPair>();
        private static string _goSearch = "";
        private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };
        private bool _showHidden = false;
        private static string _logsFilter = "";
#endregion

#if HONEYSELECT
        private static readonly string _has630Patch;
        private GameObject _miniProfilerUI;
        //private Mesh _cubeMesh;
#endif

        static DebugConsole()
        {
            Application.logMessageReceived += HandleLog;
            _process = Process.GetCurrentProcess();
            if (IntPtr.Size == 4)
                _bits = 32;
            else if (IntPtr.Size == 8)
                _bits = 64;
#if HONEYSELECT
            bool vectrosity = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "HoneySelect_" + _bits + "_Data\\Managed\\Vectrosity.dll"));
            bool type = Type.GetType("Studio.OCIPathMove,Assembly-CSharp") != null;
            if (vectrosity && type)
                _has630Patch = "Yes";
            else if (vectrosity)
                _has630Patch = "Partial (missing classes)";
            else if (type)
                _has630Patch = "Partial (missing Vectrosity)";
            else
                _has630Patch = "No";
            UnityEngine.Debug.Log("HSUS " + HSUS._version + ": " + _process.ProcessName + " | " + _bits + "bits" + " | 630 patch: " + _has630Patch);
#endif

#if HONEYSELECT || PLAYHOME
            Console.SetOut(new FunctionTextWriter(Console.Out));
#endif
        }

#region Unity Methods
        private void Awake()
        {
        }

        private void Start()
        {
#if HONEYSELECT
            if (DebugFeature._miniProfilerEnabled)
            {
                AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.HSUSResources);
                this._miniProfilerUI = Instantiate(bundle.LoadAsset<GameObject>("MiniProfilerCanvas"));
                bundle.Unload(true);
                this._miniProfilerUI.gameObject.SetActive(Manager.Config.DebugStatus.FPS);
                this._miniProfilerUI.transform.SetAsLastSibling();
                foreach (MiniProfiler profiler in this._miniProfilerUI.GetComponentsInChildren<MiniProfiler>())
                    profiler.Collapsed = DebugFeature._miniProfilerStartCollapsed;
                this._miniProfilerUI.GetComponent<Canvas>().sortingOrder = 1000;
                DontDestroyOnLoad(this._miniProfilerUI);
            }
#endif
        }

        private void Update()
        {
            if (HSUS.DebugHotkey.Value.IsDown())
                _debug = !_debug;
#if HONEYSELECT
            if (DebugFeature._miniProfilerEnabled)
            {
                if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.Delete))
                    this._miniProfilerUI.gameObject.SetActive(Manager.Config.DebugStatus.FPS);
            }
#endif
            //if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C) && Studio.Studio.Instance != null)
            //{
            //    ObjectCtrlInfo objectCtrlInfo;
            //    if (Studio.Studio.Instance.dicInfo.TryGetValue(Studio.Studio.Instance.treeNodeCtrl.selectNode, out objectCtrlInfo))
            //    foreach (Renderer target in objectCtrlInfo.guideObject.transformTarget.GetComponentsInChildren<Renderer>())
            //    {
            //        {
            //            SkinnedMeshRenderer smr = target as SkinnedMeshRenderer;
            //            if (smr != null)
            //            {
            //                smr.sharedMesh = this._cubeMesh;
            //            }
            //            else if (target is MeshRenderer)
            //            {
            //                target.GetComponent<MeshFilter>().sharedMesh = this._cubeMesh;
            //            }
            //        }
            //    }
            //}
        }

        private void OnGUI()
        {
            if (_debug == false)
                return;
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.6f);
            GUI.Box(_rect, "", _customBoxStyle);
            GUI.backgroundColor = c;
            _rect = GUILayout.Window(_uniqueId, _rect, WindowFunc, "Debug Console " + HSUS._version + ": " + _process.ProcessName + " | " + _bits + "bits"
#if HONEYSELECT
                                                                                       + " | 630 patch: " + _has630Patch
#endif
            );
        }
#endregion

#region Private Methods
        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            string s = DateTime.Now.ToString("[HH:mm:ss] ") + condition + " " + stackTrace;
            if (s.Length > 10000)
            {
                s = s.Substring(0, 10000);
                s += "...";
            }
            _lastLogs.AddLast(new KeyValuePair<LogType, string>(type, s));
            if (_lastLogs.Count == 1001)
                _lastLogs.RemoveFirst();
            _scroll3.y += 999999;
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            float alpha = 1;
            if ((go.hideFlags & HideFlags.HideInHierarchy) > 0)
            {
                if (_showHidden == false)
                    return;
                alpha = 0.5f;
            }
            if (_goSearch.Length == 0 || go.name.IndexOf(_goSearch, StringComparison.OrdinalIgnoreCase) != -1)
            {
                Color c = GUI.color;
                if (_target == go.transform)
                    GUI.color = Color.cyan;
                GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);
                GUILayout.BeginHorizontal();

                if (_goSearch.Length == 0)
                {
                    GUILayout.Space(indent * 20f);
                    if (go.transform.childCount != 0)
                    {
                        if (GUILayout.Toggle(_openedGameObjects.Contains(go), "", GUILayout.ExpandWidth(false)))
                        {
                            if (_openedGameObjects.Contains(go) == false)
                                _openedGameObjects.Add(go);
                        }
                        else
                        {
                            if (_openedGameObjects.Contains(go))
                                _openedGameObjects.Remove(go);
                        }
                    }
                    else
                        GUILayout.Space(20f);
                }
                if (GUILayout.Button(go.name, GUILayout.ExpandWidth(false)))
                {
                    _target = go.transform;
                    if (_goSearch.Length != 0)
                    {
                        Transform t = _target.parent;
                        while (t != null)
                        {
                            if (_openedGameObjects.Contains(t.gameObject) == false)
                                _openedGameObjects.Add(t.gameObject);
                            t = t.parent;
                        }
                    }
                }
                GUI.color = c;
                go.SetActive(GUILayout.Toggle(go.activeSelf, "", GUILayout.ExpandWidth(false)));
                GUILayout.EndHorizontal();
            }
            if (_goSearch.Length != 0 || _openedGameObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private void WindowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(300));
            _goSearch = GUILayout.TextField(_goSearch);
            _scroll = GUILayout.BeginScrollView(_scroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.parent == null)
                    DisplayObjectTree(t.gameObject, 0);
            GUILayout.EndScrollView();
            _showHidden = GUILayout.Toggle(_showHidden, "Show Hidden");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            _scroll2 = GUILayout.BeginScrollView(_scroll2, GUI.skin.box);
            if (_target != null)
            {
                Transform t = _target.parent;
                string n = _target.name;
                while (t != null)
                {
                    n = t.name + "/" + n;
                    t = t.parent;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(n);
                if (GUILayout.Button("Copy to clipboard", GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = n;
                GUILayout.EndHorizontal();
                GUILayout.Label("Layer: " + LayerMask.LayerToName(_target.gameObject.layer) + " " + _target.gameObject.layer);
                GUILayout.Label("Tag: " + _target.gameObject.tag);
                string hf = "";
                if ((_target.gameObject.hideFlags & HideFlags.HideInHierarchy) != 0)
                    hf += (hf.Length != 0 ? " | " : "") + "HideInHierarchy";
                if ((_target.gameObject.hideFlags & HideFlags.HideInInspector) != 0)
                    hf += (hf.Length != 0 ? " | " : "") + "HideInInspector";
                if ((_target.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0)
                    hf += (hf.Length != 0 ? " | " : "") + "DontSaveInEditor";
                if ((_target.gameObject.hideFlags & HideFlags.NotEditable) != 0)
                    hf += (hf.Length != 0 ? " | " : "") + "NotEditable";
                if ((_target.gameObject.hideFlags & HideFlags.DontSaveInBuild) != 0)
                    hf += (hf.Length != 0 ? " | " : "") + "DontSaveInBuild";
                if ((_target.gameObject.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0)
                    hf += (hf.Length != 0 ? " | " : "") + "DontUnloadUnusedAsset";
                GUILayout.Label("HideFlags: " + hf);
                GUILayout.Label("HashCode: " + _target.gameObject.GetHashCode());
                GUILayout.Label("InstanceID: " + _target.gameObject.GetInstanceID());
                foreach (Component c in _target.GetComponents<Component>())
                {
                    if (c == null)
                        continue;
                    GUILayout.BeginHorizontal();
                    MonoBehaviour m = c as MonoBehaviour;
                    if (m != null)
                        m.enabled = GUILayout.Toggle(m.enabled, c.GetType().FullName, GUILayout.ExpandWidth(false));
                    else if (c is Animator)
                    {
                        Animator an = (Animator)c;
                        an.enabled = GUILayout.Toggle(an.enabled, c.GetType().FullName, GUILayout.ExpandWidth(false));
                    }
                    else
                        GUILayout.Label(c.GetType().FullName, GUILayout.ExpandWidth(false));

                    ObjectPair pair = new ObjectPair(_target, c);
                    if (GUILayout.Toggle(_openedObjects.Contains(pair), ""))
                    {
                        if (_openedObjects.Contains(pair) == false)
                            _openedObjects.Add(pair);
                    }
                    else
                    {
                        if (_openedObjects.Contains(pair))
                            _openedObjects.Remove(pair);
                    }

                    if (c is Image)
                    {
                        Image img = c as Image;
                        if (img.sprite != null && img.sprite.texture != null)
                        {
                            GUILayout.Label(img.sprite.name);
                            GUILayout.Label(img.color.ToString());
                            try
                            {
                                Color[] newImg = img.sprite.texture.GetPixels((int)img.sprite.textureRect.x, (int)img.sprite.textureRect.y, (int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
                                Texture2D tex = new Texture2D((int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
                                tex.SetPixels(newImg);
                                tex.Apply();
                                GUILayout.Label(tex);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    else if (c is Slider)
                    {
                        Slider b = c as Slider;
                        for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                        IList calls = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                        for (int i = 0; i < calls.Count; ++i)
                        {
                            UnityAction<float> unityAction = ((UnityAction<float>)calls[i].GetPrivate("Delegate"));
                            GUILayout.Label(unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                        }
                    }
                    else if (c is Text)
                    {
                        Text text = c as Text;
                        GUILayout.Label(text.text + " " + text.font + " " + text.fontStyle + " " + text.fontSize + " " + text.alignment + " " + text.alignByGeometry + " " + text.resizeTextForBestFit + " " + text.color);
                    }
                    else if (c is RawImage)
                    {
                        GUILayout.Label(((RawImage)c).mainTexture.name);
                        GUILayout.Label(((RawImage)c).color.ToString());
                        GUILayout.Label(((RawImage)c).mainTexture);
                    }
                    else if (c is Renderer)
                        GUILayout.Label(((Renderer)c).material != null ? ((Renderer)c).material.shader.name : "");
                    else if (c is Button)
                    {
                        Button b = c as Button;
                        for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onClick.GetPersistentTarget(i).GetType().FullName + "." + b.onClick.GetPersistentMethodName(i));
                        IList calls = b.onClick.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                        for (int i = 0; i < calls.Count; ++i)
                        {
                            UnityAction unityAction = ((UnityAction)calls[i].GetPrivate("Delegate"));
                            GUILayout.Label(unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                        }
                    }
                    else if (c is Toggle)
                    {
                        Toggle b = c as Toggle;
                        for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                        IList calls = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                        for (int i = 0; i < calls.Count; ++i)
                        {
                            UnityAction<bool> unityAction = ((UnityAction<bool>)calls[i].GetPrivate("Delegate"));
                            GUILayout.Label(unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                        }
                    }
                    else if (c is InputField)
                    {
                        InputField b = c as InputField;
                        if (b.onValueChanged != null)
                        {
                            for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                                GUILayout.Label("OnValueChanged " + b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                            IList calls = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                            for (int i = 0; i < calls.Count; ++i)
                            {
                                UnityAction<string> unityAction = ((UnityAction<string>)calls[i].GetPrivate("Delegate"));
                                GUILayout.Label("OnValueChanged " + unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                            }
                        }
                        if (b.onEndEdit != null)
                        {
                            for (int i = 0; i < b.onEndEdit.GetPersistentEventCount(); ++i)
                                GUILayout.Label("OnEndEdit " + b.onEndEdit.GetPersistentTarget(i).GetType().FullName + "." + b.onEndEdit.GetPersistentMethodName(i));
                            IList calls = b.onEndEdit.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                            for (int i = 0; i < calls.Count; ++i)
                            {
                                UnityAction<string> unityAction = ((UnityAction<string>)calls[i].GetPrivate("Delegate"));
                                GUILayout.Label("OnEndEdit " + unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                            }
                        }
                        if (b.onValidateInput != null)
                            GUILayout.Label("OnValidateInput " + b.onValidateInput.Target.GetType().FullName + "." + b.onValidateInput.Method.Name);
                    }
                    else if (c is RectTransform)
                    {
                        RectTransform rt = c as RectTransform;
                        GUILayout.Label("anchorMin " + rt.anchorMin);
                        GUILayout.Label("anchorMax " + rt.anchorMax);
                        GUILayout.Label("offsetMin " + rt.offsetMin);
                        GUILayout.Label("offsetMax " + rt.offsetMax);
                        GUILayout.Label("rect " + rt.rect);
                        GUILayout.Label("localRotation " + rt.localEulerAngles);
                        GUILayout.Label("localScale " + rt.localScale);
                    }
                    else if (c is Transform)
                    {
                        Transform tr = c as Transform;
                        GUILayout.Label("localPosition " + tr.localPosition);
                        GUILayout.Label("localRotation " + tr.localEulerAngles);
                        GUILayout.Label("localScale " + tr.localScale);
                    }
#if HONEYSELECT || KOIKATSU
                    else if (c is UI_OnEnableEvent)
                    {
                        UI_OnEnableEvent e = c as UI_OnEnableEvent;
                        if (e._event != null)
                        {
                            for (int i = 0; i < e._event.GetPersistentEventCount(); ++i)
                                GUILayout.Label("_event " + e._event.GetPersistentTarget(i).GetType().FullName + "." + e._event.GetPersistentMethodName(i));
                            IList calls = e._event.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                            for (int i = 0; i < calls.Count; ++i)
                            {
                                UnityAction<string> unityAction = ((UnityAction<string>)calls[i].GetPrivate("Delegate"));
                                GUILayout.Label("_event " + unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                            }

                        }
                    }
#endif
                    GUILayout.EndHorizontal();
                    RecurseObjects(pair, 1);
                }
            }
            GUILayout.EndScrollView();
            _scroll3 = GUILayout.BeginScrollView(_scroll3, GUI.skin.box, GUILayout.Height(Screen.height / 5f));
            foreach (KeyValuePair<LogType, string> lastLog in _lastLogs)
            {
                if (lastLog.Value.IndexOf(_logsFilter, StringComparison.OrdinalIgnoreCase) == -1)
                    continue;
                Color c = GUI.color;
                switch (lastLog.Key)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        GUI.color = Color.red;
                        break;
                    case LogType.Warning:
                        GUI.color = Color.yellow;
                        break;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(lastLog.Value);
                GUI.color = c;
                if (GUILayout.Button("Copy to clipboard", GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = lastLog.Value;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear AssetBundle Cache", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<string, AssetBundleManager.BundlePack> pair in AssetBundleManager.ManifestBundlePack)
                {
                    foreach (KeyValuePair<string, LoadedAssetBundle> bundle in new Dictionary<string, LoadedAssetBundle>(pair.Value.LoadedAssetBundles))
                    {
                        AssetBundleManager.UnloadAssetBundle(bundle.Key, true, pair.Key);
                    }
                }
            }
            GUILayout.Label("Filter Logs ", GUILayout.ExpandWidth(false));
            _logsFilter = GUILayout.TextField(_logsFilter, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                _logsFilter = "";
            if (GUILayout.Button("Clear logs", GUILayout.ExpandWidth(false)))
                _lastLogs.Clear();
            if (GUILayout.Button("Open log file", GUILayout.ExpandWidth(false)))
#if AISHOUJO
                Process.Start(Path.Combine(Application.persistentDataPath, "output_log.txt"));
#else
                Process.Start(Path.Combine(Application.dataPath, "output_log.txt"));
#endif
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void RecurseObjects(ObjectPair obj, int indent)
        {
            if (!_openedObjects.Contains(obj))
                return;
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = indent % 2 != 0 ? new Color(0f, 0f, 0f, 0.7f) : new Color(0.35f, 0.35f, 0.35f, 0.7f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical(_customBoxStyle);
            GUI.backgroundColor = c;
            Type t = obj.child.GetType();
            if (obj.child is IEnumerable array && array is Transform == false)
            {
                int i = 0;
                if (array != null)
                {
                    foreach (object o in array)
                    {
                        if (o != null && obj.child == o)
                            continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label(i + ": " + (o == null ? "null" : o), GUILayout.ExpandWidth(false));

                        ObjectPair pair = new ObjectPair(array, o);
                        if (o != null)
                        {
                            Type oType = o.GetType();
                            if (oType.IsPrimitive == false && (oType.BaseType == null || oType.BaseType.IsPrimitive == false))
                            {
                                if (GUILayout.Toggle(_openedObjects.Contains(pair), ""))
                                {
                                    if (_openedObjects.Contains(pair) == false)
                                        _openedObjects.Add(pair);
                                }
                                else
                                {
                                    if (_openedObjects.Contains(pair))
                                        _openedObjects.Remove(pair);
                                }
                            }
                        }
                        SpecialObjectBehaviour(o);
                        GUILayout.EndHorizontal();
                        RecurseObjects(pair, indent + 1);
                        ++i;
                    }
                    if (i == 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label("empty", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label("null", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                IOrderedEnumerable<FieldInfo> fields = t.GetFields(AccessTools.all).OrderBy(f => f.Name);
                foreach (FieldInfo field in fields)
                {
                    object o = null;
                    bool exception = false;
                    try
                    {
                        o = field.GetValue(obj.child);
                    }
                    catch (Exception)
                    {
                        exception = true;
                    }
                    if (o != null && obj.child == o)
                        continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    if (o != null)
                        GUILayout.Label(field.Name + ": " + o, GUILayout.ExpandWidth(false));
                    else
                        GUILayout.Label(field.Name + ": " + (exception ? "Exception caught while getting value" : "null"), GUILayout.ExpandWidth(false));

                    ObjectPair pair = new ObjectPair(obj.child, o);
                    if (o != null)
                    {
                        Type oType = o.GetType();
                        if (oType.IsPrimitive == false && (oType.BaseType == null || oType.BaseType.IsPrimitive == false))
                        {
                            if (GUILayout.Toggle(_openedObjects.Contains(pair), ""))
                            {
                                if (_openedObjects.Contains(pair) == false)
                                    _openedObjects.Add(pair);
                            }
                            else
                            {
                                if (_openedObjects.Contains(pair))
                                    _openedObjects.Remove(pair);
                            }
                        }
                    }
                    SpecialObjectBehaviour(o);
                    GUILayout.EndHorizontal();
                    RecurseObjects(pair, indent + 1);
                }
                IOrderedEnumerable<PropertyInfo> properties = t.GetProperties(AccessTools.all).OrderBy(p => p.Name);
                foreach (PropertyInfo property in properties)
                {
                    object o = null;
                    Exception exceptionCaught = null;
                    try
                    {
                        o = property.GetValue(obj.child, null);
                    }
                    catch (Exception e)
                    {
                        exceptionCaught = e;
                    }
                    if (o != null && obj.child == o)
                        continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    if (o != null)
                        GUILayout.Label(property.Name + ": " + o, GUILayout.ExpandWidth(false));
                    else
                    {
                        GUILayout.Label(property.Name + ": " + (exceptionCaught != null ? "Exception caught while getting value" : "null"), GUILayout.ExpandWidth(false));
                        GUILayout.FlexibleSpace();
                        if (exceptionCaught != null && GUILayout.Button("Print", GUILayout.ExpandWidth(false)))
                            UnityEngine.Debug.LogError(exceptionCaught.ToString());
                    }

                    ObjectPair pair = new ObjectPair(obj.child, o);
                    if (o != null)
                    {
                        Type oType = o.GetType();
                        if (oType.IsPrimitive == false && (oType.BaseType == null || oType.BaseType.IsPrimitive == false))
                        {
                            if (GUILayout.Toggle(_openedObjects.Contains(pair), ""))
                            {
                                if (_openedObjects.Contains(pair) == false)
                                    _openedObjects.Add(pair);
                            }
                            else
                            {
                                if (_openedObjects.Contains(pair))
                                    _openedObjects.Remove(pair);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    RecurseObjects(pair, indent + 1);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("HashCode: " + obj.child.GetHashCode());
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void SpecialObjectBehaviour(object o)
        {
            if (o is GameObject)
            {
                if (GUILayout.Button("Go to", GUILayout.ExpandWidth(false)))
                    _target = ((GameObject)o).transform;
            }
            else if (o is Transform)
            {
                if (GUILayout.Button("Go to", GUILayout.ExpandWidth(false)))
                    _target = (Transform)o;
            }
        }
    }
#endregion
}

#endif