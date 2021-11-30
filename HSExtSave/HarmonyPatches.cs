using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using Harmony;
#if PLAYHOME
using SEXY;
#endif
using Studio;
using UnityEngine;

namespace HSExtSave
{
#if HONEYSELECT
    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("SaveWithoutPNG")]
    [HarmonyPatch(new[] {typeof(BinaryWriter)})]
    public class CharFile_SaveWithoutPNG_Patches
    {
        public static bool Prepare()
        {
            return HSExtSave._binary == HSExtSave.Binary.Game;
        }

        public static void Postfix(CharFile __instance, BinaryWriter writer)
        {

            Debug.Log(HSExtSave._logPrefix + "Saving extended data for character...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(writer.BaseStream, Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("charExtData");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                {
                    if (kvp.Value.onCharWrite == null)
                        continue;
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onCharWrite(__instance, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                // Checking if xml is well formed
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.LoadXml(stringWriter.ToString());

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during character saving. The exception was: " + e);
                            }
                        }
                    }
                }
                xmlWriter.WriteEndElement();
            }
            Debug.Log(HSExtSave._logPrefix + "Saving done.");
        }
    }

    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("Load")]
    [HarmonyPatch(new[] {typeof(BinaryReader), typeof(Boolean), typeof(Boolean)})]
    public class CharFile_Load_Patches
    {
        public static void Postfix(CharFile __instance, BinaryReader reader, Boolean noSetPNG, Boolean noLoadStatus)
        {
            Debug.Log(HSExtSave._logPrefix + "Loading extended data for character...");
            long cachedPosition = reader.BaseStream.Position;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader.BaseStream);
                HashSet<HSExtSave.HandlerGroup> calledHandlers = new HashSet<HSExtSave.HandlerGroup>();

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "charExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onCharRead != null)
                                    {
                                        group.onCharRead(__instance, child);
                                        calledHandlers.Add(group);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + child.Name + "\" during character loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> handler in HSExtSave._handlers)
                {
                    if (handler.Value.onCharRead != null && calledHandlers.Contains(handler.Value) == false)
                        handler.Value.onCharRead(__instance, null);
                }

            }
            catch (XmlException)
            {
                Debug.Log(HSExtSave._logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onCharRead != null)
                        kvp.Value.onCharRead(__instance, null);
                reader.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }
    }
#endif

    [HarmonyPatch]
    public static class SceneInfo_Load_Patches
    {
        private static MethodInfo TargetMethod()
        {
            return typeof(SceneInfo).GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Load" && m.GetParameters().Length == 2);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod("Dispose", AccessTools.all);
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            foreach (CodeInstruction inst in instructionsList)
            {
                if (set == false && inst.opcode == OpCodes.Callvirt && inst.operand == disposeMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(SceneInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
                yield return inst;
            }
        }

        private static BinaryReader Injected(BinaryReader reader, string path)
        {
            reader.ReadString();
            UnityEngine.Debug.Log(HSExtSave._logPrefix + "Loading extended data for scene...");
            long cachedPosition = reader.BaseStream.Position;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader.BaseStream);
                HashSet<HSExtSave.HandlerGroup> calledHandlers = new HashSet<HSExtSave.HandlerGroup>();

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "sceneExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onSceneReadLoad != null)
                                    {
                                        group.onSceneReadLoad(path, child);
                                        calledHandlers.Add(group);
                                    }
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + child.Name + "\" during scene loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> handler in HSExtSave._handlers)
                {
                    if (handler.Value.onSceneReadLoad != null && calledHandlers.Contains(handler.Value) == false)
                        handler.Value.onSceneReadLoad(path, null);
                }
            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(HSExtSave._logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onSceneReadLoad != null)
                        kvp.Value.onSceneReadLoad(path, null);
                reader.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
            return reader;
        }
    }

    [HarmonyPatch(typeof(SceneInfo), "Import", new[] { typeof(string) })]
    public static class SceneInfo_Import_Patches
    {

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod("Dispose", AccessTools.all);
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                if (set == false && inst.opcode == OpCodes.Callvirt && inst.operand == disposeMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
#if HONEYSELECT
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
#elif PLAYHOME
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
#endif
                    yield return new CodeInstruction(OpCodes.Call, typeof(SceneInfo_Import_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
                yield return inst;
            }
        }

        private static BinaryReader Injected(BinaryReader binaryReader, string path, Version version)
        {
            //Reading useless data
#if HONEYSELECT
            binaryReader.ReadInt32();
            if (version.CompareTo(new Version(1, 0, 3)) >= 0)
            {
                binaryReader.ReadSingle(); binaryReader.ReadSingle(); binaryReader.ReadSingle();
                binaryReader.ReadSingle(); binaryReader.ReadSingle(); binaryReader.ReadSingle();
                binaryReader.ReadSingle(); binaryReader.ReadSingle(); binaryReader.ReadSingle();
            }
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            int version2 = binaryReader.ReadInt32();

            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();

            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadBoolean();

            int num = binaryReader.ReadInt32();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            if (num == 1)
            {
                binaryReader.ReadSingle();
            }
            else
            {
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
            }
            binaryReader.ReadSingle();
            for (int j = 0; j < 10; j++)
            {
                num = binaryReader.ReadInt32();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                if (num == 1)
                {
                    binaryReader.ReadSingle();
                }
                else
                {
                    binaryReader.ReadSingle();
                    binaryReader.ReadSingle();
                    binaryReader.ReadSingle();
                }
                binaryReader.ReadSingle();
            }

            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();
            binaryReader.ReadDouble();

            binaryReader.ReadSingle();
            if (version.CompareTo(new Version(0, 1, 3)) >= 0)
            {
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
            }
            if (version.CompareTo(new Version(1, 0, 1)) >= 0)
            {
                binaryReader.ReadBoolean();
            }
            binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            binaryReader.ReadBoolean();

            binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            binaryReader.ReadBoolean();

            binaryReader.ReadInt32();
            binaryReader.ReadString();
            binaryReader.ReadBoolean();
            if (version.CompareTo(new Version(1, 0, 3)) >= 0)
            {
                binaryReader.ReadString();
            }

            binaryReader.ReadString();

#elif PLAYHOME
            binaryReader.ReadInt32();
            binaryReader.ReadString();
            binaryReader.ReadString();
            binaryReader.ReadString();
            if (version.CompareTo(new Version(0, 1, 3)) >= 0)
            {
                binaryReader.ReadInt32();
            }
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadString();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            binaryReader.ReadSingle();

            int num = binaryReader.ReadInt32();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            if (num == 1)
            {
                binaryReader.ReadSingle();
            }
            else
            {
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
            }
            binaryReader.ReadSingle();
            for (int j = 0; j < 10; j++)
            {
                num = binaryReader.ReadInt32();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                if (num == 1)
                {
                    binaryReader.ReadSingle();
                }
                else
                {
                    binaryReader.ReadSingle();
                    binaryReader.ReadSingle();
                    binaryReader.ReadSingle();
                }
                binaryReader.ReadSingle();
            }
            binaryReader.ReadString();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadBoolean();
            if (version.CompareTo(new Version(1, 1, 0)) >= 0)
            {
                binaryReader.ReadInt32();
            }
            binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            binaryReader.ReadBoolean();
            binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            binaryReader.ReadBoolean();
            binaryReader.ReadInt32();
            binaryReader.ReadString();
            binaryReader.ReadBoolean();
            binaryReader.ReadString();
            if (version.CompareTo(new Version(1, 1, 0)) >= 0)
            {
                binaryReader.ReadInt32();
            }
            binaryReader.ReadString();
#endif

            UnityEngine.Debug.Log(HSExtSave._logPrefix + "Loading extended data for scene (import)...");

            long cachedPosition = binaryReader.BaseStream.Position;
            //byte[] buffer = new byte[4096];
            //int count = binaryReader.BaseStream.Read(buffer, 0, 4096);
            //byte[] b = new byte[count];
            //Array.Copy(buffer, b, count);
            //UnityEngine.Debug.Log(System.Text.Encoding.UTF8.GetString(b));
            //UnityEngine.Debug.Log(count);
            //File.WriteAllBytes("./bite.txt", b);
            //binaryReader.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(binaryReader.BaseStream);
                HashSet<HSExtSave.HandlerGroup> calledHandlers = new HashSet<HSExtSave.HandlerGroup>();

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "sceneExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onSceneReadImport != null)
                                    {
                                        group.onSceneReadImport(path, child);
                                        calledHandlers.Add(group);
                                    }
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + child.Name + "\" during scene import. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> handler in HSExtSave._handlers)
                {
                    if (handler.Value.onSceneReadImport != null && calledHandlers.Contains(handler.Value) == false)
                        handler.Value.onSceneReadImport(path, null);
                }

            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(HSExtSave._logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onSceneReadImport != null)
                        kvp.Value.onSceneReadImport(path, null);
                binaryReader.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
            return binaryReader;
        }
    }

    [HarmonyPatch]
    public static class SceneInfo_Save_Patches
    {
        private static MethodInfo TargetMethod()
        {
#if PLAYHOME
            //Fuck you plasticmind.
            Type phiblSaveClass = Type.GetType("PHIBL.Patch.SceneSavePatch,PHIBL");
            if (phiblSaveClass != null)
                return phiblSaveClass.GetMethod("Prefix", AccessTools.all);
#endif
            return AccessTools.Method(typeof(SceneInfo), "Save", new[] {typeof(string)});
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod("Dispose", AccessTools.all);
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            foreach (CodeInstruction inst in instructionsList)
            {
                if (set == false && inst.opcode == OpCodes.Callvirt && inst.operand == disposeMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(SceneInfo_Save_Patches).GetMethod(nameof(Injected), BindingFlags.Static | BindingFlags.NonPublic));
                    set = true;
                }
                yield return inst;
            }
        }

        private static BinaryWriter Injected(BinaryWriter writer, string path)
        {
            UnityEngine.Debug.Log(HSExtSave._logPrefix + "Saving extended data for scene...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(writer.BaseStream, System.Text.Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("sceneExtData");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                {
                    if (kvp.Value.onSceneWrite == null)
                        continue;
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onSceneWrite(path, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                // Checking if xml is well formed
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.LoadXml(stringWriter.ToString());

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during scene saving. The exception was: " + e);
                            }
                        }
                    }
                }
                xmlWriter.WriteEndElement();
            }
            UnityEngine.Debug.Log(HSExtSave._logPrefix + "Saving done.");
            return writer;
        }
    }


#if HONEYSELECT
    [HarmonyPatch(typeof(CharFileInfoClothes), "Load", new[] { typeof(BinaryReader), typeof(bool) })]
    public class CharFileInfoClothesFemale_LoadSub_Patches
    {
        public static void Postfix(CharFileInfoClothes __instance, BinaryReader br, bool noSetPng = false)
        {
            CharFileInfoClothes_Extensions.Load(__instance, br);
        }
    }

    [HarmonyPatch(typeof(CharFileInfoClothes), "Save", new[] { typeof(string) })]
    public class CharFileInfoClothesMale_Save_Patches
    {
        public static bool Prepare()
        {
            return HSExtSave._binary == HSExtSave.Binary.Game;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Call && instructionsList[i + 1].opcode == OpCodes.Stloc_S)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, typeof(CharFileInfoClothesMale_Save_Patches).GetMethod(nameof(Injected)));
                    set = true;
                }
            }
        }

        public static void Injected(CharFileInfoClothes __instance, BinaryWriter bw)
        {
            CharFileInfoClothes_Extensions.Save(__instance, bw);
        }
    }

    public class CharFileInfoClothes_Extensions
    {
        public static void Load(CharFileInfoClothes __instance, BinaryReader br)
        {
            Debug.Log(HSExtSave._logPrefix + "Loading extended data for coordinate...");
            long cachedPosition = br.BaseStream.Position;
            //br.ReadInt64();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(br.BaseStream);
                HashSet<HSExtSave.HandlerGroup> calledHandlers = new HashSet<HSExtSave.HandlerGroup>();

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "clothesExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onClothesRead != null)
                                    {
                                        group.onClothesRead(__instance, child);
                                        calledHandlers.Add(group);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + child.Name + "\" during coordinate loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> handler in HSExtSave._handlers)
                {
                    if (handler.Value.onClothesRead != null && calledHandlers.Contains(handler.Value) == false)
                        handler.Value.onClothesRead(__instance, null);
                }

            }
            catch (XmlException)
            {
                Debug.Log(HSExtSave._logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onClothesRead != null)
                        kvp.Value.onClothesRead(__instance, null);
                br.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }

        public static void Save(CharFileInfoClothes __instance, BinaryWriter bw)
        {
            Debug.Log(HSExtSave._logPrefix + "Saving extended data for coordinates...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(bw.BaseStream, Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("clothesExtData");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                {
                    if (kvp.Value.onClothesWrite == null)
                        continue;
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onClothesWrite(__instance, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                // Checking if xml is well formed
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.LoadXml(stringWriter.ToString());

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(HSExtSave._logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during coordinates saving. The exception was: " + e);
                            }
                        }
                    }
                }
                xmlWriter.WriteEndElement();
            }
            Debug.Log(HSExtSave._logPrefix + "Saving done.");
        }
    }
#endif
}