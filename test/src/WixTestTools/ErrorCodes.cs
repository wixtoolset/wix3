// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;

    /// <summary>
    /// Common windows error codes (from winerror.h).  This is a static class of constants rather than an enum so it can be inherited and extended for custom errors.
    /// </summary>
    public static class ErrorCodes
    {
        public const Int32 ERROR_SUCCESS = 0;
        public const Int32 ERROR_INVALID_FUNCTION = 1;
        public const Int32 ERROR_SUCCESS_REBOOT_INITIATED = 1641;
        public const Int32 ERROR_SUCCESS_REBOOT_REQUIRED = 3010;
        public const Int32 ERROR_INSTALL_USEREXIT = 1602;
        public const Int32 ERROR_INSTALL_FAILURE = 1603;

        public static int ToHresult(int errorCode)
        {
            return unchecked((int)(((uint)errorCode & 0x0000FFFF) | (7 << 16) | 0x80000000));
        }
    }
}
