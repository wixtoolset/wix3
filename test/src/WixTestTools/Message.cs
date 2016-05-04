// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A message. That is, a warning or an error.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The message number
        /// </summary>
        private readonly int messageNumber;

        /// <summary>
        /// The message text
        /// </summary>
        private readonly string messageText;

        /// <summary>
        /// The message type
        /// </summary>
        private readonly MessageTypeEnum messageType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageNumber">The message number</param>
        /// <param name="messageText">The message text</param>
        /// <param name="messageType">The message type</param>
        public Message(int messageNumber, string messageText, MessageTypeEnum messageType)
        {
            this.messageNumber = messageNumber;
            this.messageText = messageText;
            this.messageType = messageType;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageNumber">The message number</param>
        /// <param name="messageType">The message type</param>
        public Message(int messageNumber, MessageTypeEnum messageType)
        {
            this.messageNumber = messageNumber;
            this.messageText = String.Empty;
            this.messageType = messageType;
        }

        /// <summary>
        /// The type of message
        /// </summary>
        public enum MessageTypeEnum
        {
            /// <summary>
            /// Error
            /// </summary>
            Error,

            /// <summary>
            /// Warning
            /// </summary>
            Warning
        }

        /// <summary>
        /// The message number
        /// </summary>
        public int MessageNumber
        {
            get { return this.messageNumber; }
        }

        /// <summary>
        /// The message text
        /// </summary>
        public string MessageText
        {
            get { return this.messageText; }
        }

        /// <summary>
        /// The message type
        /// </summary>
        public MessageTypeEnum MessageType
        {
            get { return this.messageType; }
        }
    
        /// <summary>
        /// Determines equality between two Message objects
        /// </summary>
        /// <param name="wm1">A Message</param>
        /// <param name="wm2">A Message</param>
        /// <returns>True if the Messages are equal and false if they are not equal</returns>
        public static bool operator ==(Message wm1, Message wm2)
        {
            if (0 == Message.Compare(wm1, wm2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines inequality between two Message objects
        /// </summary>
        /// <param name="wm1">A Message</param>
        /// <param name="wm2">A Message</param>
        /// <returns>True if the Messages are not equal and false if they are equal</returns>
        public static bool operator !=(Message wm1, Message wm2)
        {
            return !(wm1 == wm2);
        }

        /// <summary>
        /// Compares two specified Message objects and returns an integer that indicates their relationship to one another in the sort order
        /// </summary>
        /// <param name="wm1">The first Message</param>
        /// <param name="wm2">The second Message</param>
        /// <returns>
        /// Less than zero if wm1 is less than wm2
        /// Zero if wm1 is equal to wm2
        /// Greater than zero if wm1 is greater than wm2
        /// </returns>
        public static int Compare(Message wm1, Message wm2)
        {
            return Message.Compare(wm1, wm2, false);
        }

        /// <summary>
        /// Compares two specified Message objects and returns an integer that indicates their relationship to one another in the sort order
        /// </summary>
        /// <param name="wm1">The first Message</param>
        /// <param name="wm2">The second Message</param>
        /// <param name="ignoreText">True if the message text should be ignored when comparing Message objects</param>
        /// <returns>
        /// Less than zero if wm1 is less than wm2
        /// Zero if wm1 is equal to wm2
        /// Greater than zero if wm1 is greater than wm2
        /// </returns>
        public static int Compare(Message wm1, Message wm2, bool ignoreText)
        {
            // cast the WixMessages to Objects for null comparison, otherwise the == operator call would be recursive
            Object obj1 = (Object)wm1;
            Object obj2 = (Object)wm2;

            if (null == obj1 && null == obj2)
            {
                // both objects are null
                return 0;
            }
            else if (null == obj1 && null != obj2)
            {
                // obj1 is null and obj2 is not null
                return -1;
            }
            else if (null != obj1 && null == obj2)
            {
                // obj1 is not null and obj1 is null
                return 1;
            }

            // wm1 and wm2 are both not null, so compare their values

            if (wm1.MessageNumber < wm2.MessageNumber)
            {
                return -1;
            }
            else if (wm1.MessageNumber > wm2.MessageNumber)
            {
                return 1;
            }
            else
            {
                // MessageNumbers are equal so compare MessageTypes

                if (wm1.MessageType != wm2.MessageType)
                {
                    // MessageNumbers are equal but MessageTypes are not

                    if (wm1.MessageType == MessageTypeEnum.Warning)
                    {
                        // A warning is considered to be 'less' than an error

                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    // MessageNumbers and MessageTypes are equal so compare MessageText

                    if (ignoreText)
                    {
                        return 0;
                    }
                    else
                    {
                        return String.Compare(wm1.MessageText, wm2.MessageText, StringComparison.InvariantCulture);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified WixMessage is equal to the current WixMessage
        /// </summary>
        /// <param name="obj">The Message to compare</param>
        /// <returns>True if the message type, number and text are equal</returns>
        public override bool Equals(object obj)
        {
            if (null == obj || this.GetType() != obj.GetType())
            {
                return false;
            }
            else
            {
                return (this == (Message)obj);
            }
        }

        /// <summary>
        /// Serves as a hash function for Message
        /// </summary>
        /// <returns>A hash code for a Message</returns>
        /// <remarks>
        /// This method should be overridden when the Equals method is overridden to ensure that equal objects return the same hash code
        /// </remarks>
        public override int GetHashCode()
        {
            int hashCode = this.MessageType.GetHashCode() ^ this.MessageNumber.GetHashCode() ^ this.MessageText.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// String representation of a Message
        /// </summary>
        /// <returns>A string</returns>
        public override string ToString()
        {
            string messageType = Enum.GetName(this.MessageType.GetType(), this.MessageType);
            return String.Format("{0} {1} {2}", messageType, this.MessageNumber, this.messageText);
        }

        /// <summary>
        /// Converts a string message type to an enum message type
        /// </summary>
        /// <param name="messageType">A string message type, eg "warning" or "error"</param>
        /// <returns>An enum message type, eg. MessageTypeEnum.Warning</returns>
        protected static MessageTypeEnum ConvertToMessageTypeEnum(string messageType)
        {
            if (messageType.Equals("error", StringComparison.InvariantCultureIgnoreCase))
            {
                return MessageTypeEnum.Error;
            }
            else if (messageType.Equals("warning", StringComparison.InvariantCultureIgnoreCase))
            {
                return MessageTypeEnum.Warning;
            }
            else
            {
                throw new ArgumentException(String.Format("The message type '{0}' is not valid"));
            }
        }
    }
}
