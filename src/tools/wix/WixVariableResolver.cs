//-------------------------------------------------------------------------------------------------
// <copyright file="WixVariableResolver.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// WiX variable resolver.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// WiX variable resolver.
    /// </summary>
    public sealed class WixVariableResolver
    {
        private bool encounteredError;
        private Localizer localizer;
        private Hashtable wixVariables;

        /// <summary>
        /// Instantiate a new WixVariableResolver.
        /// </summary>
        public WixVariableResolver()
        {
            this.wixVariables = new Hashtable();
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Gets whether an error was encountered while resolving variables.
        /// </summary>
        /// <value>Whether an error was encountered while resolving variables.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Gets or sets the localizer.
        /// </summary>
        /// <value>The localizer.</value>
        public Localizer Localizer
        {
            get { return this.localizer; }
            set { this.localizer = value; }
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        public void AddVariable(string name, string value)
        {
            if (!this.wixVariables.Contains(name))
            {
                this.wixVariables.Add(name, value);
            }
            else
            {
                this.OnMessage(WixErrors.WixVariableCollision(null, name));
            }
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="wixVariableRow">The WixVariableRow to add.</param>
        public void AddVariable(WixVariableRow wixVariableRow)
        {
            if (!this.wixVariables.Contains(wixVariableRow.Id))
            {
                this.wixVariables.Add(wixVariableRow.Id, wixVariableRow.Value);
            }
            else if (!wixVariableRow.Overridable) // collision
            {
                this.OnMessage(WixErrors.WixVariableCollision(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
            }
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumberCollection sourceLineNumbers, string value, bool localizationOnly)
        {
            bool isDefault = false;
            bool delayedResolve = false;

            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, ref isDefault, ref delayedResolve);
        }
        
        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="isDefault">true if the resolved value was the default.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumberCollection sourceLineNumbers, string value, bool localizationOnly, ref bool isDefault)
        {
            bool delayedResolve = false;

            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, ref isDefault, ref delayedResolve);
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="errorOnUnknown">true if unknown variables should throw errors.</param>
        /// <param name="isDefault">true if the resolved value was the default.</param>
        /// <param name="delayedResolve">true if the value has variables that cannot yet be resolved.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumberCollection sourceLineNumbers, string value, bool localizationOnly, ref bool isDefault, ref bool delayedResolve)
        {
            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, true, ref isDefault, ref delayedResolve);
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="errorOnUnknown">true if unknown variables should throw errors.</param>
        /// <param name="isDefault">true if the resolved value was the default.</param>
        /// <param name="delayedResolve">true if the value has variables that cannot yet be resolved.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumberCollection sourceLineNumbers, string value, bool localizationOnly, bool errorOnUnknown, ref bool isDefault, ref bool delayedResolve)
        {
            MatchCollection matches = Common.WixVariableRegex.Matches(value);

            // the value is the default unless its substituted further down
            isDefault = true;
            delayedResolve = false;

            if (0 < matches.Count)
            {
                StringBuilder sb = new StringBuilder(value);

                // notice how this code walks backward through the list
                // because it modifies the string as we through it
                for (int i = matches.Count - 1; 0 <= i; i--)
                {
                    string variableNamespace = matches[i].Groups["namespace"].Value;
                    string variableId = matches[i].Groups["fullname"].Value;
                    string variableDefaultValue = null;

                    // get the default value if one was specified
                    if (matches[i].Groups["value"].Success)
                    {
                        variableDefaultValue = matches[i].Groups["value"].Value;

                        // localization variables to not support inline default values
                        if ("loc" == variableNamespace)
                        {
                            this.OnMessage(WixErrors.IllegalInlineLocVariable(sourceLineNumbers, variableId, variableDefaultValue));
                        }
                    }

                    // get the scope if one was specified
                    if (matches[i].Groups["scope"].Success)
                    {
                        if ("bind" == variableNamespace)
                        {
                            variableId = matches[i].Groups["name"].Value;
                        }
                    }

                    // check for an escape sequence of !! indicating the match is not a variable expression
                    if (0 < matches[i].Index && '!' == sb[matches[i].Index - 1])
                    {
                        if (!localizationOnly)
                        {
                            sb.Remove(matches[i].Index - 1, 1);
                        }
                    }
                    else
                    {
                        string resolvedValue = null;

                        if ("loc" == variableNamespace)
                        {
                            // warn about deprecated syntax of $(loc.var)
                            if ('$' == sb[matches[i].Index])
                            {
                                this.OnMessage(WixWarnings.DeprecatedLocalizationVariablePrefix(sourceLineNumbers, variableId));
                            }

                            if (null != this.localizer)
                            {
                                resolvedValue = this.localizer.GetLocalizedValue(variableId);
                            }
                        }
                        else if (!localizationOnly && "wix" == variableNamespace)
                        {
                            // illegal syntax of $(wix.var)
                            if ('$' == sb[matches[i].Index])
                            {
                                this.OnMessage(WixErrors.IllegalWixVariablePrefix(sourceLineNumbers, variableId));
                            }
                            else
                            {
                                // default the resolved value to the inline value if one was specified
                                if (null != variableDefaultValue)
                                {
                                    resolvedValue = variableDefaultValue;
                                }

                                if (this.wixVariables.Contains(variableId))
                                {
                                    resolvedValue = (string)this.wixVariables[variableId] ?? String.Empty;
                                    isDefault = false;
                                }
                            }
                        }

                        if ("bind" == variableNamespace)
                        {
                            // can't resolve these yet, but keep track of where we find them so they can be resolved later with less effort
                            delayedResolve = true;
                        }
                        else
                        {
                            // insert the resolved value if it was found or display an error
                            if (null != resolvedValue)
                            {
                                sb.Remove(matches[i].Index, matches[i].Length);
                                sb.Insert(matches[i].Index, resolvedValue);
                            }
                            else if ("loc" == variableNamespace && errorOnUnknown) // unresolved loc variable
                            {
                                this.OnMessage(WixErrors.LocalizationVariableUnknown(sourceLineNumbers, variableId));
                            }
                            else if (!localizationOnly && "wix" == variableNamespace && errorOnUnknown) // unresolved wix variable
                            {
                                this.OnMessage(WixErrors.WixVariableUnknown(sourceLineNumbers, variableId));
                            }
                        }
                    }
                }

                value = sb.ToString();
            }

            return value;
        }

        /// <summary>
        /// Resolve the delay variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="resolutionData"></param>
        /// <returns>The resolved value.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sourceLineNumbers")]
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This string is not round tripped, and not used for any security decisions")]
        public static string ResolveDelayedVariables(SourceLineNumberCollection sourceLineNumbers, string value, IDictionary<string, string> resolutionData)
        {
            MatchCollection matches = Common.WixVariableRegex.Matches(value);

            if (0 < matches.Count)
            {
                StringBuilder sb = new StringBuilder(value);

                // notice how this code walks backward through the list
                // because it modifies the string as we go through it
                for (int i = matches.Count - 1; 0 <= i; i--)
                {
                    string variableNamespace = matches[i].Groups["namespace"].Value;
                    string variableId = matches[i].Groups["fullname"].Value;
                    string variableDefaultValue = null;
                    string variableScope = null;

                    // get the default value if one was specified
                    if (matches[i].Groups["value"].Success)
                    {
                        variableDefaultValue = matches[i].Groups["value"].Value;
                    }

                    // get the scope if one was specified
                    if (matches[i].Groups["scope"].Success)
                    {
                        variableScope = matches[i].Groups["scope"].Value;
                        if ("bind" == variableNamespace)
                        {
                            variableId = matches[i].Groups["name"].Value;
                        }
                    }

                    // check for an escape sequence of !! indicating the match is not a variable expression
                    if (0 < matches[i].Index && '!' == sb[matches[i].Index - 1])
                    {
                        sb.Remove(matches[i].Index - 1, 1);
                    }
                    else
                    {
                        string key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", variableId, variableScope).ToLower(CultureInfo.InvariantCulture);
                        string resolvedValue = variableDefaultValue;

                        if (resolutionData.ContainsKey(key))
                        {
                            resolvedValue = resolutionData[key];
                        }

                        if ("bind" == variableNamespace)
                        {
                            // insert the resolved value if it was found or display an error
                            if (null != resolvedValue)
                            {
                                sb.Remove(matches[i].Index, matches[i].Length);
                                sb.Insert(matches[i].Index, resolvedValue);
                            }
                            else
                            { 
                               throw new WixException(WixErrors.UnresolvedBindReference(sourceLineNumbers,value));
                            }
                        }
                    }
                }

                value = sb.ToString();
            }

            return value;
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        private void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            if (null != this.Message)
            {
                this.Message(this, e);
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
