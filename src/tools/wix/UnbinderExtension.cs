// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Base class for creating an unbinder extension.
    /// </summary>
    public abstract class UnbinderExtension
    {
        /// <summary>
        /// Called during the generation of sectionIds for an admin image.
        /// </summary>
        public virtual void GenerateSectionIds(Output output)
        {
        }
    }
}
