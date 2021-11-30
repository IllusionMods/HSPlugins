
using System;
using System.Collections.Generic;
#if HONEYSELECT
using IllusionPlugin;
using Harmony;
#elif KOIKATSU
using BepInEx;
using UnityEngine.SceneManagement;
using HarmonyLib;
#elif AISHOUJO
using BepInEx;
using HarmonyLib;
#endif
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace Instrumentation
{
#if KOIKATSU || AISHOUJO
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.instrumentation", Name: "Instrumentation", Version: Instrumentation.versionNum)]
#endif
    public class Instrumentation : GenericPlugin
#if HONEYSELECT
            , IEnhancedPlugin
#endif
    {
        public class SpecialMethodData
        {
            public Type type;
            public MethodData method;
        }

        public class MethodData
        {
            public string name;
            public Type[] arguments = new Type[0];
        }

        private readonly MethodData[] _methods = {
            new MethodData(){name = "Awake"},
            new MethodData(){name = "OnEnable"},
            new MethodData(){name = "OnLevelWasLoaded", arguments = new []{typeof(int)}},
            new MethodData(){name = "Start"},
            new MethodData(){name = "OnApplicationPause", arguments = new []{typeof(bool)}},
            new MethodData(){name = "FixedUpdate"},
            new MethodData(){name = "Update"},
            new MethodData(){name = "LateUpdate"},
            new MethodData(){name = "OnPreCull"},
            new MethodData(){name = "OnBecameVisible"},
            new MethodData(){name = "OnBecameInvisible"},
            new MethodData(){name = "OnWillRenderObject"},
            new MethodData(){name = "OnPreRender"},
            new MethodData(){name = "OnRenderObject"},
            new MethodData(){name = "OnPostRender"},
            new MethodData(){name = "OnRenderImage", arguments = new []{typeof(RenderTexture), typeof(RenderTexture)}},
            new MethodData(){name = "OnRenderImage", arguments = new []{typeof(Camera), typeof(RenderTexture), typeof(RenderTexture)}},
            new MethodData(){name = "Render", arguments = new []{typeof(RenderTexture), typeof(RenderTexture)}},
            new MethodData(){name = "OnGUI"},
            new MethodData(){name = "OnDestroy"},
            new MethodData(){name = "OnApplicationQuit"},
            new MethodData(){name = "OnDisable"},

            new MethodData(){name = "OnAnimatorIK", arguments = new []{typeof(int)}},
            new MethodData(){name = "OnAnimatorMove"},
            new MethodData(){name = "OnApplicationFocus", arguments = new []{typeof(bool)}},
            new MethodData(){name = "OnAudioFilterRead", arguments = new []{typeof(float[]), typeof(int)}},
            new MethodData(){name = "OnCollisionEnter", arguments = new []{typeof(Collision)}},
            new MethodData(){name = "OnCollisionEnter2D", arguments = new []{typeof(Collision2D)}},
            new MethodData(){name = "OnCollisionExit", arguments = new []{typeof(Collision)}},
            new MethodData(){name = "OnCollisionExit2D", arguments = new []{typeof(Collision2D)}},
            new MethodData(){name = "OnCollisionStay", arguments = new []{typeof(Collision)}},
            new MethodData(){name = "OnCollisionStay2D", arguments = new []{typeof(Collision2D)}},
            new MethodData(){name = "OnControllerColliderHit", arguments = new []{typeof(ControllerColliderHit)}},
            new MethodData(){name = "OnJointBreak", arguments = new []{typeof(float)}},
            new MethodData(){name = "OnJointBreak2D", arguments = new []{typeof(Joint2D)}},
            new MethodData(){name = "OnMouseDown"},
            new MethodData(){name = "OnMouseDrag"},
            new MethodData(){name = "OnMouseEnter"},
            new MethodData(){name = "OnMouseExit"},
            new MethodData(){name = "OnMouseOver"},
            new MethodData(){name = "OnMouseUp"},
            new MethodData(){name = "OnMouseUpAsButton"},
            new MethodData(){name = "OnParticleCollision", arguments = new []{typeof(GameObject)}},
            new MethodData(){name = "OnTransformChildrenChanged"},
            new MethodData(){name = "OnTransformParentChanged"},
            new MethodData(){name = "OnTriggerEnter", arguments = new []{typeof(Collider)}},
            new MethodData(){name = "OnTriggerEnter2D", arguments = new []{typeof(Collider2D)}},
            new MethodData(){name = "OnTriggerExit", arguments = new []{typeof(Collider)}},
            new MethodData(){name = "OnTriggerExit2D", arguments = new []{typeof(Collider2D)}},
            new MethodData(){name = "OnTriggerStay", arguments = new []{typeof(Collider)}},
            new MethodData(){name = "OnTriggerStay2D", arguments = new []{typeof(Collider2D)}},
            new MethodData(){name = "Reset"}
        };

        private readonly SpecialMethodData[] _specialMethods =
        {
            new SpecialMethodData() {type = typeof(GC), method = new MethodData() {name = "Collect"}}
        };

        public static Dictionary<int, Dictionary<Type, ulong>>[] times;

        public static Dictionary<int, ulong>[] specialTimes;

        public static readonly Dictionary<Type, Stopwatch> _stopwatches = new Dictionary<Type, Stopwatch>();

        public const string versionNum = "1.0.0";

        public static bool patched = false;

#if HONEYSELECT
        public override string Name { get { return "Instrumentation"; } }
        public override string Version { get { return versionNum; } }
        public override string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64", "HoneySelect_64", "HoneySelect_32" }; } }
#endif
        protected override void LevelLoaded(int level)
        {
            if (patched == false && level > 2)
            {
                Thread patchThread = new Thread(this.PatchAsync);
                patchThread.Start();
            }
        }

        private void PatchAsync()
        {
            patched = true;
            times = new Dictionary<int, Dictionary<Type, ulong>>[this._methods.Length];
            for (int i = 0; i < this._methods.Length; i++)
                times[i] = new Dictionary<int, Dictionary<Type, ulong>>();

            specialTimes = new Dictionary<int, ulong>[this._specialMethods.Length];
            for (int i = 0; i < this._specialMethods.Length; i++)
                specialTimes[i] = new Dictionary<int, ulong>();

            
            var harmony = HarmonyExtensions.CreateInstance("com.joan6694.hsplugins.instrumentation");
            Type component = typeof(Component);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (component.IsAssignableFrom(type) == false && type.Name.Equals("SMAA") == false)
                            continue;
                        //UnityEngine.Debug.LogError(type.Name);
                        if (type.Name.Equals("Singleton`1") ||
                            type.Name.StartsWith("PresenterBase") || type.Name.Equals("MonoBehaviourSingleton`1"))
                            continue;
                        for (int i = 0; i < this._methods.Length; i++)
                        {
                            MethodData methodData = this._methods[i];
                            try
                            {

                                MethodInfo info = type.GetMethod(methodData.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, methodData.arguments, null);

                                if (info != null)
                                {
                                    harmony.Patch(
                                            info,
                                            new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.UpdatePrefixes), BindingFlags.Public | BindingFlags.Static)),
                                            new HarmonyMethod(typeof(Patches).GetMethod($"UpdatePostfixes{i}", BindingFlags.Public | BindingFlags.Static))
                                    );
                                }
                            }
                            catch (Exception)
                            {
                                //UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
                            }
                        }
                    }
                    for (int i = 0; i < this._specialMethods.Length; i++)
                    {
                        SpecialMethodData data = this._specialMethods[i];
                        try
                        {
                            MethodInfo info = data.type.GetMethod(data.method.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, data.method.arguments, null);
                            if (info != null)
                            {
                                harmony.Patch(
                                        info,
                                        new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.SpecialUpdatePrefixes), BindingFlags.Public | BindingFlags.Static)),
                                        new HarmonyMethod(typeof(Patches).GetMethod($"SpecialUpdatePostfixes{i}", BindingFlags.Public | BindingFlags.Static))
                                );
                            }
                        }
                        catch (Exception)
                        {
                            //UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
                        }
                    }
                }
                catch (Exception)
                {
                    //UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
                }
            }
        }

        protected override void Update()
        {
            if (patched == false)
                return;
            if (Input.GetKeyDown(KeyCode.W))
            {
                UnityEngine.Debug.Log((Time.frameCount - 1) + " " + (Time.frameCount - 60));
                Dictionary<Type, ulong>[] avgDic = new Dictionary<Type, ulong>[this._methods.Length];
                for (int j = 0; j < this._methods.Length; j++)
                {
                    Dictionary<Type, ulong> avg = new Dictionary<Type, ulong>();
                    for (int i = 1; i < 60; i++)
                    {
                        Dictionary<Type, ulong> dic;
                        if (times[j].TryGetValue(Time.frameCount - i, out dic))
                        {
                            foreach (KeyValuePair<Type, ulong> pair in dic)
                            {
                                if (avg.ContainsKey(pair.Key) == false)
                                    avg.Add(pair.Key, 0);
                                avg[pair.Key] += pair.Value;
                            }
                        }
                    }
                    avgDic[j] = avg;
                }
                Dictionary<Type, ulong>[] specialAvgDic = new Dictionary<Type, ulong>[this._specialMethods.Length];
                for (int j = 0; j < this._specialMethods.Length; j++)
                {
                    SpecialMethodData data = this._specialMethods[j];
                    Dictionary<Type, ulong> avg = new Dictionary<Type, ulong>();
                    for (int i = 1; i < 60; i++)
                    {
                        ulong t;
                        if (specialTimes[j].TryGetValue(Time.frameCount - i, out t))
                        {
                            if (avg.ContainsKey(data.type) == false)
                                avg.Add(data.type, 0);
                            avg[data.type] += t;
                        }
                    }
                    specialAvgDic[j] = avg;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < this._specialMethods.Length; i++)
                {
                    Dictionary<Type, ulong> avg = specialAvgDic[i];
                    List<KeyValuePair<Type, ulong>> list = avg.ToList();
                    list.Sort((a, b) => b.Value.CompareTo(a.Value));
                    foreach (KeyValuePair<Type, ulong> pair in list)
                    {
                        sb.AppendLine(pair.Key.FullName + "." + this._specialMethods[i].method.name + "," + (pair.Value / 59));
                    }
                }
                for (int i = 0; i < this._methods.Length; i++)
                {
                    Dictionary<Type, ulong> avg = avgDic[i];
                    List<KeyValuePair<Type, ulong>> list = avg.ToList();
                    list.Sort((a, b) => b.Value.CompareTo(a.Value));
                    foreach (KeyValuePair<Type, ulong> pair in list)
                    {
                        sb.AppendLine(pair.Key.FullName + "." + this._methods[i].name + "," + (pair.Value / 59));
                    }
                }
                UnityEngine.Debug.Log(sb.ToString());
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < 2400; i++)
                {
                    sb.AppendLine((Time.frameCount - i).ToString());
                    for (int j = 0; j < this._specialMethods.Length; j++)
                    {
                        SpecialMethodData data = this._specialMethods[j];
                        ulong t;
                        if (specialTimes[j].TryGetValue(Time.frameCount - i, out t))
                            sb.AppendLine(data.type.FullName + "." + data.method.name + "," + t);
                    }
                    for (int j = 0; j < this._methods.Length; j++)
                    {
                        Dictionary<Type, ulong> dic;
                        if (times[j].TryGetValue(Time.frameCount - i, out dic))
                        {
                            foreach (KeyValuePair<Type, ulong> pair in dic)
                                sb.AppendLine(pair.Key.FullName + "." + this._methods[j].name + "," + pair.Value);
                        }
                    }
                }
                System.IO.File.WriteAllText("FrameSample.txt", sb.ToString());
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                List<IGrouping<Type, UnityEngine.Object>> groups = objs.GroupBy(o => o.GetType()).ToList();
                groups.Sort((x, y) => x.Count().CompareTo(y.Count()));
                StringBuilder sb = new StringBuilder();
                foreach (IGrouping<Type, UnityEngine.Object> group in groups)
                {
                    sb.AppendLine(group.Key.FullName + " " + group.Count());
                }
                sb.AppendLine(objs.Length.ToString());
                UnityEngine.Debug.LogError(sb.ToString());

            }
            if (times != null)
                foreach (Dictionary<int, Dictionary<Type, ulong>> dic in times)
                {
                    if (dic.ContainsKey(Time.frameCount - 2401))
                        dic.Remove(Time.frameCount - 2401);

                }
            if (specialTimes != null)
                foreach (Dictionary<int, ulong> dic in specialTimes)
                {
                    if (dic.ContainsKey(Time.frameCount - 2401))
                        dic.Remove(Time.frameCount - 2401);

                }
        }
    }


    public static class Patches
    {
        public static void InitStopwatch(Type t)
        {
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            watch.Reset();
            watch.Start();
        }

        public static Stopwatch StopStopwatch(Type t)
        {
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            watch.Stop();
            return watch;
        }

        public static void UpdatePrefixes(object __instance)
        {
            Type t = __instance.GetType();
            InitStopwatch(t);
        }

        public static void UpdatePostfixes0(object __instance) { UpdatePostfixes(__instance, 0); }
        public static void UpdatePostfixes1(object __instance) { UpdatePostfixes(__instance, 1); }
        public static void UpdatePostfixes2(object __instance) { UpdatePostfixes(__instance, 2); }
        public static void UpdatePostfixes3(object __instance) { UpdatePostfixes(__instance, 3); }
        public static void UpdatePostfixes4(object __instance) { UpdatePostfixes(__instance, 4); }
        public static void UpdatePostfixes5(object __instance) { UpdatePostfixes(__instance, 5); }
        public static void UpdatePostfixes6(object __instance) { UpdatePostfixes(__instance, 6); }
        public static void UpdatePostfixes7(object __instance) { UpdatePostfixes(__instance, 7); }
        public static void UpdatePostfixes8(object __instance) { UpdatePostfixes(__instance, 8); }
        public static void UpdatePostfixes9(object __instance) { UpdatePostfixes(__instance, 9); }
        public static void UpdatePostfixes10(object __instance) { UpdatePostfixes(__instance, 10); }
        public static void UpdatePostfixes11(object __instance) { UpdatePostfixes(__instance, 11); }
        public static void UpdatePostfixes12(object __instance) { UpdatePostfixes(__instance, 12); }
        public static void UpdatePostfixes13(object __instance) { UpdatePostfixes(__instance, 13); }
        public static void UpdatePostfixes14(object __instance) { UpdatePostfixes(__instance, 14); }
        public static void UpdatePostfixes15(object __instance) { UpdatePostfixes(__instance, 15); }
        public static void UpdatePostfixes16(object __instance) { UpdatePostfixes(__instance, 16); }
        public static void UpdatePostfixes17(object __instance) { UpdatePostfixes(__instance, 17); }
        public static void UpdatePostfixes18(object __instance) { UpdatePostfixes(__instance, 18); }
        public static void UpdatePostfixes19(object __instance) { UpdatePostfixes(__instance, 19); }
        public static void UpdatePostfixes20(object __instance) { UpdatePostfixes(__instance, 20); }
        public static void UpdatePostfixes21(object __instance) { UpdatePostfixes(__instance, 21); }
        public static void UpdatePostfixes22(object __instance) { UpdatePostfixes(__instance, 22); }
        public static void UpdatePostfixes23(object __instance) { UpdatePostfixes(__instance, 23); }
        public static void UpdatePostfixes24(object __instance) { UpdatePostfixes(__instance, 24); }
        public static void UpdatePostfixes25(object __instance) { UpdatePostfixes(__instance, 25); }
        public static void UpdatePostfixes26(object __instance) { UpdatePostfixes(__instance, 26); }
        public static void UpdatePostfixes27(object __instance) { UpdatePostfixes(__instance, 27); }
        public static void UpdatePostfixes28(object __instance) { UpdatePostfixes(__instance, 28); }
        public static void UpdatePostfixes29(object __instance) { UpdatePostfixes(__instance, 29); }
        public static void UpdatePostfixes30(object __instance) { UpdatePostfixes(__instance, 30); }
        public static void UpdatePostfixes31(object __instance) { UpdatePostfixes(__instance, 31); }
        public static void UpdatePostfixes32(object __instance) { UpdatePostfixes(__instance, 32); }
        public static void UpdatePostfixes33(object __instance) { UpdatePostfixes(__instance, 33); }
        public static void UpdatePostfixes34(object __instance) { UpdatePostfixes(__instance, 34); }
        public static void UpdatePostfixes35(object __instance) { UpdatePostfixes(__instance, 35); }
        public static void UpdatePostfixes36(object __instance) { UpdatePostfixes(__instance, 36); }
        public static void UpdatePostfixes37(object __instance) { UpdatePostfixes(__instance, 37); }
        public static void UpdatePostfixes38(object __instance) { UpdatePostfixes(__instance, 38); }
        public static void UpdatePostfixes39(object __instance) { UpdatePostfixes(__instance, 39); }
        public static void UpdatePostfixes40(object __instance) { UpdatePostfixes(__instance, 40); }
        public static void UpdatePostfixes41(object __instance) { UpdatePostfixes(__instance, 41); }
        public static void UpdatePostfixes42(object __instance) { UpdatePostfixes(__instance, 42); }
        public static void UpdatePostfixes43(object __instance) { UpdatePostfixes(__instance, 43); }
        public static void UpdatePostfixes44(object __instance) { UpdatePostfixes(__instance, 44); }
        public static void UpdatePostfixes45(object __instance) { UpdatePostfixes(__instance, 45); }
        public static void UpdatePostfixes46(object __instance) { UpdatePostfixes(__instance, 46); }
        public static void UpdatePostfixes47(object __instance) { UpdatePostfixes(__instance, 47); }
        public static void UpdatePostfixes48(object __instance) { UpdatePostfixes(__instance, 48); }
        public static void UpdatePostfixes49(object __instance) { UpdatePostfixes(__instance, 49); }
        public static void UpdatePostfixes50(object __instance) { UpdatePostfixes(__instance, 50); }
        public static void UpdatePostfixes51(object __instance) { UpdatePostfixes(__instance, 51); }

        public static void UpdatePostfixes(object __instance, int i)
        {
            Type t = __instance.GetType();
            Stopwatch watch = StopStopwatch(t);
            Dictionary<Type, ulong> frameDic;
            Dictionary<int, Dictionary<Type, ulong>> times = Instrumentation.times[i];
            if (times.TryGetValue(Time.frameCount, out frameDic) == false)
            {
                frameDic = new Dictionary<Type, ulong>();
                times.Add(Time.frameCount, frameDic);
            }
            if (frameDic.ContainsKey(t) == false)
                frameDic.Add(t, 0);
            frameDic[t] += (ulong)watch.ElapsedTicks;
        }


        public static void SpecialUpdatePrefixes(MethodInfo __originalMethod)
        {
            InitStopwatch(__originalMethod.DeclaringType);
        }

        public static void SpecialUpdatePostfixes0(MethodInfo __originalMethod) { SpecialUpdatePostfixes(__originalMethod, 0); }

        public static void SpecialUpdatePostfixes(MethodInfo __originalMethod, int i)
        {
            Type t = __originalMethod.DeclaringType;
            Stopwatch watch = StopStopwatch(t);
            Dictionary<int, ulong> times = Instrumentation.specialTimes[i];
            if (times.ContainsKey(Time.frameCount) == false)
                times.Add(Time.frameCount, 0);
            times[Time.frameCount] += (ulong)watch.ElapsedTicks;
        }
    }
}
