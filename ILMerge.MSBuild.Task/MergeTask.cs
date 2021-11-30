#region MIT License
/*
    MIT License

    Copyright (c) 2016 Emerson Brito

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
 */
#endregion

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ILMerge.MsBuild.Task
{

    // project properties
    // http://stackoverflow.com/questions/2772426/how-to-access-the-msbuild-s-properties-list-when-coding-a-custom-task


    // dependencies
    // http://stackoverflow.com/questions/8849289/get-dependent-assemblies

    public class MergeTask : Microsoft.Build.Utilities.Task
    {

        #region Public Properties

        public virtual ITaskItem[] InputAssemblies { get; set; }

        public string[] AdditionalLocations { get; set; }

        public string[] AllNone { get; set; }

        [Required]
        public string SolutionDir { get; set; }

        public string SolutionPath { get; set; }

        [Required]
        public string ProjectDir { get; set; }

        public string ProjectFileName { get; set; }

        public string ProjectPath { get; set; }

        [Required]
        public string TargetDir { get; set; }

        public string TargetPath { get; set; }

        public string TargetFileName { get; set; }

        [Required]
        public string TargetFrameworkVersion { get; set; }

        public string TargetArchitecture { get; set; }

        public string ILMergeConsolePath { get; set; }

        public string KeyFile { get; set; }

        #endregion

        #region Constructors

        public MergeTask()
        {
            this.InputAssemblies = new ITaskItem[0];
        }

        #endregion

        #region Public Methods

        public override bool Execute()
        {            
            LogInputVariables();

            string jsonConfig;
            string exePath;

            var settings = new MergerSettings();

            // try to read configuration if file exists
            if (!ReadConfigFile(out jsonConfig))
            {
                return false;
            }

            // replace tokens if applicable
            if (!string.IsNullOrWhiteSpace(jsonConfig))
            {
                jsonConfig = ReplaceTokens(jsonConfig);
            }

            // if json config exists, try to deserialize into settings object
            if (!string.IsNullOrWhiteSpace(jsonConfig) && !DeserializeJsonConfig(jsonConfig, out settings))
            {
                return false;
            }

            if (settings == null)
            {
                // create instance if seetings still null which indicates a custom json config was not used
                settings = new MergerSettings();
            }

            // apply defaults
            SetDefaults(settings);

            if (settings.General.AlternativeILMergePath.HasValue())
            {
                if (!File.Exists(settings.General.AlternativeILMergePath))
                {
                    Log.LogError($"An alternative path for ILMerge.exe was provided but the file was not found: {settings.General.AlternativeILMergePath}");
                    return false;
                }
                else
                {
                    exePath = settings.General.AlternativeILMergePath;
                    Log.LogMessage("Using alternative ILMerge path: {0}", settings.General.AlternativeILMergePath);
                }
            }
            else
            {
                exePath = this.GetILMergePath();
            }

            if (!exePath.HasValue())
            {
                Log.LogError("ILMerge.exe was no located. Make sure you have the ILMerge nuget package installed. "
                    + "If you defined a custom packages folder in your Nuget.Config it is possible we are having a hard time figuring it out. "
                    + "In this case please use attribute 'AlternativeILMergePath' in the configuration file to indicate the full path for ILMerge.exe.");
                return false;
            }

            // log configuration file used by this task.
            LogConfigFile(settings);

            if (!MergeAssemblies(exePath, settings))
            {
                return false;
            }


            return true;

        }

        #endregion

        #region Private Methods

        private void LogInputVariables()
        {

            Log.LogMessage($"SolutionDir: {SolutionDir}");
            Log.LogMessage($"SolutionPath: {SolutionPath}");
            Log.LogMessage($"ProjectDir: {ProjectDir}");
            Log.LogMessage($"ProjectFileName: {ProjectFileName}");
            Log.LogMessage($"ProjectPath: {ProjectPath}");
            Log.LogMessage($"TargetDir: {TargetDir}");
            Log.LogMessage($"TargetPath: {TargetPath}");
            Log.LogMessage($"TargetFileName: {TargetFileName}");
            Log.LogMessage($"TargetFrameworkVersion: {TargetFrameworkVersion}");
            Log.LogMessage($"TargetArchitecture: {TargetArchitecture}");
            Log.LogMessage($"ILMergeConsolePath: {ILMergeConsolePath}");
            Log.LogMessage($"KeyFile: {KeyFile}");

        }

        private bool DeserializeJsonConfig(string jsonConfig, out MergerSettings settings)
        {

            settings = null;
            bool success = true;

            if (string.IsNullOrWhiteSpace(jsonConfig))
            {
                Log.LogError("Unable to deserialize configuration. Configuration string is null or empty.");
                return false;
            }

            try
            {
                Log.LogMessage("Deserializing configuration.");
                settings = MergerSettings.FromJson(jsonConfig);
                Log.LogMessage("Configuration file deserialized successfully.");
            }
            catch (Exception ex)
            {
                Log.LogError("Error deserializing configuration.");
                Log.LogErrorFromException(ex);
                success = false;
            }

            return success;

        }

        private string ReplaceTokens(string jsonConfig)
        {
            return jsonConfig
                .Replace("$(SolutionDir)", EscapePath(this.SolutionDir))
                .Replace("$(SolutionPath)", EscapePath(this.SolutionPath))
                .Replace("$(ProjectDir)", EscapePath(this.ProjectDir))
                .Replace("$(ProjectFileName)", EscapePath(this.ProjectFileName))
                .Replace("$(ProjectPath)", EscapePath(this.ProjectPath))
                .Replace("$(TargetDir)", EscapePath(this.TargetDir))
                .Replace("$(TargetPath)", EscapePath(this.TargetPath))
                .Replace("$(TargetFileName)", EscapePath(this.TargetFileName))
                .Replace("$(AssemblyOriginatorKeyFile)", EscapePath(this.KeyFile));
        }

        private void SetDefaults(MergerSettings settings)
        {

            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.General == null)
            {
                settings.General = new GeneralSettings();
            }

            if (!settings.General.KeyFile.HasValue() && this.KeyFile.HasValue())
            {
                settings.General.KeyFile = ToAbsolutePath(KeyFile);
                Log.LogMessage("Applying default value for KeyFile.");
            }

            if (!settings.General.OutputFile.HasValue())
            {
                settings.General.OutputFile = Path.Combine(this.TargetDir, "ILMerge", this.TargetFileName);
                Log.LogMessage("Applying default value for OutputFile.");
            }

            if (!settings.General.TargetPlatform.HasValue())
            {
                settings.General.TargetPlatform = FrameworkInfo.ToILmergeTargetPlatform(this.TargetFrameworkVersion, this.TargetArchitecture);
                if (settings.General.TargetPlatform.Equals(@"v2,C:\Windows\Microsoft.NET\Framework\v3.5", StringComparison.OrdinalIgnoreCase))
                    settings.General.TargetPlatform = @"v2,C:\Windows\Microsoft.NET\Framework\v2.0.50727";
                Log.LogMessage($"Applying default value for TargetPlatform: {settings.General.TargetPlatform}");
            }

            if (settings.General.InputAssemblies == null || settings.General.InputAssemblies.Count == 0)
            {
                Log.LogMessage("No input assembles were found in configuration.");

                settings.General.InputAssemblies = new List<string>();

                Log.LogMessage($"Adding target assembly: {this.TargetPath}");
                settings.General.InputAssemblies.Add(this.TargetPath);

                foreach (var item in this.InputAssemblies)
                {
                    Log.LogMessage($"Adding assembly: {item.ItemSpec}");
                    settings.General.InputAssemblies.Add(item.ItemSpec);
                }

            }
            else
            {
                foreach (var item in settings.General.InputAssemblies)
                {
                    Log.LogMessage($"Config input assembly: {item}");
                }

                Log.LogMessage($"Adding target assembly at position [0]: {this.TargetPath}");
                settings.General.InputAssemblies.Insert(0, this.TargetPath);
            }

            if (settings.Advanced == null)
            {
                settings.Advanced = new AdvancedSettings();
            }

            if (settings.Advanced.SearchDirectories == null)
            {
                settings.Advanced.SearchDirectories = new List<string>();
            }

            if (!settings.Advanced.SearchDirectories.Contains(this.TargetDir))
            {
                settings.Advanced.SearchDirectories.Add(this.TargetDir);
            }

            if (this.AdditionalLocations != null && this.AdditionalLocations.Any())
            {
                foreach (var dir in this.AdditionalLocations)
                {
                    if (!settings.Advanced.SearchDirectories.Contains(dir))
                    {
                        settings.Advanced.SearchDirectories.Add(dir);
                    }
                }
            }
        }

        private bool MergeAssemblies(string mergerPath, MergerSettings settings)
        {
            bool success = true;

            if (File.Exists(mergerPath) == false) 
                throw new InvalidOperationException("Cannot find 'ILMerging.ILMerge' in executable.");

            Log.LogMessage("Instantianting ILMerge.");
            StringBuilder commandLine = new StringBuilder();

            Log.LogMessage("Setting up ILMerge.");

            foreach (string directory in settings.Advanced.SearchDirectories)
                commandLine.Append($" /lib={this.EscapePathForCommandLine(directory)}");
            if (settings.Advanced.Log)
            {
                commandLine.Append(" /log");
                if (string.IsNullOrEmpty(settings.Advanced.LogFile) == false)
                    commandLine.Append($":{this.EscapePathForCommandLine(settings.Advanced.LogFile)}");
            }
            if (string.IsNullOrEmpty(settings.General.KeyFile) == false)
            {
                commandLine.Append($" /keyfile:{this.EscapePathForCommandLine(settings.General.KeyFile)}");
                if (settings.Advanced.DelaySign)
                    commandLine.Append(" /delaysign");
            }
            if (settings.Advanced.Internalize)
            {
                commandLine.Append(" /internalize");
                if (!string.IsNullOrWhiteSpace(settings.Advanced.ExcludeFile))
                    commandLine.Append($":{this.EscapePathForCommandLine(settings.Advanced.ExcludeFile)}");
            }
            if (settings.Advanced.TargetKind.HasValue())
                commandLine.Append($" /target:{settings.Advanced.TargetKind}");
            if (settings.Advanced.Closed)
                commandLine.Append(" /closed");
            if (settings.Advanced.DebugInfo == false)
                commandLine.Append(" /ndebug");
            if (settings.Advanced.Version.HasValue())
                commandLine.Append($" /ver:{settings.Advanced.Version}");
            if (settings.Advanced.CopyAttributes)
            {
                commandLine.Append(" /copyattrs");
                if (settings.Advanced.AllowMultipleAssemblyLevelAttributes)
                    commandLine.Append(" /allowMultiple");
            }
            if (settings.Advanced.XmlDocumentation)
                commandLine.Append(" /xmldocs");
            if (string.IsNullOrEmpty(settings.Advanced.AttributeFile) == false)
                commandLine.Append($" /attr:{this.EscapePathForCommandLine(settings.Advanced.AttributeFile)}");
            if (string.IsNullOrEmpty(settings.General.TargetPlatform) == false)
                commandLine.Append($" /targetplatform:{settings.General.TargetPlatform}");
            if (settings.Advanced.PublicKeyTokens)
                commandLine.Append(" /useFullPublicKeyForReferences");
            if (settings.Advanced.AllowZeroPeKind)
                commandLine.Append(" /zeroPeKind");
            if (settings.Advanced.AllowWildCards)
                commandLine.Append(" /wildcards");
            if (settings.Advanced.AllowDuplicateType.HasValue())
            {
                if (settings.Advanced.AllowDuplicateType == "*")
                    commandLine.Append(" /allowDup");
                else
                {
                    foreach (string typeName in settings.Advanced.AllowDuplicateType.Split(','))
                        commandLine.Append($" /allowDup:{typeName}");
                }
            }
            if (settings.Advanced.UnionMerge)
                commandLine.Append(" /union");
            commandLine.Append($" /align:{(settings.Advanced.FileAlignment > 0 ? settings.Advanced.FileAlignment : 512)}");

            if (string.IsNullOrEmpty(settings.General.OutputFile) == false)
                commandLine.Append($" /out:{this.EscapePathForCommandLine(settings.General.OutputFile)}");

            foreach (string assembly in settings.General.InputAssemblies)
                commandLine.Append($" {this.EscapePathForCommandLine(assembly)}");

            try
            {

                string outputDir = Path.GetDirectoryName(settings.General.OutputFile);
                if (!Directory.Exists(outputDir))
                {
                    Log.LogWarning($"Output directory not found. An attempt to create the directory will be made: {outputDir}");
                    Directory.CreateDirectory(outputDir);
                    Log.LogMessage("Output directory created.");
                }

                Log.LogMessage(MessageImportance.High,
                    "Merging {0} assembl{1} to '{2}'.",
                    settings.General.InputAssemblies.Count.ToString(),
                    (settings.General.InputAssemblies.Count != 1) ? "ies" : "y",
                    settings.General.OutputFile);

                foreach (string assembly in settings.General.InputAssemblies)
                {
                    if (assembly != settings.General.OutputFile)
                        Log.LogMessage(MessageImportance.High, assembly);
                }
                Log.LogMessage(MessageImportance.High, "Arguments: " + commandLine.ToString());
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = mergerPath,
                        Arguments = commandLine.ToString(),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Directory.GetCurrentDirectory(),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(output) == false)
                    Log.LogMessage(MessageImportance.High, "ILMerge: " + output);
                if (string.IsNullOrEmpty(error) == false)
                    Log.LogError("ILMerge: " + error);

            }
            catch (Exception exception)
            {
                Log.LogErrorFromException(exception);
                success = false;
            }

            if (settings.Advanced.DeleteCopiesOverwriteTarget)            
                DeleteCopiesAndCopyTargetPath(settings);            

            return success;

        }

        private string EscapePathForCommandLine(string path)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            if (path.StartsWith(currentDirectory))
                path = path.Remove(0, currentDirectory.Length + 1);
            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            if (path.Contains(' '))
                path = "\"" + path + "\"";
            return path;
        }

        private void DeleteCopiesAndCopyTargetPath(MergerSettings settings)
        {
            // Delete input assemblies and copy the merged outputfile to the TargetPath

            Log.LogMessage(MessageImportance.Normal, "Deleting Merged Assembl{0}",
                (settings.General.InputAssemblies.Count != 1) ? "ies" : "y");

            foreach (string file in settings.General.InputAssemblies)
            {
                try
                {
                    // Input can reference dependency project, make sure the assembly is removed from the TargetDir                    
                    DeleteFileAndPDB(Path.Combine(this.TargetDir, Path.GetFileName(file)));
                }
                catch (Exception ex)
                {
                    Log.LogErrorFromException(ex);
                }
            }

            // Copy the output-file to the target-dir
            Log.LogMessage(MessageImportance.Normal, "Overwriting OutputTarget");
            try
            {
                // Remove and copy Target
                DeleteFileAndPDB(this.TargetPath);

                if (File.Exists(settings.General.OutputFile))
                    File.Copy(settings.General.OutputFile, this.TargetPath);

                // Also copy debug-information
                string outputFilePDB = Path.ChangeExtension(settings.General.OutputFile, "pdb");
                if (File.Exists(outputFilePDB))
                    File.Copy(outputFilePDB, Path.ChangeExtension(this.TargetPath, "pdb"));
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }
        }

        private static void DeleteFileAndPDB(string targetFile)
        {
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            // Also do debug-output, if existing
            string targetFilePDB = Path.ChangeExtension(targetFile, "pdb");
            if (File.Exists(targetFilePDB))
                File.Delete(targetFilePDB);
        }

        private Assembly LoadILMerge(string mergerPath)
        {

            if (string.IsNullOrWhiteSpace(mergerPath)) throw new ArgumentNullException(nameof(mergerPath));

            Log.LogMessage($"Loading ILMerge from '{mergerPath}'.");

            Assembly ilmerge = Assembly.LoadFrom(mergerPath);

            return ilmerge;

        }

        private bool ReadConfigFile(out string jsonConfig)
        {

            jsonConfig = string.Empty;
            bool success = true;
            Regex regex = new Regex(@"ILMergeConfig.*?\.json");

            List<string> configFiles = this.AllNone.Where(f => regex.IsMatch(f)).ToList();

            if (configFiles.Count == 0)
            {
                Log.LogWarning("Path for configuration file is empty. Default values will be applied.");
            }
            else
            {
                if (configFiles.Count > 1)
                    Log.LogWarning("Multiple configuration files are present in the project, first one will be used.");
                string configurationFilePath = configFiles[0];
                if (!File.Exists(configurationFilePath))
                {
                    Log.LogMessage($"Using default configuration. A custom configuration file was not found at: {configurationFilePath}");
                }
                else
                {
                    try
                    {

                        Log.LogMessage($"Loading configuration from: {configurationFilePath}");
                        jsonConfig = File.ReadAllText(configurationFilePath, Encoding.UTF8);
                        Log.LogMessage($"Configuration file loaded successfully.");

                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Error reading configuration file.");
                        Log.LogErrorFromException(ex);
                        success = false;
                    }
                }
            }

            return success;

        }

        public string GetILMergePath()
        {

            string exePath = null;
            string errMsg;
            var failedPaths = new List<string>();

            // Look at property containing ILMerge's full path. Supported in ILMerge nuget package 3.0.17 and above.
            if (string.IsNullOrWhiteSpace(ILMergeConsolePath))
            {
                Log.LogWarning("Variable $(ILMergeConsolePath) is not available. For a better experience please make sure you are using the latest version of ilmerge's nuget pakcage.");
            }
            else
            {
                exePath = Path.GetFullPath(ILMergeConsolePath);
                Log.LogMessage($"ILMerge.exe found at $(ILMergeConsolePath): {exePath}");
                return exePath;
            }

            // look at same directory as this assembly (task dll);
            if (ExeLocationHelper.TryValidateILMergePath(Path.GetDirectoryName(this.GetType().Assembly.Location), out exePath))
            {
                Log.LogMessage($"ILMerge.exe found at (task location): {Path.GetDirectoryName(this.GetType().Assembly.Location)}");
                return exePath;
            }
            else
            {
                errMsg = $"ILMerge.exe not found at (task location): {Path.GetDirectoryName(this.GetType().Assembly.Location)}";
                failedPaths.Add(errMsg);
                Log.LogMessage(errMsg);
            }

            // look at target dir;
            if (!string.IsNullOrWhiteSpace(TargetDir))
            {
                if (ExeLocationHelper.TryValidateILMergePath(this.TargetDir, out exePath))
                {
                    Log.LogMessage($"ILMerge.exe found at (target dir): {this.TargetDir}");
                    return exePath;
                }
                else
                {
                    errMsg = $"ILMerge.exe not found at (target dir): {this.TargetDir}";
                    failedPaths.Add(errMsg);
                    Log.LogMessage(errMsg);
                }
            }

            // look for "packages" folder at the solution root and if one is found, look for ILMerge package folder
            if (!string.IsNullOrWhiteSpace(this.SolutionDir))
            {
                if (ExeLocationHelper.TryILMergeInSolutionDir(this.SolutionDir, out exePath))
                {
                    Log.LogMessage($"ILMerge.exe found at (solution dir): {this.SolutionDir}");
                    return exePath;
                }
                {
                    errMsg = $"ILMerge.exe not found at (solution dir): {this.SolutionDir}";
                    failedPaths.Add(errMsg);
                    Log.LogMessage(errMsg);
                }
            }

            // get the location of the this assembly (task dll) and assumes it is under the packages folder.
            // use this information to determine the possible location of the executable.
            if (ExeLocationHelper.TryLocatePackagesFolder(Log, out exePath))
            {
                Log.LogMessage($"ILMerge.exe found at custom package location: {exePath}");
                return exePath;
            }
            {

                foreach (var err in failedPaths)
                {
                    Log.LogWarning(err);
                }

                Log.LogWarning($"Unable to determine custom package location or, location was determined but an ILMerge package folder was not found.");

            }

            return exePath;

        }

        private string ToAbsolutePath(string relativePath)
        {

            // if path is not rooted assume it is relative.
            // convert relative to absolute using project dir as root.

            if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentNullException(relativePath);

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.GetFullPath(Path.Combine(ProjectDir, relativePath));

        }

        private void LogConfigFile(MergerSettings settings)
        {

            string outputPath = Path.Combine(Path.GetDirectoryName(settings.General.OutputFile), Path.GetFileNameWithoutExtension(settings.General.OutputFile) + ".merge.json");
            string outputDir = Path.GetDirectoryName(outputPath);

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            Log.LogMessage("Saving current configuration to: {0}", outputPath);
            File.WriteAllText(outputPath, settings.ToJson(), Encoding.UTF8);
        }

        private string EscapePath(string path)
        {
            return Regex.Replace(path ?? "", @"\\", @"\\");
        }

        #endregion

    }
}


