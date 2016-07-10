// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.WindowsInstaller
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Contains specific information about an error encountered by the <see cref="View.Validate"/>,
    /// <see cref="View.ValidateNew"/>, or <see cref="View.ValidateFields"/> methods of the
    /// <see cref="View"/> class.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct ValidationErrorInfo
    {
        private ValidationError error;
        private string column;
        
        internal ValidationErrorInfo(ValidationError error, string column)
        {
            this.error = error;
            this.column = column;
        }

        /// <summary>
        /// Gets the type of validation error encountered.
        /// </summary>
        public ValidationError Error
        {
            get
            {
                return this.error;
            }
        }

        /// <summary>
        /// Gets the column containing the error, or null if the error applies to the whole row.
        /// </summary>
        public string Column
        {
            get
            {
                return this.column;
            }
        }
    }
}
