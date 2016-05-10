// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Flattens an XML file by removing all unnecessary whitespace.
    /// </summary>
    public sealed class FlattenXml
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private bool showHelp;
        private bool showLogo = true;
        private List<string> sourceFiles = new List<string>();
        private List<string> destinationFiles = new List<string>();

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="FlattenXml"/> class.
        /// </summary>
        public FlattenXml()
        {
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            FlattenXml flattenXml = new FlattenXml();
            return flattenXml.Run(args);
        }

        /// <summary>
        /// Flattens the source file by removing all extraneous whitespace.
        /// </summary>
        /// <returns>The number of errors that were found.</returns>
        private void Flatten()
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreWhitespace = true;

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Encoding = UTF8Encoding.UTF8;
            writerSettings.Indent = false;
            writerSettings.NewLineChars = "";
            writerSettings.NewLineHandling = NewLineHandling.Replace;
            writerSettings.NewLineOnAttributes = false;

            for (int i = 0; i < this.sourceFiles.Count; i++)
            {
                string sourceFile = this.sourceFiles[i];
                string destinationFile = this.destinationFiles[i];
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                using (XmlReader reader = XmlReader.Create(sourceFile, readerSettings))
                {
                    using (XmlWriter writer = XmlWriter.Create(destinationFile, writerSettings))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.XmlDeclaration)
                            {
                                writer.WriteStartDocument();
                                continue;
                            }
                            else if (reader.LocalName == "annotation")
                            {
                                reader.Skip();
                            }

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                                writer.WriteAttributes(reader, false);

                                if (reader.IsEmptyElement)
                                {
                                    writer.WriteEndElement();
                                }
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                writer.WriteEndElement();
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (String.IsNullOrEmpty(arg)) // skip blank arguments
                {
                    continue;
                }

                if (arg[0] == '-' || arg[0] == '/')
                {
                    string parameter = arg.Substring(1);

                    switch (parameter)
                    {
                        case "?":
                            this.showHelp = true;
                            break;

                        case "nologo":
                            this.showLogo = false;
                            break;

                        default: // other parameters
                            throw new ArgumentException("Invalid argument.", parameter);
                    }
                }
                else if (arg[0] == '@')
                {
                    using (StreamReader reader = new StreamReader(arg.Substring(1)))
                    {
                        string line;
                        List<string> newArgs = new List<string>();

                        while ((line = reader.ReadLine()) != null)
                        {
                            string newArg = "";
                            bool betweenQuotes = false;

                            for (int j = 0; j < line.Length; j++)
                            {
                                // skip whitespace
                                if (!betweenQuotes && (line[j] == ' ' || line[j] == '\t'))
                                {
                                    if (!String.IsNullOrEmpty(newArg))
                                    {
                                        newArgs.Add(newArg);
                                        newArg = null;
                                    }

                                    continue;
                                }

                                // if we're escaping a quote
                                if (line[j] == '\\' && j < line.Length - 1 && line[j + 1] == '"')
                                {
                                    j++;
                                }
                                else if (line[j] == '"')   // if we've hit a new quote
                                {
                                    betweenQuotes = !betweenQuotes;
                                    continue;
                                }

                                newArg += line[j];
                            }

                            if (!String.IsNullOrEmpty(newArg))
                            {
                                newArgs.Add(newArg);
                            }
                        }

                        this.ParseCommandLine(newArgs.ToArray());
                    }
                }
                else
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing destination file");
                    }

                    string sourceFile = Path.GetFullPath(arg);
                    string destinationFile = Path.GetFullPath(args[++i]);

                    this.sourceFiles.Add(sourceFile);
                    this.destinationFiles.Add(destinationFile);
                }
            }

            if (this.sourceFiles.Count == 0 || this.destinationFiles.Count == 0)
            {
                this.showHelp = true;
            }
        }

        /// <summary>
        /// Run the application with the given arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        private int Run(string[] args)
        {
            try
            {
                this.ParseCommandLine(args);

                if (this.showLogo)
                {
                    Assembly thisAssembly = Assembly.GetExecutingAssembly();

                    Console.WriteLine("Microsoft (R) Windows Installer Flatten XML version {0}", thisAssembly.GetName().Version.ToString());
                    Console.WriteLine("Copyright (C) Microsoft Corporation 2006. All rights reserved.");
                    Console.WriteLine();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(" usage:  FlattenXml.exe sourceFile destinationFile [sourceFile destinationFile ...]");
                    Console.WriteLine();
                    Console.WriteLine("   -?                  this help information");
                    Console.WriteLine();

                    return 0;
                }

                this.Flatten();
            }
            catch (Exception e)
            {
                Console.WriteLine("FlattenXml.exe : fatal error FXML0001 : {0}\r\n\n\nStack Trace:\r\n{1}", e.Message, e.StackTrace);

                return 1;
            }

            return 0;
        }
    }
}
