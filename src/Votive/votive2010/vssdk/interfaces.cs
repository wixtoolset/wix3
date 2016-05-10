// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
using MSBuild = Microsoft.Build.Evaluation;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package
{

    /// <summary>
    /// This interface defines the rules for handling build dependency on a project container.
    /// </summary>
    /// <remarks>Normally this should be an internal interface but since it shouldbe available for the aggregator it must be made public.</remarks>
    [ComVisible(true)]
    [CLSCompliant(false)]
    public interface IBuildDependencyOnProjectContainer
    {
        /// <summary>
        /// Defines whether the nested projects should be build with the parent project.
        /// </summary>
        bool BuildNestedProjectsOnBuild
        {
            get;
            set;
        }

        /// <summary>
        /// Enumerates the nested hierachies present that will participate in the build dependency update.
        /// </summary>
        /// <returns>A list of hierrachies.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Hierachies")]
        IVsHierarchy[] EnumNestedHierachiesForBuildDependency();
    }

    /// <summary>
    /// Interface for manipulating build dependency
    /// </summary>
    /// <remarks>Normally this should be an internal interface but since it shouldbe available for the aggregator it must be made public.</remarks>
    [ComVisible(true)]
    [CLSCompliant(false)]
    public interface IBuildDependencyUpdate
    {
        /// <summary>
        /// Defines a container for storing BuildDependencies
        /// </summary>

        IVsBuildDependency[] BuildDependencies
        {
            get;
        }

        /// <summary>
        /// Adds a BuildDependency to the container
        /// </summary>
        /// <param name="dependency">The dependency to add</param>
        void AddBuildDependency(IVsBuildDependency dependency);

        /// <summary>
        /// Removes the builddependency from teh container.
        /// </summary>
        /// <param name="dependency">The dependency to add</param>
        void RemoveBuildDependency(IVsBuildDependency dependency);

    }

    /// <summary>
    /// Provides access to the reference data container.
    /// </summary>
    /// <remarks>Normally this should be an internal interface but since it should be available for
    /// the aggregator it must be made public.</remarks>
    [CLSCompliant(false), ComVisible(true)]
    public interface IReferenceContainerProvider
    {
        IReferenceContainer GetReferenceContainer();
    }

    /// <summary>
    /// Defines a container for manipulating references
    /// </summary>
    /// <remarks>Normally this should be an internal interface but since it should be available for
    /// the aggregator it must be made public.</remarks>
    [CLSCompliant(false), ComVisible(true)]
    public interface IReferenceContainer
    {
        IList<ReferenceNode> EnumReferences();
        ReferenceNode AddReferenceFromSelectorData(VSCOMPONENTSELECTORDATA selectorData);
        void LoadReferencesFromBuildProject(MSBuild.Project buildProject);
    }

    /// <summary>
    /// Defines the events that are internally defined for communication with other subsytems.
    /// </summary>
    [ComVisible(true)]
    public interface IProjectEvents
    {
        /// <summary>
        /// Event raised just after the project file opened.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1713:EventsShouldNotHaveBeforeOrAfterPrefix")]
        event EventHandler<AfterProjectFileOpenedEventArgs> AfterProjectFileOpened;

        /// <summary>
        /// Event raised before the project file closed.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1713:EventsShouldNotHaveBeforeOrAfterPrefix")]
        event EventHandler<BeforeProjectFileClosedEventArgs> BeforeProjectFileClosed;
    }

    /// <summary>
    /// Defines the interface that will specify ehethrr the object is a project events listener.
    /// </summary>
    [ComVisible(true)]
    public interface IProjectEventsListener
    {

        /// <summary>
        /// Is the object a project events listener.
        /// </summary>
        /// <returns></returns>
        bool IsProjectEventsListener
        { get; set; }

    }

    /// <summary>
    /// Enable getting and setting the project events provider
    /// </summary>
    [ComVisible(true)]
    public interface IProjectEventsProvider
    {
        /// <summary>
        /// Defines the provider for the project events
        /// </summary>
        IProjectEvents ProjectEventsProvider
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Defines support for single file generator
    /// </summary>
    public interface ISingleFileGenerator
    {
        ///<summary>
        /// Runs the generator on the item represented by the document moniker.
        /// </summary>
        /// <param name="document"></param>
        void RunGenerator(string document);
    }
}
