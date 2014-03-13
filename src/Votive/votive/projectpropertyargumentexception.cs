//-------------------------------------------------------------------------------------------------
// <copyright file="projectpropertyargumentexception.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the ProjectPropertyArgumentException class.
// </summary>
//-------------------------------------------------------------------------------------------------

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
