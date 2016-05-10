// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuild.Tools.MdCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using MarkdownSharp;

    public class Document
    {
        private static readonly Regex metaAreaRegex = new Regex(@"^---\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex keyValuesRegex = new Regex(@"^(?<key>\w+):\s?(?<value>.+)$", RegexOptions.Compiled);

        private Document()
        {
            this.Meta = new Dictionary<string, string>();
        }

        public string Filename { get; set; }

        public IDictionary<string, string> Meta { get; private set; }

        public string Text { get; set; }

        public static Document Create(string filename)
        {
            Document doc = new Document();
            doc.Filename = filename;

            string text = File.ReadAllText(doc.Filename).Trim();

            Match metaMatch = metaAreaRegex.Match(text);
            if (metaMatch.Success && 0 == metaMatch.Index)
            {
                Match endMetaMatch = metaAreaRegex.Match(text, metaMatch.Length);
                if (endMetaMatch.Success)
                {
                    string[] meta = text.Substring(metaMatch.Length, endMetaMatch.Index - metaMatch.Length).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in meta)
                    {
                        Match m = keyValuesRegex.Match(line);
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

            var mdOptions = new MarkdownOptions { AutoHyperlink = true, EmptyElementSuffix = " />" };
            var markdown = new Markdown(mdOptions);
            doc.Text = markdown.Transform(text).Trim().Replace("\n", Environment.NewLine).Replace("\r\r", "\r");

            return doc;
        }
    }
}
