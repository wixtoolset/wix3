// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Interface for handling messages (error/warning/verbose).
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Sends a message with the given arguments.
        /// </summary>
        /// <param name="e">Message arguments.</param>
        void OnMessage(MessageEventArgs e);
    }
}
