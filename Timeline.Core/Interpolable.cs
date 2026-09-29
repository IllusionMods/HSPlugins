using Studio;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Timeline
{
    public class Interpolable : InterpolableModel
    {
        private readonly int _hashCode;

        public override string name { get { return _getFinalName != null ? _getFinalName(_name, oci, parameter) : base.name; } }

        public readonly ObjectCtrlInfo oci;
        public readonly SortedList<float, Keyframe> keyframes = new SortedList<float, Keyframe>();
        public bool enabled = true;
        /// <summary>
        /// Interpolate with a spline through the keyframes instead of the per segment easing curves,
        /// so speed carries through a keyframe rather than resetting at it. See <see cref="KeyframeSpline"/>.
        /// </summary>
        public bool smooth = false;
        /// <summary>
        /// What the track does outside its own first and last keyframe. See <see cref="TrackCycle"/>.
        /// </summary>
        public TrackExtrapolation extrapolation = TrackExtrapolation.Hold;
        public Color color = Color.white;
        public string alias = "";
        private bool _reportedBroken;

        public Interpolable(ObjectCtrlInfo oci, InterpolableModel interpolableModel) : base(interpolableModel.GetParameter(oci), interpolableModel)
        {
            if (useOciInHash)
                this.oci = oci;

            unchecked
            {
                int hash = base.GetHashCode();
                _hashCode = hash * 31 + (this.oci != null ? this.oci.GetHashCode() : 0);
            }
        }

        public Interpolable(ObjectCtrlInfo oci, object parameter, InterpolableModel interpolableModel) : base(parameter, interpolableModel)
        {
            if (useOciInHash)
                this.oci = oci;

            unchecked
            {
                int hash = base.GetHashCode();
                _hashCode = hash * 31 + (this.oci != null ? this.oci.GetHashCode() : 0);
            }
        }

        public bool InterpolateBefore(object leftValue, object rightValue, float factor)
        {
            if (canInterpolateBefore == false)
                return true;
            if (CheckIntegrity(leftValue, rightValue) == false)
                return false;
            try
            {
                _interpolateBefore(oci, parameter, leftValue, rightValue, factor);
            }
            catch (System.Exception e)
            {
                ReportBroken(e, leftValue);
            }
            return true;
        }

        public bool InterpolateAfter(object leftValue, object rightValue, float factor)
        {
            if (canInterpolateAfter == false)
                return true;
            if (CheckIntegrity(leftValue, rightValue) == false)
                return false;
            try
            {
                _interpolateAfter(oci, parameter, leftValue, rightValue, factor);
            }
            catch (System.Exception e)
            {
                ReportBroken(e, leftValue);
            }
            return true;
        }

        /// <summary>
        /// Interpolation runs every frame, so a value the delegate can't handle used to throw on every
        /// single one. Report it once with enough detail to identify the culprit and switch the
        /// interpolable off, which keeps its keyframes intact rather than deleting them.
        /// </summary>
        private void ReportBroken(System.Exception e, object value)
        {
            if (_reportedBroken)
                return;
            _reportedBroken = true;
            enabled = false;

            Timeline.Logger.LogError(
                    $"Interpolable '{name}' (owner: {owner}, id: {id}) failed and has been disabled.\n" +
                    $"oci: {(oci == null ? "null" : oci.GetType().Name)}, " +
                    $"parameter: {(parameter == null ? "null" : parameter.GetType().Name)}, " +
                    $"value: {(value == null ? "null" : value.GetType().Name)}\n{e}");
        }

        public object ReadValueFromXml(XmlNode node)
        {
            return _readValueFromXml(parameter, node);
        }

        public void WriteValueToXml(XmlTextWriter writer, object value)
        {
            _writeValueToXml(parameter, writer, value);
        }

        public object GetValue()
        {
            return _getValue(oci, parameter);
        }

        private bool CheckIntegrity(object leftValue, object rightValue)
        {
            return (useOciInHash == false || oci != null) && (_checkIntegrity == null || _checkIntegrity(oci, parameter, leftValue, rightValue));
        }

        public bool ShouldShow()
        {
            if (_shouldShow == null)
                return true;
            return _shouldShow(oci, parameter);
        }

        public int GetBaseHashCode()
        {
            return base.GetHashCode();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return $"oci: [{oci}] " + base.ToString();
        }
    }
}
