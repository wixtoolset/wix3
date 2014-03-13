//--------------------------------------------------------------------------------------------------
// <copyright file="WixBuildMacroCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBuildMacroCollection class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using EnvDTE;
    using Microsoft.Build.BuildEngine;
    using Microsoft.Build.Execution;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Utilities = Microsoft.VisualStudio.Package.Utilities;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Collection class for a series of solution and project variables with their associated
    /// values. For example: SolutionDir=C:\MySolution, ProjectName=MyProject, etc.
    /// </summary>
    internal class WixBuildMacroCollection : ICollection, IEnumerable<WixBuildMacroCollection.MacroNameValuePair>
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private static readonly string[] globalMacroNames =
            {
                WixProjectFileConstants.DevEnvDir,
                WixProjectFileConstants.SolutionDir,
                WixProjectFileConstants.SolutionExt,
                WixProjectFileConstants.SolutionName,
                WixProjectFileConstants.SolutionFileName,
                WixProjectFileConstants.SolutionPath,
            };

        private static readonly string[] macroNames =
            {
                "ConfigurationName",
                "OutDir",
                "PlatformName",
                "ProjectDir",
                "ProjectExt",
                "ProjectFileName",
                "ProjectName",
                "ProjectPath",
                "TargetDir",
                "TargetExt",
                "TargetFileName",
                "TargetName",
                "TargetPath",
                "TargetPdbName",
                "TargetPdbPath",
            };

        private SortedList<string, string> list = new SortedList<string, string>(macroNames.Length, StringComparer.OrdinalIgnoreCase);

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildMacroCollection"/> class.
        /// </summary>
        /// <param name="project">The project from which to read the properties.</param>
        public WixBuildMacroCollection(WixProjectNode project)
        {
            WixHelperMethods.VerifyNonNullArgument(project, "project");

            // get the global SolutionX properties
            WixBuildMacroCollection.DefineSolutionProperties(project);
            foreach (string globalMacroName in globalMacroNames)
            {
                string property = null;
                project.BuildProject.GlobalProperties.TryGetValue(globalMacroName, out property);
                if (null == property)
                {
                    this.list.Add(globalMacroName, "*Undefined*");
                }
                else
                {
                    this.list.Add(globalMacroName, property);
                }
            }

            // we need to call GetTargetPath first so that TargetDir and TargetPath are resolved correctly
            ConfigCanonicalName configCanonicalName;
            if (!Utilities.TryGetActiveConfigurationAndPlatform(project.Site, project, out configCanonicalName))
            {
                throw new InvalidOperationException();
            }
            Microsoft.VisualStudio.Package.BuildResult res = project.Build(configCanonicalName, WixProjectFileConstants.MsBuildTarget.GetTargetPath);

            // get the ProjectX and TargetX variables
            foreach (string macroName in macroNames)
            {
                string value;
                if (res.ProjectInstance != null)
                {
                    value = res.ProjectInstance.GetPropertyValue(macroName);
                }
                else
                {
                    value = project.GetProjectProperty(macroName);
                }

                this.list.Add(macroName, value);
            }
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count
        {
            get { return this.list.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)this.list).IsSynchronized; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return ((ICollection)this.list).SyncRoot; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting
        /// at a particular <b>Array</b> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements copied
        /// from <see cref="ICollection"/>. The <b>Array</b> must have zero-based indexing.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.list).CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator&lt;T&gt;"/> object that can be used to iterate through the collection.</returns>
        IEnumerator<MacroNameValuePair> IEnumerable<MacroNameValuePair>.GetEnumerator()
        {
            foreach (KeyValuePair<string, string> pair in this.list)
            {
                yield return (MacroNameValuePair)pair;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<MacroNameValuePair>)this).GetEnumerator();
        }

        /// <summary>
        /// When building with only a wixproj in the solution, the SolutionX variables are not
        /// defined, so we have to define them here.
        /// </summary>
        /// <param name="project">The project where the properties are defined.</param>
        internal static void DefineSolutionProperties(WixProjectNode project)
        {
            IVsSolution solution = WixHelperMethods.GetService<IVsSolution, SVsSolution>(project.Site);
            object solutionPathObj;
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int) __VSPROPID.VSPROPID_SolutionFileName, out solutionPathObj));
            string solutionPath = (string) solutionPathObj;
            WixPackageSettings settings = project.WixPackage.Settings;
            string devEnvDir = WixHelperMethods.EnsureTrailingDirectoryChar(Path.GetDirectoryName(settings.DevEnvPath));

            string[][] properties = new string[][]
                {
                    new string[] { WixProjectFileConstants.DevEnvDir, devEnvDir },
                    new string[] { WixProjectFileConstants.SolutionPath, solutionPath },
                    new string[] { WixProjectFileConstants.SolutionDir, WixHelperMethods.EnsureTrailingDirectoryChar(Path.GetDirectoryName(solutionPath)) },
                    new string[] { WixProjectFileConstants.SolutionExt, Path.GetExtension(solutionPath) },
                    new string[] { WixProjectFileConstants.SolutionFileName, Path.GetFileName(solutionPath) },
                    new string[] { WixProjectFileConstants.SolutionName, Path.GetFileNameWithoutExtension(solutionPath) },
                };

            foreach (string[] property in properties)
            {
                string propertyName = property[0];
                string propertyValue = property[1];

                project.BuildProject.SetGlobalProperty(propertyName, propertyValue);
            }
        }

        /// <summary>
        /// When building a wixproj in VS, the configuration of referenced projects cannot be determined
        /// by MSBuild or from within an MSBuild task. So we'll get them from the VS project system here.
        /// </summary>
        /// <param name="project">The project where the properties are being defined; also the project
        /// whose references are being examined.</param>
        internal static void DefineProjectReferenceConfigurations(WixProjectNode project)
        {
            StringBuilder configList = new StringBuilder();

            IVsSolutionBuildManager solutionBuildManager =
                WixHelperMethods.GetService<IVsSolutionBuildManager, SVsSolutionBuildManager>(project.Site);

            List<WixProjectReferenceNode> referenceNodes = new List<WixProjectReferenceNode>();
            project.FindNodesOfType(referenceNodes);

            foreach (WixProjectReferenceNode referenceNode in referenceNodes)
            {
                IVsHierarchy hierarchy = VsShellUtilities.GetHierarchy(referenceNode.ProjectMgr.Site, referenceNode.ReferencedProjectGuid);

                string configuration = null;
                IVsProjectCfg2 projectCfg2 = null;
                IVsProjectCfg[] projectCfgArray = new IVsProjectCfg[1];

                int hr = solutionBuildManager.FindActiveProjectCfg(IntPtr.Zero, IntPtr.Zero, hierarchy, projectCfgArray);
                ErrorHandler.ThrowOnFailure(hr);

                projectCfg2 = projectCfgArray[0] as IVsProjectCfg2;

                if (projectCfg2 != null)
                {
                    hr = projectCfg2.get_DisplayName(out configuration);
                    if (hr != 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }

                if (configuration != null)
                {
                    if (configList.Length > 0)
                    {
                        configList.Append(';');
                    }

                    configList.Append(referenceNode.ReferencedProjectName);
                    configList.Append('=');
                    configList.Append(configuration);
                }
            }

            if (configList.Length > 0)
            {
                project.BuildProject.SetGlobalProperty("VSProjectConfigurations", configList.ToString());
            }
        }

        // =========================================================================================
        // Classes
        // =========================================================================================

        /// <summary>
        /// Defines a macro name/value pair that can be set or retrieved.
        /// </summary>
        public struct MacroNameValuePair
        {
            private KeyValuePair<string, string> pair;

            /// <summary>
            /// Initializes a new instance of the <see cref="MacroNameValuePair"/> class.
            /// </summary>
            /// <param name="pair">The KeyValuePair&lt;string, string&gt; to store.</param>
            private MacroNameValuePair(KeyValuePair<string, string> pair)
            {
                this.pair = pair;
            }

            /// <summary>
            /// Gets the macro name in the macro name/value pair.
            /// </summary>
            public string MacroName
            {
                get { return this.pair.Key; }
            }

            /// <summary>
            /// Gets the value in the macro name/value pair.
            /// </summary>
            public string Value
            {
                get { return this.pair.Value; }
            }

            /// <summary>
            /// Converts a <see cref="KeyValuePair&lt;T, T&gt;">KeyValuePair&lt;string, string&gt;</see>
            /// to a <see cref="MacroNameValuePair"/>.
            /// </summary>
            /// <param name="source">The KeyValuePair&lt;string, string&gt; to convert.</param>
            /// <returns>The converted <see cref="MacroNameValuePair"/>.</returns>
            public static implicit operator MacroNameValuePair(KeyValuePair<string, string> source)
            {
                return new MacroNameValuePair(source);
            }
        }
    }
}
