//-------------------------------------------------------------------------------------------------
// <copyright file="CommandLine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Windows Installer Xml toolset scanner command line parser.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Shine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    [Flags]
    internal enum GroupType
    {
        None = 0,
        Projects = 1,
        Files = 2,
    }

    [Flags]
    internal enum ShowType
    {
        None = 0,
        Projects = 1,
        Files = 2,
        Symbols = 4,
        References = 8,
        All = 15,
    }

    internal class CommandLine
    {
        public CommandLine()
        {
            this.Paths = new List<string>();
            this.IncludeSymbols = new SortedSet<string>();
            this.ExcludeSymbols = new SortedSet<string>();
            this.RecurseProjects = true;
            this.ShowLogo = true;
            this.Show = ShowType.All;
        }

        public string Dgml { get; set; }

        public string DgmlTemplate { get; set; }

        public GroupType Group { get; set; }

        public IList<string> Paths { get; private set; }

        public ISet<string> IncludeSymbols { get; private set; }

        public ISet<string> ExcludeSymbols { get; private set; }

        public bool RecurseProjects { get; set; }

        public ShowType Show { get; set; }

        public bool ShowLogo { get; set; }

        public bool ShowHelp { get; set; }

        public static CommandLine Parse(string[] args)
        {
            CommandLine cmdLine = new CommandLine();

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "dgml":
                            ++i;
                            cmdLine.Dgml = args[i];
                            break;

                        case "dgmltemplate":
                            ++i;
                            cmdLine.DgmlTemplate = args[i];
                            break;

                        case "excludepath":
                        case "xp":
                            ++i;
                            //scanner.ExcludePaths.Add(Path.GetFullPath(args[i]));
                            break;

                        case "excludesymbol":
                        case "xs":
                            ++i;
                            cmdLine.ExcludeSymbols.Add(args[i]);
                            break;

                        case "includesymbol":
                        case "is":
                            ++i;
                            cmdLine.IncludeSymbols.Add(args[i]);
                            break;

                        case "nologo":
                            cmdLine.ShowLogo = false;
                            break;

                        case "srp":
                            cmdLine.RecurseProjects = false;
                            break;

                        case "help":
                        case "?":
                            cmdLine.ShowHelp = true;
                            return cmdLine;

                        case "group":
                            ++i;
                            string[] groupNames = args[i].ToLowerInvariant().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string groupName in groupNames)
                            {
                                switch (groupName)
                                {
                                    case "proj":
                                    case "projs":
                                    case "project":
                                    case "projects":
                                        cmdLine.Group |= GroupType.Projects;
                                        break;

                                    case "file":
                                    case "files":
                                        cmdLine.Group |= GroupType.Files;
                                        break;
                                }
                            }
                            break;

                        case "show":
                            ++i;
                            cmdLine.Show = ShowType.None;
                            string[] showNames = args[i].ToLowerInvariant().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string showName in showNames)
                            {
                                switch (showName)
                                {
                                    case "all":
                                        cmdLine.Show |= ShowType.All;
                                        break;

                                    case "proj":
                                    case "projs":
                                    case "project":
                                    case "projects":
                                        cmdLine.Show |= ShowType.Projects;
                                        break;

                                    case "file":
                                    case "files":
                                        cmdLine.Show |= ShowType.Files;
                                        break;

                                    case "sym":
                                    case "syms":
                                    case "symbol":
                                    case "symbols":
                                        cmdLine.Show |= ShowType.Symbols;
                                        break;

                                    case "ref":
                                    case "refs":
                                    case "reference":
                                    case "references":
                                        cmdLine.Show |= ShowType.References;
                                        break;
                                }
                            }
                            break;

                        default:
                            Console.WriteLine("Unknown command line parameter: {0}", arg);
                            cmdLine.ShowHelp = true;
                            break;
                    }
                }
                else if (Directory.Exists(arg) || File.Exists(arg))
                {
                    cmdLine.Paths.Add(Path.GetFullPath(arg));
                }
                else
                {
                    Console.WriteLine("Unknown command line parameter: {0}", arg);
                    cmdLine.ShowHelp = true;
                }
            }

            if (cmdLine.Paths.Count == 0)
            {
                cmdLine.ShowHelp = true;
            }

            return cmdLine;
        }
    }
}
