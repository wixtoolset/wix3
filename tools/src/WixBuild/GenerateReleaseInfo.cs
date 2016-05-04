// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for creating the metadata info for a release.
    /// </summary>
    public class GenerateReleaseInfo : Task
    {
        /// <summary>
        /// Gets and sets the version for the upload metadata.
        /// </summary>
        [Required]
        public string Version
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the set history file to process.
        /// </summary>
        [Required]
        public ITaskItem HistoryFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the set of files that will be uploaded.
        /// </summary>
        [Required]
        public ITaskItem[] UploadFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the output file name.
        /// </summary>
        [Output]
        public ITaskItem OutputFile
        {
            get;
            set;
        }

        /// <summary>
        /// Executes the task by converting upload items into metadata.
        /// </summary>
        /// <returns><see langword="true"/> if the task successfully executed; otherwise, <see langword="false"/>.</returns>
        public override bool Execute()
        {
            string files = this.UploadFilesToJsonString();
            string history = this.GatherHistoryInString();

            string[] lines = {
                "---",
                "title: v" + this.Version,
                "date: " + DateTime.Now.ToString("yyyy-MM-dd"),
                "files: [",
                files,
                " ]",
                "---",
                String.Empty,
                history
            };

            Directory.CreateDirectory(Path.GetDirectoryName(this.OutputFile.ItemSpec));
            using (StreamWriter stream = File.CreateText(this.OutputFile.ItemSpec))
            {
                stream.Write(String.Join(Environment.NewLine, lines));
            }

            return true;
        }

        private string GatherHistoryInString()
        {
            List<string> lines = new List<string>();

            string[] history = File.ReadAllLines(this.HistoryFile.ItemSpec);

            int start = -1;
            int end = -1;

            for (int i = 0; i < history.Length; ++i)
            {
                string line = history[i];
                if (String.IsNullOrEmpty(line) || line.StartsWith("# "))
                {
                    continue;
                }
                else if (line.StartsWith("## "))
                {
                    if (-1 == start)
                    {
                        start = i;
                    }
                    else
                    {
                        end = i;
                        break;
                    }
                }
                else if (-1 == start)
                {
                    this.Log.LogWarning("Expected to find '## WixBuild' line but found this instead: '{0}'", line);
                }
                else
                {
                    lines.Add(line);
                }
            }

            if (0 > end)
            {
                this.Log.LogError("Could not find beginning and ending lines for history.");
                return String.Empty;
            }

            return String.Join(Environment.NewLine, lines.ToArray());
        }

        private string UploadFilesToJsonString()
        {
            string[] files = new string[this.UploadFiles.Length];

            for (int i = 0; i < this.UploadFiles.Length; ++i)
            {
                ITaskItem item = this.UploadFiles[i];
                FileInfo file = new FileInfo(item.ItemSpec);

                StringBuilder sb = new StringBuilder();
                sb.Append("  {");
                sb.AppendFormat(" \"name\" : \"{0}\"", Path.Combine(item.GetMetadata("relativefolder"), file.Name)).Replace("\\", "/");
                sb.AppendFormat(", \"contentType\" : \"{0}\"", String.IsNullOrEmpty(item.GetMetadata("contenttype")) ? this.GuessContentType(file.Extension) : item.GetMetadata("contenttype"));
                sb.AppendFormat(", \"size\" : {0}", file.Length);

                if (!String.IsNullOrEmpty(item.GetMetadata("title")))
                {
                    sb.AppendFormat(", \"title\" : \"{0}\"", item.GetMetadata("title").Replace("\\", "\\\\"));
                }

                bool promoted;
                if (Boolean.TryParse(item.GetMetadata("promoted"), out promoted) && promoted)
                {
                    sb.Append(", \"promoted\" : true");
                }

                bool show;
                if (Boolean.TryParse(item.GetMetadata("show"), out show) && show)
                {
                    sb.Append(", \"show\" : true");
                }

                bool protectedItem;
                if (Boolean.TryParse(item.GetMetadata("protected"), out protectedItem) && protectedItem)
                {
                    sb.Append(", \"protected\" : true");
                }

                files[i] = sb.Append(" }").ToString();
            }

            return String.Join("," + Environment.NewLine, files);
        }

        private string GuessContentType(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".cab":
                    return "application/vnd.ms-cab-compressed";

                case ".zip":
                    return "application/zip";

                default:
                    return "application/octet-stream";
            }
        }
    }
}
