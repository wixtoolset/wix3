//-------------------------------------------------------------------------------------------------
// <copyright file="WixCop.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wix source code style inspector and repair utility.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The WiXCop application class.
    /// </summary>
    public class WixCop
    {
        private string[] errorsAsWarnings;
        private string[] ignoreErrorsHash;
        private Hashtable exemptFiles;
        private bool fixErrors;
        private Inspector inspector;
        private ArrayList searchPatterns;
        private Dictionary<string, bool> searchPatternResults;
        private string settingsFile1;
        private string settingsFile2;
        private string settingsFileDefault;
        private bool showHelp;
        private bool showLogo;
        private bool subDirectories;
        private int indentationAmount;

        /// <summary>
        /// Instantiate a new WixCop class.
        /// </summary>
        private WixCop()
        {
            this.exemptFiles = new Hashtable();
            this.searchPatterns = new ArrayList();
            this.searchPatternResults = new Dictionary<string, bool>();
            this.showLogo = true;
            this.indentationAmount = 4;
            this.settingsFileDefault = "wixcop.settings.xml";
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            WixCop wixCop = new WixCop();
            return wixCop.Run(args);
        }

        /// <summary>
        /// Get the files that match a search path pattern.
        /// </summary>
        /// <param name="baseDir">The base directory at which to begin the search.</param>
        /// <param name="searchPath">The search path pattern.</param>
        /// <returns>The files matching the pattern.</returns>
        private static string[] GetFiles(string baseDir, string searchPath)
        {
            // convert alternate directory separators to the standard one
            string filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            int lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            string[] files = null;

            try
            {
                if (0 > lastSeparator)
                {
                    files = Directory.GetFiles(baseDir, filePath);
                }
                else // found directory separator
                {
                    string searchPattern = filePath.Substring(lastSeparator + 1);

                    files = Directory.GetFiles(filePath.Substring(0, lastSeparator + 1), searchPattern);
                }
            }
            catch (DirectoryNotFoundException)
            {
                // don't let this function throw the DirectoryNotFoundException. (this exception
                // occurs for non-existant directories and invalid characters in the searchPattern)
            }

            return files;
        }

        /// <summary>
        /// Inspect sub-directories.
        /// </summary>
        /// <param name="directory">The directory whose sub-directories will be inspected.</param>
        /// <returns>The number of errors that were found.</returns>
        private int InspectSubDirectories(string directory)
        {
            int errors = 0;

            foreach (string searchPattern in this.searchPatterns)
            {
                foreach (string sourceFile in GetFiles(directory, searchPattern))
                {
                    FileInfo file = new FileInfo(sourceFile);

                    if (!this.exemptFiles.Contains(file.Name.ToUpperInvariant()))
                    {
                        searchPatternResults[searchPattern] = true;
                        errors += this.inspector.InspectFile(file.FullName, this.fixErrors);
                    }
                }
            }

            if (this.subDirectories)
            {
                foreach (string childDirectory in Directory.GetDirectories(directory))
                {
                    errors += this.InspectSubDirectories(childDirectory);
                }
            }

            return errors;
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
                    Assembly wixcopAssembly = this.GetType().Assembly;
                    FileVersionInfo fv = FileVersionInfo.GetVersionInfo(wixcopAssembly.Location);

                    Console.WriteLine("Windows Installer Xml Cop version {0}", fv.FileVersion);
                    Console.WriteLine("Copyright (C) Outercurve Foundation. All rights reserved.");
                    Console.WriteLine();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(" usage:  wixcop.exe sourceFile [sourceFile ...]");
                    Console.WriteLine();
                    Console.WriteLine("   -f       fix errors automatically for writable files");
                    Console.WriteLine("   -nologo  suppress displaying the logo information");
                    Console.WriteLine("   -s       search for matching files in current dir and subdirs");
                    Console.WriteLine("   -set1<file> primary settings file");
                    Console.WriteLine("   -set2<file> secondary settings file (overrides primary)");
                    Console.WriteLine("   -indent:<n> indentation multiple (overrides default of 4)");
                    Console.WriteLine("   -?       this help information");
                    Console.WriteLine();
                    Console.WriteLine("   sourceFile may use wildcards like *.wxs");

                    return 0;
                }

                // parse the settings if any were specified
                if (null != this.settingsFile1 || null != this.settingsFile2)
                {
                    this.ParseSettingsFiles(this.settingsFile1, this.settingsFile2);
                }
                else
                {
                    if (File.Exists(settingsFileDefault))
                    {
                        this.ParseSettingsFiles(this.settingsFileDefault, null);
                    }
                }

                this.inspector = new Inspector(this.errorsAsWarnings, this.ignoreErrorsHash, this.indentationAmount);

                int errors = this.InspectSubDirectories(Path.GetFullPath("."));

                foreach (string searchPattern in this.searchPatterns)
                {
                    if (!this.searchPatternResults.ContainsKey(searchPattern))
                    {
                        Console.WriteLine("Could not find file \"{0}\"", searchPattern);
                        errors++;
                    }
                }

                return errors != 0 ? 2 : 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("wixcop.exe : fatal error WXCP0001 : {0}\r\n\n\nStack Trace:\r\n{1}", e.Message, e.StackTrace);

                return 1;
            }
        }

        /// <summary>
        /// Parse the primary and secondary settings files.
        /// </summary>
        /// <param name="localSettingsFile1">The primary settings file.</param>
        /// <param name="localSettingsFile2">The secondary settings file.</param>
        private void ParseSettingsFiles(string localSettingsFile1, string localSettingsFile2)
        {
            if (null == localSettingsFile1 && null != localSettingsFile2)
            {
                throw new ArgumentException("Cannot specify a secondary settings file (set2) without a primary settings file (set1).", "localSettingsFile2");
            }

            Hashtable errorsAsWarningsHash = new Hashtable();
            Hashtable localIgnoreErrorsHash = new Hashtable();
            string settingsFile = localSettingsFile1;
            while (null != settingsFile)
            {
                XmlTextReader reader = null;
                try
                {
                    reader = new XmlTextReader(settingsFile);
                    XmlDocument doc = new XmlDocument();
                    doc.Load(reader);

                    // get the types of tests that will have their errors displayed as warnings
                    XmlNodeList testsIgnoredElements = doc.SelectNodes("/Settings/IgnoreErrors/Test");
                    foreach (XmlNode test in testsIgnoredElements)
                    {
                        localIgnoreErrorsHash[((XmlElement)test).GetAttribute("Id")] = null;
                    }

                    // get the types of tests that will have their errors displayed as warnings
                    XmlNodeList testsAsWarningsElements = doc.SelectNodes("/Settings/ErrorsAsWarnings/Test");
                    foreach (XmlNode test in testsAsWarningsElements)
                    {
                        errorsAsWarningsHash[((XmlElement)test).GetAttribute("Id")] = null;
                    }

                    // get the exempt files
                    XmlNodeList localExemptFiles = doc.SelectNodes("/Settings/ExemptFiles/File");
                    foreach (XmlNode file in localExemptFiles)
                    {
                        this.exemptFiles[((XmlElement)file).GetAttribute("Name").ToUpperInvariant()] = null;
                    }
                }
                finally
                {
                    if (null != reader)
                    {
                        reader.Close();
                    }
                }

                settingsFile = localSettingsFile2;
                localSettingsFile2 = null;
            }

            // copy the settings to nice string arrays
            this.errorsAsWarnings = new string[errorsAsWarningsHash.Keys.Count];
            errorsAsWarningsHash.Keys.CopyTo(this.errorsAsWarnings, 0);
            this.ignoreErrorsHash = new string[localIgnoreErrorsHash.Keys.Count];
            localIgnoreErrorsHash.Keys.CopyTo(this.ignoreErrorsHash, 0);
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            this.showHelp = 0 == args.Length;

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (String.IsNullOrEmpty(arg)) // skip blank arguments
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    switch (parameter)
                    {
                        case "?":
                            this.showHelp = true;
                            break;
                        case "f":
                            this.fixErrors = true;
                            break;
                        case "nologo":
                            this.showLogo = false;
                            break;
                        case "s":
                            this.subDirectories = true;
                            break;
                        default: // other parameters
                            if (parameter.StartsWith("set1", StringComparison.Ordinal))
                            {
                                this.settingsFile1 = parameter.Substring(4);
                            }
                            else if (parameter.StartsWith("set2", StringComparison.Ordinal))
                            {
                                this.settingsFile2 = parameter.Substring(4);
                            }
                            else if (parameter.StartsWith("indent:", StringComparison.Ordinal))
                            {
                                try
                                {
                                    this.indentationAmount = Int32.Parse(parameter.Substring(7), CultureInfo.InvariantCulture);
                                }
                                catch
                                {
                                    throw new ArgumentException("Invalid numeric argument.", parameter);
                                }
                            }
                            else
                            {
                                throw new ArgumentException("Invalid argument.", parameter);
                            }
                            break;
                    }
                }
                else if ('@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else
                {
                    this.searchPatterns.Add(arg);
                }
            }
        }
    }
}
