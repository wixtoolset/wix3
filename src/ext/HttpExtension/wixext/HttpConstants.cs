//-------------------------------------------------------------------------------------------------
// <copyright file="HttpConstants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Constants used by HttpExtension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;

    internal static class HttpConstants
    {
        // from winnt.h
        public const int GENERIC_ALL = 0x10000000;
        public const int GENERIC_EXECUTE = 0x20000000;
        public const int GENERIC_WRITE = 0x40000000;

        // from wixhttpca.cpp
        public const int heReplace = 0;
        public const int heIgnore = 1;
        public const int heFail = 2;
    }
}
