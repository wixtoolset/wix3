//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildPropertyPage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBuildPropertyPage class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.Build.BuildEngine;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Property page for the compiler (candle) settings.
    /// </summary>
    [ComVisible(true)]
    [Guid("E26242E0-4175-48e7-821E-FD0B17603811")]
    internal class WixBuildPropertyPage : WixPropertyPage
    {
        internal const string DebugDefine = "Debug";

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildPropertyPage"/> class.
        /// </summary>
        public WixBuildPropertyPage()
        {
            this.PageName = WixStrings.WixSettingsPropertyPageName;
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Gets a project property.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>
        /// Value of the property, or null if the property is unset or inconsistent across configurations.
        /// </returns>
        public override string GetProperty(string propertyName)
        {
            if (propertyName == WixProjectFileConstants.DefineConstants)
            {
                string unifiedValue = null;

                ProjectProperty property = new ProjectProperty(this.ProjectMgr, WixProjectFileConstants.DefineConstants);
                for (int i = 0; i < this.ProjectConfigs.Count; i++)
                {
                    ProjectConfig config = this.ProjectConfigs[i];

                    string constantsString = property.GetValue(false, new ProjectConfig[] { config });
                    if (constantsString != null)
                    {
                        List<string> constants = new List<string>(constantsString.Split(';'));
                        WixHelperMethods.RemoveAllMatch(constants, DebugDefine);
                        constantsString = String.Join(";", constants.ToArray());
                    }

                    if (i == 0)
                    {
                        unifiedValue = constantsString;
                    }
                    else if (constantsString != unifiedValue)
                    {
                        unifiedValue = null;
                    }
                }

                return unifiedValue;
            }
            else if (propertyName == WixProjectFileConstants.WarningLevel)
            {
                CheckState pedantic = this.GetPropertyCheckState(WixProjectFileConstants.Pedantic);
                CheckState suppressAllWarnings = this.GetPropertyCheckState(WixProjectFileConstants.SuppressAllWarnings);

                if (pedantic == CheckState.Indeterminate || suppressAllWarnings == CheckState.Indeterminate ||
                    (pedantic == CheckState.Checked && suppressAllWarnings == CheckState.Checked))
                {
                    return null;
                }
                else if (pedantic == CheckState.Checked)
                {
                    return ((int)WixWarningLevel.Pedantic).ToString(CultureInfo.InvariantCulture);
                }
                else if (suppressAllWarnings == CheckState.Checked)
                {
                    return ((int)WixWarningLevel.None).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    return ((int)WixWarningLevel.Normal).ToString(CultureInfo.InvariantCulture);
                }
            }
            else
            {
                return base.GetProperty(propertyName);
            }
        }

        /// <summary>
        /// Gets a property value as a tri-state checkbox value.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns>Boolean check state, or Indeterminate if the property is
        /// inconsistent across configurations.</returns>
        public override CheckState GetPropertyCheckState(string propertyName)
        {
            if (propertyName == WixProjectFileConstants.DefineDebugConstant)
            {
                CheckState unifiedCheckState = CheckState.Indeterminate;

                ProjectProperty property = new ProjectProperty(this.ProjectMgr, WixProjectFileConstants.DefineConstants);
                for (int i = 0; i < this.ProjectConfigs.Count; i++)
                {
                    ProjectConfig config = this.ProjectConfigs[i];

                    CheckState checkState = CheckState.Unchecked;
                    string constantsString = property.GetValue(false, new ProjectConfig[] { config });
                    if (constantsString != null)
                    {
                        List<string> constants = new List<string>(constantsString.Split(';'));
                        checkState = constants.Contains(DebugDefine) ? CheckState.Checked : CheckState.Unchecked;
                    }

                    if (i == 0)
                    {
                        unifiedCheckState = checkState;
                    }
                    else if (checkState != unifiedCheckState)
                    {
                        unifiedCheckState = CheckState.Indeterminate;
                    }
                }

                return unifiedCheckState;
            }
            else
            {
                return base.GetPropertyCheckState(propertyName);
            }
        }

        /// <summary>
        /// Sets a project property.
        /// </summary>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">Value of the property.</param>
        public override void SetProperty(string propertyName, string value)
        {
            if (propertyName == WixProjectFileConstants.DefineConstants)
            {
                ProjectProperty property = new ProjectProperty(this.ProjectMgr, WixProjectFileConstants.DefineConstants);
                foreach (ProjectConfig config in this.ProjectConfigs)
                {
                    string existingConstantsString = property.GetValue(false, new ProjectConfig[] { config });
                    if (existingConstantsString == null)
                    {
                        existingConstantsString = String.Empty;
                    }

                    List<string> existingConstants = new List<string>(existingConstantsString.Split(';'));
                    string constantsString = value.Trim();
                    List<string> constants = new List<string>(constantsString.Split(';'));

                    if (WixHelperMethods.RemoveAllMatch(constants, DebugDefine) > 0 || existingConstants.Contains(DebugDefine))
                    {
                        constants.Insert(0, DebugDefine);
                    }

                    constantsString = String.Join(";", constants.ToArray());
                    if (constantsString != existingConstantsString)
                    {
                        property.SetValue(constantsString, new ProjectConfig[] { config });
                        this.IsDirty = true;
                    }
                }
            }
            else if (propertyName == WixProjectFileConstants.DefineDebugConstant)
            {
                ProjectProperty property = new ProjectProperty(this.ProjectMgr, WixProjectFileConstants.DefineConstants);
                foreach (ProjectConfig config in this.ProjectConfigs)
                {
                    string existingConstantsString = property.GetValue(false, new ProjectConfig[] { config });
                    if (existingConstantsString == null)
                    {
                        existingConstantsString = String.Empty;
                    }

                    List<string> constants = new List<string>(existingConstantsString.Split(';'));

                    WixHelperMethods.RemoveAllMatch(constants, DebugDefine);
                    if (value == Boolean.TrueString)
                    {
                        constants.Insert(0, DebugDefine);
                    }

                    string constantsString = String.Join(";", constants.ToArray());
                    if (constantsString != existingConstantsString)
                    {
                        property.SetValue(constantsString, new ProjectConfig[] { config });
                        this.IsDirty = true;
                    }
                }
            }
            else if (propertyName == WixProjectFileConstants.WarningLevel)
            {
                WixWarningLevel warningLevel = (WixWarningLevel)Int32.Parse(value, CultureInfo.InvariantCulture);
                if (warningLevel == WixWarningLevel.None)
                {
                    base.SetProperty(WixProjectFileConstants.SuppressAllWarnings, Boolean.TrueString);
                    base.SetProperty(WixProjectFileConstants.Pedantic, Boolean.FalseString);
                }
                else if (warningLevel == WixWarningLevel.Normal)
                {
                    base.SetProperty(WixProjectFileConstants.SuppressAllWarnings, Boolean.FalseString);
                    base.SetProperty(WixProjectFileConstants.Pedantic, Boolean.FalseString);
                }
                else if (warningLevel == WixWarningLevel.Pedantic)
                {
                    base.SetProperty(WixProjectFileConstants.SuppressAllWarnings, Boolean.FalseString);
                    base.SetProperty(WixProjectFileConstants.Pedantic, Boolean.TrueString);
                }
            }
            else
            {
                base.SetProperty(propertyName, value);
            }
        }

        /// <summary>
        /// Normalizes the OutputPath property by ensuring a trailing slash.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">Property value entered by the user.</param>
        /// <returns>Normalized property value.</returns>
        public override string Normalize(string propertyName, string value)
        {
            value = base.Normalize(propertyName, value);

            if (propertyName == WixProjectFileConstants.OutputPath && value.Length > 0)
            {
                value = WixHelperMethods.EnsureTrailingDirectoryChar(value);
            }

            return value;
        }

        /// <summary>
        /// Creates the controls that constitute the property page. This should be safe to re-entrancy.
        /// </summary>
        /// <returns>The newly created main control that hosts the property page.</returns>
        protected override WixPropertyPagePanel CreatePropertyPagePanel()
        {
            return new WixBuildPropertyPagePanel(this);
        }
    }
}