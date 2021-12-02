using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HSUS.Features
{
    public class AutomaticMemoryClean : IFeature
    {
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
            if (HSUS.AutomaticMemoryClean.Value
#if HONEYSELECT
                && OptimizeNEO._isCleaningResources == false
#endif
            )
            {
                if (Time.unscaledTime - _lastCleanup > HSUS.AutomaticMemoryCleanInterval.Value)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    _lastCleanup = Time.unscaledTime;
                    if (EventSystem.current.sendNavigationEvents)
                        EventSystem.current.sendNavigationEvents = false;
                }
            }
        }
    }
}
