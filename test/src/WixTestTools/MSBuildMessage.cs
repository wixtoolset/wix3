//-----------------------------------------------------------------------
// <copyright file="MSBuildMessage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     A class represents an MSBuild message. That is, a warning or an error.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A MSBuild message. That is, a warning or an error.
    /// </summary>
    public class MSBuildMessage : Message
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageNumber">The message number</param>
        /// <param name="messageText">The message text</param>
        /// <param name="messageType">The message type</param>
        public MSBuildMessage(int messageNumber, string messageText, MessageTypeEnum messageType)
            : base(messageNumber, messageText, messageType)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageText">The message tesxt</param>
        /// <param name="messageType">The message type</param>
        public MSBuildMessage(string messageText, MessageTypeEnum messageType)
            : base(0, messageText, messageType)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageNumber">The message number</param>
        /// <param name="messageType">The message type</param>
        public MSBuildMessage(int messageNumber, MessageTypeEnum messageType)
            : base(messageNumber, messageType)
        {
        }

        /// <summary>
        /// Check if a line of text contains a MSBuild message
        /// </summary>
        /// <param name="text">The text to search</param>
        /// <returns>A MSBuildMessage if one exists in the text. Otherwise, return null.</returns>
        public static MSBuildMessage FindMSBuildMessage(string text)
        {
            Match messageMatch = MSBuildMessage.MSBuildMessageRegex.Match(text);

            if (messageMatch.Success)
            {
                int messageNumber = String.IsNullOrEmpty(messageMatch.Groups["messageNumber"].Value) ? 0 : Convert.ToInt32(messageMatch.Groups["messageNumber"].Value);
                string messageText = messageMatch.Groups["messageText"].Value;
                MSBuildMessage.MessageTypeEnum messageType = Message.ConvertToMessageTypeEnum(messageMatch.Groups["messageType"].Value);

                return new MSBuildMessage(messageNumber, messageText, messageType);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Regex matching MSBuild error or warrning.
        /// </summary>
        private static Regex MSBuildMessageRegex = new Regex(@"^.*: (?<messageType>error|warning) ([^:\d]*(?<messageNumber>\d*)){0,1}: (?<messageText>[^\n\r]*).*$", RegexOptions.ExplicitCapture | RegexOptions.Singleline);
    }
}
