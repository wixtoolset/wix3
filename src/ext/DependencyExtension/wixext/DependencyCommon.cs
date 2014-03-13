//-------------------------------------------------------------------------------------------------
// <copyright file="DependencyCommon.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML toolset dependency extension common functionality.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using Microsoft.Tools.WindowsInstallerXml;

    internal static class DependencyCommon
    {
        // Bundle attributes are in the upper 32-bits.
        internal const int ProvidesAttributesBundle = 0x10000;

        // Same values as for the Upgrade table in Windows Installer.
        internal const int RequiresAttributesMinVersionInclusive = 256;
        internal const int RequiresAttributesMaxVersionInclusive = 512;

        // The root registry key for the dependency extension. We write to Software\Classes explicitly
        // based on the current security context instead of HKCR. See
        // http://msdn.microsoft.com/en-us/library/ms724475(VS.85).aspx for more information.
        internal static readonly string RegistryRoot = @"Software\Classes\Installer\Dependencies\";
        internal static readonly string RegistryDependents = "Dependents";

        // The following characters cannot be used in a provider key.
        internal static readonly char[] InvalidCharacters = new char[] { ' ', '\"', ';', '\\' };
    }
}
