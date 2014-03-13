//-------------------------------------------------------------------------------------------------
// <copyright file="ReplaceString.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the ReplaceString class.
// </summary>
//-------------------------------------------------------------------------------------------------
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
