//---------------------------------------------------------------------
// <copyright file="ArchiveException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.Compression
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base exception class for compression operations. Compression libraries should
    /// derive subclass exceptions with more specific error information relevent to the
    /// file format.
    /// </summary>
    [Serializable]
    public class ArchiveException : IOException
    {
        /// <summary>
        /// Creates a new ArchiveException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public ArchiveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new ArchiveException with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ArchiveException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Creates a new ArchiveException.
        /// </summary>
        public ArchiveException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ArchiveException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
