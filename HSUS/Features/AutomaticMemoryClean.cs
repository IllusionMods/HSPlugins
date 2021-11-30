using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            HSUS._self._onUpdate += this.Update;
        }

        public void LevelLoaded()
        {
        }

        private void Update()
        {
            if (this._automaticMemoryClean
#if HONEYSELECT
                && OptimizeNEO._isCleaningResources == false
#endif
            )
            {
                if (Time.unscaledTime - this._lastCleanup > this._automaticMemoryCleanInterval)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    this._lastCleanup = Time.unscaledTime;
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
                this._automaticMemoryClean = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            if (node.Attributes["interval"] != null)
                this._automaticMemoryCleanInterval = XmlConvert.ToInt32(node.Attributes["interval"].Value);
        }

        public void SaveParams(XmlTextWriter writer)
        {
            writer.WriteStartElement("automaticMemoryClean");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._automaticMemoryClean));
            writer.WriteAttributeString("interval", XmlConvert.ToString(this._automaticMemoryCleanInterval));
            writer.WriteEndElement();
        }
    }
}
