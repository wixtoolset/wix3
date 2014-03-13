//-------------------------------------------------------------------------------------------------
// <copyright file="PreprocessorExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The base preprocessor extension.  Any of these methods can be overridden to change
// the behavior of the preprocessor.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Base class for creating a preprocessor extension.
    /// </summary>
    public abstract class PreprocessorExtension
    {
        private PreprocessorCore core;

        /// <summary>
        /// Gets or sets the preprocessor core for the extension.
        /// </summary>
        /// <value>Preprocessor core for the extension.</value>
        public PreprocessorCore Core
        {
            get { return this.core; }
            set { this.core = value; }
        }

        /// <summary>
        /// Gets or sets the variable prefixes for the extension.
        /// </summary>
        /// <value>The variable prefixes for the extension.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public virtual string[] Prefixes
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the value of a variable whose prefix matches the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the variable to be processed by the extension.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable or null if the variable is undefined.</returns>
        public virtual string GetVariableValue(string prefix, string name)
        {
            return null;
        }

        /// <summary>
        /// Evaluates a function defined in the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the function to be processed by the extension.</param>
        /// <param name="function">The name of the function.</param>
        /// <param name="args">The list of arguments.</param>
        /// <returns>The value of the function or null if the function is not defined.</returns>
        public virtual string EvaluateFunction(string prefix, string function, string[] args)
        {
            return null;
        }

        /// <summary>
        /// Processes a pragma defined in the extension.
        /// </summary>
        /// <param name="sourceLineNumbers">The location of this pragma's PI.</param>
        /// <param name="prefix">The prefix of the pragma to be processed by the extension.</param>
        /// <param name="pragma">The name of the pragma.</param>
        /// <param name="args">The pragma's arguments.</param>
        /// <param name="writer">The xml writer.</param>
        /// <returns>false if the pragma is not defined.</returns>
        /// <comments>Don't return false for any condition except for unrecognized pragmas. Throw errors that are fatal to the compile. use core.OnMessage for warnings and messages.</comments>
        public virtual bool ProcessPragma(SourceLineNumberCollection sourceLineNumbers, string prefix, string pragma, string args, XmlWriter writer)
        {
            return false;
        }

        /// <summary>
        /// Called at the end of the preprocessing of a source file.
        /// </summary>
        public virtual void FinalizePreprocess()
        {
        }

        /// <summary>
        /// Called at the beginning of the preprocessing of a source file.
        /// </summary>
        public virtual void InitializePreprocess()
        {
        }

        /// <summary>
        /// Preprocess a document after normal preprocessing has completed.
        /// </summary>
        /// <param name="document">The document to preprocess.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public virtual void PreprocessDocument(XmlDocument document)
        {
        }

        /// <summary>
        /// Preprocesses a parameter.
        /// </summary>
        /// <param name="name">Name of parameter that matches extension.</param>
        /// <returns>The value of the parameter after processing.</returns>
        /// <remarks>By default this method will cause an error if its called.</remarks>
        public virtual string PreprocessParameter(string name)
        {
            return null;
        }
    }
}
