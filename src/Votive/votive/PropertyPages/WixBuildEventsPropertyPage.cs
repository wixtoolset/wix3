//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildEventsPropertyPage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBuildEventsPropertyPage class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.Build.BuildEngine;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Property page for the build events.
    /// </summary>
    [ComVisible(true)]
    [Guid("A71983CF-33B9-4241-9B5A-80091BCE57DA")]
    internal class WixBuildEventsPropertyPage : WixPropertyPage
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixBuildEventsPropertyPage"/> class.
        /// </summary>
        public WixBuildEventsPropertyPage()
        {
            this.PageName = WixStrings.WixBuildEventsPropertyPageName;
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
            string value = base.GetProperty(propertyName);

            if (propertyName == WixProjectFileConstants.RunPostBuildEvent)
            {
                try
                {
                    RunPostBuildEvent outputType = (RunPostBuildEvent)Enum.Parse(typeof(RunPostBuildEvent), value, true);
                    value = ((int)outputType).ToString(CultureInfo.InvariantCulture);
                }
                catch (ArgumentException)
                {
                    value = null;
                }

                if (value == null)
                {
                    value = ((int)RunPostBuildEvent.OnBuildSuccess).ToString(CultureInfo.InvariantCulture);
                }
            }

            return value;
        }

        /// <summary>
        /// Sets a project property.
        /// </summary>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">Value of the property.</param>
        public override void SetProperty(string propertyName, string value)
        {
            if (propertyName == WixProjectFileConstants.RunPostBuildEvent)
            {
                RunPostBuildEvent runType = (RunPostBuildEvent)Int32.Parse(value, CultureInfo.InvariantCulture);
                if (Enum.IsDefined(typeof(RunPostBuildEvent), runType))
                {
                    value = runType.ToString();
                }
                else
                {
                    value = null;
                }
            }

            base.SetProperty(propertyName, value);
        }

        /// <summary>
        /// Creates the controls that constitute the property page. This should be safe to re-entrancy.
        /// </summary>
        /// <returns>The newly created main control that hosts the property page.</returns>
        protected override WixPropertyPagePanel CreatePropertyPagePanel()
        {
            return new WixBuildEventsPropertyPagePanel(this);
        }
    }
}