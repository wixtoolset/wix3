// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Security;
    using System.Security.AccessControl;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    /// <summary>
    /// MSBuild task for reading a value from the registry.
    /// </summary>
    public class ReadRegistry : RegistryBase
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private bool failIfMissing;
        private bool keyExists;
        private bool nameExists;
        private string regValue;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadRegistry"/> class.
        /// </summary>
        public ReadRegistry()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets an optional value indicating whether to fail the task if the registry key or name is not found.
        /// </summary>
        public bool FailIfMissing
        {
            get { return this.failIfMissing; }
            set { this.failIfMissing = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the registry key was found.
        /// </summary>
        [Output]
        public bool KeyExists
        {
            get { return this.keyExists; }
        }

        /// <summary>
        /// Gets a value indicating whether the registry name was found.
        /// </summary>
        [Output]
        public bool NameExists
        {
            get { return this.nameExists; }
        }

        /// <summary>
        /// Gets the value of the registry key.
        /// </summary>
        [Output]
        public string Value
        {
            get { return this.regValue; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Executes the task by reading a value from the registry.
        /// </summary>
        /// <returns><see langword="true"/> if the task successfully executed; otherwise, <see langword="false"/>.</returns>
        public override bool Execute()
        {
            // default to an empty string for the value
            this.regValue = String.Empty;

            // get the registry hive
            RegistryKey hiveKey = this.HiveRegistryKey;
            if (hiveKey == null)
            {
                return false;
            }

            try
            {
                using (RegistryKey regKey = hiveKey.OpenSubKey(this.Key, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
                {
                    this.keyExists = (regKey != null);

                    if (regKey != null)
                    {
                        object readValue = regKey.GetValue(this.Name);
                        this.nameExists = (readValue != null);

                        if (readValue != null)
                        {
                            this.regValue = readValue.ToString();
                            this.Log.LogMessage(MessageImportance.Low, @"Registry value at '{0}\{1}\{2}' is '{3}'.", hiveKey.Name, this.Key, this.Name, this.Value);
                        }
                        else
                        {
                            if (this.FailIfMissing)
                            {
                                this.Log.LogError("Registry name '{0}' not found at '{1}'.", this.Name, this.Key);
                                return false;
                            }
                            else
                            {
                                this.Log.LogMessage(MessageImportance.Low, "Registry name '{0}' not found at '{1}'.", this.Name, this.Key);
                            }
                        }
                    }
                    else
                    {
                        if (this.FailIfMissing)
                        {
                            this.Log.LogError("Registry key not found at '{0}'.", this.Key);
                            return false;
                        }
                        else
                        {
                            this.Log.LogMessage(MessageImportance.Low, "Registry key not found at '{0}'.", this.Key);
                        }
                    }
                }
            }
            catch (SecurityException e)
            {
                this.Log.LogErrorFromException(e);
                return false;
            }

            return true;
        }
    }
}
