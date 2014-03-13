//-------------------------------------------------------------------------------------------------
// <copyright file="GenerateReleaseHistory.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for creating the metadata info for a release.
    /// </summary>
    public class GenerateReleaseHistory : Task
    {
        /// <summary>
        /// Gets and sets the version for the metadata.
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
            List<string> lines = new List<string>();
            lines.Add("---");
            lines.Add(String.Format("title: v{0}", this.Version));
            lines.Add(String.Format("date: {0}", DateTime.Now.ToString("yyyy-MM-ddTHH:mmzzz")));
            lines.Add("---");

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
                else if (start < -1)
                {
                    if (line.StartsWith("## "))
                    {
                        // could get the version of the build now.
                    }
                    else
                    {
                        lines.Add(line);
                    }

                    start = i;
                }
                else if (line.StartsWith("## "))
                {
                    end = i;
                    break;
                }
                else
                {
                    lines.Add(line);
                }
            }

            if (0 > end)
            {
                this.Log.LogError("Could not find beginning and ending lines for history.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(this.OutputFile.ItemSpec));
            using (StreamWriter stream = File.CreateText(this.OutputFile.ItemSpec))
            {
                stream.Write(String.Join(Environment.NewLine, lines.ToArray()));
            }

            return true;
        }
    }
}
