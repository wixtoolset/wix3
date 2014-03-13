//-------------------------------------------------------------------------------------------------
// <copyright file="FileSearchHelperMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the FileSearchHelperMethods class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Contains helper methods on searching for files
    /// </summary>
    public static class FileSearchHelperMethods
    {
        /// <summary>
        /// Searches for the existence of a file in multiple directories. 
        /// Search is satisfied if default file path is valid and exists. If not,
        /// file name is extracted from default path and combined with each of the directories
        /// looking to see if it exists. If not found, input default path is returned.
        /// </summary>
        /// <param name="directories">Array of directories to look in, without filenames in them</param>
        /// <param name="defaultFullPath">Default path - to use if not found</param>
        /// <returns>File path if file found. Empty string if not found</returns>
        public static string SearchFilePaths(string[] directories, string defaultFullPath)
        {
            if (String.IsNullOrEmpty(defaultFullPath))
            {
                return String.Empty;
            }

            if (File.Exists(defaultFullPath))
            {
                return defaultFullPath;
            }

            if (directories == null)
            {
                return string.Empty;
            }

            string fileName = Path.GetFileName(defaultFullPath);
            foreach (string currentPath in directories)
            {
                if (String.IsNullOrEmpty(currentPath) || String.IsNullOrEmpty(currentPath.Trim()))
                {
                    continue;
                }

                if (File.Exists(Path.Combine(currentPath, fileName)))
                {
                    return Path.Combine(currentPath, fileName);
                }
            }

            return String.Empty;
        }
    }
}
