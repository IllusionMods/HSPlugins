using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace ToolBox
{
    public class TranslationDictionary<T> where T : struct, IConvertible
    {
        private readonly Dictionary<T, string> _strings = new Dictionary<T, string>();
        public TranslationDictionary(string resourceDictionary)
        {
            if (typeof(T).IsEnum == false)
                throw new ArgumentException("T must be an enumerated type");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceDictionary))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);
                Type enumType = typeof(T);
                foreach (XmlNode node in doc.FirstChild)
                {
                    try
                    {
                        switch (node.Name)
                        {
                            // Might have different types of nodes in the future
                            case "string":
                                string key = node.Attributes["key"].Value;
                                string value = node.Attributes["value"].Value;
                                T e = (T)Enum.Parse(enumType, key);
                                this._strings.Add(e, value);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        //Debug.LogWarning("Could not parse translation string " + node.OuterXml + " in " + resourceDictionary + "\n" + e);
                    }
                }
            }
        }

        public string GetString(T key)
        {
            if (this._strings.TryGetValue(key, out string res) == false)
            {
                res = "";
                //Debug.LogError("Could not find string " + key + " in translation dictionary.");
            }
            return res;
        }
    }
}
