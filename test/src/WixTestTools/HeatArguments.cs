// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Heat arguments
    /// </summary>
    public partial class Heat
    {
        /// <summary>
        /// Supported harvesting types.
        /// </summary>
        public enum HarvestTypes
        {
            /// <summary>
            /// Default, not set
            /// </summary>
            NotSet = 0,
            /// <summary>
            /// harvest a directory
            /// </summary>
            Directory,
            /// <summary>
            /// harvest a file
            /// </summary>
            File,
            /// <summary>
            /// harvest outputs of a VS project
            /// </summary>
            Project,
            /// <summary>
            /// harvest an IIS web site
            /// </summary>
            Website,
        }

        /// <summary>
        /// Output group of VS project
        /// </summary>
        public enum ProjectOutputGroups
        {
            Binaries,
            Symbols,
            Documents,
            Satellites,
            Sources,
            Content,
        }

        /// <summary>
        /// Harvest template
        /// </summary>
        public enum Templates
        {
            NotSet = 0,
            Fragment,
            Module,
            Product,
        }

        #region Private Members
 
        /// <summary>
        /// -ag
        /// </summary>
        private bool autogenerateGUIDs;

        /// <summary>
        /// -cg
        /// </summary>
        private string componentGroupName;

        /// <summary>
        /// -dr
        /// </summary>
        private string directoryName;

        /// <summary>
        /// -ext
        /// </summary>
        private List<string> extensions;

        /// <summary>
        /// -gg
        /// </summary>
        private bool generateGUIDs;

        /// <summary>
        /// Harvest Type
        /// </summary>
        private HarvestTypes harvestType;

        /// <summary>
        /// Harvest Source
        /// </summary>
        private string harvestSource;

        /// <summary>
        //  -ke
        /// </summary>
        private bool keepEmptyDirectories;

        /// <summary>
        /// -pog
        /// </summary>
        private List<ProjectOutputGroups> outputGroups;

        /// <summary>
        /// -scom   
        /// </summary>
        private bool suppressCOMElements;

        /// <summary>
        /// -sfrag
        /// </summary>
        private bool suppressFragments;

        /// <summary>
        /// -srd
        /// </summary>
        private bool suppressRootDirectory;

        /// <summary>
        /// -sreg
        /// </summary>
        private bool suppressRegistry;

        /// <summary>
        /// -suid
        /// </summary>
        private bool suppressUniqueIdentifiers;

        /// <summary>
        ///  -sw
        /// </summary>
        private bool suppressAllWarnings;

        /// <summary>
        //  -sw<N>
        /// </summary>
        private List<string> suppressWarnings;

        /// <summary>
        /// -t
        /// </summary>
        private string transformXSLFile;

        /// <summary>
        /// -template
        /// </summary>
        private Templates template;

        /// <summary>
        // -wx<N>
        /// </summary>
        private List<string> treatWarningsAsErrors;

        /// <summary>
        /// -wx
        /// </summary>
        private bool treatAllWarningsAsErrors;

        /// <summary>
        /// -v
        /// </summary>
        private bool verboseOutput;

        /// <summary>
        // -var <VariableName>
        /// </summary>
        private string sourceDirVariableName;

        #endregion

        #region Public Properties

        /// <summary>
        /// The arguments as they would be passed on the command line
        /// </summary>
        /// <remarks>
        /// To allow for negative testing, checking for invalid combinations
        /// of arguments is not performed.
        /// </remarks>
        public override string Arguments
        {
            get
            {
                StringBuilder arguments = new StringBuilder();

                // Harvest Type
                if (HarvestTypes.NotSet != this.HarvestType)
                {
                    arguments.AppendFormat(" {0}", this.HarvestType.ToString());
                }

                // Harvest source
                if (!string.IsNullOrEmpty(this.HarvestSource))
                {
                    arguments.AppendFormat(" {0}", this.HarvestSource);
                }

                // AutogenerateGUIDs
                if (this.AutogenerateGUIDs)
                {
                    arguments.Append(" -ag");
                }

                // ComponentGroupName
                if (!string.IsNullOrEmpty(this.ComponentGroupName))
                {
                    arguments.AppendFormat(" -cg {0}",this.ComponentGroupName);
                }

                // DirectoryName
                if (!string.IsNullOrEmpty(this.DirectoryName))
                {
                    arguments.AppendFormat(" -dr {0}", this.DirectoryName);
                }

                // Extensions
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // GenerateGUIDs
                if (this.GenerateGUIDs)
                {
                    arguments.Append(" -gg");
                }

                // KeepEmptyDirectories
                if (this.KeepEmptyDirectories)
                {
                    arguments.Append(" -ke");
                }

                // Other arguments from parent
                arguments.Append(base.Arguments);

                // OutputFile
                if (!String.IsNullOrEmpty(this.OutputFile))
                {
                    arguments.AppendFormat(@" -out ""{0}""", this.OutputFile);
                }

                // OutputGroups
                foreach (ProjectOutputGroups outputGroup in this.OutputGroups)
                {
                    arguments.AppendFormat(@" -pog:{0}", outputGroup);
                }

                // SppressCOMElements
                if (this.SppressCOMElements)
                {
                    arguments.Append(" -scom");
                }

                // SuppressFragments
                if (this.SuppressFragments)
                {
                    arguments.Append(" -sfrag");
                }

                // SuppressRootDirectory
                if (this.SuppressRootDirectory)
                {
                    arguments.Append(" -srd");
                }

                // SuppressRegistry
                if (this.SuppressRegistry)
                {
                    arguments.Append(" -sreg");
                }

                // SuppressUniqueIdentifiers
                if (this.SuppressUniqueIdentifiers)
                {
                    arguments.Append(" -suid");
                }

                // SuppressAllWarnings
                if (this.SuppressAllWarnings)
                {
                    arguments.Append(" -sw");
                }

                // SuppressWarnings
                foreach (string warning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(@" -sw{0}", warning);
                }

                // TransformXSLFile
                if (!String.IsNullOrEmpty(this.TransformXSLFile))
                {
                    arguments.AppendFormat(@" -t:""{0}""", this.TransformXSLFile);
                }

                // Template
                if (Templates.NotSet != this.Template)
                {
                    arguments.AppendFormat(@" -template:{0}", this.Template.ToString());
                }

                // VerboseOutput
                if (this.VerboseOutput)
                {
                    arguments.Append(" -v");
                }

                // TreatAllWarningsAsErrors
                if (!string.IsNullOrEmpty(this.SourceDirVariableName))
                {
                    arguments.AppendFormat(" -var {0}", this.SourceDirVariableName);
                }

                // TreatAllWarningsAsErrors
                if (this.TreatAllWarningsAsErrors)
                {
                    arguments.Append(" -wx");
                }

                // SuppressWarnings
                foreach (string warning in this.TreatWarningsAsErrors)
                {
                    arguments.AppendFormat(@" -wx{0}", warning);
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// -ag
        /// </summary>
        public bool AutogenerateGUIDs
        {
            get { return this.autogenerateGUIDs; }
            set { this.autogenerateGUIDs = value; }
        }

        /// <summary>
        /// -cg
        /// </summary>
        public string ComponentGroupName
        {
            get { return this.componentGroupName; }
            set { this.componentGroupName = value; }
        }

        /// <summary>
        /// -dr
        /// </summary>
        public string DirectoryName
        {
            get { return this.directoryName; }
            set { this.directoryName = value; }
        }

        /// <summary>
        /// -ext
        /// </summary>
        public List<string> Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        /// <summary>
        /// -gg
        /// </summary>
        public bool GenerateGUIDs
        {
            get { return this.generateGUIDs; }
            set { this.generateGUIDs = value; }
        }

        /// <summary>
        /// Harvest Source
        /// </summary>
        public string HarvestSource
        {
            get { return this.harvestSource; }
            set { this.harvestSource = value; }
        }

        /// <summary>
        /// Harvest Type
        /// </summary>
        public HarvestTypes HarvestType
        {
            get { return this.harvestType; }
            set { this.harvestType = value; }
        }

        /// <summary>
        //  -ke
        /// </summary>
        public bool KeepEmptyDirectories
        {
            get { return this.keepEmptyDirectories; }
            set { this.keepEmptyDirectories = value; }
        }

        /// <summary>
        /// -pog
        /// </summary>
        public List<ProjectOutputGroups> OutputGroups
        {
            get { return this.outputGroups; }
            set { this.outputGroups = value; }
        }

        /// <summary>
        /// -scom   
        /// </summary>
        public bool SppressCOMElements
        {
            get { return this.suppressCOMElements; }
            set { this.suppressCOMElements = value; }
        }

        /// <summary>
        /// -sfrag
        /// </summary>
        public bool SuppressFragments
        {
            get { return this.suppressFragments; }
            set { this.suppressFragments = value; }
        }

        /// <summary>
        /// -srd
        /// </summary>
        public bool SuppressRootDirectory
        {
            get { return this.suppressRootDirectory; }
            set { this.suppressRootDirectory = value; }
        }

        /// <summary>
        /// -sreg
        /// </summary>
        public bool SuppressRegistry
        {
            get { return this.suppressRegistry; }
            set { this.suppressRegistry = value; }
        }

        /// <summary>
        /// -suid
        /// </summary>
        public bool SuppressUniqueIdentifiers
        {
            get { return this.suppressUniqueIdentifiers; }
            set { this.suppressUniqueIdentifiers = value; }
        }

        /// <summary>
        ///  -sw
        /// </summary>
        public bool SuppressAllWarnings
        {
            get { return this.suppressAllWarnings; }
            set { this.suppressAllWarnings = value; }
        }

        /// <summary>
        //  -sw<N>
        /// </summary>
        public List<string> SuppressWarnings
        {
            get { return this.suppressWarnings; }
            set { this.suppressWarnings = value; }
        }

        /// <summary>
        /// -t
        /// </summary>
        public string TransformXSLFile
        {
            get { return this.transformXSLFile; }
            set { this.transformXSLFile = value; }
        }

        /// <summary>
        /// -template
        /// </summary>
        public Templates Template
        {
            get { return this.template; }
            set { this.template = value; }
        }

        /// <summary>
        // -wx<N>
        /// </summary>
        public List<string> TreatWarningsAsErrors
        {
            get { return this.treatWarningsAsErrors; }
            set { this.treatWarningsAsErrors = value; }
        }

        /// <summary>
        /// -wx
        /// </summary>
        public bool TreatAllWarningsAsErrors
        {
            get { return this.treatAllWarningsAsErrors; }
            set { this.treatAllWarningsAsErrors = value; }
        }

        /// <summary>
        /// -v
        /// </summary>
        public bool VerboseOutput
        {
            get { return this.verboseOutput; }
            set { this.verboseOutput = value; }
        }

        /// <summary>
        // -var <VariableName>
        /// </summary>
        public string SourceDirVariableName
        {
            get { return this.sourceDirVariableName; }
            set { this.sourceDirVariableName = value; }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values
        /// </summary>
        public override void SetDefaultArguments()
        {
            this.AutogenerateGUIDs = false;
            this.ComponentGroupName = string.Empty;
            this.DirectoryName = string.Empty;
            this.Extensions = new List<string>();
            this.GenerateGUIDs = false;
            this.HarvestSource = string.Empty;
            this.HarvestType = HarvestTypes.NotSet;
            this.KeepEmptyDirectories = false;
            this.OutputFile = string.Empty;
            this.OutputGroups = new List<ProjectOutputGroups>();
            this.SourceDirVariableName = string.Empty;
            this.SppressCOMElements = false;
            this.SuppressAllWarnings = false;
            this.SuppressFragments = false;
            this.SuppressRegistry = false;
            this.SuppressRootDirectory = false;
            this.SuppressWarnings = new List<string>();
            this.Template = Templates.NotSet;
            this.TransformXSLFile = string.Empty;
            this.TreatAllWarningsAsErrors = false;
            this.TreatWarningsAsErrors = new List<string>();
            this.VerboseOutput = false;
        }
    }
}
