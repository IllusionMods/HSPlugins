using UnityEngine;

namespace kleberswf.tools.miniprofiler
{
#if UNITY_5
	[HelpURL("http://kleber-swf.com/docs/mini-profiler/#framerate-value-provider")]
#endif
    public class FramerateValueProvider : AbstractValueProvider
    {
        private int _frameCount;
        private float _dt;
        private float _fps;

        public override void Refresh(float readInterval)
        {
            if (readInterval < Time.unscaledDeltaTime)
            {
                _fps = 1f / Time.unscaledDeltaTime;
                return;
            }
            _frameCount++;
            _dt += Time.unscaledDeltaTime;
            if (_dt < 1f * readInterval) return;
            _fps = _frameCount / _dt;
            _frameCount = 0;
            _dt -= 1f * readInterval;
        }

        public override float Value { get { return _fps; } }
    }
}