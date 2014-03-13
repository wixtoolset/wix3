//-------------------------------------------------------------------------------------------------
// <copyright file="IndexedDocument.cs" company="Outercurve Foundation">
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
    using System.Linq;

    public class IndexedDocument : IComparable<IndexedDocument>
    {
        private List<IndexedDocument> next;
        private bool requiresSorting;

        public IndexedDocument(Document document, string outputFolder)
        {
            string filename = this.GetFilenameOnly(document.RelativePath);
            bool defaultDocument = filename.Equals("index", StringComparison.OrdinalIgnoreCase);

            this.Id = this.GenerateId(outputFolder, document.RelativePath);

            this.SourcePath = document.FullPath;
            this.RelativeOutputPath = document.RelativeOutputPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Default documents show as "folders" so treat them a level higher so the folder shows in correct
            // location. Of course, keep track of the real depth of the document since that is required sometimes.
            this.RealDepth = this.RelativeOutputPath.Count(c => (c == Path.DirectorySeparatorChar));
            this.Depth = defaultDocument && (this.RealDepth > 0) ? this.RealDepth - 1 : this.RealDepth;

            string title;
            if (!document.Meta.TryGetValue("title", out title))
            {
                Console.Error.WriteLine("warning - document: {0} does not have a title. It is highly recommend that all documents have a title.", document.RelativePath);
            }

            this.Title = title ?? String.Empty;
            this.TitleHtmlSafe = this.Title.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");

            string chmMeta = null;
            if (document.Meta.TryGetValue("chm", out chmMeta))
            {
                this.ChmDefault = "default".Equals(chmMeta, StringComparison.OrdinalIgnoreCase);
                this.ChmIgnored = "ignore".Equals(chmMeta, StringComparison.OrdinalIgnoreCase);
            }

            this.next = new List<IndexedDocument>();

            string after;
            if (!document.Meta.TryGetValue("after", out after))
            {
                // Implicitly documents come after the default document, but the default document
                // should come after the parent default document, if there can be a parent.
                if (!defaultDocument)
                {
                    after = "index";
                    this.OriginalAfter = "[implicit default document: index]";
                }
                else if (0 < this.RealDepth)
                {
                    after = "..\\index";
                    this.OriginalAfter = "[implicit parent default document: ..\\index]";
                }
            }
            else // remember the after string for error reporting purposes.
            {
                this.OriginalAfter = after;
            }

            // Support some syntatic sugar on after document paths then generate the id
            // for the after.
            if (null != after)
            {
                after = after.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                if (after.EndsWith(@"\"))
                {
                    after = String.Concat(after, "index");
                }

                if (after.StartsWith(@"~\"))
                {
                    after = after.Substring(2);
                }
                else if (after.StartsWith(@"\"))
                {
                    after = after.Substring(1);
                }
                else
                {
                    string relativeFolder = Path.GetDirectoryName(document.RelativePath);
                    after = Path.Combine(relativeFolder, after);
                }

                after = this.GenerateId(outputFolder, after);
            }

            this.AfterId = after;
        }

        public IndexedDocument AddAfter(IndexedDocument afterDoc)
        {
            this.next.Add(afterDoc);
            afterDoc.Parent = this;

            if (1 < this.next.Count)
            {
                this.requiresSorting = true;
            }

            return this;
        }

        public string Id { get; private set; }

        public string AfterId { get; private set; }

        public string OriginalAfter { get; private set; }

        public int Depth { get; private set; }

        public int RealDepth { get; private set; }

        public string RelativeOutputPath { get; private set; }

        public string SourcePath { get; private set; }

        public string Title { get; private set; }

        public string TitleHtmlSafe { get; private set; }

        public bool ChmDefault { get; private set; }

        public bool ChmIgnored { get; private set; }

        public IndexedDocument Parent { get; private set; }

        public IEnumerable<IndexedDocument> Next
        {
            get
            {
                if (this.requiresSorting)
                {
                    this.next.Sort();
                    this.requiresSorting = false;
                }

                return this.next;
            }
        }

        public int CompareTo(IndexedDocument other)
        {
            if (this.AfterId == null)
            {
                return -1;
            }

            if (this.Depth == other.Depth)
            {
                return this.Id.CompareTo(other.Id);
            }
            else if (null != this.Parent && null != other.Parent)
            {
                if (this.Depth == this.Parent.RealDepth)
                {
                    return -1;
                }
                else if (other.Depth == this.Parent.RealDepth)
                {
                    return 1;
                }
            }

            return this.Depth.CompareTo(other.Depth) * -1; // reverse depth, so things deeper in the tree end up before things higher in the tree.
        }

        private string GenerateId(string outputFolder, string name)
        {
            string relativeFolder = Path.GetDirectoryName(name);
            string filename = this.GetFilenameOnly(name);

            return Path.GetFullPath(Path.Combine(outputFolder, Path.Combine(relativeFolder, filename))).ToLowerInvariant();
        }

        private string GetFilenameOnly(string name)
        {
            string filename = Path.GetFileName(name);

            while (true)
            {
                // Chop off well known extensions.
                string extension = Path.GetExtension(filename);
                if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
                {
                    filename = Path.GetFileNameWithoutExtension(filename);
                }
                else // unknown extension, don't cut it.
                {
                    break;
                }
            }

            return filename;
        }
    }
}
