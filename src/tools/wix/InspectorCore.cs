//-------------------------------------------------------------------------------------------------
// <copyright file="InspectorCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Core facilities for inspector extensions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Core facilities for inspector extensions.
    /// </summary>
    public sealed class InspectorCore : IMessageHandler
    {
        private bool encounteredError;
        private MessageEventHandler messageHandler;

        /// <summary>
        /// Creates a new instance of the <see cref="InspectorCore"/> class.
        /// </summary>
        /// <param name="messageHandler">The <see cref="MessageEventHandler"/> for sending messages to the logger.</param>
        internal InspectorCore(MessageEventHandler messageHandler)
        {
            this.messageHandler = messageHandler;
        }

        /// <summary>
        /// Gets whether an error occured.
        /// </summary>
        /// <value>Whether an error occured.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Logs a message to the log handler.
        /// </summary>
        /// <param name="e">The <see cref="MessageEventArgs"/> that contains information to log.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            if (null != this.messageHandler)
            {
                MessageEventHandler handler = this.messageHandler;
                handler(this, e);

                if (MessageLevel.Error == e.Level)
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
