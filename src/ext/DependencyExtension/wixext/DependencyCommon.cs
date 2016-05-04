// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
