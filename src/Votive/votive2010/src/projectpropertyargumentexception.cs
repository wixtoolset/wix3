// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Runtime.Serialization;
    using System.ComponentModel;

    /// <summary>
    /// Exception thrown when an invalid property is entered on the project property pages
    /// </summary>
    [Serializable]
    internal class ProjectPropertyArgumentException : ArgumentException
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Constructor for the ProjectPropertyArgumentException
        /// </summary>
        /// <param name="message">Error message associated with the exception</param>
        public ProjectPropertyArgumentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new deserialized exception instance.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected ProjectPropertyArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
