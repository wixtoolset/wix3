//-------------------------------------------------------------------------------------------------
// <copyright file="retina.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Tool to extract files from binary Wixlibs and rebuild those Wixlibs with updated files
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    //using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Cab;

    /// <summary>
    /// Entry point for the library rebuilder
    /// </summary>
    public sealed class Retina
    {
        private StringCollection invalidArgs;
        private string inputFile;
        private ConsoleMessageHandler messageHandler;
        private string outputFile;
        private bool showHelp;
        private bool showLogo;

        /// <summary>
        /// Instantiate a new Retina class.
        /// </summary>
        private Retina()
        {
            this.invalidArgs = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("RETI", "retina.exe");
            this.showLogo = true;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Arguments to decompiler.</param>
        /// <returns>0 if sucessful, otherwise 1.</returns>
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Retina retina = new Retina();
            return retina.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            try
            {
                // parse the command line
                this.ParseCommandLine(args);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (!(String.IsNullOrEmpty(this.inputFile) ^ String.IsNullOrEmpty(this.outputFile)))
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(RetinaStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                if (!String.IsNullOrEmpty(this.inputFile))
                {
                    this.ExtractBinaryWixlibFiles();
                }
                else
                {
                    this.RebuildWixlib();
                }
            }
            catch (WixException we)
            {
                this.messageHandler.Display(this, we.Error);
            }
            catch (Exception e)
            {
                this.messageHandler.Display(this, WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return this.messageHandler.LastErrorNumber;
        }

        /// <summary>
        /// Extracts files from a binary Wixlib.
        /// </summary>
        public void ExtractBinaryWixlibFiles()
        {
            Dictionary<string, string> mapCabinetFileIdToFileName = Retina.GetCabinetFileIdToFileNameMap(this.inputFile);
            if (0 == mapCabinetFileIdToFileName.Count)
            {
                this.messageHandler.Display(this, WixWarnings.NotABinaryWixlib(this.inputFile));
                return;
            }

            // extract the files using their cabinet names ("0", "1", etc.)
            using (WixExtractCab extractor = new WixExtractCab())
            {
                extractor.Extract(this.inputFile, Path.GetDirectoryName(this.inputFile));
            }

            // the same file can be authored multiple times in the same Wixlib
            Dictionary<string, bool> uniqueFiles = new Dictionary<string, bool>();

            // rename those files to what was authored
            foreach (KeyValuePair<string, string> kvp in mapCabinetFileIdToFileName)
            {
                string cabinetFileId = Path.Combine(Path.GetDirectoryName(this.inputFile), kvp.Key);
                string fileName = Path.Combine(Path.GetDirectoryName(this.inputFile), kvp.Value);

                uniqueFiles[fileName] = true;

                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(this.inputFile), Path.GetDirectoryName(fileName)));
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                File.Move(cabinetFileId, fileName);
            }

            foreach (string fileName in uniqueFiles.Keys)
            {
                Console.WriteLine(fileName);
            }
        }

        /// <summary>
        /// Rebuild the Wixlib using the original Wixlib and updated files.
        /// </summary>
        private void RebuildWixlib()
        {
            Librarian librarian = new Librarian();
            WixVariableResolver wixVariableResolver = new WixVariableResolver();
            BlastBinderFileManager binderFileManager = new BlastBinderFileManager(this.outputFile);

            if (0 == Retina.GetCabinetFileIdToFileNameMap(this.outputFile).Count)
            {
                this.messageHandler.Display(this, WixWarnings.NotABinaryWixlib(this.outputFile));
                return;
            }

            Library library = Library.Load(this.outputFile, librarian.TableDefinitions, false, false);
            library.Save(this.outputFile, binderFileManager, wixVariableResolver);
        }

        /// <summary>
        /// Map from cabinet file ids to a normalized relative path.
        /// </summary>
        /// <param name="path">Path to Wixlib.</param>
        /// <returns>Returns the map.</returns>
        private static Dictionary<string, string> GetCabinetFileIdToFileNameMap(string path)
        {
            Dictionary<string, string> mapCabinetFileIdToFileName = new Dictionary<string, string>();
            BlastBinderFileManager binderFileManager = new BlastBinderFileManager(path);
            Librarian librarian = new Librarian();
            Library library = Library.Load(path, librarian.TableDefinitions, false, false);

            foreach (Section section in library.Sections)
            {
                foreach (Table table in section.Tables)
                {
                    foreach (Row row in table.Rows)
                    {
                        foreach (Field field in row.Fields)
                        {
                            ObjectField objectField = field as ObjectField;

                            if (null != objectField && null != objectField.Data)
                            {
                                string filePath = binderFileManager.ResolveFile(objectField.Data as string, "source", row.SourceLineNumbers, BindStage.Normal);
                                mapCabinetFileIdToFileName[objectField.CabinetFileId] = filePath;
                            }
                        }
                    }
                }
            }

            return mapCabinetFileIdToFileName;
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
                if (null == arg || 0 == arg.Length) // skip blank arguments
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
                    else if ("i" == parameter || "in" == parameter)
                    {
                        this.inputFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.inputFile))
                        {
                            return;
                        }
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.outputFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else if ("v" == parameter)
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else
                    {
                        this.invalidArgs.Add(parameter);
                    }
                }
            }
        }

        /// <summary>
        /// A custom binder file manager that returns a normalized path for both extracting files
        /// from a Wixlib and for finding them when rebuilding the same Wixlib.
        /// </summary>
        class BlastBinderFileManager : BinderFileManager
        {
            private string basePath = null;

            // shamelessly stolen from wix\Common.cs
            private static readonly Regex WixVariableRegex = new Regex(@"(\!|\$)\((?<namespace>loc|wix|bind|bindpath)\.(?<fullname>(?<name>[_A-Za-z][0-9A-Za-z_]+)(\.(?<scope>[_A-Za-z][0-9A-Za-z_\.]*))?)(\=(?<value>.+?))?\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

            public BlastBinderFileManager(string path)
            {
                this.basePath = Path.GetDirectoryName(path);
            }

            /// <summary>
            /// Resolves the source path of a file to a normalized path relative to the Wixlib.
            /// </summary>
            /// <param name="source">Original source value.</param>
            /// <param name="type">Optional type of source file being resolved.</param>
            /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
            /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
            /// <returns>Should return a valid path for the stream to be imported.</returns>
            public override string ResolveFile(string source, string type, SourceLineNumberCollection sourceLineNumbers, BindStage bindStage)
            {
                Match match = BlastBinderFileManager.WixVariableRegex.Match(source);
                if (match.Success)
                {
                    string variableNamespace = match.Groups["namespace"].Value;
                    if ("wix" == variableNamespace && match.Groups["value"].Success)
                    {
                        source = match.Groups["value"].Value;
                    }
                    else if ("bindpath" == variableNamespace)
                    {
                        string dir = String.Concat("bindpath_", match.Groups["fullname"].Value);
                        // bindpaths might or might not be followed by a backslash, depending on the pedantic nature of the author
                        string file = source.Substring(match.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        source = Path.Combine(dir, file);
                    }
                }

                if (Path.IsPathRooted(source))
                {
                    source = Path.GetFileName(source);
                }

                if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                {
                    source = source.Substring(10);
                }

                return Path.Combine(this.basePath, source);
            }
        }
    }
}
