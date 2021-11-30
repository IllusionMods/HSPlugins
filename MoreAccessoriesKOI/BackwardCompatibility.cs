using System;
using System.Reflection;
using ChaCustom;
using HarmonyLib;
using ToolBox.Extensions;
using UnityEngine;

namespace MoreAccessoriesKOI
{
    internal static class BackwardCompatibility
    {
        private static object _customHistory_Instance = null;
        private static Action<ChaControl, Func<bool>> _customHistory_Add1 = null;
        private static Action<ChaControl, Func<bool, bool>, bool> _customHistory_Add2 = null;
        private static Action<ChaControl, Func<bool, bool, bool>, bool, bool> _customHistory_Add3 = null;
        private static ToolBox.Extensions.Action<ChaControl, Func<bool, bool, bool, bool, bool>, bool, bool, bool, bool> _customHistory_Add5 = null;

        private static void CheckInstance()
        {
            if (_customHistory_Instance == null)
            {
                Type t = Type.GetType("ChaCustom.CustomHistory,Assembly-CSharp.dll");
                _customHistory_Instance = t.GetPrivateProperty("Instance");
                _customHistory_Add1 = (Action<ChaControl, Func<bool>>)Delegate.CreateDelegate(typeof(Action<ChaControl, Func<bool>>), _customHistory_Instance, _customHistory_Instance.GetType().GetMethod("Add1", AccessTools.all));
                _customHistory_Add2 = (Action<ChaControl, Func<bool, bool>, bool>)Delegate.CreateDelegate(typeof(Action<ChaControl, Func<bool, bool>, bool>), _customHistory_Instance, _customHistory_Instance.GetType().GetMethod("Add2", AccessTools.all));
                _customHistory_Add3 = (Action<ChaControl, Func<bool, bool, bool>, bool, bool>)Delegate.CreateDelegate(typeof(Action<ChaControl, Func<bool, bool, bool>, bool, bool>), _customHistory_Instance, _customHistory_Instance.GetType().GetMethod("Add3", AccessTools.all));
                _customHistory_Add5 = (ToolBox.Extensions.Action<ChaControl, Func<bool, bool, bool, bool, bool>, bool, bool, bool, bool>)Delegate.CreateDelegate(typeof(ToolBox.Extensions.Action<ChaControl, Func<bool, bool, bool, bool, bool>, bool, bool, bool, bool>), _customHistory_Instance, _customHistory_Instance.GetType().GetMethod("Add5", AccessTools.all));
            }
        }

        public static void CustomHistory_Instance_Add1(ChaControl instanceChaCtrl, Func<bool> updateAccessoryMoveAllFromInfo)
        {
            CheckInstance();
            _customHistory_Add1(instanceChaCtrl, updateAccessoryMoveAllFromInfo);
        }

        public static void CustomHistory_Instance_Add2(ChaControl instanceChaCtrl, Func<bool, bool> funcUpdateAcsColor, bool b)
        {
            CheckInstance();
            _customHistory_Add2(instanceChaCtrl, funcUpdateAcsColor, b);
        }

        internal static void CustomHistory_Instance_Add3(ChaControl instanceChaCtrl, Func<bool, bool, bool> funcUpdateAccessory, bool b, bool b1)
        {
            CheckInstance();
            _customHistory_Add3(instanceChaCtrl, funcUpdateAccessory, b, b1);
        }

        internal static void CustomHistory_Instance_Add5(ChaControl chaCtrl, Func<bool, bool, bool, bool, bool> reload, bool v1, bool v2, bool v3, bool v4)
        {
            CheckInstance();
            _customHistory_Add5(chaCtrl, reload, v1, v2, v3, v4);
        }

        private static MethodInfo _cvsColor_Setup;

        internal static void Setup(this CvsColor self, string winTitle, CvsColor.ConnectColorKind kind, Color color, Action<Color> _actUpdateColor, Action _actUpdateHistory, bool _useAlpha)
        {
            if (_cvsColor_Setup == null)
                _cvsColor_Setup = self.GetType().GetMethod("Setup", AccessTools.all);
            if (MoreAccessories._self._hasDarkness)
                _cvsColor_Setup.Invoke(self, new object[] { winTitle, kind, color, _actUpdateColor, _useAlpha });
            else
                _cvsColor_Setup.Invoke(self, new object[] { winTitle, kind, color, _actUpdateColor, _actUpdateHistory, _useAlpha });
        }

        internal static Action UpdateAcsColorHistory(this CvsAccessory __instance)
        {
            if (MoreAccessories._self._hasDarkness)
                return null;
            MethodInfo methodInfo = __instance.GetType().GetMethod("UpdateAcsColorHistory", AccessTools.all);
            if (methodInfo != null)
                return (Action)Delegate.CreateDelegate(typeof(Action), __instance, methodInfo);
            return null;
        }

        private static MethodInfo _cvsAccessory_UpdateAcsMoveHistory;
        internal static void UpdateAcsMoveHistory(this CvsAccessory self)
        {
            if (_cvsAccessory_UpdateAcsMoveHistory == null)
                _cvsAccessory_UpdateAcsMoveHistory = self.GetType().GetMethod("UpdateAcsMoveHistory", AccessTools.all);
            _cvsAccessory_UpdateAcsMoveHistory.Invoke(self, null);
        }
    }
}
