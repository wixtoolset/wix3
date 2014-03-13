//-------------------------------------------------------------------------------------------------
// <copyright file="MelterCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Decompiles an msi database into WiX source.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Melts a Module Wix document into a ComponentGroup representation.
    /// </summary>
    public sealed class MelterCore : IMessageHandler
    {
        private bool encounteredError;

        /// <summary>
        /// Instantiate a new melter core.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        public MelterCore(MessageEventHandler messageHandler)
        {
            this.MessageHandler = messageHandler;
        }

        /// <summary>
        /// Gets whether the melter core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        private event MessageEventHandler MessageHandler;

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            if (null != this.MessageHandler)
            {
                this.MessageHandler(this, e);
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
