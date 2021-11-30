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

using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ILMerge.MsBuild.Task
{
    static class ExeLocationHelper
    {

        public static bool TryILMergeInSolutionDir(string solutionDir, out string executablePath)
        {

            executablePath = null;
            string basePath = Path.Combine(solutionDir, "packages");

            if(!Directory.Exists(basePath))
            {
                return false;
            }

            // retrieve all ILMerge directories (can have multiple versions)
            IEnumerable<string> dirs = Directory.EnumerateDirectories(basePath, "ILMerge.*", SearchOption.TopDirectoryOnly);
            if (dirs == null || dirs.Count() == 0)
            {
                return false;
            }

            // sort by verion number and retrieve latest
            string latestVersionDir = Directory.EnumerateDirectories(basePath, "ILMerge.*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(d => ExtractVersionFromName(Path.GetFileName(d)))
                .FirstOrDefault();

            // check presence of executable
            if (TryValidateILMergePath(latestVersionDir, out executablePath))
            {
                return true;
            }

            executablePath = null;
            return false;

        }

        public static string ExtractVersionFromName(string nameWithVersion)
        {

            Regex patern = new Regex(@"\.\d+(\.\d+)+", RegexOptions.RightToLeft);
            Match m = patern.Match(nameWithVersion);
            string version = m.Value.Replace(".", "");

            return version;

        }

        public static bool TryValidateILMergePath(string basePath, out string executablePath)
        {

            executablePath = null;
            string fullPath = Path.Combine(basePath, @"ILMerge.exe");

            if (File.Exists(fullPath))
            {
                executablePath = fullPath;
                return true;
            }

            fullPath = Path.Combine(basePath, "tools", @"ILMerge.exe");
            if (File.Exists(fullPath))
            {
                executablePath = fullPath;
                return true;
            }

            return false;

        }

        public static bool TryLocatePackagesFolder(TaskLoggingHelper logger, out string executablePath)
        {

            executablePath = null;
            var taskLibPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = new DirectoryInfo(taskLibPath);

            DirectoryInfo root = null;
            DirectoryInfo[] subDirs = null;

            logger.LogMessage("Task lib location: {0}", dir.FullName);            

            if (dir.Parent == null)
            {
                logger.LogWarning("Unable to determine parent folder.");
                return false;
            }

            if (dir.Parent.Parent == null)
            {
                logger.LogWarning("Unable to determine root folder.");
                return false;   
            }

            root = dir.Parent.Parent;
            subDirs = root.GetDirectories("ILMerge.*", SearchOption.TopDirectoryOnly);

            logger.LogMessage("Package location: {0}", root.FullName);

            if (subDirs == null || subDirs.Count() == 0)
            {
                logger.LogWarning("No folder starting with 'ILMerge' were found under {0}.", root.FullName);
                return false;
            }

            foreach (var item in subDirs)
            {
                
                var files = item.GetFiles("ILMerge.exe", SearchOption.AllDirectories);
                if(files != null && files.Any())
                {
                    logger.LogMessage("Executable found by dynamic searach at: {0}", item.FullName);
                    executablePath = files[0].FullName;
                    return true;
                }
            }

            return false;

        }


    }
}
