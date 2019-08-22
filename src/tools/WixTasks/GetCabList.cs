// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Win32;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class GetCabList : Task
    {
        private ITaskItem database;
        private ITaskItem[] cabList;

        /// <summary>
        /// The list of database files to find cabs in
        /// </summary>
        [Required]
        public ITaskItem Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// The total list of cabs in this database
        /// </summary>
        [Output]
        public ITaskItem[] CabList
        {
            get { return this.cabList; }
        }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            string databaseFile = this.database.ItemSpec;
            Object []args = { };
            System.Collections.Generic.List<ITaskItem> cabNames = new System.Collections.Generic.List<ITaskItem>();

            // If the file doesn't exist, no cabs to return, so exit now
            if (!File.Exists(databaseFile))
            {
                return true;
            }

            using (Database database = new Database(databaseFile))
            {
                // If the media table doesn't exist, no cabs to return, so exit now
                if (null == database.Tables["Media"])
                {
                    return true;
                }

                System.Collections.IList records = database.ExecuteQuery("SELECT `Cabinet` FROM `Media`", args);

                foreach (string cabName in records)
                {
                    if (String.IsNullOrEmpty(cabName) || cabName.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    cabNames.Add(new TaskItem(Path.Combine(Path.GetDirectoryName(databaseFile), cabName)));
                }
            }

            this.cabList = cabNames.ToArray();

            return true;
        }
    }
}
