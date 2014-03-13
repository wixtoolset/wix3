//-------------------------------------------------------------------------------------------------
// <copyright file="operatortypes.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Lux unit-test framework test operator types enum.
// </summary>
//-------------------------------------------------------------------------------------------------



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
