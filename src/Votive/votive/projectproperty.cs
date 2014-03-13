//-------------------------------------------------------------------------------------------------
// <copyright file="projectproperty.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the ProjectProperty class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Microsoft.Build.BuildEngine;
    using Microsoft.VisualStudio.Package;
    
    /// <summary>
    /// Describes attributes of WiX project properties and allows getting/setting in context of those attributes.
    /// </summary>
    internal class ProjectProperty
    {
        // =========================================================================================
        // Constants
        // =========================================================================================

        private static readonly ICollection<char> MSBuildReservedChars = new char[]
        {
            '%', '$', '@', '(', ')', '\'', ';', '?', '*',
        };

        private static readonly ICollection<string> PerUserProperties = new string[]
        {
            WixProjectFileConstants.ReferencePaths,
        };

        private static readonly ICollection<string> NonPerConfigProperties = new string[]
        {
            WixProjectFileConstants.IncludeSearchPaths,
            WixProjectFileConstants.OutputName,
            WixProjectFileConstants.OutputType,
            WixProjectFileConstants.PreBuildEvent,
            WixProjectFileConstants.PostBuildEvent,
            WixProjectFileConstants.ReferencePaths,
            WixProjectFileConstants.RunPostBuildEvent,
        };

        private static readonly ICollection<string> AllowVariablesProperties = new string[]
        {
            WixProjectFileConstants.CompilerAdditionalOptions,
            WixProjectFileConstants.DefineConstants,
            WixProjectFileConstants.IntermediateOutputPath,
            WixProjectFileConstants.LibAdditionalOptions,
            WixProjectFileConstants.LinkerAdditionalOptions,
            WixProjectFileConstants.OutputName,
            WixProjectFileConstants.OutputPath,
            WixProjectFileConstants.PostBuildEvent,
            WixProjectFileConstants.PreBuildEvent,
            WixProjectFileConstants.WixVariables,
        };

        private static readonly ICollection<string> ListProperties = new string[]
        {
            WixProjectFileConstants.Cultures,
            WixProjectFileConstants.DefineConstants,
            WixProjectFileConstants.IncludeSearchPaths,
            WixProjectFileConstants.ReferencePaths,
            WixProjectFileConstants.SuppressIces,
            WixProjectFileConstants.SuppressSpecificWarnings,
            WixProjectFileConstants.WixVariables,
        };

        private static readonly ICollection<string> EndOfProjectFileProperties = new string[]
        {
            WixProjectFileConstants.PreBuildEvent,
            WixProjectFileConstants.PostBuildEvent,
        };

        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private WixProjectNode project;
        private string propertyName;
        private bool perUser;
        private bool perConfig;
        private bool allowVariables;
        private bool list;
        private bool endOfProjectFile;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Creates a new project property object.
        /// </summary>
        /// <param name="project">Project that owns the property.</param>
        /// <param name="propertyName">Name of the property.</param>
        public ProjectProperty(WixProjectNode project, string propertyName)
        {
            WixHelperMethods.VerifyNonNullArgument(project, "project");
            WixHelperMethods.VerifyNonNullArgument(propertyName, "propertyName");

            this.project = project;
            this.propertyName = propertyName;

            this.perUser = ProjectProperty.PerUserProperties.Contains(propertyName);
            this.perConfig = !ProjectProperty.NonPerConfigProperties.Contains(propertyName);
            this.allowVariables = ProjectProperty.AllowVariablesProperties.Contains(propertyName);
            this.list = ProjectProperty.ListProperties.Contains(propertyName);
            this.endOfProjectFile = ProjectProperty.EndOfProjectFileProperties.Contains(propertyName);
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Name
        {
            get { return this.propertyName; }
        }

        /// <summary>
        /// Gets a flag indicating whether the property is stored in the user project file.
        /// </summary>
        public bool PerUser
        {
            get { return this.perUser; }
        }

        /// <summary>
        /// Gets a flag indicating whether the property is stored in a property group conditioned on the configuration.
        /// </summary>
        public bool PerConfig
        {
            get { return this.perConfig; }
        }

        /// <summary>
        /// Gets a flag indicating wether the property allows variables in the value (affects escaping behavior).
        /// </summary>
        public bool AllowVariables
        {
            get { return this.allowVariables; }
        }

        /// <summary>
        /// Gets a flag indicating wether the property value is a list of items (affects escaping behavior).
        /// </summary>
        public bool List
        {
            get { return this.list; }
        }

        /// <summary>
        /// Gets a flag indicating wether the property is stored at the end of the project file.
        /// </summary>
        public bool EndOfProjectFile
        {
            get { return this.endOfProjectFile; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Gets the value of the property for the current project configuration.
        /// </summary>
        /// <param name="finalValue">Whether to evaluate variables in the value.</param>
        /// <returns>Value of the property, or null if the property is unset.</returns>
        public string GetValue(bool finalValue)
        {
            return this.GetValue(finalValue, null);
        }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        /// <param name="finalValue">Whether to evaluate variables in the value.</param>
        /// <param name="configs">Optional list of configurations to retrieve the property from;
        /// defaults to the project current configuration</param>
        /// <returns>Value (unified across configs) of the property, or null if the property is unset or
        /// inconsistent across configurations.</returns>
        public string GetValue(bool finalValue, IList<ProjectConfig> configs)
        {
            Project buildProject = this.PerUser ? this.project.UserBuildProject : this.project.BuildProject;
            if (buildProject == null)
            {
                return null;
            }

            return this.GetValue(finalValue, configs, false);
        }

        /// <summary>
        /// Gets the value of a boolean property.
        /// </summary>
        /// <param name="configs">Optional list of configurations to retrieve the property from;
        /// defaults to the project current configuration</param>
        /// <returns>Value (unified across configs) of the property, null if the property is
        /// inconsistent across configurations, or false if the property is unset.</returns>
        public bool? GetBooleanValue(IList<ProjectConfig> configs)
        {
            Project buildProject = this.PerUser ? this.project.UserBuildProject : this.project.BuildProject;
            if (buildProject == null)
            {
                return null;
            }

            string value = this.GetValue(false, configs, true);

            if (String.Equals(value, Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (String.Equals(value, Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the value of the property for the current project configuration.
        /// </summary>
        /// <param name="value">Property value to set.</param>
        /// <remarks>
        /// Before calling this method, the caller must ensure that the value is valid according to
        /// the <see cref="PropertyValidator"/> class, and that the project file is writable.
        /// In most cases the caller should also ensure that the new value is different from the
        /// existing value, to avoid dirtying the project file unnecessarily.
        /// </remarks>
        public void SetValue(string value)
        {
            this.SetValue(value, null);
        }

        /// <summary>
        /// Sets the value of the property.
        /// </summary>
        /// <param name="value">Property value to set.</param>
        /// <param name="configs">Optional list of configurations to set the property in;
        /// defaults to the project current configuration</param>
        /// <remarks>
        /// Before calling this method, the caller must ensure that the value is valid according to
        /// the <see cref="PropertyValidator"/> class, and that the project file is writable.
        /// In most cases the caller should also ensure that the new value is different from the
        /// existing value, to avoid dirtying the project file unnecessarily.
        /// </remarks>
        public void SetValue(string value, IList<ProjectConfig> configs)
        {
            WixHelperMethods.VerifyNonNullArgument(value, "value");

            value = value.Trim();

            PropertyPosition position = this.EndOfProjectFile ?
                PropertyPosition.UseExistingOrCreateAfterLastImport : PropertyPosition.UseExistingOrCreateAfterLastPropertyGroup;

            Project buildProject = this.project.BuildProject;
            if (this.PerUser)
            {
                if (this.project.UserBuildProject == null)
                {
                    this.project.CreateUserBuildProject();
                }

                buildProject = this.project.UserBuildProject;
            }

            value = this.Escape(value);

            if (this.PerConfig)
            {
                if (configs == null || configs.Count == 0)
                {
                    configs = new ProjectConfig[] { this.project.CurrentConfig };
                }

                foreach (ProjectConfig config in configs)
                {
                    buildProject.SetProperty(this.propertyName, value, config.Condition, position);
                }
            }
            else
            {
                buildProject.SetProperty(this.propertyName, value, null, position);
            }

            this.project.InvalidatePropertyCache();
            this.project.SetProjectFileDirty(true);
        }

        private string GetValue(bool finalValue, IList<ProjectConfig> configs, bool booleanValue)
        {
            Project buildProject = this.PerUser ? this.project.UserBuildProject : this.project.BuildProject;
            if (buildProject == null)
            {
                return null;
            }

            string value;
            if (this.PerConfig)
            {
                if (configs == null || configs.Count == 0)
                {
                    configs = new ProjectConfig[] { this.project.CurrentConfig };
                }

                value = this.GetPerConfigValue(buildProject, finalValue, configs, booleanValue);
            }
            else
            {
                BuildProperty buildProperty = buildProject.EvaluatedProperties[this.propertyName];
                value = this.GetBuildPropertyValue(buildProperty, finalValue);
                if (booleanValue && String.IsNullOrEmpty(value))
                {
                    value = Boolean.FalseString;
                }
            }

            return value;
        }

        private string GetPerConfigValue(Project buildProject, bool finalValue, IList<ProjectConfig> configs, bool nullIsFalse)
        {
            string unifiedValue = null;

            for (int i = 0; i < configs.Count; i++)
            {
                ProjectConfig config = configs[i];
                bool resetCache = (i == 0);

                BuildProperty buildProperty = config.GetMsBuildProperty(this.propertyName, resetCache, buildProject);
                string value = this.GetBuildPropertyValue(buildProperty, finalValue);

                if (value != null)
                {
                    value = value.Trim();
                }

                if (nullIsFalse && String.IsNullOrEmpty(value))
                {
                    value = Boolean.FalseString;
                }

                if (i == 0)
                {
                    unifiedValue = value;
                }
                else if (unifiedValue != value)
                {
                    unifiedValue = null; // indicates indeterminate value
                    break;
                }
            }

            return unifiedValue;
        }

        private string GetBuildPropertyValue(BuildProperty buildProperty, bool finalValue)
        {
            if (buildProperty == null)
            {
                return null;
            }
            else if (finalValue)
            {
                return buildProperty.FinalValue;
            }
            else
            {
                // If the property definition contains the property itself, we want to return the final result
                // Ideally, we would like to expand only the value for the property but MSBuild does not allow that
                // That solves the case where OutputPath is define as $(OutputPath)\ if it's not ending with a backslash
                string propertyNameEscaped = "$(" + this.propertyName + ")";
                if (buildProperty.Value.Contains(propertyNameEscaped))
                {
                    return buildProperty.FinalValue;
                }
                else
                {
                    return this.Unescape(buildProperty.Value);
                }
            }
        }

        private string Escape(string value)
        {
            IList<char> charsToEscape = this.GetCharsToEscape();

            foreach (char c in charsToEscape)
            {
                if (value.IndexOf(Convert.ToString(c, CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0)
                {
                    string escapeCode = String.Format(CultureInfo.InvariantCulture, "%{0:x2}", (int) c);
                    value = value.Replace(c.ToString(), escapeCode);
                }
            }

            return value;
        }

        private string Unescape(string value)
        {
            IList<char> charsToEscape = this.GetCharsToEscape();

            foreach (char c in charsToEscape)
            {
                string escapeCode = String.Format(CultureInfo.InvariantCulture, "%{0:x2}", (int)c);
                if (value.IndexOf(escapeCode, StringComparison.Ordinal) >= 0)
                {
                    value = value.Replace(escapeCode, c.ToString());
                }
            }

            return value;
        }

        private IList<char> GetCharsToEscape()
        {
            IList<char> charsToEscape = new List<char>(ProjectProperty.MSBuildReservedChars);

            if (this.AllowVariables)
            {
                charsToEscape.Remove('$');
                charsToEscape.Remove('(');
                charsToEscape.Remove(')');
            }

            if (this.List)
            {
                charsToEscape.Remove(';');
            }

            return charsToEscape;
        }
    }
}
