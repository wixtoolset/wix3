// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The Windows Installer XML Toolset DirectX Extension.
    /// </summary>
    public sealed class DirectXExtension : WixExtension
    {
        private DirectXDecompiler decompilerExtension;
        private Library library;

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
                    this.decompilerExtension = new DirectXDecompiler();
                }

                return this.decompilerExtension;
            }
        }

        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The loaded library.</returns>
        public override Library GetLibrary(TableDefinitionCollection tableDefinitions)
        {
            if (null == this.library)
            {
                this.library = LoadLibraryHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Data.DirectX.wixlib", tableDefinitions);
            }

            return this.library;
        }
    }
}
