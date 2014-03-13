//-------------------------------------------------------------------------------------------------
// <copyright file="WixCommandLineBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Improved CommandLineBuilder with additional calls.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Helper class for appending the command line arguments.
    /// </summary>
    public class WixCommandLineBuilder : CommandLineBuilder
    {
        internal const int Unspecified = -1;
        
        /// <summary>
        /// Append a switch to the command line if the value has been specified.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="value">Value specified by the user.</param>
        public void AppendIfSpecified(string switchName, int value)
        {
            if (value != Unspecified)
            {
                this.AppendSwitchIfNotNull(switchName, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Append a switch to the command line if the condition is true.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="condition">Condition specified by the user.</param>
        public void AppendIfTrue(string switchName, bool condition)
        {
            if (condition)
            {
                this.AppendSwitch(switchName);
            }
        }

        /// <summary>
        /// Append a switch to the command line if any values in the array have been specified.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="values">Values specified by the user.</param>
        public void AppendArrayIfNotNull(string switchName, ITaskItem[] values)
        {
            if (values != null)
            {
                foreach (ITaskItem value in values)
                {
                    this.AppendSwitchIfNotNull(switchName, value);
                }
            }
        }

        /// <summary>
        /// Append a switch to the command line if any values in the array have been specified.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="values">Values specified by the user.</param>
        public void AppendArrayIfNotNull(string switchName, string[] values)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    this.AppendSwitchIfNotNull(switchName, value);
                }
            }
        }

        /// <summary>
        /// Build the extensions argument. Each extension is searched in the current folder, user defined search 
        /// directories (ReferencePath), HintPath, and under Wix Extension Directory in that order.
        /// The order of precednce is based off of that described in Microsoft.Common.Targets's SearchPaths
        /// property for the ResolveAssemblyReferences task.
        /// </summary>
        /// <param name="extensions">The list of extensions to include.</param>
        /// <param name="wixExtensionDirectory">Evaluated default folder for Wix Extensions</param>
        /// <param name="referencePaths">User defined reference directories to search in</param>
        public void AppendExtensions(ITaskItem[] extensions, string wixExtensionDirectory, string [] referencePaths)
        {
            if (extensions == null)
            {
                return;
            }

            string resolvedPath;

            foreach (ITaskItem extension in extensions)
            {
                string className = extension.GetMetadata("Class");

                string fileName = Path.GetFileName(extension.ItemSpec);

                if (Path.GetExtension(fileName).Length == 0)
                {
                    fileName += ".dll";
                }

                // First try reference paths
                resolvedPath = FileSearchHelperMethods.SearchFilePaths(referencePaths, fileName);

                if (String.IsNullOrEmpty(resolvedPath))
                {
                    // Now try HintPath
                    resolvedPath = extension.GetMetadata("HintPath");

                    if (!File.Exists(resolvedPath))
                    {
                        // Now try the item itself
                        resolvedPath = extension.ItemSpec;

                        if (Path.GetExtension(resolvedPath).Length == 0)
                        {
                            resolvedPath += ".dll";
                        }

                        if (!File.Exists(resolvedPath))
                        {
                            if (!String.IsNullOrEmpty(wixExtensionDirectory))
                            {
                                // Now try the extension directory
                                resolvedPath = Path.Combine(wixExtensionDirectory, Path.GetFileName(resolvedPath));
                            }

                            if (!File.Exists(resolvedPath))
                            {
                                // Extesnion wasn't found, just set it to the extension name passed in
                                resolvedPath = extension.ItemSpec;
                            }
                        }
                    }
                }

                if (String.IsNullOrEmpty(className))
                {
                    this.AppendSwitchIfNotNull("-ext ", resolvedPath);
                }
                else
                {
                    this.AppendSwitchIfNotNull("-ext ", className + ", " + resolvedPath);
                }
            }
        }

        /// <summary>
        /// Append arbitrary text to the command-line if specified.
        /// </summary>
        /// <param name="textToAppend">Text to append.</param>
        public void AppendTextIfNotNull(string textToAppend)
        {
            if (!String.IsNullOrEmpty(textToAppend))
            {
                this.AppendSpaceIfNotEmpty();
                this.AppendTextUnquoted(textToAppend);
            }
        }
    }
}