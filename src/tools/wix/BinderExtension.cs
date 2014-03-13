//-------------------------------------------------------------------------------------------------
// <copyright file="BinderExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The base binder extension.  Any of these methods can be overridden to perform binding tasks at
// various stages during the binding process.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Base class for creating an binder extension.
    /// </summary>
    public abstract class BinderExtension
    {
        private BinderCore binderCore;

        /// <summary>
        /// Gets or sets the binder core for the extension.
        /// </summary>
        /// <value>Binder core for the extension.</value>
        public BinderCore Core
        {
            get { return this.binderCore; }
            set { this.binderCore = value; }
        }

        /// <summary>
        /// Called before transform binding occurs.
        /// </summary>
        public virtual void TransformInitialize(Output transform)
        {
        }

        /// <summary>
        /// Called after all changes to the transform occur and right before the transform is bound into an mst.
        /// </summary>
        public virtual void TransformFinalize(Output transform)
        {
        }

        /// <summary>
        /// Called before database binding occurs.
        /// </summary>
        public virtual void DatabaseInitialize(Output output)
        {
        }

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public virtual void DatabaseFinalize(Output output)
        {
        }

        /// <summary>
        /// Called before bundle binding occurs.
        /// </summary>
        public virtual void BundleInitialize(Output bundle)
        {
        }

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public virtual void BundleFinalize(Output bundle)
        {
        }
    }
}