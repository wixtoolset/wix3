//-------------------------------------------------------------------------------------------------
// <copyright file="projectfactory.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSBuild = Microsoft.Build.Evaluation;
using MSBuildExecution = Microsoft.Build.Execution;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;
using System.Globalization;

namespace Microsoft.VisualStudio.Package
{
    /// <summary>
    /// Creates projects within the solution
    /// </summary>
    [CLSCompliant(false)]
    public abstract class ProjectFactory : Microsoft.VisualStudio.Shell.Flavor.FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory
    {
        #region fields
        private Microsoft.VisualStudio.Shell.Package package;
        private System.IServiceProvider site;

        /// <summary>
        /// The msbuild engine that we are going to use.
        /// </summary>
        private MSBuild.ProjectCollection buildEngine;

        /// <summary>
        /// The msbuild project for the project file.
        /// </summary>
        private MSBuild.Project buildProject;
        #endregion

        #region properties
        protected Microsoft.VisualStudio.Shell.Package Package
        {
            get
            {
                return this.package;
            }
        }

        protected System.IServiceProvider Site
        {
            get
            {
                return this.site;
            }
        }

        /// <summary>
        /// The msbuild engine that we are going to use.
        /// </summary>
        protected MSBuild.ProjectCollection BuildEngine
        {
            get
            {
                return this.buildEngine;
            }
        }

        /// <summary>
        /// The msbuild project for the temporary project file.
        /// </summary>
        protected MSBuild.Project EvaluationProject
        {
            get
            {
                return this.buildProject;
            }
            set
            {
                this.buildProject = value;
            }
        }
        #endregion

        #region ctor
        protected ProjectFactory(Microsoft.VisualStudio.Shell.Package package)
        {
            this.package = package;
            this.site = package;

            // Please be aware that this methods needs that ServiceProvider is valid, thus the ordering of calls in the ctor matters.
            this.buildEngine = Utilities.InitializeMsBuildEngine(this.buildEngine, this.site);
        }
        #endregion

        #region abstract methods
        protected abstract ProjectNode CreateProject();
        #endregion

        #region overriden methods
        /// <summary>
        /// Rather than directly creating the project, ask VS to initate the process of
        /// creating an aggregated project in case we are flavored. We will be called
        /// on the IVsAggregatableProjectFactory to do the real project creation.
        /// </summary>
        /// <param name="fileName">Project file</param>
        /// <param name="location">Path of the project</param>
        /// <param name="name">Project Name</param>
        /// <param name="flags">Creation flags</param>
        /// <param name="projectGuid">Guid of the project</param>
        /// <param name="project">Project that end up being created by this method</param>
        /// <param name="canceled">Was the project creation canceled</param>
        protected override void CreateProject(string fileName, string location, string name, uint flags, ref Guid projectGuid, out IntPtr project, out int canceled)
        {
            project = IntPtr.Zero;
            canceled = 0;

            // Get the list of GUIDs from the project/template
            string guidsList = this.ProjectTypeGuids(fileName);

            // Launch the aggregate creation process (we should be called back on our IVsAggregatableProjectFactoryCorrected implementation)
            IVsCreateAggregateProject aggregateProjectFactory = (IVsCreateAggregateProject)this.Site.GetService(typeof(SVsCreateAggregateProject));
            int hr = aggregateProjectFactory.CreateAggregateProject(guidsList, fileName, location, name, flags, ref projectGuid, out project);
            if (hr == VSConstants.E_ABORT)
                canceled = 1;
            ErrorHandler.ThrowOnFailure(hr);

            // This needs to be done after the aggregation is completed (to avoid creating a non-aggregated CCW) and as a result we have to go through the interface
            IProjectEventsProvider eventsProvider = (IProjectEventsProvider)Marshal.GetTypedObjectForIUnknown(project, typeof(IProjectEventsProvider));
            eventsProvider.ProjectEventsProvider = this.GetProjectEventsProvider();

            this.buildProject = null;
        }


        /// <summary>
        /// Instantiate the project class, but do not proceed with the
        /// initialization just yet.
        /// Delegate to CreateProject implemented by the derived class.
        /// </summary>
        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            // Please be very carefull what is initialized here on the ProjectNode. Normally this should only instantiate and return a project node.
            // The reason why one should very carefully add state to the project node here is that at this point the aggregation has not yet been created and anything that would cause a CCW for the project to be created would cause the aggregation to fail
            // Our reasoning is that there is no other place where state on the project node can be set that is known by the Factory and has to execute before the Load method.
            ProjectNode node = this.CreateProject();
            Debug.Assert(node != null, "The project failed to be created");
            node.BuildEngine = this.buildEngine;
            node.BuildProject = this.buildProject;
            return node;
        }

        /// <summary>
        /// Retrives the list of project guids from the project file.
        /// If you don't want your project to be flavorable, override
        /// to only return your project factory Guid:
        ///      return this.GetType().GUID.ToString("B");
        /// </summary>
        /// <param name="file">Project file to look into to find the Guid list</param>
        /// <returns>List of semi-colon separated GUIDs</returns>
        protected override string ProjectTypeGuids(string file)
        {
            // Load the project so we can extract the list of GUIDs

            this.buildProject = Utilities.ReinitializeMsBuildProject(this.buildEngine, file, this.buildProject, this.Site);

            // Retrieve the list of GUIDs, if it is not specify, make it our GUID
            string guids = buildProject.GetPropertyValue(ProjectFileConstants.ProjectTypeGuids);
            if (String.IsNullOrEmpty(guids))
                guids = this.GetType().GUID.ToString("B");

            return guids;
        }
        #endregion

        #region helpers
        private IProjectEvents GetProjectEventsProvider()
        {
            ProjectPackage projectPackage = this.package as ProjectPackage;
            Debug.Assert(projectPackage != null, "Package not inherited from framework");
            if (projectPackage != null)
            {
                foreach (SolutionListener listener in projectPackage.SolutionListeners)
                {
                    IProjectEvents projectEvents = listener as IProjectEvents;
                    if (projectEvents != null)
                    {
                        return projectEvents;
                    }
                }
            }

            return null;
        }

        #endregion

        #region IVsProjectUpgradeViaFactory
        private string m_lastUpgradedProjectFile;
        private const string SCC_PROJECT_NAME = "SccProjectName";
        private string m_sccProjectName;
        private const string SCC_AUX_PATH = "SccAuxPath";
        private string m_sccAuxPath;
        private const string SCC_LOCAL_PATH = "SccLocalPath";
        private string m_sccLocalPath;
        private const string SCC_PROVIDER = "SccProvider";
        private string m_sccProvider;
        public virtual int GetSccInfo(string projectFileName, out string sccProjectName, out string sccAuxPath, out string sccLocalPath, out string provider)
        {
            // we should only be asked for SCC info on a project that we have just upgraded.
            if (!String.Equals(this.m_lastUpgradedProjectFile, projectFileName, StringComparison.OrdinalIgnoreCase))
            {
                sccProjectName = "";
                sccAuxPath = "";
                sccLocalPath = "";
                provider = "";
                return VSConstants.E_FAIL;
            }
            sccProjectName = this.m_sccProjectName;
            sccAuxPath = this.m_sccAuxPath;
            sccLocalPath = this.m_sccLocalPath;
            provider = this.m_sccProvider;
            return VSConstants.S_OK;
        }

        public virtual int UpgradeProject_CheckOnly(string projectFileName, IVsUpgradeLogger upgradeLogger, out int upgradeRequired, out Guid newProjectFactory, out uint upgradeCapabilityFlags)
        {
            newProjectFactory = GetType().GUID;
            upgradeCapabilityFlags = 0; // VSPPROJECTUPGRADEVIAFACTORYFLAGS: we only support in-place upgrade with no back-up

            ProjectRootElement project = ProjectRootElement.Open(projectFileName);

            // only upgrade known tool versions.
            if (string.Equals("3.5", project.ToolsVersion, StringComparison.Ordinal) || string.Equals("2.0", project.ToolsVersion, StringComparison.Ordinal))
            {
                upgradeRequired = 1;
                return VSConstants.S_OK;
            }

            upgradeRequired = 0;
            return VSConstants.S_OK;
        }
        public virtual int UpgradeProject(string projectFileName, uint upgradeFlag, string copyLocation, out string upgradeFullyQualifiedFileName, IVsUpgradeLogger logger, out int upgradeRequired,
                                                out Guid newProjectFactory)
        {
            uint ignore;

            this.UpgradeProject_CheckOnly(projectFileName, logger, out upgradeRequired, out newProjectFactory, out ignore);
            if (upgradeRequired == 0)
            {
                upgradeFullyQualifiedFileName = projectFileName;
                return VSConstants.S_OK;
            }

            string projectName = Path.GetFileNameWithoutExtension(projectFileName);
            upgradeFullyQualifiedFileName = projectFileName;

            // Query for edit
            IVsQueryEditQuerySave2 queryEdit = site.GetService(typeof(SVsQueryEditQuerySave)) as IVsQueryEditQuerySave2;

            if (queryEdit != null)
            {
                uint editVerdict;
                uint queryEditMoreInfo;
                const tagVSQueryEditFlags tagVSQueryEditFlags_QEF_AllowUnopenedProjects = (tagVSQueryEditFlags)0x80;

                int hr = queryEdit.QueryEditFiles(
                    (uint)(tagVSQueryEditFlags.QEF_ForceEdit_NoPrompting | tagVSQueryEditFlags.QEF_DisallowInMemoryEdits | tagVSQueryEditFlags_QEF_AllowUnopenedProjects),
                    1, new[] { projectFileName }, null, null, out editVerdict, out queryEditMoreInfo);
                if (ErrorHandler.Failed(hr))
                {
                    return VSConstants.E_FAIL;
                }

                if (editVerdict != (uint)tagVSQueryEditResult.QER_EditOK)
                {
                    if (logger != null)
                    {
                        logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, projectFileName,
                            SR.GetString(SR.UpgradeCannotOpenProjectFileForEdit));
                    }
                    return VSConstants.E_FAIL;
                }

                // If file was modified during the checkout, maybe upgrade is not needed
                if ((queryEditMoreInfo & (uint)tagVSQueryEditResultFlags.QER_MaybeChanged) != 0)
                {
                    this.UpgradeProject_CheckOnly(projectFileName, logger, out upgradeRequired, out newProjectFactory, out ignore);
                    if (upgradeRequired == 0)
                    {
                        if (logger != null)
                        {
                            logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, projectFileName,
                                SR.GetString(SR.UpgradeNoNeedToUpgradeAfterCheckout));
                        }

                        return VSConstants.S_OK;
                    }
                }
            }

            // Convert the project
            Microsoft.Build.Conversion.ProjectFileConverter projectConverter = new Microsoft.Build.Conversion.ProjectFileConverter();
            projectConverter.OldProjectFile = projectFileName;
            projectConverter.NewProjectFile = projectFileName;
            ProjectRootElement convertedProject = null;
            try
            {
                convertedProject = projectConverter.ConvertInMemory();
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, projectFileName, ex.Message);
            }

            if (convertedProject != null)
            {
                this.m_lastUpgradedProjectFile = projectFileName;
                foreach (ProjectPropertyElement property in convertedProject.Properties)
                {
                    switch (property.Name)
                    {
                        case SCC_LOCAL_PATH:
                            this.m_sccLocalPath = property.Value;
                            break;
                        case SCC_AUX_PATH:
                            this.m_sccAuxPath = property.Value;
                            break;
                        case SCC_PROVIDER:
                            this.m_sccProvider = property.Value;
                            break;
                        case SCC_PROJECT_NAME:
                            this.m_sccProjectName = property.Value;
                            break;
                        default:
                            break;
                    }
                }
                try
                {
                    convertedProject.Save(projectFileName);
                }
                catch (Exception ex)
                {
                    if (logger != null)
                        logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, projectFileName, ex.Message);
                    return VSConstants.E_FAIL;
                }
                if (logger != null)
                {
                    logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_STATUSMSG, projectName, projectFileName,
                        SR.GetString(SR.UpgradeSuccessful));
                }
                return VSConstants.S_OK;

            }

            this.m_lastUpgradedProjectFile = null;
            upgradeFullyQualifiedFileName = "";
            return VSConstants.E_FAIL;
        }

        #endregion
    }
}
