// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The Windows Installer XML Toolset PowerShell Extension.
    /// </summary>
    public sealed class PSExtension : WixExtension
    {
        private Library library;
        private PSCompiler compilerExtension;

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
                    this.compilerExtension = new PSCompiler();
                }

                return this.compilerExtension;
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
                this.library = LoadLibraryHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Data.ps.wixlib", tableDefinitions);
            }

            return this.library;
        }
    }
}
