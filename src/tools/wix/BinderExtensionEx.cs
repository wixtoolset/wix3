//-------------------------------------------------------------------------------------------------
// <copyright file="BinderExtensionEx.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The extended base binder extension.  Any of these methods can be overridden to perform binding tasks at
// various stages during the binding process.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Base class for creating an extended binder extension.
    /// </summary>
    public abstract class BinderExtensionEx : BinderExtension
    {
        /// <summary>
        /// Called after database variable resolution occurs.
        /// </summary>
        public virtual void DatabaseAfterResolvedFields(Output output)
        {
        }
    }
}