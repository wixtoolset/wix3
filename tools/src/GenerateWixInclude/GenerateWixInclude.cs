// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.MsgGen
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// The main entry point for GenerateWixInclude.
    /// </summary>
    public class MsgGen
    {
        /// <summary>
        /// The main entry point for GenerateWixInclude.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                GenerateWixIncludeMain generateWixInclude = new GenerateWixIncludeMain(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("GenerateWixInclude.exe : fatal error GWI0000: {0}\r\n\r\nStack Trace:\r\n{1}", e.Message, e.StackTrace);
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Main class for GenerateWixInclude.
        /// </summary>
        private class GenerateWixIncludeMain
        {
            private bool showLogo;
            private bool showHelp;

            private string inputFile;
            private string outputFile;

            /// <summary>
            /// Main method for the GenerateWixInclude application within the GenerateWixIncludeMain class.
            /// </summary>
            /// <param name="args">Commandline arguments to the application.</param>
            public GenerateWixIncludeMain(string[] args)
            {
                this.showLogo = true;
                this.showHelp = false;

                this.inputFile = null;
                this.outputFile = null;

                // parse the command line
                this.ParseCommandLine(args);

                if (null == this.inputFile || null == this.outputFile)
                {
                    this.showHelp = true;
                }

                // get the assemblies
                Assembly genWixIncludeAssembly = Assembly.GetExecutingAssembly();

                if (this.showLogo)
                {
                    Console.WriteLine("Microsoft (R) Generate Wix Include Tool version {0}", genWixIncludeAssembly.GetName().Version.ToString());
                    Console.WriteLine("Copyright (C) Microsoft Corporation 2004. All rights reserved.");
                    Console.WriteLine();
                }
                if (this.showHelp)
                {
                    Console.WriteLine(" usage:  GenerateWixInclude.exe [-?] [-nologo] input.h output.wxi");
                    Console.WriteLine();
                    Console.WriteLine("   -? this help information");
                    Console.WriteLine();
                    Console.WriteLine("For more information see: http://wix.sourceforge.net");
                    return; // exit
                }

                // load the source file and process it
                using (StreamReader input = new StreamReader(this.inputFile))
                {
                    XmlTextWriter output = null;
                    try
                    {
                        Regex regex = new Regex(@"^#define (?<name>msierr[a-zA-Z0-9_]*)\s+(?<number>[0-9]+)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
                        string str;

                        output = new XmlTextWriter(this.outputFile, Encoding.Default);
                        output.Formatting = Formatting.Indented;
                        output.WriteStartElement("Include");

                        while (null != (str = input.ReadLine()))
                        {
                            Match m = regex.Match(str);
                            if (m.Success)
                            {
                                output.WriteProcessingInstruction("define", String.Format("{0} = {1}", m.Groups["name"], m.Groups["number"]));
                            }
                        }

                        output.WriteEndElement(); // </Include>
                    }
                    finally
                    {
                        if (null != output)
                        {
                            output.Close();
                        }
                    }
                }
            }

            /// <summary>
            /// Parse the commandline arguments.
            /// </summary>
            /// <param name="args">Commandline arguments.</param>
            private void ParseCommandLine(string[] args)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (null == arg || "" == arg)   // skip blank arguments
                    {
                        continue;
                    }

                    //Console.WriteLine("arg: {0}, length: {1}", arg, arg.Length);
                    if ('-' == arg[0] || '/' == arg[0])
                    {
                        string parameter = arg.Substring(1);
                        if ("nologo" == parameter)
                        {
                            this.showLogo = false;
                        }
                        else if ("?" == parameter || "help" == parameter)
                        {
                            this.showHelp = true;
                        }
                    }
                    else if ('@' == arg[0])
                    {
                        using (StreamReader reader = new StreamReader(arg.Substring(1)))
                        {
                            string line;
                            ArrayList newArgs = new ArrayList();

                            while (null != (line = reader.ReadLine()))
                            {
                                string newArg = "";
                                bool betweenQuotes = false;
                                for (int j = 0; j < line.Length; ++j)
                                {
                                    // skip whitespace
                                    if (!betweenQuotes && (' ' == line[j] || '\t' == line[j]))
                                    {
                                        if ("" != newArg)
                                        {
                                            newArgs.Add(newArg);
                                            newArg = null;
                                        }

                                        continue;
                                    }

                                    // if we're escaping a quote
                                    if ('\\' == line[j] && '"' == line[j])
                                    {
                                        ++j;
                                    }
                                    else if ('"' == line[j])   // if we've hit a new quote
                                    {
                                        betweenQuotes = !betweenQuotes;
                                        continue;
                                    }

                                    newArg = String.Concat(newArg, line[j]);
                                }
                                if ("" != newArg)
                                {
                                    newArgs.Add(newArg);
                                }
                            }
                            string[] ar = (string[])newArgs.ToArray(typeof(string));
                            this.ParseCommandLine(ar);
                        }
                    }
                    else if (null == this.inputFile)
                    {
                        this.inputFile = arg;
                    }
                    else if (null == this.outputFile)
                    {
                        this.outputFile = arg;
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("Unknown argument '{0}'.", arg));
                    }
                }
            }
        }
    }
}
