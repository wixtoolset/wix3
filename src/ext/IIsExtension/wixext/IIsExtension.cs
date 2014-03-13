//-------------------------------------------------------------------------------------------------
// <copyright file="IIsExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset Internet Information Services Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsExtension : WixExtension
    {
        private Library library;
        private IIsCompiler compilerExtension;
        private IIsDecompiler decompilerExtension;
        private TableDefinitionCollection tableDefinitions;

        /// <summary>
        /// Gets the optional compiler extension.
        /// </summary>
        /// <value>The optional compiler extension.</value>
        public override CompilerExtension CompilerExtension
        {
            get
            {
                if (null == this.compilerExtension)
                {
                    this.compilerExtension = new IIsCompiler();
                }

                return this.compilerExtension;
            }
        }

        /// <summary>
        /// Gets the optional decompiler extension.
        /// </summary>
        /// <value>The optional decompiler extension.</value>
        public override DecompilerExtension DecompilerExtension
        {
            get
            {
                if (null == this.decompilerExtension)
                {
                    this.decompilerExtension = new IIsDecompiler();
                }

                return this.decompilerExtension;
            }
        }

        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>The optional table definitions for this extension.</value>
        public override TableDefinitionCollection TableDefinitions
        {
            get
            {
                if (null == this.tableDefinitions)
                {
                    this.tableDefinitions = LoadTableDefinitionHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Data.tables.xml");
                }

                return this.tableDefinitions;
            }
        }

        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The library for this extension.</returns>
        public override Library GetLibrary(TableDefinitionCollection tableDefinitions)
        {
            if (null == this.library)
            {
                this.library = LoadLibraryHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Data.iis.wixlib", tableDefinitions);
            }

            return this.library;
        }

        /// <summary>
        /// Gets the default culture.
        /// </summary>
        /// <value>The default culture.</value>
        public override string DefaultCulture
        {
            get { return "en-us"; }
        }
    }
}
