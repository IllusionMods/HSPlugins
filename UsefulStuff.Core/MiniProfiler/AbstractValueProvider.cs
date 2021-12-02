using UnityEngine;

namespace kleberswf.tools.miniprofiler
{
    [DisallowMultipleComponent]
#if UNITY_5
	[HelpURL("http://kleber-swf.com/docs/mini-profiler/#value-provider")]
#endif
    public abstract class AbstractValueProvider : MonoBehaviour
    {
        public string Title;
        public string NumberFormat = "#,##0";
        public abstract float Value { get; }
        public virtual void Refresh(float readInterval) { }
    }
}