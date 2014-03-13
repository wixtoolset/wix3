//-------------------------------------------------------------------------------------------------
// <copyright file="CreateItemAvoidingInference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build task to create items from inputs without allowing MSBuild inference to kick in.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class CreateItemAvoidingInference : Task
    {
        private string inputProperties;
        private ITaskItem[] outputItems;

        /// <summary>
        /// The output items.
        /// </summary>
        [Output]
        public ITaskItem[] OuputItems
        {
            get { return this.outputItems; }
        }

        /// <summary>
        /// The properties to converty to items.
        /// </summary>
        [Required]
        public string InputProperties
        {
            get { return this.inputProperties; }
            set { this.inputProperties = value; }
        }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            List<ITaskItem> newItems = new List<ITaskItem>();

            foreach (string property in this.inputProperties.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                newItems.Add(new TaskItem(property));
            }

            this.outputItems = newItems.ToArray();

            return true;
        }
    }
}
