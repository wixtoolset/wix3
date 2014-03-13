//-------------------------------------------------------------------------------------------------
// <copyright file="Document.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixBuild.Tools.DocCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using MarkdownSharp;

    public class Document
    {
        private static readonly Regex MetaAreaRegex = new Regex(@"^---\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex KeyValuesRegex = new Regex(@"^(?<key>\w+):\s?(?<value>.+)$", RegexOptions.Compiled);

        private Document()
        {
            this.Meta = new Dictionary<string, string>();
        }

        public string FullPath { get; set; }

        public string RelativePath { get; set; }

        public string RelativeOutputPath { get; set; }

        public IDictionary<string, string> Meta { get; private set; }

        public string Content { get; set; }

        public string Text { get; set; }

        public static Document Create(string documentPath, string inputFolder)
        {
            bool markdown = false;

            Document doc = new Document();
            doc.FullPath = documentPath;
            doc.RelativePath = documentPath.Substring(inputFolder.Length);

            if (Path.GetExtension(doc.RelativePath).Equals(".md", StringComparison.OrdinalIgnoreCase))
            {
                doc.RelativeOutputPath = doc.RelativePath.Substring(0, doc.RelativePath.Length - 3);
                markdown = true;
            }
            else
            {
                doc.RelativeOutputPath = doc.RelativePath;
            }

            string text = File.ReadAllText(doc.FullPath).Trim();

            Match metaMatch = MetaAreaRegex.Match(text);
            if (metaMatch.Success && 0 == metaMatch.Index)
            {
                Match endMetaMatch = MetaAreaRegex.Match(text, metaMatch.Length);
                if (endMetaMatch.Success)
                {
                    string[] meta = text.Substring(metaMatch.Length, endMetaMatch.Index - metaMatch.Length).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in meta)
                    {
                        Match m = KeyValuesRegex.Match(line);
                        if (m.Success)
                        {
                            string key = m.Groups["key"].Value.ToLowerInvariant();
                            string value = m.Groups["value"].Value.Trim();

                            doc.Meta.Add(key, value);
                        }
                    }

                    text = text.Substring(endMetaMatch.Index + endMetaMatch.Length).Trim();
                }
            }

            doc.Text = text;

            if (markdown)
            {
                var md = new Markdown(new MarkdownOptions { AutoHyperlink = true, EmptyElementSuffix = " />" });
                doc.Content = md.Transform(text).Trim().Replace("\n", Environment.NewLine).Replace("\r\r", "\r");
            }
            else
            {
                doc.Content = doc.Text;
            }

            return doc;
        }
    }
}
