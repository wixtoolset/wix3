// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// Parses localization files and localizes database values.
    /// </summary>
    public sealed class Localizer
    {
        private int codepage;
        private Hashtable variables;
        private Dictionary<string, LocalizedControl> localizedControls;

        /// <summary>
        /// Instantiate a new Localizer.
        /// </summary>
        public Localizer()
        {
            this.codepage = -1;
            this.variables = new Hashtable();
            this.localizedControls = new Dictionary<string, LocalizedControl>();
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Gets the codepage.
        /// </summary>
        /// <value>The codepage.</value>
        public int Codepage
        {
            get { return this.codepage; }
        }

        /// <summary>
        /// Add a localization file.
        /// </summary>
        /// <param name="localization">The localization file to add.</param>
        public void AddLocalization(Localization localization)
        {
            if (-1 == this.codepage)
            {
                this.codepage = localization.Codepage;
            }

            foreach (WixVariableRow wixVariableRow in localization.Variables)
            {
                WixVariableRow existingWixVariableRow = (WixVariableRow)this.variables[wixVariableRow.Id];

                if (null == existingWixVariableRow || (existingWixVariableRow.Overridable && !wixVariableRow.Overridable))
                {
                    this.variables[wixVariableRow.Id] = wixVariableRow;
                }
                else if (!wixVariableRow.Overridable)
                {
                    this.OnMessage(WixErrors.DuplicateLocalizationIdentifier(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
                }
            }

            foreach (KeyValuePair<string, LocalizedControl> localizedControl in localization.LocalizedControls)
            {
                if (!this.localizedControls.ContainsKey(localizedControl.Key))
                {
                    this.localizedControls.Add(localizedControl.Key, localizedControl.Value);
                }
            }
        }

        /// <summary>
        /// Get a localized data value.
        /// </summary>
        /// <param name="id">The name of the localization variable.</param>
        /// <returns>The localized data value or null if it wasn't found.</returns>
        public string GetLocalizedValue(string id)
        {
            WixVariableRow wixVariableRow = (WixVariableRow)this.variables[id];

            if (null != wixVariableRow)
            {
                return wixVariableRow.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a localized control.
        /// </summary>
        /// <param name="dialog">The optional id of the control's dialog.</param>
        /// <param name="control">The id of the control.</param>
        /// <returns>The localized control or null if it wasn't found.</returns>
        public LocalizedControl GetLocalizedControl(string dialog, string control)
        {
            LocalizedControl localizedControl;
            this.localizedControls.TryGetValue(LocalizedControl.GetKey(dialog, control), out localizedControl);
            return localizedControl;
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        private void OnMessage(MessageEventArgs mea)
        {
            WixErrorEventArgs errorEventArgs = mea as WixErrorEventArgs;

            if (null != this.Message)
            {
                this.Message(this, mea);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }
}
