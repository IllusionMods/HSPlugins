using System;
using System.Xml;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HSUS.Features
{
    public class AutomaticMemoryClean : IFeature
    {
        private bool _automaticMemoryClean = true;
        private int _automaticMemoryCleanInterval = 300;
        private float _lastCleanup;

        public void Awake()
        {
            HSUS._self._onUpdate += Update;
        }

        public void LevelLoaded()
        {
        }

        private void Update()
        {
            if (_automaticMemoryClean
#if HONEYSELECT
                && OptimizeNEO._isCleaningResources == false
#endif
            )
            {
                if (Time.unscaledTime - _lastCleanup > _automaticMemoryCleanInterval)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    _lastCleanup = Time.unscaledTime;
                    if (EventSystem.current.sendNavigationEvents)
                        EventSystem.current.sendNavigationEvents = false;
                }
            }
        }


        public void LoadParams(XmlNode node)
        {
            node = node.FindChildNode("automaticMemoryClean");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _automaticMemoryClean = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            if (node.Attributes["interval"] != null)
                _automaticMemoryCleanInterval = XmlConvert.ToInt32(node.Attributes["interval"].Value);
        }

        public void SaveParams(XmlTextWriter writer)
        {
            writer.WriteStartElement("automaticMemoryClean");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_automaticMemoryClean));
            writer.WriteAttributeString("interval", XmlConvert.ToString(_automaticMemoryCleanInterval));
            writer.WriteEndElement();
        }
    }
}
