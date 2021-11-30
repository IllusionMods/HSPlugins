using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Studio;
using UnityEngine;

namespace Timeline
{
    public class Interpolable : InterpolableModel
    {
        private readonly int _hashCode;

        public override string name { get { return this._getFinalName != null ? this._getFinalName(this._name, this.oci, this.parameter) : base.name; } }

        public readonly ObjectCtrlInfo oci;
        public readonly SortedList<float, Keyframe> keyframes = new SortedList<float, Keyframe>();
        public bool enabled = true;
        public Color color = Color.white;
        public string alias = "";

        public Interpolable(ObjectCtrlInfo oci, InterpolableModel interpolableModel) : base(interpolableModel.GetParameter(oci), interpolableModel)
        {
            if (this.useOciInHash)
                this.oci = oci;

            unchecked
            {
                int hash = base.GetHashCode();
                this._hashCode = hash * 31 + (this.oci != null ? this.oci.GetHashCode() : 0);
            }
        }

        public Interpolable(ObjectCtrlInfo oci, object parameter, InterpolableModel interpolableModel) : base(parameter, interpolableModel)
        {
            if (this.useOciInHash)
                this.oci = oci;

            unchecked
            {
                int hash = base.GetHashCode();
                this._hashCode = hash * 31 + (this.oci != null ? this.oci.GetHashCode() : 0);
            }
        }

        public bool InterpolateBefore(object leftValue, object rightValue, float factor)
        {
            if (this.CheckIntegrity(leftValue, rightValue))
                this._interpolateBefore(this.oci, this.parameter, leftValue, rightValue, factor);
            else
                return false;
            return true;
        }

        public bool InterpolateAfter(object leftValue, object rightValue, float factor)
        {
            if (this.CheckIntegrity(leftValue, rightValue))
                this._interpolateAfter(this.oci, this.parameter, leftValue, rightValue, factor);
            else
                return false;
            return true;
        }

        public object ReadValueFromXml(XmlNode node)
        {
            return this._readValueFromXml(this.parameter, node);
        }

        public void WriteValueToXml(XmlTextWriter writer, object value)
        {
            this._writeValueToXml(this.parameter, writer, value);
        }

        public object GetValue()
        {
            return this._getValue(this.oci, this.parameter);
        }

        private bool CheckIntegrity(object leftValue, object rightValue)
        {
            return (this.useOciInHash == false || this.oci != null) && (this._checkIntegrity == null || this._checkIntegrity(this.oci, this.parameter, leftValue, rightValue));
        }

        public bool ShouldShow()
        {
            if (this._shouldShow == null)
                return true;
            return this._shouldShow(this.oci, this.parameter);
        }

        public int GetBaseHashCode()
        {
            return base.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }

        public override string ToString()
        {
            return $"oci: [{this.oci}] " + base.ToString();
        }
    }
}
