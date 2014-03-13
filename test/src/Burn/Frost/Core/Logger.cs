//-----------------------------------------------------------------------
// <copyright file="Logger.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A logger for Frost.</summary>
//-----------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Test.Frost
{
    using System;
    using System.IO;
    using System.Xml;

    public enum LoggingLevel { ERROR = 0, TRACE = 1, INFO = 2 }

    delegate void LogWriterDelegate(string Message);

    public class Logger
    {
        private static object StdOutLocker;
        private static object FileLocker;

        private StreamWriter LogWriter;
        private LoggingLevel CurrentLevel;
        private string TimeStamp;
        private LogWriterDelegate TheWriterDelegate;

        public enum OutputType { STDOUT = 1, FILE = 2, }

        /// <summary>
        /// Sets up the logger for stdout only
        /// </summary>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        public Logger(LoggingLevel InitLoggingLevel) : this((int)OutputType.STDOUT, null, (int)InitLoggingLevel, null) { }

        /// <summary>
        /// Sets up the logger for stdout only
        /// </summary>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        public Logger(int InitLoggingLevel): this((int)OutputType.STDOUT, null, InitLoggingLevel, null)
        {
        }

        /// <summary>
        /// Sets up the logger for file logging only with a specified timestamp
        /// </summary>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        /// <param name="LogFileName">The name of the file to use. If the file exists, it will be appended to</param>
        /// <param name="TimeStampMask">A mask to apply to the DateTime object. For no timestamps, set to null</param>
        public Logger(LoggingLevel InitLoggingLevel, string LogFileName, string TimeStampMask)
            : this((int)OutputType.FILE, LogFileName, (int)InitLoggingLevel, TimeStampMask)
        {
        }

        /// <summary>
        /// Sets up the logger for file logging only with a specified timestamp
        /// </summary>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        /// <param name="LogFileName">The name of the file to use. If the file exists, it will be appended to</param>
        /// <param name="TimeStampMask">A mask to apply to the DateTime object. For no timestamps, set to null</param>
        public Logger(int InitLoggingLevel, string LogFileName, string TimeStampMask)
            : this((int)OutputType.FILE, LogFileName, InitLoggingLevel, TimeStampMask)
        {
        }

        /// <summary>
        /// Sets up the logger for file logging only with a default timestamp
        /// </summary>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        /// <param name="LogFileName">The name of the file to use. If the file exists, it will be appended to</param>
        public Logger(LoggingLevel InitLoggingLevel, string LogFileName)
            : this((int)OutputType.FILE, LogFileName, (int)InitLoggingLevel, null)
        {
        }

        /// <summary>
        /// Sets up the logger for file logging only with a default timestamp
        /// </summary>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        /// <param name="LogFileName">The name of the file to use. If the file exists, it will be appended to</param>
        public Logger(int InitLoggingLevel, string LogFileName):this((int)OutputType.FILE, LogFileName, InitLoggingLevel, null)
        {
        }

        /// <summary>
        /// Sets up the logger object
        /// </summary>
        /// <param name="OutputTypes">A bitmask of OutputType (enum) specifying the output channels</param>
        /// <param name="LogFileName">The name of the file to use. If the file exists, it will be appended to</param>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        /// <param name="TimeStampMask">A mask to apply to the DateTime object. For no timestamps, set to null</param>
        public Logger(int OutputTypes, string LogFileName, LoggingLevel InitLoggingLevel, string TimeStampMask)
            : this(OutputTypes, LogFileName, (int)InitLoggingLevel, TimeStampMask)
        {
        }

        /// <summary>
        /// Sets up the logger object
        /// </summary>
        /// <param name="OutputTypes">A bitmask of OutputType (enum) specifying the output channels</param>
        /// <param name="LogFileName">The name of the file to use. If the file exists, it will be appended to</param>
        /// <param name="InitLoggingLevel">The level of the messages to log</param>
        /// <param name="TimeStampMask">A mask to apply to the DateTime object. For no timestamps, set to null</param>
        public Logger(int OutputTypes, string LogFileName, int InitLoggingLevel, string TimeStampMask)
        {
            StdOutLocker = new object();
            FileLocker = new object();

            ValidateLogLevel(InitLoggingLevel);
            CurrentLevel = (LoggingLevel)InitLoggingLevel;

            if ((OutputTypes & (int)OutputType.STDOUT) != 0)
            {
                TheWriterDelegate += this.ProcessSTDOut;
            }

            if ((OutputTypes & (int)OutputType.FILE) != 0)
            {
                if (String.IsNullOrEmpty(LogFileName))
                {
                    throw new Frost.Core.FrostException("Specified logging to file, but no filename was given");
                }

                LogWriter = new StreamWriter(LogFileName, File.Exists(LogFileName));
                if (String.IsNullOrEmpty(TimeStampMask))
                {
                    TimeStamp = "MMM-dd-yyyy HH:mm:ss.fffff";
                }
                else
                {
                    TimeStamp = TimeStampMask;
                }

                TheWriterDelegate += this.ProcessFileOut;
            }
        }

        public void WriteLog(int LogLevel, params object[] ConcatenationObjects)
        {
            if (LogLevel <= (int)this.CurrentLevel)
            {
                string Concatenated = String.Concat(ConcatenationObjects);
                string Concatenator = String.Concat(LogLevelToString(LogLevel), Concatenated);
                this.TheWriterDelegate(Concatenator);
            }
        }

        public void WriteLog(int LogLevel, string TheMessage) 
        {
            if (LogLevel <= (int)this.CurrentLevel)
            {
                string Concatenator = String.Concat(LogLevelToString(LogLevel), TheMessage);
                this.TheWriterDelegate(Concatenator);
            }
        }

        public void WriteLog(LoggingLevel LogLevel, params object[] ConcatenationObjects)
        {
            WriteLog((int)LogLevel, ConcatenationObjects);
        }

        public void WriteLog(LoggingLevel LogLevel, string TheMessage)
        {
            WriteLog((int)LogLevel, TheMessage);
        }

        static public int LogOutputMask(XmlNode SwitchNode)
        {
            if (SwitchNode == null || String.IsNullOrEmpty(SwitchNode.InnerText))
            {
                return 0;
            }

            return LogOutputMask(SwitchNode.InnerText);
        }

        static public int LogOutputMask(XmlAttribute SwitchAttribute)
        {
            if (SwitchAttribute == null)
            {
                return 0;
            }

            return LogOutputMask(SwitchAttribute.Value);
        }

        static public int LogOutputMask(string SwitchString)
        {
            int RetVal = 0;

            if (!String.IsNullOrEmpty(SwitchString))
            {
                string[] OutputSwitch = SwitchString.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);

                foreach (string ThisValue in OutputSwitch)
                {
                    string CapturedSwitch = ThisValue.Trim().ToLower();
                    Int32 DirectValue = 0;

                    if (Int32.TryParse(CapturedSwitch, out DirectValue))
                    {
                        RetVal |= DirectValue;
                    }
                    else
                    {
                        string[] EnumNames = Enum.GetNames(typeof(OutputType));

                        for (int i = 0; i < EnumNames.Length; i++)
                        {
                            if (String.Equals(CapturedSwitch, EnumNames[i], StringComparison.OrdinalIgnoreCase))
                            {
                                OutputType[] TheValues = (OutputType[])Enum.GetValues(typeof(OutputType));
                                RetVal |= (int)TheValues[i];
                                break;
                            }
                        }
                    }
                }
            }

            return RetVal;
        }

        private void ValidateLogLevel(int LogLevelValue)
        {
            if (LogLevelValue < 0)
            {
                throw new Frost.Core.FrostException("Defined LogLevel index should be positive");
            }

            int MaxLogValue = Enum.GetNames(typeof(LoggingLevel)).Length;
            if (LogLevelValue >= MaxLogValue)
            {
                throw new Frost.Core.FrostException("Defined LogLevel index should be less than ", MaxLogValue);
            }
        }

        private string LogLevelToString(int LogLevelValue)
        {
            ValidateLogLevel(LogLevelValue);

            return String.Concat("[", Enum.GetName(typeof(LoggingLevel), LogLevelValue), "]");
        }

        private void ProcessSTDOut(string Msg)
        {
            lock (StdOutLocker)
            {
                Console.WriteLine("-+-+--+-+-+-+-+-+--+-+-+-+-+-+--+-+-+-+-+-+--+-+-+-+-+-+--+-+-+-+-+-+--+-+-+-+");
                Console.WriteLine(Msg);
            }
        }

        private void ProcessFileOut(string Msg)
        {
            lock (FileLocker)
            {
                Msg = String.Concat("{", DateTime.Now.ToString(this.TimeStamp), "}", Environment.NewLine, Msg);
                this.LogWriter.WriteLine(Msg);
                this.LogWriter.Flush();
            }
        }
    }
}
