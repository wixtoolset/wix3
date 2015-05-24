//-------------------------------------------------------------------------------------------------
// <copyright file="TransformHistoryEntries.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using LibGit2Sharp;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// MSBuild task for converting history files into entries in a git project's History.md.
    /// </summary>
    public class TransformHistoryEntries : Task
    {
        /// <summary>
        /// Gets and sets the root directory of the git project.
        /// </summary>
        [Required]
        public string ProjectFolder
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the history folder that has the history file entries.
        /// </summary>
        [Required]
        public string HistoryFolderRelativePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the history file to update.
        /// </summary>
        [Required]
        public string HistoryFile
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets and sets the name of the history file entry that should be ignored.
        /// </summary>
        [Required]
        public string SpecialHistoryFileName
        {
            get;
            set;
        }

        class HistoryFileEntry
        {
            public Blob CurrentBlob { get; set; }
            public DateTime? CreationTime { get; set; }
            public string Path { get; set; }
        }

        /// <summary>
        /// Executes the task by converting history files into entries in a git project's History.md.
        /// </summary>
        /// <returns><see langword="true"/> if the task successfully executed; otherwise, <see langword="false"/>.</returns>
        public override bool Execute()
        {
            // Start building the string to write to History.md.
            StringBuilder newHistory = new StringBuilder();

            // Connect to the git repo.
            using (Repository repo = new Repository(ProjectFolder))
            {
                // Store the history files to prepare for walking back through history to get their creation dates.
                Dictionary<string, HistoryFileEntry> historyFiles = new Dictionary<string, HistoryFileEntry>();

                // Get the tree from the latest commit in HEAD for the history folder.
                TreeEntry latestHistory = repo.Head.Tip[HistoryFolderRelativePath];

                if (null == latestHistory)
                {
                    this.Log.LogError("Could not find the project's history folder.");
                    return false;
                }

                if (TreeEntryTargetType.Tree != latestHistory.TargetType)
                {
                    this.Log.LogError("The project's history folder must be a directory.");
                    return false;
                }

                foreach (TreeEntry childHistoryEntry in ((Tree)latestHistory.Target))
                {
                    // Only consider files and skip the special file.
                    if (TreeEntryTargetType.Blob != childHistoryEntry.TargetType ||
                        childHistoryEntry.Name == SpecialHistoryFileName)
                    {
                        continue;
                    }

                    // Assume that missing files were processed in a previous run.
                    if (!File.Exists(Path.Combine(ProjectFolder, childHistoryEntry.Path)))
                    {
                        Log.LogMessage("Skipping deleted history entry: {0}", childHistoryEntry.Path);
                        continue;
                    }

                    HistoryFileEntry historyFile = new HistoryFileEntry
                    {
                        // Store the contents of the file so it can be added to the StringBuilder once we know how to sort them.
                        CurrentBlob = (Blob)childHistoryEntry.Target,
                        // Store the path so that the file can be deleted from git and the filesystem once we're done.
                        Path = childHistoryEntry.Path,
                    };

                    historyFiles.Add(childHistoryEntry.Path, historyFile);
                }

                if (historyFiles.Count == 0)
                {
                    Log.LogMessage("No history entries found");
                    return true;
                }

                // Walk through history, looking for added/renamed files.
                // Only look at each commit's first parent to try to stick to the merge commits.
                // Keep track of the number of files that were found to try to avoid going back to the beginning of time.

                int foundFiles = 0;
                Commit commit = repo.Head.Tip;
                Commit previousCommit = commit.Parents.FirstOrDefault();
                string[] targetPaths = new string[] { HistoryFolderRelativePath };
                HistoryFileEntry value;

                while (previousCommit != null && foundFiles < historyFiles.Count)
                {
                    TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>(previousCommit.Tree, commit.Tree, targetPaths);
                    foreach (TreeEntryChanges treeEntryChange in treeChanges.Added)
                    {
                        if (historyFiles.TryGetValue(treeEntryChange.Path, out value) && !value.CreationTime.HasValue)
                        {
                            value.CreationTime = commit.Committer.When.LocalDateTime;
                            ++foundFiles;
                        }
                    }
                    foreach (TreeEntryChanges treeEntryChange in treeChanges.Renamed)
                    {
                        if (historyFiles.TryGetValue(treeEntryChange.Path, out value) && !value.CreationTime.HasValue)
                        {
                            value.CreationTime = commit.Committer.When.LocalDateTime;
                            ++foundFiles;
                        }
                    }

                    commit = previousCommit;
                    previousCommit = commit.Parents.FirstOrDefault();
                }

                // Sort in descending order based on CreationTime.
                List<HistoryFileEntry> sortedHistoryFiles = historyFiles.Values.ToList();
                sortedHistoryFiles.Sort((x, y) =>
                {
                    return DateTime.Compare(y.CreationTime.HasValue ? y.CreationTime.Value : DateTime.Now, x.CreationTime.HasValue ? x.CreationTime.Value : DateTime.Now);
                });

                // Add the contents of each history entry, then delete it.
                foreach (HistoryFileEntry historyFile in sortedHistoryFiles)
                {
                    newHistory.AppendLine(historyFile.CurrentBlob.GetContentText().Trim());
                    newHistory.AppendLine();

                    Log.LogMessage("Processed history entry: {0}", historyFile.Path);
                    repo.Remove(historyFile.Path, true);
                }
            }

            // Add the new entries to the top.
            newHistory.Append(File.ReadAllText(HistoryFile));
            File.WriteAllText(HistoryFile, newHistory.ToString());

            // A different part of the build process stages History.md and does the commit and push.
            return true;
        }
    }
}
