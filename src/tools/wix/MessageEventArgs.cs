// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Resources;

    /// <summary>
    /// Event args for message events.
    /// </summary>
    public abstract class MessageEventArgs : EventArgs
    {
        private SourceLineNumberCollection sourceLineNumbers;
        private int id;
        private string resourceName;
        private object[] messageArgs;
        private MessageLevel level;

        /// <summary>
        /// Creates a new MessageEventArgs.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for the message.</param>
        /// <param name="id">Id for the message.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="messageArgs">Arguments for the format string.</param>
        protected MessageEventArgs(SourceLineNumberCollection sourceLineNumbers, int id, string resourceName, params object[] messageArgs)
        {
            this.sourceLineNumbers = sourceLineNumbers;
            this.id = id;
            this.resourceName = resourceName;
            this.messageArgs = messageArgs;

            // Default to Nothing, since the default MessageEventArgs container
            // classes define a level, and only WixErrorEventArgs previously
            // determined that an error occured without throwing.
            this.level = MessageLevel.Nothing;
        }

        /// <summary>
        /// Gets the resource manager for this event args.
        /// </summary>
        /// <value>The resource manager for this event args.</value>
        public abstract ResourceManager ResourceManager
        {
            get;
        }

        /// <summary>
        /// Gets the source line numbers.
        /// </summary>
        /// <value>The source line numbers.</value>
        public SourceLineNumberCollection SourceLineNumbers
        {
            get { return this.sourceLineNumbers; }
        }

        /// <summary>
        /// Gets the Id for the message.
        /// </summary>
        /// <value>The Id for the message.</value>
        public int Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the name of the resource.
        /// </summary>
        /// <value>The name of the resource.</value>
        public string ResourceName
        {
            get { return this.resourceName; }
        }

        /// <summary>
        /// Gets or sets the <see cref="MessageLevel"/> for the message.
        /// </summary>
        /// <value>The <see cref="MessageLevel"/> for the message.</value>
        /// <remarks>
        /// The <see cref="MessageHandler"/> may set the level differently
        /// depending on suppression and escalation of different message levels.
        /// Message handlers should check the level to determine if an error
        /// or other message level was raised.
        /// </remarks>
        public MessageLevel Level
        {
            get { return this.level; }
            set { this.level = value; }
        }

        /// <summary>
        /// Gets the arguments for the format string.
        /// </summary>
        /// <value>The arguments for the format string.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] MessageArgs
        {
            get { return this.messageArgs; }
        }
    }
}
