using System;
using UnityEngine;

namespace kleberswf.tools.miniprofiler {
#if UNITY_5
	[HelpURL("http://kleber-swf.com/docs/mini-profiler/#memory-value-provider")]
#endif
	public class MemoryValueProvider : AbstractValueProvider {
		public override float Value { get { return GC.GetTotalMemory(false); } }
	}
}