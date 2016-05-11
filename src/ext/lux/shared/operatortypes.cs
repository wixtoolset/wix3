// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Lux
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// The allowed operators for the LuxUnitTest MSI table 'op' column.
    /// </summary>
    public enum LuxOperator
    {
        /// <summary>No value specified.</summary>
        NotSet = 0,

        /// <summary>Equality comparison.</summary>
        Equal,

        /// <summary>Inequality comparison.</summary>
        NotEqual,

        /// <summary>Case-insensitive equality comparison.</summary>
        CaseInsensitiveEqual,

        /// <summary>Case-insensitive inequality comparison.</summary>
        CaseInsensitiveNotEqual,
    }    
}
