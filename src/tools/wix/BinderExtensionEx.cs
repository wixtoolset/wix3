// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
