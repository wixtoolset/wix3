// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Replaces occurances of OldValues with NewValues in String.
    /// </summary>
    public class ReplaceString : Task
    {
        /// <summary>
        /// Text to operate on.
        /// </summary>
        [Output]
        [Required]
        public string Text { get; set; }

        /// <summary>
        /// List of old values to replace.
        /// </summary>
        [Required]
        public string OldValue { get; set; }

        /// <summary>
        /// List of new values to replace old values with.  If not specified, occurances of OldValue will be removed.
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Does the string replacement.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            if (String.IsNullOrEmpty(this.Text))
            {
                return true;
            }

            if (String.IsNullOrEmpty(this.OldValue))
            {
                Log.LogError("OldValue must be specified");
                return false;
            }

            this.Text = this.Text.Replace(this.OldValue, this.NewValue);

            return true;
        }
    }
}
