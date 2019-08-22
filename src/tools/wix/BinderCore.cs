// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Tools.WindowsInstallerXml.Msi;

    /// <summary>
    /// Core class for the binder.
    /// </summary>
    public sealed class BinderCore : IMessageHandler
    {
        public static readonly string IntermediateFolder = "IntermediateFolder";
        public static readonly string OutputPath = "OutputPath";

        private bool encounteredError;
        private TableDefinitionCollection tableDefinitions;
        private Dictionary<string, object> additionalProperties;

        /// <summary>
        /// Event for messages.
        /// </summary>
        private event MessageEventHandler MessageHandler;

        /// <summary>
        /// Constructor for binder core.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        internal BinderCore(MessageEventHandler messageHandler)
        {
            this.tableDefinitions = Installer.GetTableDefinitions();
            this.MessageHandler = messageHandler;
            this.additionalProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets whether the binder core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
            set { this.encounteredError = value; }
        }

        /// <summary>
        /// Gets the table definitions used by the Binder.
        /// </summary>
        /// <value>Table definitions used by the binder.</value>
        public TableDefinitionCollection TableDefinitions
        {
            get { return this.tableDefinitions; }
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public string GenerateIdentifier(string prefix, params string[] args)
        {
            // Backward compatibility not required for new code.
            return Common.GenerateIdentifier(prefix, true, args);
        }

        /// <summary>
        /// Gets the value of the additional property by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the additional property.</param>
        /// <param name="defaultValue">The value to return if not found.</param>
        /// <typeparam name="T">The type of the additional property value.</param>
        /// <returns>
        /// The value of the additional property or the <paramref name="defaultValue"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"> is null or an empty string.</exception>
        public T GetProperty<T>(string name, T defaultValue = default(T))
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (this.additionalProperties.ContainsKey(name))
            {
                return (T)this.additionalProperties[name];
            }

            return defaultValue;
        }

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

        /// <summary>
        /// Sets the value of the additional property by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the additional property.</param>
        /// <param name="value">The value of the additional property.</param>
        /// <typeparam name="T">The type of the additional property value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"> is null or an empty string.</exception>
        internal void SetProperty<T>(string name, T value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            this.additionalProperties[name] = value;
        }
    }
}
