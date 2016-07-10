// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Base class for creating a validator extension. This default implementation
    /// will fire and event with the ICE name and description.
    /// </summary>
    public class ValidatorExtension : IMessageHandler
    {
        private string databaseFile;
        private Hashtable indexedSourceLineNumbers;
        private Output output;
        private SourceLineNumberCollection sourceLineNumbers;

        /// <summary>
        /// Instantiate a new <see cref="ValidatorExtension"/>.
        /// </summary>
        public ValidatorExtension()
        {
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Gets or sets the path to the database to validate.
        /// </summary>
        /// <value>The path to the database to validate.</value>
        public string DatabaseFile
        {
            get { return this.databaseFile; }
            set { this.databaseFile = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Output"/> for inding source line information.
        /// </summary>
        /// <value>The <see cref="Output"/> for finding source line information.</value>
        public Output Output
        {
            get { return this.output; }
            set { this.output = value; }
        }

        /// <summary>
        /// Called at the beginning of the validation of a database file.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="Validator"/> will set
        /// <see cref="DatabaseFile"/> before calling InitializeValidator.</para>
        /// <para><b>Notes to Inheritors:</b> When overriding
        /// <b>InitializeValidator</b> in a derived class, be sure to call
        /// the base class's <b>InitializeValidator</b> to thoroughly
        /// initialize the extension.</para>
        /// </remarks>
        public virtual void InitializeValidator()
        {
            if (this.databaseFile != null)
            {
                this.sourceLineNumbers = SourceLineNumberCollection.FromFileName(databaseFile);
            }
        }

        /// <summary>
        /// Called at the end of the validation of a database file.
        /// </summary>
        /// <remarks>
        /// <para>The default implementation will nullify source lines.</para>
        /// <para><b>Notes to Inheritors:</b> When overriding
        /// <b>FinalizeValidator</b> in a derived class, be sure to call
        /// the base class's <b>FinalizeValidator</b> to thoroughly
        /// finalize the extension.</para>
        /// </remarks>
        public virtual void FinalizeValidator()
        {
            this.sourceLineNumbers = null;
        }

        /// <summary>
        /// Logs a message from the <see cref="Validator"/>.
        /// </summary>
        /// <param name="message">A <see cref="String"/> of tab-delmited tokens
        /// in the validation message.</param>
        public virtual void Log(string message)
        {
            this.Log(message, null);
        }

        /// <summary>
        /// Logs a message from the <see cref="Validator"/>.
        /// </summary>
        /// <param name="message">A <see cref="String"/> of tab-delmited tokens
        /// in the validation message.</param>
        /// <param name="action">The name of the action to which the message
        /// belongs.</param>
        /// <exception cref="ArgumentNullException">The message cannot be null.
        /// </exception>
        /// <exception cref="WixException">The message does not contain four (4)
        /// or more tab-delimited tokens.</exception>
        /// <remarks>
        /// <para><paramref name="message"/> a tab-delimited set of tokens,
        /// formatted according to Windows Installer guidelines for ICE
        /// message. The following table lists what each token by index
        /// should mean.</para>
        /// <para><paramref name="action"/> a name that represents the ICE
        /// action that was executed (e.g. 'ICE08').</para>
        /// <list type="table">
        /// <listheader>
        ///     <term>Index</term>
        ///     <description>Description</description>
        /// </listheader>
        /// <item>
        ///     <term>0</term>
        ///     <description>Name of the ICE.</description>
        /// </item>
        /// <item>
        ///     <term>1</term>
        ///     <description>Message type. See the following list.</description>
        /// </item>
        /// <item>
        ///     <term>2</term>
        ///     <description>Detailed description.</description>
        /// </item>
        /// <item>
        ///     <term>3</term>
        ///     <description>Help URL or location.</description>
        /// </item>
        /// <item>
        ///     <term>4</term>
        ///     <description>Table name.</description>
        /// </item>
        /// <item>
        ///     <term>5</term>
        ///     <description>Column name.</description>
        /// </item>
        /// <item>
        ///     <term>6</term>
        ///     <description>This and remaining fields are primary keys
        ///     to identify a row.</description>
        /// </item>
        /// </list>
        /// <para>The message types are one of the following value.</para>
        /// <list type="table">
        /// <listheader>
        ///     <term>Value</term>
        ///     <description>Message Type</description>
        /// </listheader>
        /// <item>
        ///     <term>0</term>
        ///     <description>Failure message reporting the failure of the
        ///     ICE custom action.</description>
        /// </item>
        /// <item>
        ///     <term>1</term>
        ///     <description>Error message reporting database authoring that
        ///     case incorrect behavior.</description>
        /// </item>
        /// <item>
        ///     <term>2</term>
        ///     <description>Warning message reporting database authoring that
        ///     causes incorrect behavior in certain cases. Warnings can also
        ///     report unexpected side-effects of database authoring.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>3</term>
        ///     <description>Informational message.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public virtual void Log(string message, string action)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            string[] messageParts = message.Split('\t');
            if (3 > messageParts.Length)
            {
                if (null == action)
                {
                    throw new WixException(WixErrors.UnexpectedExternalUIMessage(message));
                }
                else
                {
                    throw new WixException(WixErrors.UnexpectedExternalUIMessage(message, action));
                }
            }

            SourceLineNumberCollection messageSourceLineNumbers = null;
            if (6 < messageParts.Length)
            {
                string[] primaryKeys = new string[messageParts.Length - 6];

                Array.Copy(messageParts, 6, primaryKeys, 0, primaryKeys.Length);

                messageSourceLineNumbers = this.GetSourceLineNumbers(messageParts[4], primaryKeys);
            }
            else // use the file name as the source line information
            {
                messageSourceLineNumbers = this.sourceLineNumbers;
            }

            switch (messageParts[1])
            {
                case "0":
                case "1":
                    this.OnMessage(WixErrors.ValidationError(messageSourceLineNumbers, messageParts[0], messageParts[2]));
                    break;
                case "2":
                    this.OnMessage(WixWarnings.ValidationWarning(messageSourceLineNumbers, messageParts[0], messageParts[2]));
                    break;
                case "3":
                    this.OnMessage(WixVerboses.ValidationInfo(messageParts[0], messageParts[2]));
                    break;
                default:
                    throw new WixException(WixErrors.InvalidValidatorMessageType(messageParts[1]));
            }
        }
 
        /// <summary>
        /// Gets the source line information (if available) for a row by its table name and primary key.
        /// </summary>
        /// <param name="tableName">The table name of the row.</param>
        /// <param name="primaryKeys">The primary keys of the row.</param>
        /// <returns>The source line number information if found; null otherwise.</returns>
        protected SourceLineNumberCollection GetSourceLineNumbers(string tableName, string[] primaryKeys)
        {
            // source line information only exists if an output file was supplied
            if (null != this.output)
            {
                // index the source line information if it hasn't been indexed already
                if (null == this.indexedSourceLineNumbers)
                {
                    this.indexedSourceLineNumbers = new Hashtable();

                    // index each real table
                    foreach (Table table in this.output.Tables)
                    {
                        // skip unreal tables
                        if (table.Definition.IsUnreal)
                        {
                            continue;
                        }

                        // index each row
                        foreach (Row row in table.Rows)
                        {
                            // skip rows that don't contain source line information
                            if (null == row.SourceLineNumbers)
                            {
                                continue;
                            }

                            // index the row using its table name and primary key
                            string primaryKey = row.GetPrimaryKey(';');
                            if (null != primaryKey)
                            {
                                string key = String.Concat(table.Name, ":", primaryKey);

                                if (this.indexedSourceLineNumbers.ContainsKey(key))
                                {
                                    this.OnMessage(WixWarnings.DuplicatePrimaryKey(row.SourceLineNumbers, primaryKey, table.Name));
                                }
                                else
                                {
                                    this.indexedSourceLineNumbers.Add(key, row.SourceLineNumbers);
                                }
                            }
                        }
                    }
                }

                return (SourceLineNumberCollection)this.indexedSourceLineNumbers[String.Concat(tableName, ":", String.Join(";", primaryKeys))];
            }

            // use the file name as the source line information
            return this.sourceLineNumbers;
        }

        /// <summary>
        /// Sends a message to the <see cref="Message"/> delegate if there is one.
        /// </summary>
        /// <param name="e">Message event arguments.</param>
        /// <remarks>
        /// <para><b>Notes to Inheritors:</b> When overriding <b>OnMessage</b>
        /// in a derived class, be sure to call the base class's
        /// <b>OnMessage</b> method so that registered delegates recieve
        /// the event.</para>
        /// </remarks>
        public virtual void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != this.Message)
            {
                this.Message(this, e);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
   }
}
