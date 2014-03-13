//-------------------------------------------------------------------------------------------------
// <copyright file="IMessageHandler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Interface for handling messages (error/warning/verbose).
// </summary>
//-------------------------------------------------------------------------------------------------

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