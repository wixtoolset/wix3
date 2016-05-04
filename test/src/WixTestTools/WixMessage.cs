// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A WiX message. That is, a warning or an error.
    /// </summary>
    public class WixMessage : Message
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageNumber">The message number</param>
        /// <param name="messageText">The message text</param>
        /// <param name="messageType">The message type</param>
        public WixMessage(int messageNumber, string messageText, MessageTypeEnum messageType)
            :base (messageNumber, messageText, messageType)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageNumber">The message number</param>
        /// <param name="messageType">The message type</param>
        public WixMessage(int messageNumber, MessageTypeEnum messageType)
            : base(messageNumber, messageType)
        {
        }
  
        /// <summary>
        /// Check if a line of text contains a WiX message
        /// </summary>
        /// <param name="text">The text to search</param>
        /// <returns>A WixMessage if one exists in the text. Otherwise, return null.</returns>
        public static WixMessage FindWixMessage(string text)
        {
            return WixMessage.FindWixMessage(text, WixTool.WixTools.Any);
        }

        /// <summary>
        /// Check if a line of text contains a WiX message
        /// </summary>
        /// <param name="text">The text to search</param>
        /// <param name="tool">Specifies which tool the message is expected to come from</param>
        /// <returns>A WixMessage if one exists in the text. Otherwise, return null.</returns>
        public static WixMessage FindWixMessage(string text, WixTool.WixTools tool)
        {
            Match messageMatch = WixMessage.GetToolWixMessageRegex(tool).Match(text);

            if (messageMatch.Success)
            {
                int messageNumber = Convert.ToInt32(messageMatch.Groups["messageNumber"].Value);
                string messageText = messageMatch.Groups["messageText"].Value;
                Message.MessageTypeEnum messageType = Message.ConvertToMessageTypeEnum(messageMatch.Groups["messageType"].Value);

                return new WixMessage(messageNumber, messageText, messageType);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a Regex that matches a Wix Message (warning or error) for a particular tool
        /// </summary>
        /// <param name="tool">A Wix Tool</param>
        /// <returns>A Regex that matches a Wix Message for the specified tool</returns>
        public static Regex GetToolWixMessageRegex(WixTool.WixTools tool)
        {
            string toolCode = String.Empty;

            switch (tool)
            {
                case WixTool.WixTools.Candle:
                    toolCode = "CNDL";
                    break;
                case WixTool.WixTools.Dark:
                    toolCode = "DARK";
                    break;
                case WixTool.WixTools.Heat:
                    toolCode = "Heat";
                    break;
                case WixTool.WixTools.Light:
                    toolCode = "LGHT";
                    break;
                case WixTool.WixTools.Lit:
                    toolCode = "LIT";
                    break;
                case WixTool.WixTools.Melt:
                    toolCode = "MELT";
                    break;
                case WixTool.WixTools.Pyro:
                    toolCode = "PYRO";
                    break;
                case WixTool.WixTools.Smoke:
                    toolCode = "SMOK";
                    break;
                case WixTool.WixTools.Torch:
                    toolCode = "TRCH";
                    break;
                case WixTool.WixTools.Wixunit:
                    toolCode = "WUNT";
                    break;
                case WixTool.WixTools.Any:
                    // This string will match any toolCode
                    toolCode = @"[^:\d]*";
                    break;
                default:
                    throw new ArgumentException(String.Format("Unexpected argument {0}", tool.ToString()));
            }

            Regex wixMessageRegex = new Regex(String.Format(@"^.*: (?<messageType>error|warning) {0}(?<messageNumber>\d*) : (?<messageText>[^\n\r]*).*$", toolCode), RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            return wixMessageRegex;
        }  
    }
}
