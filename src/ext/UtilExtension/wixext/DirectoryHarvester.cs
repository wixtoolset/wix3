// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.IO;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring for a directory from the file system.
    /// </summary>
    public sealed class DirectoryHarvester : HarvesterExtension
    {
        private FileHarvester fileHarvester;
        private bool keepEmptyDirectories;
        private string rootedDirectoryRef;
        private bool setUniqueIdentifiers;
        private bool suppressRootDirectory;

        private static readonly string ComponentPrefix = "cmp";
        private static readonly string DirectoryPrefix = "dir";
        private static readonly string FilePrefix = "fil";


        /// <summary>
        /// Instantiate a new DirectoryHarvester.
        /// </summary>
        public DirectoryHarvester()
        {
            this.fileHarvester = new FileHarvester();
            this.keepEmptyDirectories = false;
            this.setUniqueIdentifiers = true;
            this.suppressRootDirectory = false;
        }

        /// <summary>
        /// Gets or sets the option to keep empty directories.
        /// </summary>
        /// <value>The option to keep empty directories.</value>
        public bool KeepEmptyDirectories
        {
            get { return this.keepEmptyDirectories; }
            set { this.keepEmptyDirectories = value; }
        }

        /// <summary>
        /// Gets or sets the rooted DirectoryRef Id if the user has supplied it.
        /// </summary>
        /// <value>The DirectoryRef Id to use as the root.</value>
        public string RootedDirectoryRef
        {
            get { return this.rootedDirectoryRef; }
            set { this.rootedDirectoryRef = value; }
        }

        /// <summary>
        /// Gets of sets the option to set unique identifiers.
        /// </summary>
        /// <value>The option to set unique identifiers.</value>
        public bool SetUniqueIdentifiers
        {
            get { return this.setUniqueIdentifiers; }
            set { this.setUniqueIdentifiers = value; }
        }

        /// <summary>
        /// Gets or sets the option to suppress including the root directory as an element.
        /// </summary>
        /// <value>The option to suppress including the root directory as an element.</value>
        public bool SuppressRootDirectory
        {
            get { return this.suppressRootDirectory; }
            set { this.suppressRootDirectory = value; }
        }

        /// <summary>
        /// Harvest a directory.
        /// </summary>
        /// <param name="argument">The path of the directory.</param>
        /// <returns>The harvested directory.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            Wix.Directory directory = this.HarvestDirectory(argument, "SourceDir\\", true);

            Wix.DirectoryRef directoryRef = new Wix.DirectoryRef();
            directoryRef.Id = this.rootedDirectoryRef;

            if (this.suppressRootDirectory)
            {
                foreach (Wix.ISchemaElement element in directory.Children)
                {
                    directoryRef.AddChild(element);
                }
            }
            else
            {
                directoryRef.AddChild(directory);
            }

            Wix.Fragment fragment = new Wix.Fragment();
            fragment.AddChild(directoryRef);

            return new Wix.Fragment[] { fragment };
        }

        /// <summary>
        /// Harvest a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="harvestChildren">The option to harvest child directories and files.</param>
        /// <returns>The harvested directory.</returns>
        public Wix.Directory HarvestDirectory(string path, bool harvestChildren)
        {
            return this.HarvestDirectory(path, "SourceDir\\", harvestChildren);
        }

        /// <summary>
        /// Harvest a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="relativePath">The relative path that will be used when harvesting.</param>
        /// <param name="harvestChildren">The option to harvest child directories and files.</param>
        /// <returns>The harvested directory.</returns>
        public Wix.Directory HarvestDirectory(string path, string relativePath, bool harvestChildren)
        {
            if (null == path)
            {
                throw new ArgumentNullException("path");
            }

            if (File.Exists(path))
            {
                throw new WixException(WixErrors.ExpectedDirectoryGotFile("dir", path));
            }

            if (null == this.rootedDirectoryRef)
            {
                this.rootedDirectoryRef = "TARGETDIR";
            }

            // use absolute paths
            path = Path.GetFullPath(path);

            // Remove any trailing separator to ensure Path.GetFileName() will return the directory name.
            path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            Wix.Directory directory = new Wix.Directory();

            directory.Name = Path.GetFileName(path);
            directory.FileSource = path;

            if (this.setUniqueIdentifiers)
            {
                if (this.suppressRootDirectory)
                {
                    directory.Id = this.Core.GenerateIdentifier(DirectoryPrefix, this.rootedDirectoryRef);
                }
                else
                {
                    directory.Id = this.Core.GenerateIdentifier(DirectoryPrefix, this.rootedDirectoryRef, directory.Name);
                }
            }

            if (harvestChildren)
            {
                try
                {
                    int fileCount = this.HarvestDirectory(path, relativePath, directory);

                    // its an error to not harvest anything with the option to keep empty directories off
                    if (0 == fileCount && !this.keepEmptyDirectories)
                    {
                        throw new WixException(UtilErrors.EmptyDirectory(path));
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    throw new WixException(UtilErrors.DirectoryNotFound(path));
                }
            }

            return directory;
        }

        /// <summary>
        /// Harvest a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="relativePath">The relative path that will be used when harvesting.</param>
        /// <param name="directory">The directory for this path.</param>
        /// <returns>The number of files harvested.</returns>
        private int HarvestDirectory(string path, string relativePath, Wix.Directory directory)
        {
            int fileCount = 0;

            // harvest the child directories
            foreach (string childDirectoryPath in Directory.GetDirectories(path))
            {
                Wix.Directory childDirectory = new Wix.Directory();

                childDirectory.Name = Path.GetFileName(childDirectoryPath);
                childDirectory.FileSource = childDirectoryPath;

                if (this.setUniqueIdentifiers)
                {
                    childDirectory.Id = this.Core.GenerateIdentifier(DirectoryPrefix, directory.Id, childDirectory.Name);
                }

                int childFileCount = this.HarvestDirectory(childDirectoryPath, String.Concat(relativePath, childDirectory.Name, "\\"), childDirectory);

                // keep the directory if it contained any files (or empty directories are being kept)
                if (0 < childFileCount || this.keepEmptyDirectories)
                {
                    directory.AddChild(childDirectory);
                }

                fileCount += childFileCount;
            }

            // harvest the files
            string[] files = Directory.GetFiles(path);
            if (0 < files.Length)
            {
                foreach (string filePath in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(filePath);

                    Wix.Component component = new Wix.Component();

                    Wix.File file = this.fileHarvester.HarvestFile(filePath);
                    file.Source = String.Concat(relativePath, fileName);

                    if (this.setUniqueIdentifiers)
                    {
                        file.Id = this.Core.GenerateIdentifier(FilePrefix, directory.Id, fileName);
                        component.Id = this.Core.GenerateIdentifier(ComponentPrefix, directory.Id, file.Id);
                    }

                    component.AddChild(file);

                    directory.AddChild(component);
                }
            }
            else if (0 == fileCount && this.keepEmptyDirectories)
            {
                Wix.Component component = new Wix.Component();
                component.KeyPath = Wix.YesNoType.yes;

                if (this.setUniqueIdentifiers)
                {
                    component.Id = this.Core.GenerateIdentifier(ComponentPrefix, directory.Id);
                }

                Wix.CreateFolder createFolder = new Wix.CreateFolder();
                component.AddChild(createFolder);

                directory.AddChild(component);
            }

            return fileCount + files.Length;
        }
    }
}
