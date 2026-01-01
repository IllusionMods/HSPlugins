using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Studio;
using ToolBox.Extensions;
using UnityEngine;

namespace ToolBox
{
    // WARNING: This is meant only for use internally in the HSPlugins repo! If you wish to add Timeline compatibility to your own plugin, use this instead:
    // https://github.com/IllusionMods/IllusionModdingAPI/blob/a5828a6e12c4f101266805f7fd3e635d42e98428/src/Shared.Core/Utilities/TimelineCompatibility.cs#L15
    internal class TimelineCompatibility
    {
        private static Func<float> _getPlaybackTime;
        private static Func<float> _getDuration;
        private static Func<bool> _getIsPlaying;
        private static Action _play;
        private static Action _stop;
        private static MethodInfo _addInterpolableModelStatic;
        private static MethodInfo _addInterpolableModelDynamic;
        private static Action _refreshInterpolablesList;
        private static MethodInfo _estimateRealDuration;
        private static Type _interpolableDelegate;

        public static bool Init()
        {
            try
            {
                Type timelineType = Type.GetType("Timeline.Timeline,Timeline");
                if (timelineType != null)
                {
                    _getPlaybackTime = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), timelineType.GetProperty("playbackTime", BindingFlags.Public | BindingFlags.Static).GetGetMethod());
                    _getDuration = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), timelineType.GetProperty("duration", BindingFlags.Public | BindingFlags.Static).GetGetMethod());
                    _getIsPlaying = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), timelineType.GetProperty("isPlaying", BindingFlags.Public | BindingFlags.Static).GetGetMethod());
                    _play = (Action)Delegate.CreateDelegate(typeof(Action), timelineType.GetMethod("Play", BindingFlags.Public | BindingFlags.Static));
                    _stop = (Action)Delegate.CreateDelegate(typeof(Action), timelineType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static));
                    _addInterpolableModelStatic = timelineType.GetMethod("AddInterpolableModelStatic", BindingFlags.Public | BindingFlags.Static);
                    _addInterpolableModelDynamic = timelineType.GetMethod("AddInterpolableModelDynamic", BindingFlags.Public | BindingFlags.Static);
                    _refreshInterpolablesList = (Action)Delegate.CreateDelegate(typeof(Action), timelineType.GetMethod("RefreshInterpolablesList", BindingFlags.Public | BindingFlags.Static));
                    _estimateRealDuration = timelineType.GetMethod("EstimateRealDuration", BindingFlags.Public | BindingFlags.Static);
                    _interpolableDelegate = Type.GetType("Timeline.InterpolableDelegate,Timeline");
                    return true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Exception caught when trying to find Timeline: " + e);
            }
            return false;
        }

        public static float GetPlaybackTime()
        {
            return _getPlaybackTime();
        }

        public static float GetDuration()
        {
            return _getDuration();
        }

        public static bool GetIsPlaying()
        {
            return _getIsPlaying();
        }

        public static void Play()
        {
            _play();
        }

        public static void Stop()
        {
            _stop();
        }

        /// <summary>
        /// Adds an InterpolableModel to the list with a constant parameter
        /// </summary>
        public static void AddInterpolableModelStatic(string owner,
                                                      string id,
                                                      object parameter,
                                                      string name, 
                                                      ToolBox.Extensions.Action<ObjectCtrlInfo, object, object, object, float> interpolateBefore,
                                                      ToolBox.Extensions.Action<ObjectCtrlInfo, object, object, object, float> interpolateAfter,
                                                      Func<ObjectCtrlInfo, bool> isCompatibleWithTarget,
                                                      Func<ObjectCtrlInfo, object, object> getValue,
                                                      Func<object, XmlNode, object> readValueFromXml, 
                                                      Action<object, XmlTextWriter, object> writeValueToXml,
                                                      Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null,
                                                      Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null,
                                                      Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null,
                                                      bool useOciInHash = true,
                                                      Func<string, ObjectCtrlInfo, object, string> getFinalName = null,
                                                      Func<ObjectCtrlInfo, object, bool> shouldShow = null)
        {
            Delegate ib = null;
            if (interpolateBefore != null)
                ib = Delegate.CreateDelegate(_interpolableDelegate, interpolateBefore.Target, interpolateBefore.Method);
            Delegate ia = null;
            if (interpolateAfter != null)
                ia = Delegate.CreateDelegate(_interpolableDelegate, interpolateAfter.Target, interpolateAfter.Method);
            _addInterpolableModelStatic.Invoke(null, new object[]
            {
                owner,
                id,
                parameter,
                name,
                ib,
                ia,
                isCompatibleWithTarget,
                getValue,
                readValueFromXml,
                writeValueToXml,
                readParameterFromXml,
                writeParameterToXml,
                checkIntegrity,
                useOciInHash,
                getFinalName,
                shouldShow
            });
        }

        /// <summary>
        /// Adds an interpolableModel to the list with a dynamic parameter
        /// </summary>
        public static void AddInterpolableModelDynamic(string owner,
                                                       string id,
                                                       string name,
                                                       ToolBox.Extensions.Action<ObjectCtrlInfo, object, object, object, float> interpolateBefore,
                                                       ToolBox.Extensions.Action<ObjectCtrlInfo, object, object, object, float> interpolateAfter,
                                                       Func<ObjectCtrlInfo, bool> isCompatibleWithTarget,
                                                       Func<ObjectCtrlInfo, object, object> getValue,
                                                       Func<object, XmlNode, object> readValueFromXml,
                                                       Action<object, XmlTextWriter, object> writeValueToXml,
                                                       Func<ObjectCtrlInfo, object> getParameter,
                                                       Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null,
                                                       Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null,
                                                       Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null,
                                                       bool useOciInHash = true,
                                                       Func<string, ObjectCtrlInfo, object, string> getFinalName = null,
                                                       Func<ObjectCtrlInfo, object, bool> shouldShow = null)
        {
            Delegate ib = null;
            if (interpolateBefore != null)
                ib = Delegate.CreateDelegate(_interpolableDelegate, interpolateBefore.Target, interpolateBefore.Method);
            Delegate ia = null;
            if (interpolateAfter != null)
                ia = Delegate.CreateDelegate(_interpolableDelegate, interpolateAfter.Target, interpolateAfter.Method);
            _addInterpolableModelDynamic.Invoke(null, new object[]
            {
                owner,
                id,
                name,
                ib,
                ia,
                isCompatibleWithTarget,
                getValue,
                readValueFromXml,
                writeValueToXml,
                getParameter,
                readParameterFromXml,
                writeParameterToXml,
                checkIntegrity,
                useOciInHash,
                getFinalName,
                shouldShow
            });
        }

        public static float EstimateRealDuration()
        {
            if (_estimateRealDuration != null)
                return (float)_estimateRealDuration.Invoke(null, null);
            return _getDuration();
        }

        public static void RefreshInterpolablesList()
        {
            if (_refreshInterpolablesList != null)
                _refreshInterpolablesList();
        }
    }
}
