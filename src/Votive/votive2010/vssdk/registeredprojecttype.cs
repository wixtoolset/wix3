// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Package
{
    /// <summary>
    /// Gets registry settings from for a project.
    /// </summary>
    internal class RegisteredProjectType
    {
        private string defaultProjectExtension;

        private string projectTemplatesDir;

        private string wizardTemplatesDir;

        private Guid packageGuid;

        internal const string DefaultProjectExtension = "DefaultProjectExtension";
        internal const string WizardsTemplatesDir = "WizardsTemplatesDir";
        internal const string ProjectTemplatesDir = "ProjectTemplatesDir";
        internal const string Package = "Package";



        internal string DefaultProjectExtensionValue
        {
            get
            {
                return this.defaultProjectExtension;
            }
            set
            {
                this.defaultProjectExtension = value;
            }
        }

        internal string ProjectTemplatesDirValue
        {
            get
            {
                return this.projectTemplatesDir;
            }
            set
            {
                this.projectTemplatesDir = value;
            }
        }

        internal string WizardTemplatesDirValue
        {
            get
            {
                return this.wizardTemplatesDir;
            }
            set
            {
                this.wizardTemplatesDir = value;
            }
        }

        internal Guid PackageGuidValue
        {
            get
            {
                return this.packageGuid;
            }
            set
            {
                this.packageGuid = value;
            }
        }

        /// <summary>
        /// If the project support VsTemplates, returns the path to
        /// the vstemplate file corresponding to the requested template
        /// 
        /// You can pass in a string such as: "Windows\Console Application"
        /// </summary>
        internal string GetVsTemplateFile(string templateFile)
        {
            // First see if this use the vstemplate model
            if (!String.IsNullOrEmpty(DefaultProjectExtensionValue))
            {
                EnvDTE80.DTE2 dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                if (dte != null)
                {
                    EnvDTE80.Solution2 solution = dte.Solution as EnvDTE80.Solution2;
                    if (solution != null)
                    {
                        string fullPath = solution.GetProjectTemplate(templateFile, DefaultProjectExtensionValue);
                        // The path returned by GetProjectTemplate can be in the format "path|FrameworkVersion=x.y|Language=xxx"
                        // where the framework version and language sections are optional.
                        // Here we are interested only in the full path, so we have to remove all the other sections.
                        int pipePos = fullPath.IndexOf('|');
                        if (0 == pipePos)
                        {
                            return null;
                        }
                        if (pipePos > 0)
                        {
                            fullPath = fullPath.Substring(0, pipePos);
                        }
                        return fullPath;
                    }
                }

            }
            return null;
        }

        internal static RegisteredProjectType CreateRegisteredProjectType(EnvDTE.DTE dte, Guid projectTypeGuid)
        {
            RegisteredProjectType registederedProjectType = null;

#if VS2005
            using (RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(dte.RegistryRoot))
#else
            using (RegistryKey rootKey = Microsoft.VisualStudio.Shell.VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration))
#endif
            {
                if (rootKey == null)
                {
                    return null;
                }

                string projectPath = "Projects\\" + projectTypeGuid.ToString("B");
                using (RegistryKey projectKey = rootKey.OpenSubKey(projectPath))
                {
                    if (projectKey == null)
                    {
                        return null;
                    }

                    registederedProjectType = new RegisteredProjectType();
                    registederedProjectType.DefaultProjectExtensionValue = projectKey.GetValue(DefaultProjectExtension) as string;
                    registederedProjectType.ProjectTemplatesDirValue = projectKey.GetValue(ProjectTemplatesDir) as string;
                    registederedProjectType.WizardTemplatesDirValue = projectKey.GetValue(WizardsTemplatesDir) as string;
                    registederedProjectType.PackageGuidValue = new Guid(projectKey.GetValue(Package) as string);
                }
            }

            return registederedProjectType;
        }
    }
}
