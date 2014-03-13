//-------------------------------------------------------------------------------------------------
// <copyright file="WriteRegistry.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WriteRegistry class.
// </summary>
//-------------------------------------------------------------------------------------------------

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
    /// MSBuild task for writing a value to the registry.
    /// </summary>
    public class WriteRegistry : RegistryBase
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private string value;
        private RegistryValueKind valueKind;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteRegistry"/> class.
        /// </summary>
        public WriteRegistry()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the value of the registry key.
        /// </summary>
        public string Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        /// <summary>
        /// Gets or sets the type of the registry key to write.
        /// </summary>
        public string ValueKind
        {
            get { return this.valueKind.ToString(); }
            set { this.valueKind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), value); }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Executes the task by writing a value to the registry.
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
                using (RegistryKey regKey = hiveKey.CreateSubKey(this.Key, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (regKey == null)
                    {
                        this.Log.LogError("Could not create registry key at '{0}'.", this.Key);
                        return false;
                    }

                    regKey.SetValue(this.Name, this.Value, this.valueKind);
                    this.Log.LogMessage(MessageImportance.Normal, @"Wrote registry value at '{0}\{1}\{2}' to '{3}'.", hiveKey.Name, this.Key, this.Name, this.Value);
                }
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
