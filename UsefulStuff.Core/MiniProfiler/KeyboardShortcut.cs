using System;
using UnityEngine;

namespace kleberswf.tools.util
{
    [Serializable]
#if UNITY_5
	[HelpURL("http://kleber-swf.com/docs/mini-profiler/#keyboard-shortcut")]
#endif
    public class KeyboardShortcut : MonoBehaviour
    {
        public bool Control;
        public bool Alt;
        public bool Shift;
        public KeyCode Key;

        public bool Pressed(Event currentEvent)
        {
            useGUILayout = false;
            return currentEvent.type == EventType.KeyUp && currentEvent.keyCode == Key &&
                   !((Control ^ currentEvent.control) || (Shift ^ currentEvent.shift) || (Alt ^ currentEvent.alt));
        }

        private void OnGUI()
        {
            if (!Pressed(Event.current)) return;
            SendMessage("ToggleCollapsed", SendMessageOptions.DontRequireReceiver);
        }
    }
}