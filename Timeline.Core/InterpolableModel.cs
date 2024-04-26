using Studio;

using System;
using System.Xml;

using UILib;

namespace Timeline
{
	[Obsolete("use delegate Timeline.InterpolableModel.InterpolableDelegate<Data, Param>() for more transparent visualization of data")]
	public delegate void InterpolableDelegate(ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue, float factor);

	public delegate void InterpolableDelegate<Data, Param>(ObjectCtrlInfo oci, Param parameter, Data leftValue, Data rightValue, float factor);

	[Obsolete("use class Timeline.InterpolableModel<Data, Peram> for more transparent visualization of data")]
	public class InterpolableModel
	{
		private readonly int _hashCode;

		public readonly string owner;
		public readonly string id;
		public readonly object parameter;
		protected readonly string _name;
		public virtual string name { get { return _name; } }
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
			_name = name;
			_interpolateBefore = interpolateBefore;
			_interpolateAfter = interpolateAfter;
			_isCompatibleWithTarget = isCompatibleWithTarget;
			_getValue = getValue;
			_readValueFromXml = readValueFromXml;
			_writeValueToXml = writeValueToXml;
			_getParameter = getParameter;
			this.readParameterFromXml = readParameterFromXml;
			this.writeParameterToXml = writeParameterToXml;
			_checkIntegrity = checkIntegrity;
			this.useOciInHash = useOciInHash;
			canInterpolateBefore = _interpolateBefore != null;
			canInterpolateAfter = _interpolateAfter != null;
			_getFinalName = getFinalName;
			_shouldShow = shouldShow;

			unchecked
			{
				int hash = 17;
				hash = hash * 31 + (this.owner != null ? this.owner.GetHashCode() : 0);
				hash = hash * 31 + (this.id != null ? this.id.GetHashCode() : 0);
				_hashCode = hash * 31 + (this.parameter != null ? this.parameter.GetHashCode() : 0);
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
				this(owner, id, parameter, name, interpolateBefore, interpolateAfter, isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, null, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow)
		{ }

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
				this(owner, id, null, name, interpolateBefore, interpolateAfter, isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, getParameter, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow)
		{ }

		protected InterpolableModel(InterpolableModel other) : this(other.owner, other.id, other.parameter, other.name, other._interpolateBefore, other._interpolateAfter, other._isCompatibleWithTarget, other._getValue, other._readValueFromXml, other._writeValueToXml, other._getParameter, other.readParameterFromXml, other.writeParameterToXml, other._checkIntegrity, other.useOciInHash, other._getFinalName, other._shouldShow) { }

		protected InterpolableModel(object parameter, InterpolableModel other) : this(other.owner, other.id, parameter, other.name, other._interpolateBefore, other._interpolateAfter, other._isCompatibleWithTarget, other._getValue, other._readValueFromXml, other._writeValueToXml, other._getParameter, other.readParameterFromXml, other.writeParameterToXml, other._checkIntegrity, other.useOciInHash, other._getFinalName, other._shouldShow) { }

		internal object GetParameter(ObjectCtrlInfo oci)
		{
			if(_getParameter == null)
				return parameter;
			return _getParameter(oci);
		}

		public bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
		{
			try
			{
				return _isCompatibleWithTarget(oci);
			}
			catch(Exception e)
			{
				Timeline.Logger.LogError("Exception happened while checking if OCI was compatible with target:\n" + e);
				return false;
			}
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public override string ToString()
		{
			return $"owner: [{owner}], id: [{id}], parameter: [{parameter}], name: [{name}], interpolateBefore: [{_interpolateBefore}], interpolateAfter: [{_interpolateAfter}], isCompatibleWithTarget: [{_isCompatibleWithTarget}], getValue: [{_getValue}], readValueFromXml: [{_readValueFromXml}], writeValueToXml: [{_writeValueToXml}], getParameter: [{_getParameter}], readParameterFromXml: [{readParameterFromXml}], writeParameterToXml: [{writeParameterToXml}], checkIntegrity: [{_checkIntegrity}], useOciInHash: [{useOciInHash}], getFinalName: [{_getFinalName}], shouldShow:[{_shouldShow}]";
		}

	}

	public class InterpolableModel<Data, Param> : InterpolableModelBase
	{
		public InterpolableModel(string owner, string id, Param parameter, string name, InterpolableDelegate<Data, Param> interpolateBefore, InterpolableDelegate<Data, Param> interpolateAfter, Func<ObjectCtrlInfo, bool> isCompatibleWithTarget, Func<ObjectCtrlInfo, Param, Data> getValue, Func<Param, XmlNode, Data> readValueFromXml, Action<Param, XmlTextWriter, Data> writeValueToXml, Func<ObjectCtrlInfo, XmlNode, Param> readParameterFromXml = null, Action<ObjectCtrlInfo, XmlTextWriter, Param> writeParameterToXml = null, Func<ObjectCtrlInfo, Param, Data, Data, bool> checkIntegrity = null, bool useOciInHash = true, Func<string, ObjectCtrlInfo, Param, string> getFinalName = null, Func<ObjectCtrlInfo, Param, bool> shouldShow = null)
			: this(owner, id, parameter, name,
				  interpolateBefore,
				  interpolateAfter,
				  isCompatibleWithTarget,
				  getValue,
				  readValueFromXml,
				  writeValueToXml,
				  null,
				  readParameterFromXml,
				  writeParameterToXml,
				  checkIntegrity,
				  useOciInHash,
				  getFinalName,
				  shouldShow)
		{
		}

		public InterpolableModel(string owner, string id, string name, InterpolableDelegate<Data, Param> interpolateBefore, InterpolableDelegate<Data, Param> interpolateAfter, Func<ObjectCtrlInfo, bool> isCompatibleWithTarget, Func<ObjectCtrlInfo, Param, Data> getValue, Func<Param, XmlNode, Data> readValueFromXml, Action<Param, XmlTextWriter, Data> writeValueToXml, Func<ObjectCtrlInfo, Param> getParameter, Func<ObjectCtrlInfo, XmlNode, Param> readParameterFromXml = null, Action<ObjectCtrlInfo, XmlTextWriter, Param> writeParameterToXml = null, Func<ObjectCtrlInfo, Param, Data, Data, bool> checkIntegrity = null, bool useOciInHash = true, Func<string, ObjectCtrlInfo, Param, string> getFinalName = null, Func<ObjectCtrlInfo, Param, bool> shouldShow = null)
			: this(owner, id, default(Param), name,
				  interpolateBefore,
				  interpolateAfter,
				  isCompatibleWithTarget,
				  getValue,
				  readValueFromXml,
				  writeValueToXml,
				  getParameter,
				  readParameterFromXml,
				  writeParameterToXml,
				  checkIntegrity,
				  useOciInHash,
				  getFinalName,
				  shouldShow)
		{
		}

		protected InterpolableModel(InterpolableModel<Data, Param> other) : base(other)
		{
		}

		protected InterpolableModel(object parameter, InterpolableModel<Data, Param> other) : base(parameter, other)
		{
		}

		internal InterpolableModel(string owner, string id, Param parameter, string name, InterpolableDelegate<Data, Param> interpolateBefore, InterpolableDelegate<Data, Param> interpolateAfter, Func<ObjectCtrlInfo, bool> isCompatibleWithTarget, Func<ObjectCtrlInfo, Param, Data> getValue, Func<Param, XmlNode, Data> readValueFromXml, Action<Param, XmlTextWriter, Data> writeValueToXml, Func<ObjectCtrlInfo, Param> getParameter, Func<ObjectCtrlInfo, XmlNode, Param> readParameterFromXml = null, Action<ObjectCtrlInfo, XmlTextWriter, Param> writeParameterToXml = null, Func<ObjectCtrlInfo, Param, Data, Data, bool> checkIntegrity = null, bool useOciInHash = true, Func<string, ObjectCtrlInfo, Param, string> getFinalName = null, Func<ObjectCtrlInfo, Param, bool> shouldShow = null)
			: base(owner, id, parameter, name,
				  (m, n, b, j, l) => interpolateBefore?.Invoke(m, (Param)n, (Data)b, (Data)j, l),
				  (m, n, b, j, l) => interpolateAfter?.Invoke(m, (Param)n, (Data)b, (Data)j, l),
				  isCompatibleWithTarget,

				  getValue == null ? null : new Func<ObjectCtrlInfo, object, object>((m, n) => getValue.Invoke(m, (Param)n)),
				  readValueFromXml == null ? null : new Func<object, XmlNode, object>((m, n) => readValueFromXml.Invoke((Param)m, n)),
				  writeValueToXml == null ? null : new Action<object, XmlTextWriter, object>((m, n, b) => writeValueToXml.Invoke((Param)m, n, (Data)b)),

				  getParameter == null ? null : new Func<ObjectCtrlInfo, object>((m) => getParameter.Invoke(m)),
				  readParameterFromXml == null ? null : new Func<ObjectCtrlInfo, XmlNode, object>((m, n) => readParameterFromXml.Invoke(m, n)),
				  writeParameterToXml == null ? null : new Action<ObjectCtrlInfo, XmlTextWriter, object>((m, n, b) => writeParameterToXml.Invoke(m, n, (Param)b)),

				  checkIntegrity == null ? null : new Func<ObjectCtrlInfo, object, object, object, bool>((m, n, b, j) => checkIntegrity.Invoke(m, (Param)n, (Data)b, (Data)j)),
				  useOciInHash,
				  getFinalName == null ? null : new Func<string, ObjectCtrlInfo, object, string>((m, n, b) => getFinalName.Invoke(m, n, (Param)b)),
				  shouldShow == null ? null : new Func<ObjectCtrlInfo, object, bool>((m, n) => shouldShow.Invoke(m, (Param)n)))
		{
		}
	}

	public class InterpolableModelBase : InterpolableModel
	{
		public InterpolableModelBase(string owner, string id, object parameter, string name, InterpolableDelegate<object, object> interpolateBefore, InterpolableDelegate<object, object> interpolateAfter, Func<ObjectCtrlInfo, bool> isCompatibleWithTarget, Func<ObjectCtrlInfo, object, object> getValue, Func<object, XmlNode, object> readValueFromXml, Action<object, XmlTextWriter, object> writeValueToXml, Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null, Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null, Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null, bool useOciInHash = true, Func<string, ObjectCtrlInfo, object, string> getFinalName = null, Func<ObjectCtrlInfo, object, bool> shouldShow = null)
			: base(owner, id, parameter, name,
				  (m, n, b, j, l) => interpolateBefore?.Invoke(m, n, b, j, l),
				  (m, n, b, j, l) => interpolateAfter?.Invoke(m, n, b, j, l), isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow)
		{
		}

		public InterpolableModelBase(string owner, string id, string name, InterpolableDelegate<object, object> interpolateBefore, InterpolableDelegate<object, object> interpolateAfter, Func<ObjectCtrlInfo, bool> isCompatibleWithTarget, Func<ObjectCtrlInfo, object, object> getValue, Func<object, XmlNode, object> readValueFromXml, Action<object, XmlTextWriter, object> writeValueToXml, Func<ObjectCtrlInfo, object> getParameter, Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null, Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null, Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null, bool useOciInHash = true, Func<string, ObjectCtrlInfo, object, string> getFinalName = null, Func<ObjectCtrlInfo, object, bool> shouldShow = null)
			: base(owner, id, name, (m, n, b, j, l) => interpolateBefore?.Invoke(m, n, b, j, l),
				  (m, n, b, j, l) => interpolateAfter?.Invoke(m, n, b, j, l), isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, getParameter, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow)
		{
		}

		protected InterpolableModelBase(InterpolableModelBase other) : base(other)
		{
		}

		protected InterpolableModelBase(object parameter, InterpolableModelBase other) : base(parameter, other)
		{
		}

		internal InterpolableModelBase(string owner, string id, object parameter, string name, InterpolableDelegate<object, object> interpolateBefore, InterpolableDelegate<object, object> interpolateAfter, Func<ObjectCtrlInfo, bool> isCompatibleWithTarget, Func<ObjectCtrlInfo, object, object> getValue, Func<object, XmlNode, object> readValueFromXml, Action<object, XmlTextWriter, object> writeValueToXml, Func<ObjectCtrlInfo, object> getParameter, Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null, Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null, Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null, bool useOciInHash = true, Func<string, ObjectCtrlInfo, object, string> getFinalName = null, Func<ObjectCtrlInfo, object, bool> shouldShow = null)
			: base(owner, id, parameter, name, (m, n, b, j, l) => interpolateBefore?.Invoke(m, n, b, j, l),
				  (m, n, b, j, l) => interpolateAfter?.Invoke(m, n, b, j, l), isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, getParameter, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow)
		{
		}
	}
}