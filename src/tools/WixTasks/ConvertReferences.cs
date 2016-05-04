// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
    public class ConvertReferences : Task
    {
        private string projectOutputGroups;
        private ITaskItem[] projectReferences;
        private ITaskItem[] harvestItems;

        /// <summary>
        /// The total list of cabs in this database
        /// </summary>
        [Output]
        public ITaskItem[] HarvestItems
        {
            get { return this.harvestItems; }
        }

        /// <summary>
        /// The project output groups to harvest.
        /// </summary>
        [Required]
        public string ProjectOutputGroups
        {
            get { return this.projectOutputGroups; }
            set { this.projectOutputGroups = value; }
        }

        /// <summary>
        /// All the project references in the project.
        /// </summary>
        [Required]
        public ITaskItem[] ProjectReferences
        {
            get { return this.projectReferences; }
            set { this.projectReferences = value; }
        }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            List<ITaskItem> newItems = new List<ITaskItem>();

            foreach(ITaskItem item in this.ProjectReferences)
            {
                Dictionary<string, string> newItemMetadeta = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(item.GetMetadata(Common.DoNotHarvest)))
                {
                    continue;
                }

                string refTargetDir = item.GetMetadata("RefTargetDir");
                if (!String.IsNullOrEmpty(refTargetDir))
                {
                    newItemMetadeta.Add("DirectoryIds", refTargetDir);
                }

                string refName = item.GetMetadata("Name");
                if (!String.IsNullOrEmpty(refName))
                {
                    newItemMetadeta.Add("ProjectName", refName);
                }

                newItemMetadeta.Add("ProjectOutputGroups", this.ProjectOutputGroups);

                ITaskItem newItem = new TaskItem(item.ItemSpec, newItemMetadeta);
                newItems.Add(newItem);
            }

            this.harvestItems = newItems.ToArray();

            return true;
        }
    }
}
