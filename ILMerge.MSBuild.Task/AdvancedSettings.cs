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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ILMerge.MsBuild.Task
{

    public class AdvancedSettings
    {

        #region Property Wrappers

        public bool AllowMultipleAssemblyLevelAttributes { get; set; } = false;

        public bool AllowWildCards { get; set; } = false;

        public bool AllowZeroPeKind { get; set; } = false;

        public string AttributeFile { get; set; } = null;
        
        public bool Closed { get; set; } = false;

        public bool CopyAttributes { get; set; } = true;

        public bool DebugInfo { get; set; } = true;

        public bool DelaySign { get; set; } = false;

        public bool DeleteCopiesOverwriteTarget { get; set; } = false;

        public string ExcludeFile { get; set; } = "";

        public int FileAlignment { get; set; } = 512;

        public bool Internalize { get; set; } = false;

        public bool Log { get; set; } = false;

        public string LogFile { get; set; } = null;

        public bool PublicKeyTokens { get; set; } = true;

        public string TargetKind { get; set; } = null;

        public bool UnionMerge { get; set; } = false;

        public string Version { get; set; } = null;

        public bool XmlDocumentation { get; set; } = false;

        #endregion

        #region Method Wrappers

        /// <summary>
        /// Default is none. This option allows the user to either allow all public types to be renamed when they are duplicates, or to specify it for arbitrary type names.
        /// </summary>
        public string AllowDuplicateType { get; set; } = null;

        /// <summary>
        /// internal use
        /// </summary>
        public List<string> SearchDirectories { get; set; }

        #endregion

        #region Constructors

        public AdvancedSettings()
        {
            SearchDirectories = new List<string>();
        }

        #endregion

    }
}
