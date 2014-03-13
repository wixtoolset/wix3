//-------------------------------------------------------------------------------------------------
// <copyright file="Exceptions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Exceptions used by the managed bootstrapper application classes.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class for exception returned to the bootstrapper application host.
    /// </summary>
    [Serializable]
    public abstract class BootstrapperException : Exception
    {
        /// <summary>
        /// Creates an instance of the <see cref="BootstrapperException"/> base class with the given HRESULT.
        /// </summary>
        /// <param name="hr">The HRESULT for the exception that is used by the bootstrapper application host.</param>
        public BootstrapperException(int hr)
        {
            this.HResult = hr;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public BootstrapperException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception associated with this one</param>
        public BootstrapperException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperException"/> class.
        /// </summary>
        /// <param name="info">Serialization information for this exception</param>
        /// <param name="context">Streaming context to serialize to</param>
        protected BootstrapperException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The bootstrapper application loaded by the host does not contain exactly one instance of the
    /// <see cref="BootstrapperApplicationAttribute"/> class.
    /// </summary>
    /// <seealso cref="BootstrapperApplicationAttribute"/>
    [Serializable]
    public class MissingAttributeException : BootstrapperException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        public MissingAttributeException()
            : base(NativeMethods.E_NOTFOUND)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public MissingAttributeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception associated with this one</param>
        public MissingAttributeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
        /// </summary>
        /// <param name="info">Serialization information for this exception</param>
        /// <param name="context">Streaming context to serialize to</param>
        protected MissingAttributeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The bootstrapper application specified by the <see cref="BootstrapperApplicationAttribute"/>
    ///  does not extend the <see cref="BootstrapperApplication"/> base class.
    /// </summary>
    /// <seealso cref="BootstrapperApplication"/>
    /// <seealso cref="BootstrapperApplicationAttribute"/>
    [Serializable]
    public class InvalidBootstrapperApplicationException : BootstrapperException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InvalidBootstrapperApplicationException"/> class.
        /// </summary>
        public InvalidBootstrapperApplicationException()
            : base(NativeMethods.E_UNEXPECTED)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidBootstrapperApplicationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public InvalidBootstrapperApplicationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidBootstrapperApplicationException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception associated with this one</param>
        public InvalidBootstrapperApplicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidBootstrapperApplicationException"/> class.
        /// </summary>
        /// <param name="info">Serialization information for this exception</param>
        /// <param name="context">Streaming context to serialize to</param>
        protected InvalidBootstrapperApplicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
