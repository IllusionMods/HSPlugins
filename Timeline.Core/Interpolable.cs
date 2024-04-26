using Studio;

using System;
using System.Collections.Generic;
using System.Xml;

using UnityEngine;

namespace Timeline
{
	[Obsolete("use class Timeline.Interpolable<Data, Param> for more transparent visualization of data")]
	public class Interpolable : InterpolableModel
	{
		private readonly int _hashCode;

		public override string name { get { return _getFinalName != null ? _getFinalName(_name, oci, parameter) : base.name; } }

		public readonly ObjectCtrlInfo oci;
		public readonly SortedList<float, Keyframe> keyframes = new SortedList<float, Keyframe>();
		public bool enabled = true;
		public Color color = Color.white;
		public string alias = "";

		public Interpolable(ObjectCtrlInfo oci, InterpolableModel interpolableModel) : base(interpolableModel.GetParameter(oci), interpolableModel)
		{
			if(useOciInHash)
				this.oci = oci;

			unchecked
			{
				int hash = base.GetHashCode();
				_hashCode = hash * 31 + (this.oci != null ? this.oci.GetHashCode() : 0);
			}
		}

		public Interpolable(ObjectCtrlInfo oci, object parameter, InterpolableModel interpolableModel) : base(parameter, interpolableModel)
		{
			if(useOciInHash)
				this.oci = oci;

			unchecked
			{
				int hash = base.GetHashCode();
				_hashCode = hash * 31 + (this.oci != null ? this.oci.GetHashCode() : 0);
			}
		}

		public bool InterpolateBefore(object leftValue, object rightValue, float factor)
		{
			if(CheckIntegrity(leftValue, rightValue))
				_interpolateBefore(oci, parameter, leftValue, rightValue, factor);
			else
				return false;
			return true;
		}

		public bool InterpolateAfter(object leftValue, object rightValue, float factor)
		{
			if(CheckIntegrity(leftValue, rightValue))
				_interpolateAfter(oci, parameter, leftValue, rightValue, factor);
			else
				return false;
			return true;
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
			if(_shouldShow == null)
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

	public class Interpolable<Data, Param> : InterpolableBase
	{
		public Interpolable(ObjectCtrlInfo oci, InterpolableModel<Data, Param> interpolableModel) : base(oci, interpolableModel)
		{
		}

		public Interpolable(ObjectCtrlInfo oci, Param parameter, InterpolableModel<Data, Param> interpolableModel) : base(oci, parameter, interpolableModel)
		{
		}
	}

	public class InterpolableBase : Interpolable
	{
		public InterpolableBase(ObjectCtrlInfo oci, InterpolableModelBase interpolableModel) : base(oci, interpolableModel)
		{
		}

		public InterpolableBase(ObjectCtrlInfo oci, object parameter, InterpolableModelBase interpolableModel) : base(oci, parameter, interpolableModel)
		{
		}
	}
}
