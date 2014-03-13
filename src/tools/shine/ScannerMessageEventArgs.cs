//-------------------------------------------------------------------------------------------------
// <copyright file="ScannerMessageEventArgs.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
