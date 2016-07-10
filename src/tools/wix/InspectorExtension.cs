// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.IO;

    /// <summary>
    /// Base class for inspector extensions.
    /// </summary>
    /// The inspector methods are stateless, but extensions are loaded and last for the lifetime of the
    /// containing classes like <see cref="Preprocessor"/>, <see cref="Compiler"/>, <see cref="Linker"/>,
    /// <see cref="Differ"/>, and <see cref="Binder"/>. If you want to maintain state, you should check
    /// if your data is loaded for each method and, if not, load it.
    /// <remarks>
    /// </remarks>
    public class InspectorExtension
    {
        private InspectorCore core;

        /// <summary>
        /// Creates a new instance of the <see cref="InspectorExtension"/> class.
        /// </summary>
        public InspectorExtension()
        {
        }

        /// <summary>
        /// Gets the <see cref="InspectorCore"/> for inspector extensions to use.
        /// </summary>
        public InspectorCore Core
        {
            get { return this.core; }
            internal set { this.core = value; }
        }

        /// <summary>
        /// Inspect the source before preprocessing.
        /// </summary>
        /// <param name="source">The source to preprocess.</param>
        public virtual void InspectSource(Stream source)
        {
        }

        /// <summary>
        /// Inspect the compiled output.
        /// </summary>
        /// <param name="output">The compiled output.</param>
        public virtual void InspectIntermediate(Intermediate output)
        {
        }

        /// <summary>
        /// Inspect the linked output.
        /// </summary>
        /// <param name="output">The linked output.</param>
        public virtual void InspectOutput(Output output)
        {
        }

        /// <summary>
        /// Inspect the transform containing all the differences.
        /// </summary>
        /// <param name="transform">The input transform.</param>
        public virtual void InspectTransform(Output transform)
        {
        }

        /// <summary>
        /// Inspect the patch after filtering contained transforms.
        /// </summary>
        /// <param name="transform">The <see cref="Output"/> for the patch.</param>
        /// <remarks>
        /// To inspect filtered transforms, enumerate <see cref="Output.SubStorages"/>.
        /// Transforms where the <see cref="SubStorage.Name"/> begins with "#" are
        /// called patch transforms and instruct Windows Installer how to apply the
        /// authored transforms - those that do not begin with "#". The authored
        /// transforms are the primary transforms you'll typically want to inspect
        /// and contain your changes to target products.
        /// </remarks>
        public virtual void InspectPatch(Output patch)
        {
        }

        /// <summary>
        /// Inspect the final output after binding.
        /// </summary>
        /// <param name="filePath">The file path to the final bound output.</param>
        /// <param name="output">The <see cref="Output"/> that contains source line numbers
        /// for the database and all rows.</param>
        public virtual void InspectDatabase(string filePath, Output output)
        {
        }
    }
}
