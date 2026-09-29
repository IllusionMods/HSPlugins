using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Timeline.Compat
{
    /// <summary>
    /// NodesConstraints, reached through reflection so Timeline does not need it installed: its
    /// constraints are private, and rigs and the constraint tools only list them, add new ones and read
    /// what its window has picked.
    /// </summary>
    internal static class NodesConstraintsLink
    {
        public const string Guid = "com.joan6694.illusionplugins.nodesconstraints";
        private const BindingFlags _all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static object _plugin;
        private static FieldInfo _list;
        private static MethodInfo _add;
        private static Type _lock;
        private static readonly Dictionary<string, FieldInfo> _fields = new Dictionary<string, FieldInfo>();

        public static bool Available
        {
            get { return Resolve(); }
        }

        private static bool Resolve()
        {
            if (_plugin != null)
                return true;
            PluginInfo info;
            if (Chainloader.PluginInfos.TryGetValue(Guid, out info) == false || info.Instance == null)
                return false;
            Type type = info.Instance.GetType();
            FieldInfo list = type.GetField("_constraints", _all);
            // The short overload: the rest of a constraint's settings are fields set afterwards.
            MethodInfo add = type.GetMethods(_all).FirstOrDefault(m => m.Name == "AddConstraint" && m.GetParameters().Length == 10);
            Type lockType = type.GetNestedType("TransformLock", BindingFlags.Public | BindingFlags.NonPublic);
            if (list == null || add == null || lockType == null)
            {
                Timeline.Logger.LogWarning("This version of NodesConstraints is not one rigs know how to work with.");
                return false;
            }
            _plugin = info.Instance;
            _list = list;
            _add = add;
            _lock = lockType;
            return true;
        }

        /// <summary>Every constraint in the scene that still links two transforms.</summary>
        public static List<object> Constraints()
        {
            var result = new List<object>();
            if (Resolve() == false)
                return result;
            foreach (object c in (IList)_list.GetValue(_plugin))
            {
                if (c != null && Get<Transform>(c, "parentTransform") != null && Get<Transform>(c, "childTransform") != null)
                    result.Add(c);
            }
            return result;
        }

        private static FieldInfo Field(object c, string name)
        {
            FieldInfo field;
            string key = c.GetType().FullName + "." + name;
            if (_fields.TryGetValue(key, out field) == false)
                _fields[key] = field = c.GetType().GetField(name, _all);
            return field;
        }

        public static T Get<T>(object c, string name)
        {
            FieldInfo field = Field(c, name);
            object value = field == null ? null : field.GetValue(c);
            return value is T ? (T)value : default(T);
        }

        public static void Set(object c, string name, object value)
        {
            FieldInfo field = Field(c, name);
            if (field != null && value != null)
                field.SetValue(c, value);
        }

        /// <summary>A lock's three axes, as true or false each.</summary>
        public static bool[] GetLock(object c, string name)
        {
            object l = Field(c, name) == null ? null : Field(c, name).GetValue(c);
            if (l == null)
                return new[] { true, true, true };
            return new[] { (bool)_lock.GetField("x").GetValue(l), (bool)_lock.GetField("y").GetValue(l), (bool)_lock.GetField("z").GetValue(l) };
        }

        public static void SetLock(object c, string name, bool[] axes)
        {
            Set(c, name, Activator.CreateInstance(_lock, axes[0], axes[1], axes[2]));
        }

        public static object Add(bool enabled, Transform parent, Transform child, bool position, Vector3 positionOffset,
                                 bool rotation, Quaternion rotationOffset, bool scale, Vector3 scaleOffset, string alias)
        {
            if (Resolve() == false)
                return null;
            return _add.Invoke(_plugin, new object[] { enabled, parent, child, position, positionOffset, rotation, rotationOffset, scale, scaleOffset, alias });
        }

        private static T PluginGet<T>(string name)
        {
            if (Resolve() == false)
                return default(T);
            FieldInfo field = Field(_plugin, name);
            object value = field == null ? null : field.GetValue(_plugin);
            return value is T ? (T)value : default(T);
        }

        /// <summary>Whether NodesConstraints' window is open.</summary>
        public static bool WindowShown
        {
            get { return PluginGet<bool>("_showUI"); }
        }

        /// <summary>Opens NodesConstraints' window, where the constraint tools sit beside it.</summary>
        public static void ShowWindow()
        {
            if (Resolve() == false)
                return;
            PropertyInfo show = _plugin.GetType().GetProperty("ShowUI", _all);
            if (show != null)
                show.SetValue(_plugin, true, null);
        }

        /// <summary>Where NodesConstraints' window is on the screen.</summary>
        public static Rect WindowRect
        {
            get { return PluginGet<Rect>("_windowRect"); }
        }

        /// <summary>The node or bone picked in NodesConstraints' window.</summary>
        public static Transform SelectedBone
        {
            get { return PluginGet<Transform>("_selectedBone"); }
        }

        /// <summary>Makes a constraint the one NodesConstraints' window shows.</summary>
        public static void Select(object constraint)
        {
            if (Resolve() && constraint != null)
                Set(_plugin, "_selectedConstraint", constraint);
        }
    }
}
