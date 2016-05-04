// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;

    /// <summary>
    /// Interface for passing command line arguments to tool classes.
    /// </summary>
    public interface ICommandArgs
    {
        /// <summary>
        /// Whether to remove intermediate build files.
        /// </summary>
        bool NoTidy { get; }
    }
}
