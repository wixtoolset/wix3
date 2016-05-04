// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
