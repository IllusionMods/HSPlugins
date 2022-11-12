using System;
using System.Linq;
using HarmonyLib;

namespace KKAPI.Utilities
{
    /// <summary>
    /// Class that abstracts away AutoTranslator. It lets you translate text to current language.
    /// </summary>
    public static class TranslationHelper
    {
        private static readonly Func<string, string> _tryTranslateCallback;

        /// <summary>
        /// True if a reasonably recent version of AutoTranslator is installed.
        /// It might return false for some very old versions that don't have the necessary APIs to make this class work.
        /// </summary>
        public static bool AutoTranslatorInstalled { get; }

        static TranslationHelper()
        {
            var xua = Type.GetType("XUnity.AutoTranslator.Plugin.Core.ITranslator, XUnity.AutoTranslator.Plugin.Core", false);
            if (xua != null)
            {
                //bool TryTranslate(string untranslatedText, out string translatedText);
                var tlM = AccessTools.Method(xua, "TryTranslate", new Type[] { typeof(string), typeof(string).MakeByRefType() });
                if (tlM != null)
                {
                    var inst = AccessTools.Property(Type.GetType("XUnity.AutoTranslator.Plugin.Core.AutoTranslator, XUnity.AutoTranslator.Plugin.Core"), "Default").GetValue(null, null);
                    if (inst != null)
                    {
                        var argarr = new object[2];
                        _tryTranslateCallback = s =>
                        {
                            argarr[0] = s;
                            argarr[1] = null;
                            tlM.Invoke(inst, argarr);
                            return argarr[1] as string;
                        };
                        AutoTranslatorInstalled = true;
                        return;
                    }
                }
            }

            _tryTranslateCallback = null;
        }

        /// <summary>
        /// Queries the plugin to provide a translated text for the untranslated text.
        /// If the translation cannot be found in the cache, the method returns false
        /// and returns null as the untranslated text.
        /// </summary>
        /// <param name="untranslatedText">The untranslated text to provide a translation for.</param>
        /// <param name="translatedText">The translated text.</param>
        public static bool TryTranslate(string untranslatedText, out string translatedText)
        {
            if (string.IsNullOrEmpty(untranslatedText) || _tryTranslateCallback == null)
            {
                translatedText = null;
                return false;
            }

            translatedText = _tryTranslateCallback(untranslatedText);
            return translatedText != null;
        }
    }
}