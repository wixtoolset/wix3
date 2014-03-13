//-------------------------------------------------------------------------------------------------
// <copyright file="GetLooseFileList.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Win32;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class GetLooseFileList : Task
    {
        private ITaskItem database;
        private ITaskItem[] looseFileList;

        internal const int MsidbFileAttributesNoncompressed = 8192;
        internal const int MsidbFileAttributesCompressed = 16384;

        /// <summary>
        /// The list of database files to find Loose Files in
        /// </summary>
        [Required]
        public ITaskItem Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// The total list of Loose Files in this database
        /// </summary>
        [Output]
        public ITaskItem[] LooseFileList
        {
            get { return this.looseFileList; }
        }

        /// <summary>
        /// Takes the "defaultDir" column
        /// </summary>
        /// <returns>Returns the corresponding sourceDir.</returns>
        public string SourceDirFromDefaultDir(string defaultDir)
        {
            string sourceDir;

            string[] splitted = defaultDir.Split(':');

            if (1 == splitted.Length)
            {
                sourceDir = splitted[0];
            }
            else
            {
                sourceDir = splitted[1];
            }

            splitted = sourceDir.Split('|');

            if (1 == splitted.Length)
            {
                sourceDir = splitted[0];
            }
            else
            {
                sourceDir = splitted[1];
            }

            return sourceDir;
        }

        /// <summary>
        /// Takes the "FileName" column
        /// </summary>
        /// <returns>Returns the corresponding source file name.</returns>
        public string SourceFileFromFileName(string fileName)
        {
            string sourceFile;

            string[] splitted = fileName.Split('|');

            if (1 == splitted.Length)
            {
                sourceFile = splitted[0];
            }
            else
            {
                sourceFile = splitted[1];
            }

            return sourceFile;
        }

        /// <summary>
        /// Gets a complete list of external Loose Files referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            string databaseFile = this.database.ItemSpec;
            Object []emptyArgs = { };
            System.Collections.Generic.List<ITaskItem> looseFileNames = new System.Collections.Generic.List<ITaskItem>();
            Dictionary<string, string> ComponentFullDirectory = new Dictionary<string, string>();
            Dictionary<string, string> DirectoryIdDefaultDir = new Dictionary<string, string>();
            Dictionary<string, string> DirectoryIdParent = new Dictionary<string, string>();
            Dictionary<string, string> DirectoryIdFullSource = new Dictionary<string, string>();
            int i;
            string databaseDir = Path.GetDirectoryName(databaseFile);

            // If the file doesn't exist, no Loose Files to return, so exit now
            if (!File.Exists(databaseFile))
            {
                return true;
            }

            using (Database database = new Database(databaseFile))
            {
                bool compressed = false;
                if (2 == (database.SummaryInfo.WordCount & 2))
                {
                    compressed = true;
                }

                // If the media table doesn't exist, no Loose Files to return, so exit now
                if (null == database.Tables["File"])
                {
                    return true;
                }

                // Only setup all these helpful indexes if the database is marked as uncompressed. If it's marked as compressed, files are stored at the root,
                // so none of these indexes will be used
                if (!compressed)
                {
                    if (null == database.Tables["Directory"] || null == database.Tables["Component"])
                    {
                        return true;
                    }

                    System.Collections.IList directoryRecords = database.ExecuteQuery("SELECT `Directory`,`Directory_Parent`,`DefaultDir` FROM `Directory`", emptyArgs);

                    // First setup a simple index from DirectoryId to DefaultDir
                    for (i = 0; i < directoryRecords.Count; i += 3)
                    {
                        string directoryId = (string)(directoryRecords[i]);
                        string directoryParent = (string)(directoryRecords[i + 1]);
                        string defaultDir = (string)(directoryRecords[i + 2]);

                        string sourceDir = SourceDirFromDefaultDir(defaultDir);

                        DirectoryIdDefaultDir[directoryId] = sourceDir;
                        DirectoryIdParent[directoryId] = directoryParent;
                    }

                    // Setup an index from directory Id to the full source path
                    for (i = 0; i < directoryRecords.Count; i += 3)
                    {
                        string directoryId = (string)(directoryRecords[i]);
                        string directoryParent = (string)(directoryRecords[i + 1]);
                        string defaultDir = (string)(directoryRecords[i + 2]);

                        string sourceDir = DirectoryIdDefaultDir[directoryId];

                        // The TARGETDIR case
                        if (String.IsNullOrEmpty(directoryParent))
                        {
                            DirectoryIdFullSource[directoryId] = databaseDir;
                        }
                        else
                        {
                            string tempDirectoryParent = directoryParent;

                            while (!String.IsNullOrEmpty(tempDirectoryParent) && !String.IsNullOrEmpty(DirectoryIdParent[tempDirectoryParent]))
                            {
                                sourceDir = Path.Combine(DirectoryIdDefaultDir[tempDirectoryParent], sourceDir);

                                tempDirectoryParent = DirectoryIdParent[tempDirectoryParent];
                            }

                            DirectoryIdFullSource[directoryId] = Path.Combine(databaseDir, sourceDir);
                        }
                    }

                    // Setup an index from component Id to full directory path
                    System.Collections.IList componentRecords = database.ExecuteQuery("SELECT `Component`,`Directory_` FROM `Component`", emptyArgs);

                    for (i = 0; i < componentRecords.Count; i += 2)
                    {
                        string componentId = (string)(componentRecords[i]);
                        string componentDir = (string)(componentRecords[i + 1]);

                        ComponentFullDirectory[componentId] = DirectoryIdFullSource[componentDir];
                    }
                }

                System.Collections.IList fileRecords = database.ExecuteQuery("SELECT `Component_`,`FileName`,`Attributes` FROM `File`", emptyArgs);

                for (i = 0; i < fileRecords.Count; i += 3)
                {
                    string componentId = (string)(fileRecords[i]);
                    string fileName = SourceFileFromFileName((string)(fileRecords[i + 1]));
                    int attributes = (int)(fileRecords[i + 2]);

                    // If the whole database is marked uncompressed, use the directory layout made above
                    if ((!compressed && MsidbFileAttributesCompressed != (attributes & MsidbFileAttributesCompressed)))
                    {
                        looseFileNames.Add(new TaskItem(Path.GetFullPath(Path.Combine(ComponentFullDirectory[componentId], fileName))));
                    }
                    // If the database is marked as compressed, put files at the root
                    else if (compressed && (MsidbFileAttributesNoncompressed == (attributes & MsidbFileAttributesNoncompressed)))
                    {
                        looseFileNames.Add(new TaskItem(Path.GetFullPath(Path.Combine(databaseDir, fileName))));
                    }
                }
            }

            this.looseFileList = looseFileNames.ToArray();

            return true;
        }
    }
}
