using System.Xml;
using UnityEngine;

namespace ToolBox.Extensions {
    internal static class XmlExtensions
    {
        public static XmlNode FindChildNode(this XmlNode self, string name)
        {
            if (self.HasChildNodes == false)
                return null;
            foreach (XmlNode childNode in self.ChildNodes)
                if (childNode.Name.Equals(name))
                    return childNode;
            return null;
        }

        public static int ReadInt(this XmlNode self, string label)
        {
            return XmlConvert.ToInt32(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, int value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static byte ReadByte(this XmlNode self, string label)
        {
            return XmlConvert.ToByte(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, byte value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static bool ReadBool(this XmlNode self, string label)
        {
            return XmlConvert.ToBoolean(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, bool value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static float ReadFloat(this XmlNode self, string label)
        {
            return XmlConvert.ToSingle(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, float value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static Vector2 ReadVector2(this XmlNode self, string prefix)
        {
            return new Vector2(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}X"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Y"].Value)
            );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Vector2 value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.x));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.y));
        }

        public static Vector3 ReadVector3(this XmlNode self, string prefix)
        {
            return new Vector3(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}X"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Y"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Z"].Value)
            );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Vector3 value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.x));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.y));
            self.WriteAttributeString($"{prefix}Z", XmlConvert.ToString(value.z));
        }

        public static Vector4 ReadVector4(this XmlNode self, string prefix)
        {
            return new Vector4(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}X"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Y"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Z"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}W"].Value)
            );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Vector4 value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.x));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.y));
            self.WriteAttributeString($"{prefix}Z", XmlConvert.ToString(value.z));
            self.WriteAttributeString($"{prefix}W", XmlConvert.ToString(value.w));
        }

        public static Quaternion ReadQuaternion(this XmlNode self, string prefix)
        {
            return new Quaternion(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}X"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Y"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Z"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}W"].Value)
            );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Quaternion value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.x));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.y));
            self.WriteAttributeString($"{prefix}Z", XmlConvert.ToString(value.z));
            self.WriteAttributeString($"{prefix}W", XmlConvert.ToString(value.w));
        }

        public static Color ReadColor(this XmlNode self, string prefix)
        {
            return new Color(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}R"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}G"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}B"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}A"].Value)
            );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Color value)
        {
            self.WriteAttributeString($"{prefix}R", XmlConvert.ToString(value.r));
            self.WriteAttributeString($"{prefix}G", XmlConvert.ToString(value.g));
            self.WriteAttributeString($"{prefix}B", XmlConvert.ToString(value.b));
            self.WriteAttributeString($"{prefix}A", XmlConvert.ToString(value.a));
        }

        public static string XmlEscape(string unescaped)
        {
	        XmlDocument doc = new XmlDocument();
	        XmlNode node = doc.CreateElement("root");
	        node.InnerText = unescaped;
	        return node.InnerXml;
        }

        public static string XmlUnescape(string escaped)
        {
	        XmlDocument doc = new XmlDocument();
	        XmlNode node = doc.CreateElement("root");
	        node.InnerXml = escaped;
	        return node.InnerText;
        }
    }
}