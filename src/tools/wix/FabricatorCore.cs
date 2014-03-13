//-------------------------------------------------------------------------------------------------
// <copyright file="FabricatorCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset fabricator core.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The Windows Installer XML Toolset Fabricator core.
    /// </summary>
    public class FabricatorCore
    {
        private bool encounteredError;

        /// <summary>
        /// Instantiates a new FabricatorCore.
        /// </summary>
        /// <param name="messageHandler">The message handler for the core.</param>
        public FabricatorCore(MessageEventHandler messageHandler)
        {
            this.Message = messageHandler;
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        private event MessageEventHandler Message;

        /// <summary>
        /// Gets whether the fabricator core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Gets the event handler used for messages.
        /// </summary>
        /// <value>Message handler.</value>
        public MessageEventHandler MessageEventHandler
        {
            get { return this.Message; }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            WixErrorEventArgs errorEventArgs = mea as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            if (null != this.Message)
            {
                this.Message(this, mea);
                if (MessageLevel.Error == mea.Level)
                {
                    this.encounteredError = true;
                }
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }
}
