// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Represents WiX project reference node properties.
    /// </summary>
    /// <remarks>This class must be public and marked as ComVisible in order for the DispatchWrapper to work correctly.</remarks>
    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("E1E2DC4B-8113-44CE-BD45-5375B9B66B04")]
    [SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable")]
    public class WixProjectReferenceNodeProperties : WixReferenceNodeProperties
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixProjectReferenceNodeProperties"/> class.
        /// </summary>
        /// <param name="node">The node that contains the properties to expose via the Property Browser.</param>
        public WixProjectReferenceNodeProperties(WixProjectReferenceNode node)
            : base(node)
        {
        }

        // =========================================================================================
        // Enums
        // =========================================================================================

        /// <summary>
        /// An enum for available project output groups.
        /// </summary>
        [Flags]
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public enum ProjectOutputGroups
        {
            /// <summary>
            /// No POGs
            /// </summary>
            None = 0x0,

            /// <summary>
            /// Binaries
            /// </summary>
            Binaries = 0x1,

            /// <summary>
            /// Symbols
            /// </summary>
            Symbols = 0x2,

            /// <summary>
            /// Binaries;Symbols
            /// </summary>
            BinariesAndSymbols = 0x3,

            /// <summary>
            /// Sources
            /// </summary>
            Sources = 0x4,

            /// <summary>
            /// Binaries;Sources
            /// </summary>
            BinariesAndSources = 0x5,

            /// <summary>
            /// Symbols;Sources
            /// </summary>
            SymbolsAndSources = 0x6,

            /// <summary>
            /// Binaries;Symbols;Sources
            /// </summary>
            BinariesSymbolsSources = 0x7,

            /// <summary>
            /// Content
            /// </summary>
            Content = 0x8,

            /// <summary>
            /// Binaries;Content
            /// </summary>
            BinariesAndContent = 0x9,

            /// <summary>
            /// Binaries;Sources;Content
            /// </summary>
            BinariesSourcesContent = 0xD,

            /// <summary>
            /// Binaries;Symbols;Sources;Content
            /// </summary>
            BinariesSymbolsSourcesContent = 0xF,

            /// <summary>
            /// Satellites
            /// </summary>
            Satellites = 0x10,

            /// <summary>
            /// Binaries;Content;Satellites
            /// </summary>
            BinariesContentSatellites = 0x19,

            /// <summary>
            /// Documents
            /// </summary>
            Documents = 0x20,

            /// <summary>
            /// Binaries;Symbols;Sources;Content;Satellites;Documents
            /// </summary>
            All = 0x3F
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Overrides Name to not be displayed. ReferenceName is to be used
        /// instead.
        /// </summary>
        /// <value>The reference name.</value>
        [Browsable(false)]
        public override string Name
        {
            get
            {
                return this.ReferenceName;
            }
        }

        /// <summary>
        /// Gets or sets the reference name.
        /// </summary>
        /// <value>The reference name.</value>
        [SRCategoryAttribute(SR.Misc)]
        [LocDisplayName(SR.RefName)]
        [SRDescriptionAttribute(SR.RefNameDescription)]
        [Browsable(true)]
        [AutomationBrowsable(true)]
        public string ReferenceName
        {
            get
            {
                return this.Node.Caption;
            }

            set
            {
                this.Node.SetEditLabel(value);
            }
        }

        /// <summary>
        /// Gets or sets the project output groups to harvest.
        /// </summary>
        /// <value>The project output groups to harvest.</value>
        [WixLocalizedCategory("HarvestCategory")]
        [WixLocalizedDisplayName("RefHarvest")]
        [WixLocalizedDescription("RefHarvestDescription")]
        [Browsable(true)]
        [AutomationBrowsable(true)]
        public bool RefHarvest
        {
            get
            {
                return ((WixProjectReferenceNode)this.Node).Harvest;
            }

            set
            {
                ((WixProjectReferenceNode)this.Node).Harvest = value;
            }
        }

        /// <summary>
        /// Gets or sets the project output groups to harvest.
        /// </summary>
        /// <value>The project output groups to harvest.</value>
        [WixLocalizedCategory("HarvestCategory")]
        [WixLocalizedDisplayName("RefProjectOutputGroups")]
        [WixLocalizedDescription("RefProjectOutputGroupsDescription")]
        [Browsable(true)]
        [AutomationBrowsable(true)]
        public ProjectOutputGroups RefProjectOutputGroups
        {
            get
            {
                return ProjectOutputGroupsExtension.ParsePogList(((WixProjectReferenceNode)this.Node).RefProjectOutputGroups);
            }

            set
            {
                ((WixProjectReferenceNode)this.Node).RefProjectOutputGroups = ProjectOutputGroupsExtension.PogList(value);
            }
        }

        /// <summary>
        /// Gets or sets the target directory to harvest to.
        /// </summary>
        /// <value>The target directory to harvest to.</value>
        [WixLocalizedCategory("HarvestCategory")]
        [WixLocalizedDisplayName("RefTargetDir")]
        [WixLocalizedDescription("RefTargetDirDescription")]
        [Browsable(true)]
        [AutomationBrowsable(true)]
        public string RefTargetDir
        {
            get
            {
                return ((WixProjectReferenceNode)this.Node).RefTargetDir;
            }

            set
            {
                ((WixProjectReferenceNode)this.Node).RefTargetDir = value;
            }
        }

        /// <summary>
        /// Gets the full path to the referenced project file.
        /// </summary>
        /// <value>Full path to the referenced project file.</value>
        public override string FullPath
        {
            get
            {
                return ((ProjectReferenceNode)this.Node).ReferencedProjectOutputPath;
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Returns the name that is displayed in the right hand side of the Properties window drop-down combo box.
        /// </summary>
        /// <returns>The class name of the object, or null if the class does not have a name.</returns>
        public override string GetClassName()
        {
            return WixStrings.WixProjectReferenceProperties;
        }

        /// <summary>
        /// Helper methods for the ProjectOutputGroups enum.
        /// </summary>
        private static class ProjectOutputGroupsExtension
        {
            /// <summary>
            /// Converts a string of POGs into a ProjectOutputGroups instance.
            /// </summary>
            /// <param name="pogList">String of POGs separated by semicolons.</param>
            /// <returns>A new ProjectOutputGroups instance.</returns>
            public static ProjectOutputGroups ParsePogList(string pogList)
            {
                ProjectOutputGroups ret = ProjectOutputGroups.None;

                string[] pogs = pogList.Split(';');
                foreach (string pog in pogs)
                {
                    switch (pog)
                    {
                        case "Binaries":
                            ret |= ProjectOutputGroups.Binaries;
                            break;
                        case "Symbols":
                            ret |= ProjectOutputGroups.Symbols;
                            break;
                        case "Sources":
                            ret |= ProjectOutputGroups.Sources;
                            break;
                        case "Content":
                            ret |= ProjectOutputGroups.Content;
                            break;
                        case "Satellites":
                            ret |= ProjectOutputGroups.Satellites;
                            break;
                        case "Documents":
                            ret |= ProjectOutputGroups.Documents;
                            break;
                    }
                }

                return ret;
            }

            /// <summary>
            /// Convets a ProjectOutputGroups instance into a semicolon delimited by semicolons.
            /// </summary>
            /// <param name="pogs">ProjectOutputGroups instance to convert.</param>
            /// <returns>Semicolon delimited list of POGs.</returns>
            public static string PogList(ProjectOutputGroups pogs)
            {
                StringBuilder ret = new StringBuilder();

                if ((pogs & ProjectOutputGroups.Binaries) == ProjectOutputGroups.Binaries)
                {
                    ret.Append(";Binaries");
                }

                if ((pogs & ProjectOutputGroups.Symbols) == ProjectOutputGroups.Symbols)
                {
                    ret.Append(";Symbols");
                }

                if ((pogs & ProjectOutputGroups.Sources) == ProjectOutputGroups.Sources)
                {
                    ret.Append(";Sources");
                }

                if ((pogs & ProjectOutputGroups.Content) == ProjectOutputGroups.Content)
                {
                    ret.Append(";Content");
                }

                if ((pogs & ProjectOutputGroups.Satellites) == ProjectOutputGroups.Satellites)
                {
                    ret.Append(";Satellites");
                }

                if ((pogs & ProjectOutputGroups.Documents) == ProjectOutputGroups.Documents)
                {
                    ret.Append(";Documents");
                }

                if (ret.Length > 0)
                {
                    // remove the first semicolon
                    return ret.ToString().Substring(1);
                }
                else
                {
                    return String.Empty;
                }
            }
        }
    }
}
