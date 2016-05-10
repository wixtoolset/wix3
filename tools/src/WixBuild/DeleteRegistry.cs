// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using System.IO;
    using System.Security;
    using System.Security.AccessControl;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    /// <summary>
    /// MSBuild task for deleting a key from the registry.
    /// </summary>
    public class DeleteRegistry : RegistryBase
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteRegistry"/> class.
        /// </summary>
        public DeleteRegistry()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Executes the task by deleting a key from the registry.
        /// </summary>
        /// <returns><see langword="true"/> if the task successfully executed; otherwise, <see langword="false"/>.</returns>
        public override bool Execute()
        {
            // get the registry hive
            RegistryKey hiveKey = this.HiveRegistryKey;
            if (hiveKey == null)
            {
                return false;
            }

            try
            {
                hiveKey.DeleteSubKeyTree(this.Key);
                this.Log.LogMessage(MessageImportance.Normal, @"Deleted registry key at '{0}\{1}\{2}'.", hiveKey.Name, this.Key, this.Name);
            }
            catch (Exception e)
            {
                if (e is SecurityException || e is UnauthorizedAccessException || e is IOException)
                {
                    this.Log.LogErrorFromException(e);
                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }
    }
}
