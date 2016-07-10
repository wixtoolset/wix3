// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.MsgGen
{
    using Microsoft.CSharp;
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// The main entry point for MsgGen.
    /// </summary>
    public class MsgGen
    {
        /// <summary>
        /// The main entry point for MsgGen.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                MsgGenMain msgGen = new MsgGenMain(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("MsgGen.exe : fatal error MSGG0000: {0}\r\n\r\nStack Trace:\r\n{1}", e.Message, e.StackTrace);
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Main class for MsgGen.
        /// </summary>
        private class MsgGenMain
        {
            private bool showLogo;
            private bool showHelp;

            private string sourceFile;
            private string destClassFile;
            private string destResourcesFile;

            /// <summary>
            /// Main method for the MsgGen application within the MsgGenMain class.
            /// </summary>
            /// <param name="args">Commandline arguments to the application.</param>
            public MsgGenMain(string[] args)
            {
                this.showLogo = true;
                this.showHelp = false;

                this.sourceFile = null;
                this.destClassFile = null;
                this.destResourcesFile = null;

                // parse the command line
                this.ParseCommandLine(args);

                if (null == this.sourceFile || null == this.destClassFile)
                {
                    this.showHelp = true;
                }
                if (null == this.destResourcesFile)
                {
                    this.destResourcesFile = Path.ChangeExtension(this.destClassFile, ".resources");
                }

                // get the assemblies
                Assembly msgGenAssembly = Assembly.GetExecutingAssembly();

                if (this.showLogo)
                {
                    Console.WriteLine("Microsoft (R) Message Generation Tool version {0}", msgGenAssembly.GetName().Version.ToString());
                    Console.WriteLine("Copyright (C) Microsoft Corporation 2004. All rights reserved.");
                    Console.WriteLine();
                }
                if (this.showHelp)
                {
                    Console.WriteLine(" usage:  MsgGen.exe [-?] [-nologo] sourceFile destClassFile [destResourcesFile]");
                    Console.WriteLine();
                    Console.WriteLine("   -? this help information");
                    Console.WriteLine();
                    Console.WriteLine("For more information see: http://wix.sourceforge.net");
                    return;   // exit
                }

                // load the schema
                XmlReader reader = null;
                XmlSchemaCollection schemaCollection = null;
                try
                {
                    reader = new XmlTextReader(msgGenAssembly.GetManifestResourceStream("Microsoft.Tools.MsgGen.Xsd.messages.xsd"));
                    schemaCollection = new XmlSchemaCollection();
                    schemaCollection.Add("http://schemas.microsoft.com/genmsgs/2004/07/messages", reader);
                }
                finally
                {
                    reader.Close();
                }

                // load the source file and process it
                using (StreamReader sr = new StreamReader(this.sourceFile))
                {
                    XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.None);
                    XmlValidatingReader validatingReader = new XmlValidatingReader(sr.BaseStream, XmlNodeType.Document, context);
                    validatingReader.Schemas.Add(schemaCollection);

                    XmlDocument errorsDoc = new XmlDocument();
                    errorsDoc.Load(validatingReader);

                    CodeCompileUnit codeCompileUnit = new CodeCompileUnit();

                    using (ResourceWriter resourceWriter = new ResourceWriter(this.destResourcesFile))
                    {
                        GenerateMessageFiles.Generate(errorsDoc, codeCompileUnit, resourceWriter);

                        GenerateCSharpCode(codeCompileUnit, this.destClassFile);
                    }
                }
            }

            /// <summary>
            /// Generate the actual C# code.
            /// </summary>
            /// <param name="codeCompileUnit">The code DOM.</param>
            /// <param name="destClassFile">Destination C# source file.</param>
            public static void GenerateCSharpCode(CodeCompileUnit codeCompileUnit, string destClassFile)
            {
                // generate the code with the C# code provider
                CSharpCodeProvider provider = new CSharpCodeProvider();

                // obtain an ICodeGenerator from the CodeDomProvider class
                ICodeGenerator gen = provider.CreateGenerator();

                // create a TextWriter to a StreamWriter to the output file
                using (StreamWriter sw = new StreamWriter(destClassFile))
                {
                    using (IndentedTextWriter tw = new IndentedTextWriter(sw, "    "))
                    {
                        CodeGeneratorOptions options = new CodeGeneratorOptions();

                        // code generation options
                        options.BlankLinesBetweenMembers = true;
                        options.BracingStyle = "C";

                        // generate source code using the code generator
                        gen.GenerateCodeFromCompileUnit(codeCompileUnit, tw, options);
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
                    else if (null == this.sourceFile)
                    {
                        this.sourceFile = arg;
                    }
                    else if (null == this.destClassFile)
                    {
                        this.destClassFile = arg;
                    }
                    else if (null == this.destResourcesFile)
                    {
                        this.destResourcesFile = arg;
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
