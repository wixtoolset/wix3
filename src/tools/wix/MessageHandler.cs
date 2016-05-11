// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Resources;
    using System.Text;

    /// <summary>
    /// Message event handler delegate.
    /// </summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="e">Arguments for the message event.</param>
    public delegate void MessageEventHandler(object sender, MessageEventArgs e);

    /// <summary>
    /// Enum for message to display.
    /// </summary>
    public enum MessageLevel
    {
        /// <summary>Display nothing.</summary>
        Nothing,

        /// <summary>Display information.</summary>
        Information,

        /// <summary>Display warning.</summary>
        Warning,

        /// <summary>Display error.</summary>
        Error,
    }

    /// <summary>
    /// Message handling class.
    /// </summary>
    public class MessageHandler
    {
        private const int All = 0;

        private bool encounteredError;

        private bool showVerboseMessages;
        private bool sourceTrace;

        private Dictionary<int, bool> suppressedWarnings;
        private Dictionary<int, bool> warningsAsErrors;

        /// <summary>
        /// Instantiate a new message handler.
        /// </summary>
        public MessageHandler()
        {
            this.suppressedWarnings = new Dictionary<int, bool>();
            this.warningsAsErrors = new Dictionary<int, bool>();

            this.suppressedWarnings[All] = false;
            this.warningsAsErrors[All] = false;
        }

        /// <summary>
        /// Gets a bool indicating whether an error has been found.
        /// </summary>
        /// <value>A bool indicating whether an error has been found.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Gets or sets the option to show verbose messages.
        /// </summary>
        /// <value>The option to show verbose messages.</value>
        public bool ShowVerboseMessages
        {
            get { return this.showVerboseMessages; }
            set { this.showVerboseMessages = value; }
        }

        /// <summary>
        /// Gets or sets the option to suppress all warning messages.
        /// </summary>
        /// <value>The option to suppress all warning messages.</value>
        public bool SuppressAllWarnings
        {
            get { return this.suppressedWarnings[All]; }
            set { this.suppressedWarnings[All] = value; }
        }

        /// <summary>
        /// Gets and sets the option to show a full source trace when messages are output.
        /// </summary>
        /// <value>The option to show a full source trace when messages are output.</value>
        public bool SourceTrace
        {
            get { return this.sourceTrace; }
            set { this.sourceTrace = value; }
        }

        /// <summary>
        /// Gets and sets the option to treat warnings as errors.
        /// </summary>
        /// <value>Option to treat warnings as errors.</value>
        public bool WarningAsError
        {
            get { return this.warningsAsErrors[All]; }
            set { this.warningsAsErrors[All] = value; }
        }

        /// <summary>
        /// Adds a warning message id to be elevated to an error message.
        /// </summary>
        /// <param name="warningNumber">Id of the message to elevate.</param>
        /// <remarks>
        /// Suppressed warnings will not be elevated as errors.
        /// </remarks>
        public void ElevateWarningMessage(int warningNumber)
        {
            this.warningsAsErrors[warningNumber] = true;
        }

        /// <summary>
        /// Adds a warning message id to be suppressed in message output.
        /// </summary>
        /// <param name="warningNumber">Id of the message to suppress.</param>
        /// <remarks>
        /// Suppressed warnings will not be elevated as errors.
        /// </remarks>
        public void SuppressWarningMessage(int warningNumber)
        {
            this.suppressedWarnings[warningNumber] = true;
        }

        /// <summary>
        /// Get a message string.
        /// </summary>
        /// <param name="sender">Sender of the message.</param>
        /// <param name="mea">Arguments for the message event.</param>
        /// <returns>The message string.</returns>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        public string GetMessageString(object sender, MessageEventArgs mea)
        {
            MessageLevel messageLevel = this.CalculateMessageLevel(mea);

            if (MessageLevel.Nothing != messageLevel)
            {
                return this.GenerateMessageString(messageLevel, mea);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines the level of this message, when taking into account warning-as-error, 
        /// warning level, verbosity level and message suppressed by the caller.
        /// </summary>
        /// <param name="mea">Event arguments for the message.</param>
        /// <returns>MessageLevel representing the level of this message.</returns>
        protected MessageLevel CalculateMessageLevel(MessageEventArgs mea)
        {
            if (null == mea)
            {
                throw new ArgumentNullException("mea");
            }

            MessageLevel messageLevel = MessageLevel.Nothing;
            
            if (mea is WixVerboseEventArgs)
            {
                if (this.showVerboseMessages)
                {
                    messageLevel = MessageLevel.Information;
                }
            }
            else if (mea is WixWarningEventArgs)
            {
                if (this.SuppressAllWarnings || this.suppressedWarnings.ContainsKey(mea.Id))
                {
                    messageLevel = MessageLevel.Nothing;
                }
                else if (this.WarningAsError || this.warningsAsErrors.ContainsKey(mea.Id))
                {
                    messageLevel = MessageLevel.Error;
                }
                else
                {
                    messageLevel = MessageLevel.Warning;
                }
            }
            else if (mea is WixErrorEventArgs)
            {
                messageLevel = MessageLevel.Error;
            }
            else if (mea is WixGenericMessageEventArgs)
            {
                messageLevel = mea.Level;
            }
            else
            {
                Debug.Assert(false, String.Format(CultureInfo.InvariantCulture, "Unknown MessageEventArgs type: {0}.", mea.GetType().ToString()));
            }

            mea.Level = messageLevel;
            return messageLevel;
        }

        /// <summary>
        /// Creates a properly formatted message string.
        /// </summary>
        /// <param name="messageLevel">Level of the message, as generated by MessageLevel(MessageEventArgs).</param>
        /// <param name="mea">Event arguments for the message.</param>
        /// <returns>String containing the formatted message.</returns>
        protected virtual string GenerateMessageString(MessageLevel messageLevel, MessageEventArgs mea)
        {
            if (null == mea)
            {
                throw new ArgumentNullException("mea");
            }

            if (MessageLevel.Nothing == messageLevel)
            {
                return String.Empty;
            }

            if (MessageLevel.Error == messageLevel)
            {
                this.encounteredError = true;
            }

            return String.Format(CultureInfo.InvariantCulture, mea.ResourceManager.GetString(mea.ResourceName), mea.MessageArgs);
        }
    }
}
