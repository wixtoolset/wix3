// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;

namespace Microsoft.Tools.WindowsInstallerXml
{
    public enum ScannerMessageType
    {
        Normal,
        Verbose,
        Warning,
        Error,
    }

    public delegate void ScannerMessageEventHandler(object sender, ScannerMessageEventArgs e);

    public class ScannerMessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public ScannerMessageType Type { get; set; }
    }
}
