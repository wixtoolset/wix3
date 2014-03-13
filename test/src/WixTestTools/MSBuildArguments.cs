//-----------------------------------------------------------------------
// <copyright file="MSBuildArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Fields, properties and methods for working with MSBuild arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with MSBuild arguments.
    /// </summary>
    public partial class MSBuild
    {
        /// <summary>
        /// The MSBuild verbosity level
        /// </summary>
        public enum VerbosityLevel
        {
            /// <summary>
            /// Minimal
            /// </summary>
            Minimal = 0,

            /// <summary>
            /// Normal
            /// </summary>
            Normal,

            /// <summary>
            /// Detailed
            /// </summary>
            Detailed,

            /// <summary>
            /// Diagnostics
            /// </summary>
            Diagnostics
        }

        #region MSBuildPropertyCollection
        /// <summary>
        /// Overide the basic functionality of the index, to disable failing if key if not found.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        public class MSBuildPropertyCollection<TKey, TValue> : Dictionary<TKey, TValue>
        {
            /// <summary>
            ///  Adds the specified key and value to the dictionary.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
            /// <remarks>Overide the basic functionality of the index, to disable failing if key if not found.</remarks>
            public new void Add(TKey key, TValue value)
            {
                if (!this.ContainsKey(key))
                {
                    base.Add(key, value);
                }
            }

            /// <summary>
            /// Overide the basic functionality of the index, to disable failing if key if not found.
            /// If key is found, value is set; if not it is added. 
            /// Dictinary[x] = y should always set x = y.
            /// </summary>
            /// <param name="key">The type of the keys in the dictionary.</param>
            public new TValue this[TKey key]
            {
                get
                {
                    return base[key];
                }
                set
                {
                    if (this.ContainsKey(key))
                    {
                        base[key] = value;
                    }
                    else
                    {
                        this.Add(key, value);
                    }
                }
            }
        }
        #endregion

        #region Private Members

        /// <summary>
        /// Specifies the maximum number of concurrent processes to build with.
        /// </summary>
        /// <remarks>
        /// Defaults to the number of CPUs on the current machine.
        /// </remarks>
        private int maxCPUCount;

        /// <summary>
        /// Do not display the startup banner and copyright message.
        /// </summary>
        private bool noLogo;

        /// <summary>
        /// Other arguments.
        /// </summary>
        private string otherArguments;

        /// <summary>
        /// The project file to build.
        /// </summary>
        private string projectFile;

        /// <summary>
        /// Set or override project-level properties.
        /// </summary>
        private MSBuildPropertyCollection<string, string> properties;

        /// <summary>
        /// Build these targets in the project.
        /// </summary>
        private List<string> targets;

        /// <summary>
        /// The version of the MSBuild Toolset to use during build.
        /// </summary>
        private string toolsVersion;

        /// <summary>
        /// Display this amount of information in the event log.
        /// </summary>
        private VerbosityLevel verbosity;

        #endregion

        #region Public Properties

        /// <summary>
        /// The arguments as they would be passed on the command line.
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

                // MaxCPUCount
                if (0 == this.MaxCPUCount)
                {
                    // Use the number of CPUs on this machine
                    arguments.Append(" /maxcpucount");
                }
                else if (0 < this.MaxCPUCount)
                {
                    arguments.AppendFormat(" /maxcpucount:{0}", Convert.ToString(this.MaxCPUCount));
                }

                // NoLogo
                if (this.NoLogo)
                {
                    arguments.Append(" /nologo");
                }

                // OtherArguments
                if (!String.IsNullOrEmpty(this.OtherArguments))
                {
                    arguments.AppendFormat(" {0}", this.OtherArguments);
                }

                // Properties
                foreach (string key in this.Properties.Keys)
                {
                    arguments.AppendFormat(" /property:{0}=\"{1}\"", key, this.Properties[key]);
                }

                // Targets
                foreach (string target in this.Targets)
                {
                    arguments.AppendFormat(" /target:{0}", target);
                }

                // ToolsVersion
                if (!String.IsNullOrEmpty(this.ToolsVersion))
                {
                    arguments.AppendFormat(" /toolsversion:{0}", this.ToolsVersion);
                }

                // Verbosity
                switch (this.Verbosity)
                {
                    case VerbosityLevel.Minimal:
                    case VerbosityLevel.Normal:
                    case VerbosityLevel.Detailed:
                    case VerbosityLevel.Diagnostics:
                        arguments.AppendFormat(" /verbosity:{0}", Enum.GetName(typeof(VerbosityLevel), this.Verbosity));
                        break;
                    default:
                        // do nothing
                        break;
                }

                // ProjectFile
                if (!String.IsNullOrEmpty(this.ProjectFile))
                {
                    arguments.AppendFormat(" \"{0}\"", this.ProjectFile);
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// Specifies the maximum number of concurrent processes to build with.
        /// </summary>
        /// <remarks>
        /// Defaults to the number of CPUs on the current machine.
        /// </remarks>
        public int MaxCPUCount
        {
            get { return this.maxCPUCount; }
            set { this.maxCPUCount = value; }
        }

        /// <summary>
        /// Do not display the startup banner and copyright message.
        /// </summary>
        public bool NoLogo
        {
            get { return this.noLogo; }
            set { this.noLogo = value; }
        }

        /// <summary>
        /// Other arguments.
        /// </summary>
        public string OtherArguments
        {
            get { return this.otherArguments; }
            set { this.otherArguments = value; }
        }

        /// <summary>
        /// The project file to build.
        /// </summary>
        public string ProjectFile
        {
            get { return this.projectFile; }
            set { this.projectFile = value; }
        }

        /// <summary>
        /// Set or override project-level properties.
        /// </summary>
        public MSBuildPropertyCollection<string, string> Properties
        {
            get { return this.properties; }
            set { this.properties = value; }
        }

        /// <summary>
        /// Build these targets in the project.
        /// </summary>
        public List<string> Targets
        {
            get { return this.targets; }
            set { this.targets = value; }
        }

        /// <summary>
        /// The version of the MSBuild Toolset to use during build.
        /// </summary>
        public string ToolsVersion
        {
            get { return this.toolsVersion; }
            set { this.toolsVersion = value; }
        }

        /// <summary>
        /// Display this amount of information in the event log.
        /// </summary>
        public VerbosityLevel Verbosity
        {
            get { return this.verbosity; }
            set { this.verbosity = value; }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values.
        /// </summary>
        public virtual void SetDefaultArguments()
        {
            this.MaxCPUCount = 0;
            this.NoLogo = true;
            this.OtherArguments = String.Empty;
            this.ProjectFile = String.Empty;
            this.Properties = new MSBuildPropertyCollection<string, string>();
            this.Targets = new List<string>();
            this.ToolsVersion = String.Empty;
            this.Verbosity = VerbosityLevel.Detailed;
        }
    }
}
