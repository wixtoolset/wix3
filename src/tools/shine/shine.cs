// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Shine
{
    using System;
    using System.Xml.Linq;

    public class Shine
    {
        public static readonly XNamespace XDgmlNamespace = "http://schemas.microsoft.com/vs/2009/dgml";

        public static void Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            CommandLine cmdLine = CommandLine.Parse(args);

            if (cmdLine.ShowLogo)
            {
                AppCommon.DisplayToolHeader();
            }

            if (cmdLine.ShowHelp)
            {
                Shine.ShowHelp();
                AppCommon.DisplayToolFooter();
                return;
            }

            // Execute the scan and display the results.
            Scanner scanner = new Scanner();
            scanner.RecurseProjects = cmdLine.RecurseProjects;

            ScanResult result = scanner.Scan(cmdLine.Paths, null, null);

            // If there is anything to filter, do so.
            if (cmdLine.IncludeSymbols.Count > 0 || cmdLine.ExcludeSymbols.Count > 0)
            {
                result.FilterSymbols(cmdLine.IncludeSymbols, cmdLine.ExcludeSymbols);
            }

            if (String.IsNullOrEmpty(cmdLine.Dgml))
            {
                Console.WriteLine("Displaying graph to console is not supported yet. Use the -dgml switch.");
            }
            else
            {
                Shine.SaveDgml(result, cmdLine.Group, cmdLine.Show, cmdLine.DgmlTemplate, cmdLine.Dgml);
            }
        }

        private static void ShowHelp()
        {
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine(" usage: shine.exe [options] path|*.wixproj|*.wixpdb|...");
            Console.WriteLine("   -dgml file               save scan as DGML file");
            Console.WriteLine("   -dgmlTemplate file       a valid DGML file populated with data from scan");
            //Console.WriteLine("   -excludePath file|dir    remove file or directory from scan");
            Console.WriteLine("   -excludeSymbol symbol    remove symbol and symbols it references from scan");
            Console.WriteLine("   -includeSymbol symbol    filter scan to include only specified symbol(s)");
            //Console.WriteLine("   -p <name>=<value>        define a property when loading MSBuild projects");
            Console.WriteLine("   -show proj;file;sym;ref  displays only the specified items in the scan");
            Console.WriteLine("                              proj - project files");
            Console.WriteLine("                              file - source files");
            Console.WriteLine("                              sym  - symbols");
            Console.WriteLine("                              ref  - symbol references");
            Console.WriteLine("                              all  - all of the above [default]");
            Console.WriteLine("   -? | -help             this help information");
            Console.WriteLine();
            Console.WriteLine("shine.exe scans directories, .wixproj files and .wixpdbs for WiX items such as:");
            Console.WriteLine("Features, ComponentGroups, Components and the references between them.");
            //Console.WriteLine("The resulting graph can be filtered by including and/or excluding symbols before");
            //Console.WriteLine("displaying to the console or saving to a DGML file.");
            //Console.WriteLine();
            //Console.WriteLine("A \"symbol\" is specified by its \"type\" and \"id\" separated by a colon. For");
            //Console.WriteLine("example:");
            //Console.WriteLine("   -includeSymbol Feature:MyFeature");
            //Console.WriteLine("   -excludeSymbol Component:CompA -excludeSymbol Component:CompB");
            //Console.WriteLine("   -excludeSymbol ComponentGroup:ComponentGoup_$(var.PreprocVariable)");
        }

        private static void SaveDgml(ScanResult result, GroupType group, ShowType show, string templatePath, string outputPath)
        {
            XElement dg;
            XElement nodes;
            XElement links;
            if (String.IsNullOrEmpty(templatePath))
            {
                nodes = new XElement(XDgmlNamespace + "Nodes");
                links = new XElement(XDgmlNamespace + "Links");
                dg = new XElement(XDgmlNamespace + "DirectedGraph", nodes, links);
            }
            else // load from the provided template path.
            {
                dg = XElement.Load(templatePath);
                nodes = dg.Element(XDgmlNamespace + "Nodes");
                if (nodes == null)
                {
                    nodes = new XElement(XDgmlNamespace + "Nodes");
                    dg.Add(nodes);
                }

                links = dg.Element(XDgmlNamespace + "Links");
                if (links == null)
                {
                    links = new XElement(XDgmlNamespace + "Links");
                    dg.Add(links);
                }
            }

            // Draw the projects.
            if (ShowType.Projects == (show & ShowType.Projects))
            {
                Console.WriteLine("Graphing projects...");
                foreach (ScannedProject project in result.ProjectFiles.Values)
                {
                    XElement node = new XElement(XDgmlNamespace + "Node",
                                new XAttribute("Id", project.Key),
                                new XAttribute("Category", "ProjectFile"),
                                new XAttribute("Reference", project.Path),
                                new XElement(XDgmlNamespace + "Category",
                                    new XAttribute("Ref", String.Concat(project.Type, "Project"))
                                    )
                                );

                    if (GroupType.Projects == (group & GroupType.Projects))
                    {
                        node.Add(new XAttribute("Group", "collapsed"));
                    }

                    nodes.Add(node);

                    foreach (ScannedProject projectRef in project.TargetProjects)
                    {
                        links.Add(new XElement(XDgmlNamespace + "Link",
                                    new XAttribute("Category", "ProjectReference"),
                                    new XAttribute("Source", project.Key),
                                    new XAttribute("Target", projectRef.Key)
                                    )
                            );
                    }

                    if (ShowType.Files == (show & ShowType.Files))
                    {
                        foreach (ScannedSourceFile file in project.SourceFiles)
                        {
                            links.Add(new XElement(XDgmlNamespace + "Link",
                                        new XAttribute("Category", "CompilesFile"),
                                        new XAttribute("Source", project.Key),
                                        new XAttribute("Target", file.Key),
                                        new XElement(XDgmlNamespace + "Category",
                                            new XAttribute("Ref", "Contains")
                                            )
                                        )
                                );
                        }
                    }
                }
            }

            // Draw the files.
            if (ShowType.Files == (show & ShowType.Files))
            {
                Console.WriteLine("Graphing files...");
                foreach (ScannedSourceFile file in result.SourceFiles.Values)
                {
                    XElement node = new XElement(XDgmlNamespace + "Node",
                                new XAttribute("Id", file.Key),
                                new XAttribute("Category", "SourceFile"),
                                new XAttribute("Reference", file.Path)
                                );

                    if (GroupType.Files == (group & GroupType.Files))
                    {
                        node.Add(new XAttribute("Group", "collapsed"));
                    }

                    nodes.Add(node);
                }
            }

            // Draw the symbols.
            if (ShowType.Symbols == (show & ShowType.Symbols))
            {
                Console.WriteLine("Graphing symbols...");
                foreach (ScannedSymbol symbol in result.Symbols.Values)
                {
                    nodes.Add(new XElement(XDgmlNamespace + "Node",
                                new XAttribute("Id", symbol.Key),
                                new XAttribute("Category", symbol.Type),
                                new XAttribute("Reference", symbol.SourceFiles[0].Path)
                                )
                        );

                    if (ShowType.Files == (show & ShowType.Files))
                    {
                        foreach (ScannedSourceFile fileRef in symbol.SourceFiles)
                        {
                            links.Add(new XElement(XDgmlNamespace + "Link",
                                        new XAttribute("Category", "DefinesSymbol"),
                                        new XAttribute("Source", fileRef.Key),
                                        new XAttribute("Target", symbol.Key),
                                        new XElement(XDgmlNamespace + "Category",
                                            new XAttribute("Ref", "Contains")
                                            )
                                        )
                                );
                        }
                    }

                    if (ShowType.References == (show & ShowType.References))
                    {
                        foreach (ScannedSymbol symbolRef in symbol.TargetSymbols)
                        {
                            links.Add(new XElement(XDgmlNamespace + "Link",
                                        new XAttribute("Category", "SymbolReference"),
                                        new XAttribute("Source", symbol.Key),
                                        new XAttribute("Target", symbolRef.Key)
                                        )
                                );
                        }
                    }
                }
            }

            dg.Save(outputPath, SaveOptions.None);
        }
    }
}
