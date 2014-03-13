//-------------------------------------------------------------------------------------------------
// <copyright file="PreprocessorCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The preprocessor core - handles functionality shared between the preprocessor and its extensions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The preprocessor core.
    /// </summary>
    public sealed class PreprocessorCore : IMessageHandler
    {
        private static readonly char[] variableSplitter = new char[] { '.' };
        private static readonly char[] argumentSplitter = new char[] { ',' };

        private Platform currentPlatform;
        private bool encounteredError;
        private Hashtable extensionsByPrefix;
        private string sourceFile;
        private Hashtable variables;

        /// <summary>
        /// Instantiate a new PreprocessorCore.
        /// </summary>
        /// <param name="extensionsByPrefix">The extensions indexed by their prefixes.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="sourceFile">The source file being preprocessed.</param>
        /// <param name="variables">The variables defined prior to preprocessing.</param>
        internal PreprocessorCore(Hashtable extensionsByPrefix, MessageEventHandler messageHandler, string sourceFile, Hashtable variables)
        {
            this.extensionsByPrefix = extensionsByPrefix;
            this.MessageHandler = messageHandler;
            this.sourceFile = Path.GetFullPath(sourceFile);

            this.variables = new Hashtable();
            foreach (DictionaryEntry entry in variables)
            {
                this.AddVariable(null, (string)entry.Key, (string)entry.Value);
            }
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        private event MessageEventHandler MessageHandler;

        /// <summary>
        /// Event for resolved variables.
        /// </summary>
        private event ResolvedVariableEventHandler ResolvedVariable;

        /// <summary>
        /// Sets event for ResolvedVariableEventHandler.
        /// </summary>
        public ResolvedVariableEventHandler ResolvedVariableHandler
        {
            set { this.ResolvedVariable = value; }
        }

        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        public Platform CurrentPlatform
        {
            get { return this.currentPlatform; }
            set { this.currentPlatform = value; }
        }

        /// <summary>
        /// Gets whether the core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Replaces parameters in the source text.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the function.</param>
        /// <param name="value">Text that may contain parameters to replace.</param>
        /// <returns>Text after parameters have been replaced.</returns>
        public string PreprocessString(SourceLineNumberCollection sourceLineNumbers, string value)
        {
            StringBuilder sb = new StringBuilder();
            int currentPosition = 0;
            int end = 0;

            while (-1 != (currentPosition = value.IndexOf('$', end)))
            {
                if (end < currentPosition)
                {
                    sb.Append(value, end, currentPosition - end);
                }

                end = currentPosition + 1;
                string remainder = value.Substring(end);
                if (remainder.StartsWith("$", StringComparison.Ordinal))
                {
                    sb.Append("$");
                    end++;
                }
                else if (remainder.StartsWith("(loc.", StringComparison.Ordinal))
                {
                    currentPosition = remainder.IndexOf(')');
                    if (-1 == currentPosition)
                    {
                        throw new WixException(WixErrors.InvalidPreprocessorVariable(sourceLineNumbers, remainder));
                    }

                    sb.Append("$");   // just put the resource reference back as was
                    sb.Append(remainder, 0, currentPosition + 1);

                    end += currentPosition + 1;
                }
                else if (remainder.StartsWith("(", StringComparison.Ordinal))
                {
                    int openParenCount = 1;
                    int closingParenCount = 0;
                    bool isFunction = false;
                    bool foundClosingParen = false;

                    // find the closing paren
                    int closingParenPosition;
                    for (closingParenPosition = 1; closingParenPosition < remainder.Length; closingParenPosition++)
                    {
                        switch (remainder[closingParenPosition])
                        {
                            case '(':
                                openParenCount++;
                                isFunction = true;
                                break;
                            case ')':
                                closingParenCount++;
                                break;
                        }
                        if (openParenCount == closingParenCount)
                        {
                            foundClosingParen = true;
                            break;
                        }
                    }

                    // move the currentPosition to the closing paren
                    currentPosition += closingParenPosition;

                    if (!foundClosingParen)
                    {
                        if (isFunction)
                        {
                            throw new WixException(WixErrors.InvalidPreprocessorFunction(sourceLineNumbers, remainder));
                        }
                        else
                        {
                            throw new WixException(WixErrors.InvalidPreprocessorVariable(sourceLineNumbers, remainder));
                        }
                    }

                    string subString = remainder.Substring(1, closingParenPosition - 1);
                    string result = null;
                    if (isFunction)
                    {
                        result = this.EvaluateFunction(sourceLineNumbers, subString);
                    }
                    else
                    {
                        result = this.GetVariableValue(sourceLineNumbers, subString, false);
                    }

                    if (null == result)
                    {
                        if (isFunction)
                        {
                            throw new WixException(WixErrors.UndefinedPreprocessorFunction(sourceLineNumbers, subString));
                        }
                        else
                        {
                            throw new WixException(WixErrors.UndefinedPreprocessorVariable(sourceLineNumbers, subString));
                        }
                    }
                    else
                    {
                        if (!isFunction)
                        {
                            this.OnResolvedVariable(new ResolvedVariableEventArgs(sourceLineNumbers, subString, result));
                        }
                    }
                    sb.Append(result);
                    end += closingParenPosition + 1;
                }
                else   // just a floating "$" so put it in the final string (i.e. leave it alone) and keep processing
                {
                    sb.Append('$');
                }
            }

            if (end < value.Length)
            {
                sb.Append(value.Substring(end));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Evaluate a Pragma.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the function.</param>
        /// <param name="pragmaName">The pragma's full name (<prefix>.<pragma>).</param>
        /// <param name="args">The arguments to the pragma.</param>
        /// <param name="writer">The xml writer.</param>
        public void PreprocessPragma(SourceLineNumberCollection sourceLineNumbers, string pragmaName, string args, XmlWriter writer)
        {
            string[] prefixParts = pragmaName.Split(variableSplitter, 2);
            // Check to make sure there are 2 parts and neither is an empty string.
            if (2 != prefixParts.Length)
            {
                throw new WixException(WixErrors.InvalidPreprocessorPragma(sourceLineNumbers, pragmaName));
            }
            string prefix = prefixParts[0];
            string pragma = prefixParts[1];

            if (String.IsNullOrEmpty(prefix) || String.IsNullOrEmpty(pragma))
            {
                throw new WixException(WixErrors.InvalidPreprocessorPragma(sourceLineNumbers, pragmaName));
            }

            switch (prefix)
            {
                case "wix":
                    switch (pragma)
                    {
                        // Add any core defined pragmas here
                        default:
                            this.OnMessage(WixWarnings.PreprocessorUnknownPragma(sourceLineNumbers, pragmaName));
                            break;
                    }
                    break;
                default:
                    PreprocessorExtension extension = (PreprocessorExtension)this.extensionsByPrefix[prefix];
                    if (null == extension || !extension.ProcessPragma(sourceLineNumbers, prefix, pragma, args, writer))
                    {
                        this.OnMessage(WixWarnings.PreprocessorUnknownPragma(sourceLineNumbers, pragmaName));
                    }
                    break;
            }
        }

        /// <summary>
        /// Evaluate a function.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the function.</param>
        /// <param name="function">The function expression including the prefix and name.</param>
        /// <returns>The function value.</returns>
        public string EvaluateFunction(SourceLineNumberCollection sourceLineNumbers, string function)
        {
            string[] prefixParts = function.Split(variableSplitter, 2);
            // Check to make sure there are 2 parts and neither is an empty string.
            if (2 != prefixParts.Length || 0 >= prefixParts[0].Length || 0 >= prefixParts[1].Length)
            {
                throw new WixException(WixErrors.InvalidPreprocessorFunction(sourceLineNumbers, function));
            }
            string prefix = prefixParts[0];

            string[] functionParts = prefixParts[1].Split(new char[] { '(' }, 2);
            // Check to make sure there are 2 parts, neither is an empty string, and the second part ends with a closing paren.
            if (2 != functionParts.Length || 0 >= functionParts[0].Length || 0 >= functionParts[1].Length || !functionParts[1].EndsWith(")", StringComparison.Ordinal))
            {
                throw new WixException(WixErrors.InvalidPreprocessorFunction(sourceLineNumbers, function));
            }
            string functionName = functionParts[0];

            // Remove the trailing closing paren.
            string allArgs = functionParts[1].Substring(0, functionParts[1].Length - 1);

            // Parse the arguments and preprocess them.
            string[] args = allArgs.Split(argumentSplitter);
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = this.PreprocessString(sourceLineNumbers, args[i].Trim());
            }

            string result = this.EvaluateFunction(sourceLineNumbers, prefix, functionName, args);

            // If the function didn't evaluate, try to evaluate the original value as a variable to support 
            // the use of open and closed parens inside variable names. Example: $(env.ProgramFiles(x86)) should resolve.
            if (null == result)
            {
                result = this.GetVariableValue(sourceLineNumbers, function, false);
            }

            return result;
        }

        /// <summary>
        /// Evaluate a function.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the function.</param>
        /// <param name="prefix">The function prefix.</param>
        /// <param name="function">The function name.</param>
        /// <param name="args">The arguments for the function.</param>
        /// <returns>The function value or null if the function is not defined.</returns>
        public string EvaluateFunction(SourceLineNumberCollection sourceLineNumbers, string prefix, string function, string[] args)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException("prefix");
            }

            if (String.IsNullOrEmpty(function))
            {
                throw new ArgumentNullException("function");
            }

            switch (prefix)
            {
                case "fun":
                    switch (function)
                    {
                        case "AutoVersion":
                            // Make sure the base version is specified
                            if (args.Length == 0 || args[0].Length == 0)
                            {
                                throw new WixException(WixErrors.InvalidPreprocessorFunctionAutoVersion(sourceLineNumbers));
                            }

                            // Build = days since 1/1/2000; Revision = seconds since midnight / 2
                            DateTime now = DateTime.Now.ToUniversalTime();
                            TimeSpan tsBuild = now - new DateTime(2000, 1, 1);
                            TimeSpan tsRevision = now - new DateTime(now.Year, now.Month, now.Day);

                            return String.Format("{0}.{1}.{2}", args[0], (int)tsBuild.TotalDays, (int)(tsRevision.TotalSeconds / 2));

                        // Add any core defined functions here
                        default:
                            return null;
                    }
                default:
                    PreprocessorExtension extension = (PreprocessorExtension)this.extensionsByPrefix[prefix];
                    if (null != extension)
                    {
                        try
                        {
                            return extension.EvaluateFunction(prefix, function, args);
                        }
                        catch (Exception e)
                        {
                            throw new WixException(WixErrors.PreprocessorExtensionEvaluateFunctionFailed(sourceLineNumbers, prefix, function, String.Join(",", args), e.Message));
                        }
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        /// <summary>
        /// Get the value of a variable expression like var.name.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the variable.</param>
        /// <param name="variable">The variable expression including the optional prefix and name.</param>
        /// <param name="allowMissingPrefix">true to allow the variable prefix to be missing.</param>
        /// <returns>The variable value.</returns>
        public string GetVariableValue(SourceLineNumberCollection sourceLineNumbers, string variable, bool allowMissingPrefix)
        {
            // Strip the "$(" off the front.
            if (variable.StartsWith("$(", StringComparison.Ordinal))
            {
                variable = variable.Substring(2);
            }

            string[] parts = variable.Split(variableSplitter, 2);

            if (1 == parts.Length) // missing prefix
            {
                if (allowMissingPrefix)
                {
                    return this.GetVariableValue(sourceLineNumbers, "var", parts[0]);
                }
                else
                {
                    throw new WixException(WixErrors.InvalidPreprocessorVariable(sourceLineNumbers, variable));
                }
            }
            else
            {
                // check for empty variable name
                if (0 < parts[1].Length)
                {
                    string result = this.GetVariableValue(sourceLineNumbers, parts[0], parts[1]);

                    // If we didn't find it and we allow missing prefixes and the variable contains a dot, perhaps the dot isn't intended to indicate a prefix
                    if (null == result && allowMissingPrefix && variable.Contains("."))
                    {
                        result = this.GetVariableValue(sourceLineNumbers, "var", variable);
                    }

                    return result;
                }
                else
                {
                    throw new WixException(WixErrors.InvalidPreprocessorVariable(sourceLineNumbers, variable));
                }
            }
        }

        /// <summary>
        /// Get the value of a variable.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the function.</param>
        /// <param name="prefix">The variable prefix.</param>
        /// <param name="name">The variable name.</param>
        /// <returns>The variable value or null if the variable is not set.</returns>
        public string GetVariableValue(SourceLineNumberCollection sourceLineNumbers, string prefix, string name)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException("prefix");
            }

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            switch (prefix)
            {
                case "env":
                    return Environment.GetEnvironmentVariable(name);
                case "sys":
                    switch (name)
                    {
                        case "CURRENTDIR":
                            return String.Concat(Directory.GetCurrentDirectory(), Path.DirectorySeparatorChar);
                        case "SOURCEFILEDIR":
                            return String.Concat(Path.GetDirectoryName(sourceLineNumbers[0].FileName), Path.DirectorySeparatorChar);
                        case "SOURCEFILEPATH":
                            return sourceLineNumbers[0].FileName;
                        case "PLATFORM":
                            this.OnMessage(WixWarnings.DeprecatedPreProcVariable(sourceLineNumbers, "$(sys.PLATFORM)", "$(sys.BUILDARCH)"));

                            goto case "BUILDARCH";

                        case "BUILDARCH":
                            switch (this.currentPlatform)
                            {
                                case Platform.X86:
                                    return "x86";
                                case Platform.X64:
                                    return "x64";
                                case Platform.IA64:
                                    return "ia64";
                                case Platform.ARM:
                                    return "arm";
                                default:
                                    throw new ArgumentException(WixStrings.EXP_UnknownPlatformEnum, this.currentPlatform.ToString());
                            }
                        default:
                            return null;
                    }
                case "var":
                    return (string)this.variables[name];
                default:
                    PreprocessorExtension extension = (PreprocessorExtension)this.extensionsByPrefix[prefix];
                    if (null != extension)
                    {
                        try
                        {
                            return extension.GetVariableValue(prefix, name);
                        }
                        catch (Exception e)
                        {
                            throw new WixException(WixErrors.PreprocessorExtensionGetVariableValueFailed(sourceLineNumbers, prefix, name, e.Message));
                        }
                    }
                    else
                    {
                        return null;
                    }
            }
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
        /// Sends resolved variable to delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnResolvedVariable(ResolvedVariableEventArgs mea)
        {
            if (null != this.ResolvedVariable)
            {
                this.ResolvedVariable(this, mea);
            }
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information of the variable.</param>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        internal void AddVariable(SourceLineNumberCollection sourceLineNumbers, string name, string value)
        {
            AddVariable(sourceLineNumbers, name, value, true);
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information of the variable.</param>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        /// <param name="overwrite">Set to true to show variable overwrite warning.</param>
        internal void AddVariable(SourceLineNumberCollection sourceLineNumbers, string name, string value, bool showWarning)
        {
            string currentValue = this.GetVariableValue(sourceLineNumbers, "var", name);

            if (null == currentValue)
            {
                this.variables.Add(name, value);
            }
            else
            {
                if (showWarning)
                {
                    this.OnMessage(WixWarnings.VariableDeclarationCollision(sourceLineNumbers, name, value, currentValue));
                }

                this.variables[name] = value;
            }
        }

        /// <summary>
        /// Remove a variable.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information of the variable.</param>
        /// <param name="name">The variable name.</param>
        internal void RemoveVariable(SourceLineNumberCollection sourceLineNumbers, string name)
        {
            if (this.variables.Contains(name))
            {
                this.variables.Remove(name);
            }
            else
            {
                throw new WixException(WixErrors.CannotReundefineVariable(sourceLineNumbers, name));
            }
        }
    }
}
