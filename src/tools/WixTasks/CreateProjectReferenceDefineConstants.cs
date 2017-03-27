// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// An MSBuild task to create a list of preprocessor defines to be passed to candle from the
    /// list of referenced projects.
    /// </summary>
    public sealed class CreateProjectReferenceDefineConstants : Task
    {
        private ITaskItem[] defineConstants;
        private ITaskItem[] projectConfigurations;
        private ITaskItem[] projectReferencePaths;

        [Output]
        public ITaskItem[] DefineConstants
        {
            get { return this.defineConstants; }
        }

        [Required]
        public ITaskItem[] ProjectReferencePaths
        {
            get { return this.projectReferencePaths; }
            set { this.projectReferencePaths = value; }
        }

        public ITaskItem[] ProjectConfigurations
        {
            get { return this.projectConfigurations; }
            set { this.projectConfigurations = value; }
        }

        public override bool Execute()
        {
            List<ITaskItem> outputItems = new List<ITaskItem>();
            Dictionary<string, string> defineConstants = new Dictionary<string, string>();

            for (int i = 0; i < this.ProjectReferencePaths.Length; i++)
            {
                ITaskItem item = this.ProjectReferencePaths[i];

                string configuration = item.GetMetadata("Configuration");
                string fullConfiguration = item.GetMetadata("FullConfiguration");
                string platform = item.GetMetadata("Platform");

                string projectPath = CreateProjectReferenceDefineConstants.GetProjectPath(this.ProjectReferencePaths, i);
                string projectDir = Path.GetDirectoryName(projectPath) + Path.DirectorySeparatorChar;
                string projectExt = Path.GetExtension(projectPath);
                string projectFileName = Path.GetFileName(projectPath);
                string projectName = Path.GetFileNameWithoutExtension(projectPath);

                string referenceName = CreateProjectReferenceDefineConstants.GetReferenceName(item, projectName);

                string targetPath = item.GetMetadata("FullPath");
                string targetDir = Path.GetDirectoryName(targetPath) + Path.DirectorySeparatorChar;
                string targetExt = Path.GetExtension(targetPath);
                string targetFileName = Path.GetFileName(targetPath);
                string targetName = Path.GetFileNameWithoutExtension(targetPath);

                // If there is no configuration metadata on the project reference task item,
                // check for any additional configuration data provided in the optional task property.
                if (String.IsNullOrEmpty(fullConfiguration))
                {
                    fullConfiguration = this.FindProjectConfiguration(projectName);
                    if (!String.IsNullOrEmpty(fullConfiguration))
                    {
                        string[] typeAndPlatform = fullConfiguration.Split('|');
                        configuration = typeAndPlatform[0];
                        platform = (typeAndPlatform.Length > 1 ? typeAndPlatform[1] : String.Empty);
                    }
                }

                // write out the platform/configuration defines
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.Configuration", referenceName)] = configuration;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.FullConfiguration", referenceName)] = fullConfiguration;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.Platform", referenceName)] = platform;

                // write out the ProjectX defines
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.ProjectDir", referenceName)] = projectDir;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.ProjectExt", referenceName)] = projectExt;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.ProjectFileName", referenceName)] = projectFileName;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.ProjectName", referenceName)] = projectName;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.ProjectPath", referenceName)] = projectPath;

                // write out the TargetX defines
                string targetDirDefine = String.Format(CultureInfo.InvariantCulture, "{0}.TargetDir", referenceName);
                if (defineConstants.ContainsKey(targetDirDefine))
                {
                    //if target dir was already defined, redefine it as the common root shared by multiple references from the same project
                    string commonDir = FindCommonRoot(targetDir, defineConstants[targetDirDefine]);
                    if (!String.IsNullOrEmpty(commonDir))
                    {
                        targetDir = commonDir;
                    }
                }
                defineConstants[targetDirDefine] = CreateProjectReferenceDefineConstants.EnsureEndsWithBackslash(targetDir);

                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.TargetExt", referenceName)] = targetExt;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.TargetFileName", referenceName)] = targetFileName;
                defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.TargetName", referenceName)] = targetName;

                //if target path was already defined, append to it creating a list of multiple references from the same project
                string targetPathDefine = String.Format(CultureInfo.InvariantCulture, "{0}.TargetPath", referenceName);
                if (defineConstants.ContainsKey(targetPathDefine))
                {
                    string oldTargetPath = defineConstants[targetPathDefine];
                    if (!targetPath.Equals(oldTargetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        defineConstants[targetPathDefine] += ";" + targetPath;
                    }

                    //If there was only one targetpath we need to create its culture specific define
                    if (!oldTargetPath.Contains(";"))
                    {
                        string oldSubFolder = FindSubfolder(oldTargetPath, targetDir, targetFileName);
                        if (!String.IsNullOrEmpty(oldSubFolder))
                        {
                            defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.{1}.TargetPath", referenceName, oldSubFolder.Replace('\\', '_'))] = oldTargetPath;
                        }
                    }

                    // Create a culture specific define
                    string subFolder = FindSubfolder(targetPath, targetDir, targetFileName);
                    if (!String.IsNullOrEmpty(subFolder))
                    {
                        defineConstants[String.Format(CultureInfo.InvariantCulture, "{0}.{1}.TargetPath", referenceName, subFolder.Replace('\\', '_'))] = targetPath;
                    }

                }
                else
                {
                    defineConstants[targetPathDefine] = targetPath;
                }
            }

            foreach (KeyValuePair<string, string> define in defineConstants)
            {
                outputItems.Add(new TaskItem(String.Format(CultureInfo.InvariantCulture, "{0}={1}", define.Key, define.Value)));
            }
            this.defineConstants = outputItems.ToArray();

            return true;
        }

        public static string GetProjectPath(ITaskItem[] projectReferencePaths, int i)
        {
            return projectReferencePaths[i].GetMetadata("MSBuildSourceProjectFileFullPath");
        }

        public static string GetReferenceName(ITaskItem item, string projectName)
        {
            string referenceName = item.GetMetadata("Name");
            if (String.IsNullOrEmpty(referenceName))
            {
                referenceName = projectName;
            }

            // We cannot have an equals sign in the variable name because it
            // messes with the preprocessor definitions on the command line.
            referenceName = referenceName.Replace('=', '_');

            // We cannot have a double quote on the command line because it
            // there is no way to escape it on the command line.
            referenceName = referenceName.Replace('\"', '_');

            // We cannot have parens in the variable name because the WiX
            // preprocessor will not be able to parse it.
            referenceName = referenceName.Replace('(', '_');
            referenceName = referenceName.Replace(')', '_');

            return referenceName;
        }

        /// <summary>
        /// Look through the configuration data in the ProjectConfigurations property
        /// to find the configuration for a project, if available.
        /// </summary>
        /// <param name="projectName">Name of the project that is being searched for.</param>
        /// <returns>Full configuration spec, for example "Release|Win32".</returns>
        private string FindProjectConfiguration(string projectName)
        {
            string configuration = String.Empty;

            if (this.ProjectConfigurations != null)
            {
                foreach (ITaskItem configItem in this.ProjectConfigurations)
                {
                    string configProject = configItem.ItemSpec;
                    if (configProject.Length > projectName.Length &&
                        configProject.StartsWith(projectName) &&
                        configProject[projectName.Length] == '=')
                    {
                        configuration = configProject.Substring(projectName.Length + 1);
                        break;
                    }
                }
            }

            return configuration;
        }

        /// <summary>
        /// Finds the common root between two paths
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns>common root on success, empty string on failure</returns>
        private static string FindCommonRoot(string path1, string path2)
        {
            path1 = path1.TrimEnd(Path.DirectorySeparatorChar);
            path2 = path2.TrimEnd(Path.DirectorySeparatorChar);

            while (!String.IsNullOrEmpty(path1))
            {
                for (string searchPath = path2; !String.IsNullOrEmpty(searchPath); searchPath = Path.GetDirectoryName(searchPath))
                {
                    if (path1.Equals(searchPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return searchPath;
                    }
                }

                path1 = Path.GetDirectoryName(path1);
            }

            return path1;
        }

        /// <summary>
        /// Finds the subfolder of a path, excluding a root and filename.
        /// </summary>
        /// <param name="path">Path to examine</param>
        /// <param name="rootPath">Root that must be present </param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string FindSubfolder(string path, string rootPath, string fileName)
        {
            if (Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                path = Path.GetDirectoryName(path);
            }

            if (path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                // cut out the root and return the subpath
                return path.Substring(rootPath.Length).Trim(Path.DirectorySeparatorChar);
            }

            return String.Empty;
        }

        private static string EnsureEndsWithBackslash(string dir)
        {
            if (dir[dir.Length - 1] != Path.DirectorySeparatorChar)
            {
                dir += Path.DirectorySeparatorChar;
            }

            return dir;
        }
    }
}
