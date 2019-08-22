// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;
    using Microsoft.Build.Evaluation;

    /// <summary>
    /// Scans a set of paths to return symbols and references using filter and exclusions.
    /// </summary>
    public class Scanner
    {
        private const string WixNamespace = "http://schemas.microsoft.com/wix/2006/wi";

        public bool RecurseProjects { get; set; }

        public event ScannerMessageEventHandler Messages;

        public ScanResult Scan(IEnumerable<string> paths, IList<string> includeSymbols, IList<string> excludeSymbols)
        {
            ScanResult result = new ScanResult();

            Queue<ProcessPath> queue = new Queue<ProcessPath>();
            foreach (string path in paths)
            {
                queue.Enqueue(new ProcessPath(path));
            }

            while (0 < queue.Count)
            {
                ProcessPath process = queue.Dequeue();
                if (process.Project == null && Directory.Exists(process.Path))
                {
                    foreach (string directory in Directory.GetDirectories(process.Path))
                    {
                        queue.Enqueue(new ProcessPath(directory));
                    }

                    foreach (string file in Directory.GetFiles(process.Path))
                    {
                        IEnumerable<ProcessPath> more = this.ProcessFile(process, result);
                        foreach (ProcessPath item in more)
                        {
                            queue.Enqueue(item);
                        }
                    }
                }
                else
                {
                    IEnumerable<ProcessPath> more = this.ProcessFile(process, result);
                    foreach (ProcessPath item in more)
                    {
                        queue.Enqueue(item);
                    }
                }
            }

            this.ResolveSymbols(result);
            return result;
        }

        private IEnumerable<ProcessPath> ProcessFile(ProcessPath process, ScanResult result)
        {
            IEnumerable<ProcessPath> moreFiles;

            string extension = Path.GetExtension(process.Path);
            if (extension.Equals(".wixproj", StringComparison.OrdinalIgnoreCase))
            {
                moreFiles = this.ProcessProjectFile(process, result);
            }
            else
            {
                this.ProcessSourceFile(process, result);
                moreFiles = new List<ProcessPath>();
            }

            return moreFiles;
        }

        private IEnumerable<ProcessPath> ProcessProjectFile(ProcessPath process, ScanResult result)
        {
            List<ProcessPath> newFiles = new List<ProcessPath>();

            // If this project is not processed already, read through it all.
            ScannedProject scannedProject;
            string key = ScannedProject.CalculateKey(process.Path, process.Properties);
            if (!result.ProjectFiles.TryGetValue(key, out scannedProject))
            {
                Project project = new Project(process.Path, process.Properties, null);
                string projectFolder = Path.GetDirectoryName(project.FullPath);
                string type = project.GetPropertyValue("OutputType");

                scannedProject = new ScannedProject(type, project.FullPath, process.Properties, null);
                ICollection<ProjectItem> projectReferences = project.GetItemsIgnoringCondition("ProjectReference");
                if (this.RecurseProjects && projectReferences != null)
                {
                    foreach (ProjectItem projectReference in projectReferences)
                    {
                        // TODO: process Property metadata.
                        string include = Path.Combine(projectFolder, projectReference.EvaluatedInclude);
                        newFiles.Add(new ProcessPath(include) { Project = scannedProject });
                    }
                }

                ICollection<ProjectItem> compiles = project.GetItemsIgnoringCondition("Compile");
                if (compiles != null)
                {
                    foreach (ProjectItem item in compiles)
                    {
                        // TODO: process DefineConstants property.
                        string include = Path.Combine(projectFolder, item.EvaluatedInclude);
                        newFiles.Add(new ProcessPath(include) { Project = scannedProject });
                    }
                }

                Debug.Assert(key == scannedProject.Key, String.Format("{0} should equal {1}", key, scannedProject.Key));
                result.ProjectFiles.Add(scannedProject.Key, scannedProject);
            }

            // If there is a parent project, create a reference between the two projects.
            if (process.Project != null)
            {
                process.Project.TargetProjects.Add(scannedProject);
                scannedProject.SourceProjects.Add(process.Project);
                //result.ProjectToProjectReferences.Add(new ScannedProjectProjectReference() { SourceProject = process.Project, TargetProject = scannedProject });
            }

            return newFiles;
        }

        private void ProcessSourceFile(ProcessPath process, ScanResult result)
        {
            ScannedSourceFile sourceFile = null;
            string fileKey = ScannedSourceFile.CalculateKey(process.Path, process.Properties);
            if (!result.SourceFiles.TryGetValue(fileKey, out sourceFile) && !result.UnknownFiles.Contains(process.Path))
            {
                try
                {
                    sourceFile = new ScannedSourceFile(process.Path, process.Properties);
                    if (this.AddSymbols(result, sourceFile))
                    {
                        result.SourceFiles.Add(sourceFile.Key, sourceFile);
                    }
                    else
                    {
                        result.UnknownFiles.Add(process.Path);
                    }
                }
                catch (Exception e)
                {
                    this.OnMessage(ScannerMessageType.Warning, "Skipping non-XML file: {0} - reason: {1}", process.Path, e.Message);
                    result.UnknownFiles.Add(process.Path);
                }
            }

            if (sourceFile != null && process.Project != null)
            {
                process.Project.SourceFiles.Add(sourceFile);
                sourceFile.SourceProjects.Add(process.Project);
                //result.ProjectToSourceFileReferences.Add(new ScannedProjectSourceFileReference() { SourceProject = process.Project, TargetSourceFile = sourceFile });
            }
        }

        private bool AddSymbols(ScanResult result, ScannedSourceFile sourceFile)
        {
            bool validWixSource = false;

            using (FileStream fs = new FileStream(sourceFile.Path, FileMode.Open, FileAccess.Read))
            {
                XPathDocument doc = new XPathDocument(fs);
                XPathNavigator nav = doc.CreateNavigator();

                XmlNamespaceManager manager = new XmlNamespaceManager(nav.NameTable);
                manager.AddNamespace("wix", WixNamespace);

                XPathExpression rootExpression = XPathExpression.Compile("/wix:Wix", manager);
                XPathNodeIterator rootnav = nav.Select(rootExpression);
                if (rootnav.MoveNext() && rootnav.Current.NodeType == XPathNodeType.Element)
                {
                    validWixSource = true;

                    XPathExpression exp = XPathExpression.Compile("wix:Bundle|wix:Product|//wix:PackageGroup|//wix:PayloadGroup|//wix:Payload|//wix:Feature|//wix:ComponentGroup|//wix:Component|//wix:MsiPackage|//wix:MspPackage|//wix:MsuPackage|//wix:ExePackage", manager);
                    XPathExpression bundleReferenceExpression = XPathExpression.Compile("wix:PayloadGroupRef|wix:Chain/wix:PackageGroupRef|wix:Chain/wix:MsiPackage|wix:Chain/wix:MspPackage|wix:Chain/wix:MsuPackage|wix:Chain/wix:ExePackage", manager);
                    XPathExpression packageGroupReferenceExpression = XPathExpression.Compile("wix:PackageGroupRef|wix:MsiPackage|wix:MspPackage|wix:MsuPackage|wix:ExePackage", manager);
                    XPathExpression payloadGroupReferenceExpression = XPathExpression.Compile("wix:PayloadGroupRef|wix:Payload", manager);
                    XPathExpression productReferenceExpression = XPathExpression.Compile("wix:Feature|wix:FeatureRef", manager);
                    XPathExpression featureReferenceExpression = XPathExpression.Compile("wix:Feature|wix:FeatureRef|wix:ComponentGroupRef|wix:ComponentRef|wix:Component", manager);
                    XPathExpression componentGroupReferenceExpression = XPathExpression.Compile("wix:ComponentGroupRef|wix:ComponentRef|wix:Component", manager);
                    //XPathExpression componentReferenceExpression = XPathExpression.Compile("wix:File|wix:ServiceInstall|wix:Shortcut", manager);

                    XPathNodeIterator i = rootnav.Current.Select(exp);
                    while (i.MoveNext())
                    {
                        XPathNavigator node = i.Current;
                        string type = node.LocalName;
                        string id = null;
                        XPathExpression references = null;

                        switch (type)
                        {
                            case "Bundle":
                                id = node.GetAttribute("Name", String.Empty);
                                references = bundleReferenceExpression;
                                break;

                            case "PackageGroup":
                                id = node.GetAttribute("Id", String.Empty);
                                references = packageGroupReferenceExpression;
                                break;

                            case "PayloadGroup":
                                id = node.GetAttribute("Id", String.Empty);
                                references = payloadGroupReferenceExpression;
                                break;

                            case "Product":
                                id = node.GetAttribute("Name", String.Empty);
                                references = productReferenceExpression;
                                break;

                            case "Payload":
                            case "ExePackage":
                            case "MsiPackage":
                            case "MspPackage":
                            case "MsuPackage":
                                id = node.GetAttribute("Id", String.Empty);
                                break;

                            case "Feature":
                                id = node.GetAttribute("Id", String.Empty);
                                references = featureReferenceExpression;
                                break;

                            case "ComponentGroup":
                                id = node.GetAttribute("Id", String.Empty);
                                references = componentGroupReferenceExpression;
                                break;

                            case "Component":
                                id = node.GetAttribute("Id", String.Empty);
                                //references = componentReferenceExpression;
                                break;
                        }

                        if (String.IsNullOrEmpty(id))
                        {
                            this.OnMessage(ScannerMessageType.Warning, "Symbol type: {0} in {1} skipped because it is missing an Id.", node.LocalName, sourceFile.Path);
                        }
                        else
                        {
                            ScannedSymbol symbol;
                            string key = ScannedSymbol.CalculateKey(type, id);
                            if (!result.Symbols.TryGetValue(key, out symbol))
                            {
                                symbol = new ScannedSymbol(type, id);
                                result.Symbols.Add(symbol.Key, symbol);
                            }

                            sourceFile.TargetSymbols.Add(symbol);
                            symbol.SourceFiles.Add(sourceFile);
                            //result.SourceFileToSymbolReference.Add(new ScannedSourceFileSymbolReference() { SourceSourceFile = sourceFile, TargetSymbol = symbol });
                            if (references != null)
                            {
                                AddReferences(node, references, sourceFile, symbol, result);
                            }
                        }
                    }
                }
            }

            return validWixSource;
        }

        private void AddReferences(XPathNavigator nav, XPathExpression xpath, ScannedSourceFile sourceFile, ScannedSymbol symbol, ScanResult result)
        {
            XPathNodeIterator i = nav.Select(xpath);
            while (i.MoveNext())
            {
                XPathNavigator n = i.Current;
                string id = n.GetAttribute("Id", String.Empty);

                if (String.IsNullOrEmpty(id))
                {
                    this.OnMessage(ScannerMessageType.Warning, "Reference type: {0} in Symbol: {1} in {2} skipped because it is missing an Id.", n.LocalName, symbol.Key, sourceFile.Path);
                }
                else
                {
                    string type = n.LocalName;
                    if (type.EndsWith("Ref"))
                    {
                        type = type.Substring(0, n.LocalName.Length - 3);
                    }

                    result.Unresolved.Add(new ScannedUnresolvedReference(type, id, sourceFile, symbol));
                }
            }
        }

        private void ResolveSymbols(ScanResult result)
        {
            // Walk backwards through the unresolved list because we will remove items as
            // we find matches. In the end, only the symbols that can't be found will be
            // left in the list.
            for (int i = result.Unresolved.Count; i > 0; --i)
            {
                ScannedUnresolvedReference reference = result.Unresolved[i - 1];
                ScannedSymbol symbol;
                if (result.Symbols.TryGetValue(reference.TargetSymbol, out symbol))
                {
                    reference.SourceSymbol.TargetSymbols.Add(symbol);
                    symbol.SourceSymbols.Add(reference.SourceSymbol);
                    //result.SymbolToSymbolReference.Add(new ScannedSymbolSymbolReference() { SourceSymbol = reference.SourceSymbol, TargetSymbol = symbol });

                    result.Unresolved.RemoveAt(i - 1);
                }
            }
        }

        private void OnMessage(ScannerMessageType type, string format, params object[] details)
        {
            if (this.Messages != null)
            {
                ScannerMessageEventArgs ea = new ScannerMessageEventArgs() { Message = String.Format(format, details), Type = type };
                this.Messages(this, ea);
            }
        }

        private class ProcessPath
        {
            public ProcessPath(string path)
            {
                this.Path = System.IO.Path.GetFullPath(path);
                this.Properties = new Dictionary<string, string>();
            }

            public string Path { get; private set; }

            public ScannedProject Project { get; set; }

            public IDictionary<string, string> Properties { get; set; }
        }
    }
}
