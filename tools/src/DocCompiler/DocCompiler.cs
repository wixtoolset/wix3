//-------------------------------------------------------------------------------------------------
// <copyright file="DocCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Compiles various things into documentation.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixBuild.Tools.DocCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Compiles various things into documentation.
    /// </summary>
    public class DocCompiler
    {
        private static readonly Regex RelativeUriRegex = new Regex(@"\<.+?(src|href)\s*=\s*[""'](?<uri>~/.+?)[""'].*\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        private Dictionary<string, string> layouts = new Dictionary<string, string>();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The error code for the application.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            CommandLine commandLine;
            if (!CommandLine.TryParseArguments(args, out commandLine))
            {
                CommandLine.ShowHelp();
                return 1;
            }

            try
            {
                DocCompiler docCompiler = new DocCompiler();
                return docCompiler.Run(commandLine);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
#if DEBUG
                throw;
#else
                return 1;
#endif
            }
        }

        /// <summary>
        /// Run the application.
        /// </summary>
        /// <param name="commandLine">The command line arguments.</param>
        /// <returns>The error code for the application.</returns>
        private int Run(CommandLine commandLine)
        {
            Uri outputUri = new Uri(commandLine.OutputFolder);
            List<IndexedDocument> indexedDocs = new List<IndexedDocument>();

            // Build up a list of directories to ignore when processing documents.
            var ignored = commandLine.Ignored.Select(dir => Path.Combine(commandLine.InputFolder, dir));

            foreach (string documentPath in Directory.GetFiles(commandLine.InputFolder, "*.*", SearchOption.AllDirectories))
            {
                // Skip processing if the document path is ignored.
                if (ignored.Any(str => documentPath.StartsWith(str, StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }

                Document doc = Document.Create(documentPath, commandLine.InputFolder);
                string documentOutputPath = Path.Combine(commandLine.OutputFolder, doc.RelativeOutputPath);
                string content = doc.Content;

                List<string> defines = new List<string>();
                defines.Add(String.Concat("content=", content)); // ensure "content" variable is first so it always wins.

                string layout;
                if (doc.Meta.TryGetValue("layout", out layout))
                {
                    string layoutContent;
                    if (!this.TryLoadLayout(commandLine.LayoutsFolder, layout, out layoutContent))
                    {
                        throw new ArgumentException(String.Format("Error could not find layout: {0} in the layout folder: {1} while processing document: {2}", layout, commandLine.LayoutsFolder, doc.RelativePath));
                    }

                    content = layoutContent; // replace the content with the layout, hopefully the layout has "{{content}}" in it somewhere.
                }

                defines.AddRange(commandLine.Variables); // command-line variables trump document meta.
                defines.AddRange(doc.Meta.Select(meta => String.Concat(meta.Key, "=", meta.Value))); // document meta is last.

                content = SubstituteVariables(defines, content);

                content = DocCompiler.FixRelativePaths(content, new Uri(documentOutputPath), outputUri);

                var indexedDoc = new IndexedDocument(doc, commandLine.OutputFolder);
                indexedDocs.Add(indexedDoc);

                if (!indexedDoc.ChmIgnored)
                {
                    Output(content, documentOutputPath);
                }
            }

            List<IndexedDocument> ordered = OrderIndexedDocuments(indexedDocs);
            // Useful context when debugging.
            //DumpIndex(rootDoc);
            //Console.WriteLine("------");
            //DumpOrderedIndexedDocuments(ordered);

            if (!String.IsNullOrEmpty(commandLine.AppendMarkdownTableOfContentsFile))
            {
                AppendMarkdownTableOfContents(ordered, commandLine.AppendMarkdownTableOfContentsFile, commandLine.IgnoreXsdSimpleTypeInTableOfContents);
            }

            if (!String.IsNullOrEmpty(commandLine.HtmlHelpProjectFile))
            {
                GenerateHtmlHelpProject(ordered, commandLine.HtmlHelpProjectFile, commandLine.OutputFolder);
            }

            return 0;
        }

        //private void DumpIndexedDocumentsFromRoot(IndexedDocument doc)
        //{
        //    Console.WriteLine("id: {0}\r\n   title: {1}\r\n   path: {2}", doc.Id, doc.Title, doc.Name);
        //    foreach (var next in doc.Next)
        //    {
        //        DumpIndex(next);
        //    }
        //}

        //private void DumpOrderedIndexedDocuments(List<IndexedDocument> ordered)
        //{
        //    foreach (var doc in ordered)
        //    {
        //        Console.WriteLine("id: {0}\r\n   title: {1}\r\n   path: {2}", doc.Id, doc.Title, doc.RelativeOutputPath);
        //    }
        //}

        private bool TryLoadLayout(string layoutFolder, string name, out string content)
        {
            content = null;

            if (this.layouts.TryGetValue(name, out content))
            {
                return true;
            }

            foreach (string layout in Directory.GetFiles(layoutFolder))
            {
                string filename = Path.GetFileNameWithoutExtension(layout);
                if (name.Equals(filename, StringComparison.OrdinalIgnoreCase))
                {
                    content = File.ReadAllText(layout);
                    this.layouts.Add(name, content);

                    break;
                }
            }

            return null != content;
        }

        private static string SubstituteVariables(IEnumerable<string> defines, string content)
        {
            VariableSubstitutions substitutions = new VariableSubstitutions(defines);
            string[] lines = content.Replace("\r", String.Empty).Split(new char[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = substitutions.Substitute(String.Empty, i + 1, lines[i]);
            }

            return String.Join(Environment.NewLine, lines);
        }

        private static string FixRelativePaths(string content, Uri outputUri, Uri relativeUri)
        {
            Match m = DocCompiler.RelativeUriRegex.Match(content);
            while (m.Success)
            {
                int index = m.Groups["uri"].Index;
                int length = m.Groups["uri"].Length;

                string beginning = content.Substring(0, index);
                string uriValue = m.Groups["uri"].Value.Substring(2); // trim the "~/"
                string end = content.Substring(index + length);

                Uri uri = new Uri(relativeUri, uriValue);
                string newUriValue = outputUri.MakeRelativeUri(uri).ToString();

                content = String.Concat(beginning, newUriValue, end);
                m = DocCompiler.RelativeUriRegex.Match(content, index);
            }

            return content;
        }

        private static void Output(string content, string outputPath)
        {
            string outputFolder = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            using (TextWriter output = new StreamWriter(outputPath))
            {
                output.WriteLine(content);
            }
        }

        private static List<IndexedDocument> OrderIndexedDocuments(List<IndexedDocument> indexedDocs)
        {
            Dictionary<string, IndexedDocument> index = new Dictionary<string, IndexedDocument>(indexedDocs.Count);
            IndexedDocument root = null;

            foreach (IndexedDocument doc in indexedDocs)
            {
                IndexedDocument existingDoc;
                if (index.TryGetValue(doc.Id, out existingDoc))
                {
                    throw new ApplicationException(String.Format("Document: {0} and document: {1} generate same identifier. Change one of the file names or trying cleaning and building again if you recently renamed a file.", existingDoc.RelativeOutputPath, doc.RelativeOutputPath));
                }

                index.Add(doc.Id, doc);
            }

            foreach (IndexedDocument doc in indexedDocs)
            {
                if (String.IsNullOrEmpty(doc.AfterId))
                {
                    if (null != root)
                    {
                        throw new ApplicationException(String.Format("Found multiple root documents at: {0} and {1}. Only one document can be the root 'index' file.", root.RelativeOutputPath, doc.RelativeOutputPath));
                    }

                    root = doc;
                }
                else
                {
                    IndexedDocument beforeDoc;
                    if (!index.TryGetValue(doc.AfterId, out beforeDoc))
                    {
                        throw new ArgumentException(String.Format("Error in document: {0} cannot find matching document for metadata: after={1}", doc.RelativeOutputPath, doc.OriginalAfter));
                    }

                    beforeDoc.AddAfter(doc);
                }
            }

            if (null == root)
            {
                throw new ApplicationException("Cannot find root document. There must be one and only one document named 'index' in the root that is not after any other document.");
            }

            List<IndexedDocument> ordered = new List<IndexedDocument>(indexedDocs.Count);
            TraverseIndexedDocuments(root, ordered);

            return ordered;
        }

        private static void TraverseIndexedDocuments(IndexedDocument doc, List<IndexedDocument> ordered)
        {
            ordered.Add(doc);
            foreach (var next in doc.Next)
            {
                TraverseIndexedDocuments(next, ordered);
            }
        }

        private void GenerateHtmlHelpProject(List<IndexedDocument> ordered, string projectFile, string outputFolder)
        {
            Uri projectUri = new Uri(Path.GetDirectoryName(Path.GetFullPath(projectFile)) + Path.DirectorySeparatorChar);
            Uri outputUri = new Uri(Path.GetFullPath(outputFolder + Path.DirectorySeparatorChar));
            string relativePath = projectUri.MakeRelativeUri(outputUri).ToString();

            IndexedDocument root = ordered.Where(d => d.ChmDefault).FirstOrDefault();
            if (null == root)
            {
                throw new ApplicationException("Cannot find default document. There must be one and only one document with meta 'chm: default' in set of documents compiled into a .chm.");
            }

            string chmFile = Path.ChangeExtension(projectFile, ".chm");
            string indexFile = Path.ChangeExtension(projectFile, ".hhk");
            string tocFile = Path.ChangeExtension(projectFile, ".hhc");
            string logFile = Path.ChangeExtension(projectFile, ".log");

            // create the project file
            using (StreamWriter sw = File.CreateText(projectFile))
            {
                sw.WriteLine("[OPTIONS]");
                sw.WriteLine("Compatibility=1.1 or later");
                sw.WriteLine(String.Format("Compiled file={0}", chmFile));
                sw.WriteLine("Contents file={0}", tocFile);
                sw.WriteLine("Index file={0}", indexFile);
                sw.WriteLine("Default Window=Main");
                sw.WriteLine(String.Format("Default topic={0}", Path.Combine(relativePath, root.RelativeOutputPath)));
                sw.WriteLine("Display compile progress=No");
                sw.WriteLine("Error log file={0}", logFile);
                sw.WriteLine("Full-text search=Yes");
                sw.WriteLine("Language=0x409 English (United States)");
                sw.WriteLine(String.Format("Title={0}", root.TitleHtmlSafe));
                sw.WriteLine("");
                sw.WriteLine("[WINDOWS]");
                sw.WriteLine("Main=,\"{0}\",\"{1}\",\"{2}\",\"{2}\",,,,,0x63520,,0x384e,,,,,,,,0", tocFile, indexFile, Path.Combine(relativePath, root.RelativeOutputPath));
            }

            // create the index file
            using (StreamWriter sw = File.CreateText(indexFile))
            {
                sw.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\">");
                sw.WriteLine("<HTML>");
                sw.WriteLine("<HEAD>");
                sw.WriteLine("<META NAME=\"GENERATOR\" CONTENT=\"WiX Toolset DocCompiler\"/>");
                sw.WriteLine("</HEAD>");
                sw.WriteLine("<BODY>");
                sw.WriteLine("<OBJECT TYPE=\"text/site properties\">");
                sw.WriteLine("\t<PARAM NAME=\"FrameName\" VALUE=\"TEXT\"/>");
                sw.WriteLine("</OBJECT>");
                sw.WriteLine("<UL>");

                foreach (var doc in ordered)
                {
                    if (doc.ChmIgnored)
                    {
                        continue;
                    }

                    sw.WriteLine("\t<LI> <OBJECT type=\"text/sitemap\">");
                    sw.WriteLine(String.Format("\t\t<param name=\"Keyword\" value=\"{0}\">", doc.TitleHtmlSafe));
                    sw.WriteLine(String.Format("\t\t<param name=\"Name\" value=\"{0}\">", doc.TitleHtmlSafe));
                    sw.WriteLine(String.Format("\t\t<param name=\"Local\" value=\"{0}\">", Path.Combine(relativePath, doc.RelativeOutputPath)));
                    sw.WriteLine("\t\t</OBJECT>");
                }

                sw.WriteLine("</UL>");
                sw.WriteLine("</BODY>");
                sw.WriteLine("</HTML>");
            }

            // create the table of contents file
            using (StreamWriter sw = File.CreateText(tocFile))
            {
                sw.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\">");
                sw.WriteLine("<HTML>");
                sw.WriteLine("<HEAD>");
                sw.WriteLine("<meta name=\"GENERATOR\" content=\"Microsoft&reg; HTML Help Workshop 4.1\">");
                sw.WriteLine("<!-- Sitemap 1.0 -->");
                sw.WriteLine("</HEAD><BODY>");
                sw.WriteLine("<OBJECT type=\"text/site properties\">");
                sw.WriteLine("\t<param name=\"ImageType\" value=\"Folder\">");
                sw.WriteLine("</OBJECT>");
                sw.WriteLine("<UL>");

                int depth = root.Depth;
                foreach (var doc in ordered)
                {
                    if (doc.ChmIgnored)
                    {
                        continue;
                    }

                    while (depth < doc.Depth)
                    {
                        sw.WriteLine("<UL>");
                        ++depth;
                    }

                    while (depth > doc.Depth)
                    {
                        sw.WriteLine("</UL>");
                        --depth;
                    }

                    sw.WriteLine("\t<LI> <OBJECT type=\"text/sitemap\">");
                    sw.WriteLine(String.Format("\t\t<param name=\"Name\" value=\"{0}\">", doc.TitleHtmlSafe));
                    sw.WriteLine(String.Format("\t\t<param name=\"Local\" value=\"{0}\">", Path.Combine(relativePath, doc.RelativeOutputPath)));
                    sw.WriteLine("\t\t</OBJECT>");
                }

                while (depth > root.Depth)
                {
                    sw.WriteLine("</UL>");
                    --depth;
                }

                sw.WriteLine("</UL>");
                sw.WriteLine("</BODY></HTML>");
            }
        }

        private void AppendMarkdownTableOfContents(List<IndexedDocument> ordered, string tocFile, bool ignoreXsdSimpleTypes)
        {
            using (StreamWriter sw = File.AppendText(tocFile))
            {
                foreach (var doc in ordered)
                {
                    // If we happen to be appending to ourselves, don't add ourselves to the
                    // TOC.
                    if (Path.GetFullPath(tocFile).Equals(doc.SourcePath))
                    {
                        continue;
                    }

                    if (ignoreXsdSimpleTypes)
                    {
                        if (doc.TitleHtmlSafe.EndsWith(" (Simple Type)") && Path.GetFileName(doc.SourcePath).StartsWith("simple_type_", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }


                    // prepend with the correct number of spaces to get the Markdown list
                    // indent correct.
                    for (int i = 0; i < doc.Depth; ++i)
                    {
                        sw.Write("   ");
                    }

                    sw.WriteLine("* [{0}]({1})", doc.TitleHtmlSafe, doc.RelativeOutputPath.Replace('\\', '/'));
                }
            }
        }
    }
}
