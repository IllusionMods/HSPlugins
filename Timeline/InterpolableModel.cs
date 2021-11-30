using System;
using System.Xml;
using Studio;

namespace Timeline
{
    public delegate void InterpolableDelegate(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue, float factor);
    public class InterpolableModel
    {
        private readonly int _hashCode;

        public readonly string owner;
        public readonly string id;
        public readonly object parameter;
        protected readonly string _name;
        public virtual string name { get { return this._name;} }
        protected readonly InterpolableDelegate _interpolateBefore;
        protected readonly InterpolableDelegate _interpolateAfter;
        private readonly Func<ObjectCtrlInfo, bool> _isCompatibleWithTarget;
        internal readonly Func<ObjectCtrlInfo, object, object> _getValue;
        protected readonly Func<object, XmlNode, object> _readValueFromXml;
        protected readonly Action<object, XmlTextWriter, object> _writeValueToXml;
        protected readonly Func<ObjectCtrlInfo, object> _getParameter;
        public readonly Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml;
        public readonly Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml;
        protected readonly Func<ObjectCtrlInfo, object, object, object, bool> _checkIntegrity;
        public readonly bool useOciInHash;
        internal readonly Func<string, ObjectCtrlInfo, object, string> _getFinalName;
        protected readonly Func<ObjectCtrlInfo, object, bool> _shouldShow;

        public readonly bool canInterpolateBefore;
        public readonly bool canInterpolateAfter;

        internal InterpolableModel(string owner,
                                   string id,
                                   object parameter,
                                   string name,
                                   InterpolableDelegate interpolateBefore,
                                   InterpolableDelegate interpolateAfter,
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
            this.owner = owner;
            this.id = id;
            this.parameter = parameter;
            this._name = name;
            this._interpolateBefore = interpolateBefore;
            this._interpolateAfter = interpolateAfter;
            this._isCompatibleWithTarget = isCompatibleWithTarget;
            this._getValue = getValue;
            this._readValueFromXml = readValueFromXml;
            this._writeValueToXml = writeValueToXml;
            this._getParameter = getParameter;
            this.readParameterFromXml = readParameterFromXml;
            this.writeParameterToXml = writeParameterToXml;
            this._checkIntegrity = checkIntegrity;
            this.useOciInHash = useOciInHash;
            this.canInterpolateBefore = this._interpolateBefore != null;
            this.canInterpolateAfter = this._interpolateAfter != null;
            this._getFinalName = getFinalName;
            this._shouldShow = shouldShow;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (this.owner != null ? this.owner.GetHashCode() : 0);
                hash = hash * 31 + (this.id != null ? this.id.GetHashCode() : 0);
                this._hashCode = hash * 31 + (this.parameter != null ? this.parameter.GetHashCode() : 0);
            }
        }

        public InterpolableModel(string owner,
                                 string id,
                                 object parameter,
                                 string name,
                                 InterpolableDelegate interpolateBefore,
                                 InterpolableDelegate interpolateAfter,
                                 Func<ObjectCtrlInfo, bool> isCompatibleWithTarget,
                                 Func<ObjectCtrlInfo, object, object> getValue,
                                 Func<object, XmlNode, object> readValueFromXml,
                                 Action<object, XmlTextWriter, object> writeValueToXml,
                                 Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null,
                                 Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null,
                                 Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null,
                                 bool useOciInHash = true,
                                 Func<string, ObjectCtrlInfo, object, string> getFinalName = null,
                                 Func<ObjectCtrlInfo, object, bool> shouldShow = null) : 
                this(owner, id, parameter, name, interpolateBefore, interpolateAfter, isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, null, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow) { }

        public InterpolableModel(string owner,
                                 string id,
                                 string name,
                                 InterpolableDelegate interpolateBefore,
                                 InterpolableDelegate interpolateAfter,
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
                                 Func<ObjectCtrlInfo, object, bool> shouldShow = null) : 
                this(owner, id, null, name, interpolateBefore, interpolateAfter, isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, getParameter, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow) { }

        protected InterpolableModel(InterpolableModel other) : this(other.owner, other.id, other.parameter, other.name, other._interpolateBefore, other._interpolateAfter, other._isCompatibleWithTarget, other._getValue, other._readValueFromXml, other._writeValueToXml, other._getParameter, other.readParameterFromXml, other.writeParameterToXml, other._checkIntegrity, other.useOciInHash, other._getFinalName, other._shouldShow) { }

        protected InterpolableModel(object parameter, InterpolableModel other) : this(other.owner, other.id, parameter, other.name, other._interpolateBefore, other._interpolateAfter, other._isCompatibleWithTarget, other._getValue, other._readValueFromXml, other._writeValueToXml, other._getParameter, other.readParameterFromXml, other.writeParameterToXml, other._checkIntegrity, other.useOciInHash, other._getFinalName, other._shouldShow) { }

        internal object GetParameter(ObjectCtrlInfo oci)
        {
            if (this._getParameter == null)
                return this.parameter;
            return this._getParameter(oci);
        }

        public bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
        {
            try
            {
                return this._isCompatibleWithTarget(oci);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(Timeline._name + ": Exception happened while checking if OCI was compatible with target:\n" + e);
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }

        public override string ToString()
        {
            return $"owner: [{this.owner}], id: [{this.id}], parameter: [{this.parameter}], name: [{this.name}], interpolateBefore: [{this._interpolateBefore}], interpolateAfter: [{this._interpolateAfter}], isCompatibleWithTarget: [{this._isCompatibleWithTarget}], getValue: [{this._getValue}], readValueFromXml: [{this._readValueFromXml}], writeValueToXml: [{this._writeValueToXml}], getParameter: [{this._getParameter}], readParameterFromXml: [{this.readParameterFromXml}], writeParameterToXml: [{this.writeParameterToXml}], checkIntegrity: [{this._checkIntegrity}], useOciInHash: [{this.useOciInHash}], getFinalName: [{this._getFinalName}], shouldShow:[{this._shouldShow}]";
        }

    }
}