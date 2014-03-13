//-------------------------------------------------------------------------------------------------
// <copyright file="UnbinderExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The base unbinder extension.  Any of these methods can be overridden to change
// the behavior of the unbinder.
// </summary>
//-------------------------------------------------------------------------------------------------

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
