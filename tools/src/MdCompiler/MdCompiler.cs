// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuild.Tools.MdCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class Program
    {
        private static readonly Regex UriRegex = new Regex(@"\<.+?(src|href)\s*=\s*[""'](?<uri>~/.+)[""'].*\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        private static int Main(string[] args)
        {
            CommandLine commandLine;
            if (!CommandLine.TryParseArguments(args, out commandLine))
            {
                CommandLine.ShowHelp();
                return 1;
            }

            string content = String.Empty;

            List<Document> docs = new List<Document>();
            foreach (var fileName in commandLine.Files)
            {
                Document doc = Document.Create(fileName);
                docs.Add(doc);

                content = String.Concat(String.IsNullOrEmpty(content) ? String.Empty : String.Concat(content, Environment.NewLine), doc.Text);
            }

            // Always add the defines in this order: content (if layout provided), command-line, meta.
            List<string> defines = new List<string>();

            Document firstDoc = docs[0];
            string layout;

            if (firstDoc.Meta.TryGetValue("layout", out layout))
            {
                string layoutContent;
                if (!Program.TryLoadLayout(commandLine.Layout, layout, out layoutContent))
                {
                    Console.WriteLine("Error could not find layout: {0} in the layout folder: {1}", layout, commandLine.Layout);
                    return 2;
                }

                defines.Add(String.Concat("content=", content)); // ensure "content" variable is first so it always wins.
                content = layoutContent; // replace the content with the layout, hopefully the layout has "{{content}}" in in somewhere.
            }

            defines.AddRange(commandLine.Variables); // command-line trumps document meta
            defines.AddRange(docs.SelectMany(doc => doc.Meta).Select(meta => String.Concat(meta.Key, "=", meta.Value))); // meta is last.

            // If there are defines, try to do variable subsititutions.
            if (0 < defines.Count)
            {
                content = SubstituteVariables(defines, content);
            }

            if (null != commandLine.RelativeUri)
            {
                content = Program.FixRelativePaths(content, new Uri(Path.GetFullPath(commandLine.Output)), commandLine.RelativeUri);
            }

            Program.Output(content, commandLine.Output);
            return 0;
        }

        private static bool TryLoadLayout(string layoutFolder, string name, out string content)
        {
            content = null;

            foreach (string layout in Directory.GetFiles(layoutFolder))
            {
                string filename = Path.GetFileNameWithoutExtension(layout);
                if (name.Equals(filename, StringComparison.OrdinalIgnoreCase))
                {
                    content = File.ReadAllText(layout);
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
            Match m = Program.UriRegex.Match(content);
            while (m.Success)
            {
                int index = m.Groups["uri"].Index;
                int length = m.Groups["uri"].Length;

                int offset = 0;
                string beginning = content.Substring(0, index);
                string uriValue = m.Groups["uri"].Value.Substring(2); // trim the "~/"
                string end = content.Substring(index + length);

                Uri uri = new Uri(relativeUri, uriValue);
                string newUriValue = outputUri.MakeRelativeUri(uri).ToString();

                content = String.Concat(beginning, newUriValue, end);
                m = Program.UriRegex.Match(content, index + offset);
            }

            return content;
        }

        private static void Output(string content, string outputPath)
        {
            if (!String.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetFullPath(outputPath);

                string outputFolder = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(outputFolder);

                using (TextWriter output = new StreamWriter(outputPath))
                {
                    output.WriteLine(content);
                }
            }
            else
            {
                Console.Write(content);
            }
        }
    }
}
