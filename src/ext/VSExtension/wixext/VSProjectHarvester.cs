// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    // Instead of directly referencing this .NET 2.0 assembly,
    // use reflection to allow building against .NET 1.1.
    // (Successful runtime use of this class still requires .NET 2.0.)

    ////using Microsoft.Build.BuildEngine;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring for the outputs of a VS project.
    /// </summary>
    public sealed class VSProjectHarvester : HarvesterExtension
    {
        // These format strings are used for generated element identifiers.
        //   {0} = project name
        //   {1} = POG name
        //   {2} = file name
        private static readonly string DirectoryIdFormat = "{0}.{1}";
        private static readonly string ComponentIdFormat = "{0}.{1}.{2}";
        private static readonly string FileIdFormat = "{0}.{1}.{2}";
        private const string VariableFormat = "$(var.{0}.{1})";
        private const string WixVariableFormat = "!(wix.{0}.{1})";

        private static readonly string ComponentPrefix = "cmp";
        private static readonly string DirectoryPrefix = "dir";
        private static readonly string FilePrefix = "fil";

        private string projectGUID;
        private string directoryIds;
        private string directoryRefSeed;
        private string projectName;
        private string configuration;
        private string platform;
        private bool setUniqueIdentifiers;
        private GenerateType generateType;
        private bool generateWixVars;


        private static readonly ProjectOutputGroup[] allOutputGroups = new ProjectOutputGroup[]
        {
            new ProjectOutputGroup("Binaries",   "BuiltProjectOutputGroup",         "TargetDir"),
            new ProjectOutputGroup("Symbols",    "DebugSymbolsProjectOutputGroup",  "TargetDir"),
            new ProjectOutputGroup("Documents",  "DocumentationProjectOutputGroup", "ProjectDir"),
            new ProjectOutputGroup("Satellites", "SatelliteDllsProjectOutputGroup", "TargetDir"),
            new ProjectOutputGroup("Sources",    "SourceFilesProjectOutputGroup",   "ProjectDir"),
            new ProjectOutputGroup("Content",    "ContentFilesProjectOutputGroup",  "ProjectDir"),
        };

        private string[] outputGroups;

        /// <summary>
        /// Instantiate a new VSProjectHarvester.
        /// </summary>
        /// <param name="outputGroups">List of project output groups to harvest.</param>
        public VSProjectHarvester(string[] outputGroups)
        {
            if (outputGroups == null)
            {
                throw new ArgumentNullException("outputGroups");
            }

            this.outputGroups = outputGroups;
        }

        /// <summary>
        /// Gets or sets the configuration to set when harvesting.
        /// </summary>
        /// <value>The configuration to set when harvesting.</value>
        public string Configuration
        {
            get { return this.configuration; }
            set { this.configuration = value; }
        }

        public string DirectoryIds
        {
            get { return this.directoryIds; }
            set { this.directoryIds = value; }
        }

        /// <summary>
        /// Gets or sets what type of elements are to be generated.
        /// </summary>
        /// <value>The type of elements being generated.</value>
        public GenerateType GenerateType
        {
            get { return this.generateType; }
            set { this.generateType = value; }
        }

        /// <summary>
        /// Gets or sets whether or not to use wix variables.
        /// </summary>
        /// <value>Whether or not to use wix variables.</value>
        public bool GenerateWixVars
        {
            get { return this.generateWixVars; }
            set { this.generateWixVars = value; }
        }

        /// <summary>
        /// Gets or sets the platform to set when harvesting.
        /// </summary>
        /// <value>The platform to set when harvesting.</value>
        public string Platform
        {
            get { return this.platform; }
            set { this.platform = value; }
        }

        /// <summary>
        /// Gets or sets the project name to use in wix variables.
        /// </summary>
        /// <value>The project name to use in wix variables.</value>
        public string ProjectName
        {
            get { return this.projectName; }
            set { this.projectName = value; }
        }

        /// <summary>
        /// Gets or sets the option to set unique identifiers.
        /// </summary>
        /// <value>The option to set unique identifiers.</value>
        public bool SetUniqueIdentifiers
        {
            get { return this.setUniqueIdentifiers; }
            set { this.setUniqueIdentifiers = value; }
        }

        /// <summary>
        /// Gets a list of friendly output group names that will be recognized on the command-line.
        /// </summary>
        /// <returns>Array of output group names.</returns>
        public static string[] GetOutputGroupNames()
        {
            string[] names = new string[VSProjectHarvester.allOutputGroups.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = VSProjectHarvester.allOutputGroups[i].Name;
            }
            return names;
        }

        /// <summary>
        /// Harvest a VS project.
        /// </summary>
        /// <param name="argument">The path of the VS project file.</param>
        /// <returns>The harvested directory.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            if (!File.Exists(argument))
            {
                throw new FileNotFoundException(argument);
            }

            // Match specified output group names to available POG structures
            // and collect list of build output groups to pass to MSBuild.
            ProjectOutputGroup[] pogs = new ProjectOutputGroup[this.outputGroups.Length];
            string[] buildOutputGroups = new string[this.outputGroups.Length];
            for (int i = 0; i < this.outputGroups.Length; i++)
            {
                foreach (ProjectOutputGroup pog in VSProjectHarvester.allOutputGroups)
                {
                    if (pog.Name == this.outputGroups[i])
                    {
                        pogs[i] = pog;
                        buildOutputGroups[i] = pog.BuildOutputGroup;
                    }
                }

                if (buildOutputGroups[i] == null)
                {
                    throw new WixException(VSErrors.InvalidOutputGroup(this.outputGroups[i]));
                }
            }

            string projectFile = Path.GetFullPath(argument);

            IDictionary buildOutputs = this.GetProjectBuildOutputs(projectFile, buildOutputGroups);

            ArrayList fragmentList = new ArrayList();

            for (int i = 0; i < pogs.Length; i++)
            {
                this.HarvestProjectOutputGroup(projectFile, buildOutputs, pogs[i], fragmentList);
            }

            return (Wix.Fragment[]) fragmentList.ToArray(typeof(Wix.Fragment));
        }

        /// <summary>
        /// Runs MSBuild on a project file to get the list of filenames for the specified output groups.
        /// </summary>
        /// <param name="projectFile">VS MSBuild project file to load.</param>
        /// <param name="buildOutputGroups">List of MSBuild output group names.</param>
        /// <returns>Dictionary mapping output group names to lists of filenames in the group.</returns>
        private IDictionary GetProjectBuildOutputs(string projectFile, string[] buildOutputGroups)
        {
            MSBuildProject project = GetMsbuildProject(projectFile);

            project.Load(projectFile);

            IDictionary buildOutputs = new Hashtable();

            string originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(projectFile));
            bool buildSuccess = false;
            try
            {
                buildSuccess = project.Build(projectFile, buildOutputGroups, buildOutputs);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }

            if (!buildSuccess)
            {
                throw new WixException(VSErrors.BuildFailed());
            }

            this.projectGUID = project.GetEvaluatedProperty("ProjectGuid");

            if (null == this.projectGUID)
            {
                throw new WixException(VSErrors.BuildFailed());
            }

            IDictionary newDictionary = new Dictionary<object, object>();
            foreach (string buildOutput in buildOutputs.Keys)
            {
                IEnumerable buildOutputFiles = buildOutputs[buildOutput] as IEnumerable;

                bool hasFiles = false;

                foreach (object file in buildOutputFiles)
                {
                    hasFiles = true;
                    break;
                }

                // Try the item group if no outputs
                if (!hasFiles)
                {
                    IEnumerable itemFiles = project.GetEvaluatedItemsByName(String.Concat(buildOutput, "Output"));
                    List<object> itemFileList = new List<object>();

                    // Get each BuildItem and add the file path to our list
                    foreach (object itemFile in itemFiles)
                    {
                        itemFileList.Add(project.GetBuildItem(itemFile));
                    }

                    // Use our list for this build output
                    newDictionary.Add(buildOutput, itemFileList);
                }
                else
                {
                    newDictionary.Add(buildOutput, buildOutputFiles);
                }
            }

            return newDictionary;
        }

        /// <summary>
        /// Creates WiX fragments for files in one output group.
        /// </summary>
        /// <param name="projectFile">VS MSBuild project file.</param>
        /// <param name="buildOutputs">Dictionary of build outputs retrieved from an MSBuild run on the project file.</param>
        /// <param name="pog">Project output group parameters.</param>
        /// <param name="fragmentList">List to which generated fragments will be added.</param>
        /// <returns>Count of harvested files.</returns>
        private int HarvestProjectOutputGroup(string projectFile, IDictionary buildOutputs, ProjectOutputGroup pog, IList fragmentList)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string projectBaseDir = null;

            if (this.ProjectName != null)
            {
                projectName = this.ProjectName;
            }

            string sanitizedProjectName = HarvesterCore.GetIdentifierFromName(projectName);

            Wix.IParentElement harvestParent;

            if (this.GenerateType == GenerateType.Container)
            {
                Wix.Container container = new Wix.Container();
                harvestParent = container;

                container.Name = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
            }
            else if (this.GenerateType == GenerateType.PayloadGroup)
            {
                Wix.PayloadGroup container = new Wix.PayloadGroup();
                harvestParent = container;

                container.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
            }
            else if (this.GenerateType == GenerateType.PackageGroup)
            {
                Wix.PackageGroup container = new Wix.PackageGroup();
                harvestParent = container;

                container.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
            }
            else
            {
                Wix.DirectoryRef directoryRef = new Wix.DirectoryRef();
                harvestParent = directoryRef;

                if (!String.IsNullOrEmpty(this.directoryIds))
                {
                    directoryRef.Id = this.directoryIds;
                }
                else if (this.setUniqueIdentifiers)
                {
                    directoryRef.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
                }
                else
                {
                    directoryRef.Id = HarvesterCore.GetIdentifierFromName(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.DirectoryIdFormat, sanitizedProjectName, pog.Name));
                }

                this.directoryRefSeed = this.Core.GenerateIdentifier(DirectoryPrefix, this.projectGUID, pog.Name);
            }

            IEnumerable pogFiles = buildOutputs[pog.BuildOutputGroup] as IEnumerable;
            if (pogFiles == null)
            {
                throw new WixException(VSErrors.MissingProjectOutputGroup(
                    projectFile, pog.BuildOutputGroup));
            }

            if (pog.FileSource == "ProjectDir")
            {
                projectBaseDir = Path.GetDirectoryName(projectFile) + "\\";
            }

            int harvestCount = this.HarvestProjectOutputGroupFiles(projectBaseDir, projectName, pog.Name, pog.FileSource, pogFiles, harvestParent);

            if (this.GenerateType == GenerateType.Container)
            {
                // harvestParent must be a Container at this point
                Wix.Container container = harvestParent as Wix.Container;

                Wix.Fragment fragment = new Wix.Fragment();
                fragment.AddChild(container);
                fragmentList.Add(fragment);
            }
            else if (this.GenerateType == GenerateType.PackageGroup)
            {
                // harvestParent must be a PackageGroup at this point
                Wix.PackageGroup packageGroup = harvestParent as Wix.PackageGroup;

                Wix.Fragment fragment = new Wix.Fragment();
                fragment.AddChild(packageGroup);
                fragmentList.Add(fragment);
            }
            else if (this.GenerateType == GenerateType.PayloadGroup)
            {
                // harvestParent must be a Container at this point
                Wix.PayloadGroup payloadGroup = harvestParent as Wix.PayloadGroup;

                Wix.Fragment fragment = new Wix.Fragment();
                fragment.AddChild(payloadGroup);
                fragmentList.Add(fragment);
            }
            else
            {
                // harvestParent must be a DirectoryRef at this point
                Wix.DirectoryRef directoryRef = harvestParent as Wix.DirectoryRef;

                if (harvestCount > 0)
                {
                    Wix.Fragment drf = new Wix.Fragment();
                    drf.AddChild(directoryRef);
                    fragmentList.Add(drf);
                }

                Wix.ComponentGroup cg = new Wix.ComponentGroup();

                if (this.setUniqueIdentifiers || !String.IsNullOrEmpty(this.directoryIds))
                {
                    cg.Id = String.Format(CultureInfo.InvariantCulture, DirectoryIdFormat, sanitizedProjectName, pog.Name);
                }
                else
                {
                    cg.Id = directoryRef.Id;
                }

                if (harvestCount > 0)
                {
                    this.AddComponentsToComponentGroup(directoryRef, cg);
                }

                Wix.Fragment cgf = new Wix.Fragment();
                cgf.AddChild(cg);
                fragmentList.Add(cgf);
            }

            return harvestCount;
        }

        /// <summary>
        /// Add all Components in an element tree to a ComponentGroup.
        /// </summary>
        /// <param name="parent">Parent of an element tree that will be searched for Components.</param>
        /// <param name="cg">The ComponentGroup the Components will be added to.</param>
        private void AddComponentsToComponentGroup(Wix.IParentElement parent, Wix.ComponentGroup cg)
        {
            foreach (Wix.ISchemaElement childElement in parent.Children)
            {
                Wix.Component c = childElement as Wix.Component;
                if (c != null)
                {
                    Wix.ComponentRef cr = new Wix.ComponentRef();
                    cr.Id = c.Id;
                    cg.AddChild(cr);
                }
                else
                {
                    Wix.IParentElement p = childElement as Wix.IParentElement;
                    if (p != null)
                    {
                        this.AddComponentsToComponentGroup(p, cg);
                    }
                }
            }
        }

        /// <summary>
        /// Harvest files from one output group of a VS project.
        /// </summary>
        /// <param name="baseDir">The base directory of the files.</param>
        /// <param name="projectName">Name of the project, to be used as a prefix for generated identifiers.</param>
        /// <param name="pogName">Name of the project output group, used for generating identifiers for WiX elements.</param>
        /// <param name="pogFileSource">The ProjectOutputGroup file source.</param>
        /// <param name="outputGroupFiles">The files from one output group to harvest.</param>
        /// <param name="parent">The parent element that will contain the components of the harvested files.</param>
        /// <returns>The number of files harvested.</returns>
        private int HarvestProjectOutputGroupFiles(string baseDir, string projectName, string pogName, string pogFileSource, IEnumerable outputGroupFiles, Wix.IParentElement parent)
        {
            int fileCount = 0;

            Wix.ISchemaElement exeFile = null;
            Wix.ISchemaElement dllFile = null;
            Wix.ISchemaElement appConfigFile = null;

            // Keep track of files inserted
            // Files can have different absolute paths but get mapped to the same SourceFile
            // after the project variables have been used. For example, a WiX project that
            // is building multiple cultures will have many output MSIs/MSMs, but will all get
            // mapped to $(var.ProjName.TargetDir)\ProjName.msm. These duplicates would
            // prevent generated code from compiling.
            Dictionary<string, bool> seenList = new Dictionary<string,bool>();

            foreach (object output in outputGroupFiles)
            {
                string filePath = output.ToString();
                string fileName = Path.GetFileName(filePath);
                string fileDir = Path.GetDirectoryName(filePath);
                string link = null;

                MethodInfo getMetadataMethod = output.GetType().GetMethod("GetMetadata");
                if (getMetadataMethod != null)
                {
                    link = (string)getMetadataMethod.Invoke(output, new object[] { "Link" });
                    if (!String.IsNullOrEmpty(link))
                    {
                        fileDir = Path.GetDirectoryName(Path.Combine(baseDir, link));
                    }
                }

                Wix.IParentElement parentDir = parent;
                // Ignore Containers and PayloadGroups because they do not have a nested structure.
                if (baseDir != null && !String.Equals(Path.GetDirectoryName(baseDir), fileDir, StringComparison.OrdinalIgnoreCase)
                    && this.GenerateType != GenerateType.Container && this.GenerateType != GenerateType.PackageGroup && this.GenerateType != GenerateType.PayloadGroup)
                {
                    Uri baseUri = new Uri(baseDir);
                    Uri relativeUri = baseUri.MakeRelativeUri(new Uri(fileDir));
                    parentDir = this.GetSubDirElement(parentDir, relativeUri);
                }

                string parentDirId = null;

                if (parentDir is Wix.DirectoryRef)
                {
                    parentDirId = this.directoryRefSeed;
                }
                else if (parentDir is Wix.Directory)
                {
                    parentDirId = ((Wix.Directory)parentDir).Id;
                }

                if (this.GenerateType == GenerateType.Container || this.GenerateType == GenerateType.PayloadGroup)
                {
                    Wix.Payload payload = new Wix.Payload();

                    HarvestProjectOutputGroupPayloadFile(baseDir, projectName, pogName, pogFileSource, filePath, fileName, link, parentDir, payload, seenList);
                }
                else if (this.GenerateType == GenerateType.PackageGroup)
                {
                    HarvestProjectOutputGroupPackage(projectName, pogName, pogFileSource, filePath, fileName, link, parentDir, seenList);
                }
                else
                {
                    Wix.Component component = new Wix.Component();
                    Wix.File file = new Wix.File();

                    HarvestProjectOutputGroupFile(baseDir, projectName, pogName, pogFileSource, filePath, fileName, link, parentDir, parentDirId, component, file, seenList);

                    if (String.Equals(Path.GetExtension(file.Source), ".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        exeFile = file;
                    }
                    else if (String.Equals(Path.GetExtension(file.Source), ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        dllFile = file;
                    }
                    else if (file.Source.EndsWith("app.config", StringComparison.OrdinalIgnoreCase))
                    {
                        appConfigFile = file;
                    }
                }

                fileCount++;
            }

            // if there was no exe file found fallback on the dll file found
            if (exeFile == null && dllFile != null)
            {
                exeFile = dllFile;
            }

            // Special case for the app.config file in the Binaries POG...
            // The POG refers to the files in the OBJ directory, while the
            // generated WiX code references them in the bin directory.
            // The app.config file gets renamed to match the exe name.
            if ("Binaries" == pogName && null != exeFile && null != appConfigFile)
            {
                if (appConfigFile is Wix.File)
                {
                    Wix.File appConfigFileAsWixFile = appConfigFile as Wix.File;
                    Wix.File exeFileAsWixFile = exeFile as Wix.File;
                    // Case insensitive replace
                    appConfigFileAsWixFile.Source = Regex.Replace(appConfigFileAsWixFile.Source, @"app\.config", Path.GetFileName(exeFileAsWixFile.Source) + ".config", RegexOptions.IgnoreCase);
                }
            }

            return fileCount;
        }

        private void HarvestProjectOutputGroupFile(string baseDir, string projectName, string pogName, string pogFileSource, string filePath, string fileName, string link, Wix.IParentElement parentDir, string parentDirId, Wix.Component component, Wix.File file, Dictionary<string, bool> seenList)
        {
            string varFormat = VariableFormat;
            if (this.generateWixVars)
            {
                varFormat = WixVariableFormat;
            }

            if (pogName.Equals("Satellites", StringComparison.OrdinalIgnoreCase))
            {
                Wix.Directory locDirectory = new Wix.Directory();

                locDirectory.Name = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                file.Source = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", locDirectory.Name, "\\", Path.GetFileName(filePath));

                if (!seenList.ContainsKey(file.Source))
                {
                    parentDir.AddChild(locDirectory);
                    locDirectory.AddChild(component);
                    component.AddChild(file);
                    seenList.Add(file.Source, true);

                    if (this.setUniqueIdentifiers)
                    {
                        locDirectory.Id = this.Core.GenerateIdentifier(DirectoryPrefix, parentDirId, locDirectory.Name);
                        file.Id = this.Core.GenerateIdentifier(FilePrefix, locDirectory.Id, fileName);
                        component.Id = this.Core.GenerateIdentifier(ComponentPrefix, locDirectory.Id, file.Id);
                    }
                    else
                    {
                        locDirectory.Id = HarvesterCore.GetIdentifierFromName(String.Format(DirectoryIdFormat, (parentDir is Wix.DirectoryRef) ? ((Wix.DirectoryRef)parentDir).Id : parentDirId, locDirectory.Name));
                        file.Id = HarvesterCore.GetIdentifierFromName(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.FileIdFormat, projectName, pogName, String.Concat(locDirectory.Name, ".", fileName)));
                        component.Id = HarvesterCore.GetIdentifierFromName(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.ComponentIdFormat, projectName, pogName, String.Concat(locDirectory.Name, ".", fileName)));
                    }
                }
            }
            else
            {
                file.Source = GenerateSourceFilePath(baseDir, projectName, pogFileSource, filePath, link, varFormat);

                if (!seenList.ContainsKey(file.Source))
                {
                    component.AddChild(file);
                    parentDir.AddChild(component);
                    seenList.Add(file.Source, true);

                    if (this.setUniqueIdentifiers)
                    {
                        file.Id = this.Core.GenerateIdentifier(FilePrefix, parentDirId, fileName);
                        component.Id = this.Core.GenerateIdentifier(ComponentPrefix, parentDirId, file.Id);
                    }
                    else
                    {
                        file.Id = HarvesterCore.GetIdentifierFromName(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.FileIdFormat, projectName, pogName, fileName));
                        component.Id = HarvesterCore.GetIdentifierFromName(String.Format(CultureInfo.InvariantCulture, VSProjectHarvester.ComponentIdFormat, projectName, pogName, fileName));
                    }
                }
            }
        }

        private void HarvestProjectOutputGroupPackage(string projectName, string pogName, string pogFileSource, string filePath, string fileName, string link, Wix.IParentElement parentDir, Dictionary<string, bool> seenList)
        {
            string varFormat = VariableFormat;
            if (this.generateWixVars)
            {
                varFormat = WixVariableFormat;
            }

            if (pogName.Equals("Binaries", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    Wix.ExePackage exePackage = new Wix.ExePackage();
                    exePackage.SourceFile =  String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", Path.GetFileName(filePath));
                    if (!seenList.ContainsKey(exePackage.SourceFile))
                    {
                        parentDir.AddChild(exePackage);
                        seenList.Add(exePackage.SourceFile, true);
                    }
                }
                else if (String.Equals(Path.GetExtension(filePath), ".msi", StringComparison.OrdinalIgnoreCase))
                {
                    Wix.MsiPackage msiPackage = new Wix.MsiPackage();
                    msiPackage.SourceFile = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", Path.GetFileName(filePath));
                    if (!seenList.ContainsKey(msiPackage.SourceFile))
                    {
                        parentDir.AddChild(msiPackage);
                        seenList.Add(msiPackage.SourceFile, true);
                    }
                }
            }
        }

        private void HarvestProjectOutputGroupPayloadFile(string baseDir, string projectName, string pogName, string pogFileSource, string filePath, string fileName, string link, Wix.IParentElement parentDir, Wix.Payload file, Dictionary<string, bool> seenList)
        {
            string varFormat = VariableFormat;
            if (this.generateWixVars)
            {
                varFormat = WixVariableFormat;
            }

            if (pogName.Equals("Satellites", StringComparison.OrdinalIgnoreCase))
            {
                string locDirectoryName = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                file.SourceFile = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", locDirectoryName, "\\", Path.GetFileName(filePath));

                if (!seenList.ContainsKey(file.SourceFile))
                {
                    parentDir.AddChild(file);
                    seenList.Add(file.SourceFile, true);
                }
            }
            else
            {
                file.SourceFile = GenerateSourceFilePath(baseDir, projectName, pogFileSource, filePath, link, varFormat);

                if (!seenList.ContainsKey(file.SourceFile))
                {
                    parentDir.AddChild(file);
                    seenList.Add(file.SourceFile, true);
                }
            }
        }

        /// <summary>
        /// Helper function to generates a source file path when harvesting files.
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="projectName"></param>
        /// <param name="pogFileSource"></param>
        /// <param name="filePath"></param>
        /// <param name="link"></param>
        /// <param name="varFormat"></param>
        /// <returns></returns>
        private static string GenerateSourceFilePath(string baseDir, string projectName, string pogFileSource, string filePath, string link, string varFormat)
        {
            string ret;

            if (null == baseDir && !String.IsNullOrEmpty(link))
            {
                // This needs to be the absolute path as a link can be located anywhere.
                ret = filePath;
            }
            else if (null == baseDir)
            {
                ret = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", Path.GetFileName(filePath));
            }
            else if (filePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                ret = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", filePath.Substring(baseDir.Length));
            }
            else
            {
                // come up with a relative path to the file
                Uri sourcePathUri = new Uri(filePath);
                Uri baseDirUri = new Uri(baseDir);
                Uri sourceRelativeUri = baseDirUri.MakeRelativeUri(sourcePathUri);
                string relativePath = sourceRelativeUri.ToString().Replace('/', Path.DirectorySeparatorChar);
                if (!sourceRelativeUri.UserEscaped)
                {
                    relativePath = Uri.UnescapeDataString(relativePath);
                }

                ret = String.Concat(String.Format(CultureInfo.InvariantCulture, varFormat, projectName, pogFileSource), "\\", relativePath);
            }

            return ret;
        }

        /// <summary>
        /// Gets a Directory element corresponding to a relative subdirectory within the project,
        /// either by locating a suitable existing Directory or creating a new one.
        /// </summary>
        /// <param name="parentDir">The parent element which the subdirectory is relative to.</param>
        /// <param name="relativeUri">Relative path of the subdirectory.</param>
        /// <returns>Directory element for the relative path.</returns>
        private Wix.IParentElement GetSubDirElement(Wix.IParentElement parentDir, Uri relativeUri)
        {
            string[] segments = relativeUri.ToString().Split('\\', '/');
            string firstSubDirName = Uri.UnescapeDataString(segments[0]);
            DirectoryAttributeAccessor subDir = null;

            if (String.Equals(firstSubDirName, "..", StringComparison.Ordinal))
            {
                return parentDir;
            }

            Type directoryType;
            Type directoryRefType;
            if (parentDir is Wix.Directory || parentDir is Wix.DirectoryRef)
            {
                directoryType = typeof(Wix.Directory);
                directoryRefType = typeof(Wix.DirectoryRef);
            }
            else
            {
                throw new ArgumentException("GetSubDirElement parentDir");
            }

            // Search for an existing directory element.
            foreach (Wix.ISchemaElement childElement in parentDir.Children)
            {
                if(VSProjectHarvester.AreTypesEquivalent(childElement.GetType(), directoryType))
                {
                    DirectoryAttributeAccessor childDir = new DirectoryAttributeAccessor(childElement);
                    if (String.Equals(childDir.Name, firstSubDirName, StringComparison.OrdinalIgnoreCase))
                    {
                        subDir = childDir;
                        break;
                    }
                }
            }

            if (subDir == null)
            {
                string parentId = null;
                DirectoryAttributeAccessor parentDirectory = null;
                DirectoryAttributeAccessor parentDirectoryRef = null;

                if (VSProjectHarvester.AreTypesEquivalent(parentDir.GetType(), directoryType))
                {
                    parentDirectory = new DirectoryAttributeAccessor((Wix.ISchemaElement)parentDir);
                }
                else if (VSProjectHarvester.AreTypesEquivalent(parentDir.GetType(), directoryRefType))
                {
                    parentDirectoryRef = new DirectoryAttributeAccessor((Wix.ISchemaElement)parentDir);
                }

                if (parentDirectory != null)
                {
                    parentId = parentDirectory.Id;
                }
                else if (parentDirectoryRef != null)
                {
                    if (this.setUniqueIdentifiers)
                    {
                        //Use the GUID of the project instead of the project name to help keep things stable.
                        parentId = this.directoryRefSeed;
                    }
                    else
                    {
                        parentId = parentDirectoryRef.Id;
                    }
                }

                Wix.ISchemaElement newDirectory = (Wix.ISchemaElement)directoryType.GetConstructor(new Type[] { }).Invoke(null);
                subDir = new DirectoryAttributeAccessor(newDirectory);

                if (this.setUniqueIdentifiers)
                {
                    subDir.Id = this.Core.GenerateIdentifier(DirectoryPrefix, parentId, firstSubDirName);
                }
                else
                {
                    subDir.Id = String.Format(DirectoryIdFormat, parentId, firstSubDirName);
                }

                subDir.Name = firstSubDirName;

                parentDir.AddChild(subDir.Element);
            }

            if (segments.Length == 1)
            {
                return subDir.ElementAsParent;
            }
            else
            {
                Uri nextRelativeUri = new Uri(Uri.UnescapeDataString(relativeUri.ToString()).Substring(firstSubDirName.Length + 1), UriKind.Relative);
                return GetSubDirElement(subDir.ElementAsParent, nextRelativeUri);
            }
        }

        private MSBuildProject GetMsbuildProject(string projectFile)
        {
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(projectFile);
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadProject(projectFile, e.Message));
            }

            string version = "2.0";

            foreach (XmlNode child in document.ChildNodes)
            {
                if (String.Equals(child.Name, "Project", StringComparison.Ordinal) && child.Attributes != null)
                {
                    XmlNode toolsVersionAttribute = child.Attributes["ToolsVersion"];
                    if (toolsVersionAttribute != null)
                    {
                        version = toolsVersionAttribute.Value;
                        this.Core.OnMessage(VSVerboses.FoundToolsVersion(version));
                    }
                }
            }

            this.Core.OnMessage(VSVerboses.LoadingProject(version));

            MSBuildProject project;
            switch (version)
            {
                case "2.0":
                    project = ConstructMsbuild35Project(projectFile, this.Core, this.configuration, this.platform, "2.0.0.0");
                    break;
                case "4.0":
                    project = ConstructMsbuild40Project(projectFile, this.Core, this.configuration, this.platform);
                    break;
                case "12.0":
                    project = ConstructMsbuildWrapperProject(projectFile, this.Core, this.configuration, this.platform, "12");
                    break;
                case "14.0":
                    project = ConstructMsbuildWrapperProject(projectFile, this.Core, this.configuration, this.platform, "14");
                    break;
                default:
                    project = ConstructMsbuild35Project(projectFile, this.Core, this.configuration, this.platform);
                    break;
            }

            return project;
        }

        private static MSBuildProject ConstructMsbuild35Project(string projectFile, HarvesterCore harvesterCore, string configuration, string platform)
        {
            return ConstructMsbuild35Project(projectFile, harvesterCore, configuration, platform, null);
        }

        private static MSBuildProject ConstructMsbuild35Project(string projectFile, HarvesterCore harvesterCore, string configuration, string platform, string loadVersion)
        {
            const string MSBuildEngineAssemblyName = "Microsoft.Build.Engine, Version={0}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            Assembly msbuildAssembly = null;

            loadVersion = loadVersion ?? "3.5.0.0";

            try
            {
                try
                {
                    msbuildAssembly = Assembly.Load(String.Format(MSBuildEngineAssemblyName, loadVersion));
                }
                catch (FileNotFoundException)
                {
                    loadVersion = "2.0.0.0";
                    msbuildAssembly = Assembly.Load(String.Format(MSBuildEngineAssemblyName, loadVersion));
                }
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildAssembly(e.Message));
            }

            Type engineType;
            Type projectType;
            Type buildItemType;
            object engine;
            object project;

            try
            {
                engineType = msbuildAssembly.GetType("Microsoft.Build.BuildEngine.Engine", true);
                if (msbuildAssembly.GetName().Version.Major >= 3)
                {
                    // MSBuild v3.5 uses this constructor which automatically sets the tool path.
                    engine = engineType.GetConstructor(new Type[] { }).Invoke(null);
                }
                else
                {
                    //MSBuild v2.0 uses this constructor which requires specifying the MSBuild bin path.
                    string msbuildBinPath = RuntimeEnvironment.GetRuntimeDirectory();
                    engine = engineType.GetConstructor(new Type[] { typeof(string) }).Invoke(new object[] { msbuildBinPath });

                    try
                    {
                        HarvestLogger logger = new HarvestLogger();
                        engineType.GetMethod("RegisterLogger").Invoke(engine, new object[] { logger });
                    }
                    catch (TargetInvocationException tie)
                    {
                        if (harvesterCore != null)
                        {
                            harvesterCore.OnMessage(VSWarnings.NoLogger(tie.Message));
                        }
                    }
                    catch (Exception e)
                    {
                        if (harvesterCore != null)
                        {
                            harvesterCore.OnMessage(VSWarnings.NoLogger(e.Message));
                        }
                    }
                }

                buildItemType = msbuildAssembly.GetType("Microsoft.Build.BuildEngine.BuildItem", true);

                projectType = msbuildAssembly.GetType("Microsoft.Build.BuildEngine.Project", true);
                project = projectType.GetConstructor(new Type[] { engineType }).Invoke(new object[] { engine });
            }
            catch (TargetInvocationException tie)
            {
                // An assembly redirect (i.e. VS 2005) can cause a TypeLoadException at this point
                // Try again using MSBuild 2.0.0.0 at that point.
                if (String.Equals(tie.InnerException.GetType().Name, "TypeLoadException", StringComparison.Ordinal) &&
                    !String.Equals(loadVersion, "2.0.0.0", StringComparison.Ordinal))
                {
                    return ConstructMsbuild35Project(projectFile, harvesterCore, configuration, platform, "2.0.0.0");
                }
                throw new WixException(VSErrors.CannotLoadMSBuildEngine(tie.InnerException.Message));
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildEngine(e.Message));
            }

            if (configuration != null || platform != null)
            {
                try
                {
                    object globalProperties = projectType.GetProperty("GlobalProperties").GetValue(project, null);
                    MethodInfo setPropertyMethod = globalProperties.GetType().GetMethod("SetProperty", new Type[] { typeof(string), typeof(string) });

                    if (configuration != null)
                    {
                        setPropertyMethod.Invoke(globalProperties, new object[] { "Configuration", configuration });
                    }

                    if (platform != null)
                    {
                        setPropertyMethod.Invoke(globalProperties, new object[] { "Platform", platform });
                    }
                }
                catch (TargetInvocationException tie)
                {
                    harvesterCore.OnMessage(VSWarnings.NoProjectConfiguration(tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    harvesterCore.OnMessage(VSWarnings.NoProjectConfiguration(e.Message));
                }
            }

            return new MSBuild35Project(project, projectType, buildItemType, loadVersion);
        }

        private static MSBuildProject ConstructMsbuild40Project(string projectFile, HarvesterCore harvesterCore, string configuration, string platform)
        {
            return ConstructMsbuild40Project(projectFile, harvesterCore, configuration, platform, null);
        }

        private static MSBuildProject ConstructMsbuild40Project(string projectFile, HarvesterCore harvesterCore, string configuration, string platform, string loadVersion)
        {
            const string MSBuildEngineAssemblyName = "Microsoft.Build, Version={0}, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            Assembly msbuildAssembly = null;

            loadVersion = loadVersion ?? "4.0.0.0";

            try
            {
                try
                {
                    msbuildAssembly = Assembly.Load(String.Format(MSBuildEngineAssemblyName, loadVersion));
                }
                catch (FileNotFoundException)
                {
                    loadVersion = "4.0.0.0";
                    msbuildAssembly = Assembly.Load(String.Format(MSBuildEngineAssemblyName, loadVersion));
                }
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildAssembly(e.Message));
            }

            Type projectType;
            Type buildItemType;

            Type buildManagerType;
            Type buildParametersType;
            Type buildRequestDataFlagsType;
            Type buildRequestDataType;
            Type hostServicesType;
            Type projectCollectionType;
            Type projectInstanceType;

            try
            {
                buildItemType = msbuildAssembly.GetType("Microsoft.Build.Execution.ProjectItemInstance", true);
                projectType = msbuildAssembly.GetType("Microsoft.Build.Evaluation.Project", true);

                buildManagerType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildManager", true);
                buildParametersType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildParameters", true);
                buildRequestDataFlagsType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildRequestDataFlags", true);
                buildRequestDataType = msbuildAssembly.GetType("Microsoft.Build.Execution.BuildRequestData", true);
                hostServicesType = msbuildAssembly.GetType("Microsoft.Build.Execution.HostServices", true);
                projectCollectionType = msbuildAssembly.GetType("Microsoft.Build.Evaluation.ProjectCollection", true);
                projectInstanceType = msbuildAssembly.GetType("Microsoft.Build.Execution.ProjectInstance", true);
            }
            catch (TargetInvocationException tie)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildEngine(tie.InnerException.Message));
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildEngine(e.Message));
            }

            MSBuild40Types types = new MSBuild40Types();
            types.buildManagerType = buildManagerType;
            types.buildParametersType = buildParametersType;
            types.buildRequestDataFlagsType = buildRequestDataFlagsType;
            types.buildRequestDataType = buildRequestDataType;
            types.hostServicesType = hostServicesType;
            types.projectCollectionType = projectCollectionType;
            types.projectInstanceType = projectInstanceType;
            return new MSBuild40Project(null, projectType, buildItemType, loadVersion, types, harvesterCore, configuration, platform);
        }

        private static MSBuildProject ConstructMsbuildWrapperProject(string projectFile, HarvesterCore harvesterCore, string configuration, string platform, string shortVersion)
        {
            // Until MSBuild 12.0, we were able to compile the HarvestLogger class which derives from ILogger and use that for all versions of MSBuild.
            // Starting in MSBuild 12.0, the ILogger that we compile against doesn't match the ILogger during runtime.  This DLL targets .NET 3.5,
            // so we can't reference the newer MSBuild assemblies.  This requires building new assemblies.  We reflect into these instead of MSBuild.

            const string MSBuildWrapperAssemblyName = "WixVSExtension.MSBuild{0}, Version={1}, Culture=neutral, PublicKeyToken={2}";
            Assembly msbuildWrapperAssembly = null;

            // Load the custom assembly for the requested version of MSBuild.
            try
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                AssemblyName thisAssemblyName = thisAssembly.GetName();
                StringBuilder publicKeyToken = new StringBuilder();
                foreach (byte b in thisAssemblyName.GetPublicKeyToken())
                {
                    publicKeyToken.Append(b.ToString("x2"));
                }

                msbuildWrapperAssembly = Assembly.Load(String.Format(MSBuildWrapperAssemblyName, shortVersion, thisAssemblyName.Version, publicKeyToken));
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildWrapperAssembly(e.Message));
            }

            const string MSBuildWrapperTypeName = "Microsoft.Tools.WindowsInstallerXml.Extensions.WixVSExtension.MSBuild{0}Project";
            Type projectWrapperType = null;

            // Get the type of the class that inherits from MSBuildProject.
            try
            {
                projectWrapperType = msbuildWrapperAssembly.GetType(String.Format(MSBuildWrapperTypeName, shortVersion), true);
            }
            catch (TargetInvocationException tie)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildWrapperType(tie.InnerException.Message));
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildWrapperType(e.Message));
            }

            try
            {
                // Get the constructor of the class so we can "new it up".
                ConstructorInfo wrapperCtor = projectWrapperType.GetConstructor(
                    new Type[]
                    {
                        typeof(HarvesterCore),
                        typeof(string),
                        typeof(string),
                    });
                return (MSBuildProject)wrapperCtor.Invoke(new object[] { harvesterCore, configuration, platform });
            }
            catch (TargetInvocationException tie)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildWrapperObject(tie.InnerException.Message));
            }
            catch (Exception e)
            {
                throw new WixException(VSErrors.CannotLoadMSBuildWrapperObject(e.Message));
            }
        }

        private static bool AreTypesEquivalent(Type a, Type b)
        {
            return (a == b) || (a.IsAssignableFrom(b) && b.IsAssignableFrom(a));
        }

        public abstract class MSBuildProject
        {
            protected Type projectType;
            protected Type buildItemType;
            protected object project;
            private string loadVersion;

            public MSBuildProject(object project, Type projectType, Type buildItemType, string loadVersion)
            {
                this.project = project;
                this.projectType = projectType;
                this.buildItemType = buildItemType;
                this.loadVersion = loadVersion;
            }

            public string LoadVersion
            {
                get { return this.loadVersion; }
            }

            public abstract bool Build(string projectFileName, string[] targetNames, IDictionary targetOutputs);

            public abstract MSBuildProjectItemType GetBuildItem(object buildItem);

            public abstract IEnumerable GetEvaluatedItemsByName(string itemName);

            public abstract string GetEvaluatedProperty(string propertyName);

            public abstract void Load(string projectFileName);
        }

        public abstract class MSBuildProjectItemType
        {
            public MSBuildProjectItemType(object buildItem)
            {
                this.buildItem = buildItem;
            }

            public abstract override string ToString();

            public abstract string GetMetadata(string name);

            protected object buildItem;
        }

        private class MSBuild35Project : MSBuildProject
        {
            public MSBuild35Project(object project, Type projectType, Type buildItemType, string loadVersion)
                : base(project, projectType, buildItemType, loadVersion)
            {
            }

            public override bool Build(string projectFileName, string[] targetNames, IDictionary targetOutputs)
            {
                try
                {
                    MethodInfo buildMethod = projectType.GetMethod("Build", new Type[] { typeof(string[]), typeof(IDictionary) });
                    return (bool)buildMethod.Invoke(project, new object[] { targetNames, targetOutputs });
                }
                catch (TargetInvocationException tie)
                {
                    throw new WixException(VSErrors.CannotBuildProject(projectFileName, tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    throw new WixException(VSErrors.CannotBuildProject(projectFileName, e.Message));
                }
            }

            public override MSBuildProjectItemType GetBuildItem(object buildItem)
            {
                return new MSBuild35ProjectItemType(buildItem);
            }

            public override IEnumerable GetEvaluatedItemsByName(string itemName)
            {
                MethodInfo getEvaluatedItem = projectType.GetMethod("GetEvaluatedItemsByName", new Type[] { typeof(string) });
                return (IEnumerable)getEvaluatedItem.Invoke(project, new object[] { itemName });
            }

            public override string GetEvaluatedProperty(string propertyName)
            {
                MethodInfo getProperty = projectType.GetMethod("GetEvaluatedProperty", new Type[] { typeof(string) });
                return (string)getProperty.Invoke(project, new object[] { propertyName });
            }

            public override void Load(string projectFileName)
            {
                try
                {
                    projectType.GetMethod("Load", new Type[] { typeof(string) }).Invoke(project, new object[] { projectFileName });
                }
                catch (TargetInvocationException tie)
                {
                    throw new WixException(VSErrors.CannotLoadProject(projectFileName, tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    throw new WixException(VSErrors.CannotLoadProject(projectFileName, e.Message));
                }
            }
        }

        private class MSBuild35ProjectItemType : MSBuildProjectItemType
        {
            public MSBuild35ProjectItemType(object buildItem)
                : base(buildItem)
            {
            }

            public override string ToString()
            {
                PropertyInfo includeProperty = this.buildItem.GetType().GetProperty("FinalItemSpec");
                return (string)includeProperty.GetValue(this.buildItem, null);
            }

            public override string GetMetadata(string name)
            {
                MethodInfo getMetadataMethod = this.buildItem.GetType().GetMethod("GetMetadata");
                if (null != getMetadataMethod)
                {
                    return (string)getMetadataMethod.Invoke(this.buildItem, new object[] { name });
                }
                return string.Empty;
            }
        }

        private struct MSBuild40Types
        {
            public Type buildManagerType;
            public Type buildParametersType;
            public Type buildRequestDataFlagsType;
            public Type buildRequestDataType;
            public Type hostServicesType;
            public Type projectCollectionType;
            public Type projectInstanceType;
        }

        private class MSBuild40Project : MSBuildProject
        {
            private MSBuild40Types types;
            private object projectCollection;
            private object currentProjectInstance;
            private object buildManager;
            private object buildParameters;
            private HarvesterCore harvesterCore;

            public MSBuild40Project(object project, Type projectType, Type buildItemType, string loadVersion, MSBuild40Types types, HarvesterCore harvesterCore, string configuration, string platform)
                : base(project, projectType, buildItemType, loadVersion)
            {
                this.types = types;
                this.harvesterCore = harvesterCore;

                this.buildParameters = this.types.buildParametersType.GetConstructor(new Type[] { }).Invoke(null);

                try
                {
                    HarvestLogger logger = new HarvestLogger();
                    logger.HarvesterCore = harvesterCore;
                    List<ILogger> loggers = new List<ILogger>();
                    loggers.Add(logger);

                    // this.buildParameters.Loggers = loggers;
                    this.types.buildParametersType.GetProperty("Loggers").SetValue(this.buildParameters, loggers, null);

                    // MSBuild can't handle storing operating enviornments for nested builds.
                    if (Util.RunningInMsBuild)
                    {
                        this.types.buildParametersType.GetProperty("SaveOperatingEnvironment").SetValue(this.buildParameters, false, null);
                    }
                }
                catch (TargetInvocationException tie)
                {
                    if (this.harvesterCore != null)
                    {
                        this.harvesterCore.OnMessage(VSWarnings.NoLogger(tie.InnerException.Message));
                    }
                }
                catch (Exception e)
                {
                    if (this.harvesterCore != null)
                    {
                        this.harvesterCore.OnMessage(VSWarnings.NoLogger(e.Message));
                    }
                }

                this.buildManager = this.types.buildManagerType.GetConstructor(new Type[] { }).Invoke(null);

                if (configuration != null || platform != null)
                {
                    Dictionary<string, string> globalVariables = new Dictionary<string, string>();
                    if (configuration != null)
                    {
                        globalVariables.Add("Configuration", configuration);
                    }

                    if (platform != null)
                    {
                        globalVariables.Add("Platform", platform);
                    }

                    this.projectCollection = this.types.projectCollectionType.GetConstructor(new Type[] { typeof(IDictionary<string, string>) }).Invoke(new object[] { globalVariables });
                }
                else
                {
                    this.projectCollection = this.types.projectCollectionType.GetConstructor(new Type[] {}).Invoke(null);
                }
            }

            public override bool Build(string projectFileName, string[] targetNames, IDictionary targetOutputs)
            {
                try
                {
                    // this.buildManager.BeginBuild(this.buildParameters);
                    this.types.buildManagerType.GetMethod("BeginBuild", new Type[] { this.types.buildParametersType }).Invoke(this.buildManager, new object[] { this.buildParameters });

                    // buildRequestData = new BuildRequestData(this.currentProjectInstance, targetNames, null, BuildRequestData.BuildRequestDataFlags.ReplaceExistingProjectInstance);
                    ConstructorInfo buildRequestDataCtor = this.types.buildRequestDataType.GetConstructor(
                        new Type[]
                        {
                            this.types.projectInstanceType, typeof(string[]), this.types.hostServicesType, this.types.buildRequestDataFlagsType
                        });
                    object buildRequestDataFlags = this.types.buildRequestDataFlagsType.GetField("ReplaceExistingProjectInstance").GetRawConstantValue();
                    object buildRequestData = buildRequestDataCtor.Invoke(new object[] { this.currentProjectInstance, targetNames, null, buildRequestDataFlags });

                    // BuildSubmission submission  = this.buildManager.PendBuildRequest(buildRequestData);
                    object submission = this.types.buildManagerType.GetMethod("PendBuildRequest", new Type[] { this.types.buildRequestDataType })
                        .Invoke(this.buildManager, new object[] { buildRequestData });

                    // BuildResult buildResult = submission.Execute();
                    object buildResult = submission.GetType().GetMethod("Execute", new Type[] { }).Invoke(submission, null);

                    // bool buildSucceeded = buildResult.OverallResult == BuildResult.Success;
                    object overallResult = buildResult.GetType().GetProperty("OverallResult").GetValue(buildResult, null);
                    bool buildSucceeded = String.Equals(overallResult.ToString(), "Success", StringComparison.Ordinal);

                    // this.buildManager.EndBuild();
                    this.types.buildManagerType.GetMethod("EndBuild", new Type[] { }).Invoke(this.buildManager, null);

                    // fill in empty lists for each target so that heat will look at the item group later
                    foreach (string target in targetNames)
                    {
                        targetOutputs.Add(target, new List<object>());
                    }

                    return buildSucceeded;
                }
                catch (TargetInvocationException tie)
                {
                    throw new WixException(VSErrors.CannotBuildProject(projectFileName, tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    throw new WixException(VSErrors.CannotBuildProject(projectFileName, e.Message));
                }
            }

            public override MSBuildProjectItemType GetBuildItem(object buildItem)
            {
                return new MSBuild40ProjectItemType(buildItem);
            }

            public override IEnumerable GetEvaluatedItemsByName(string itemName)
            {
                MethodInfo getEvaluatedItem = this.types.projectInstanceType.GetMethod("GetItems", new Type[] { typeof(string) });
                return (IEnumerable)getEvaluatedItem.Invoke(this.currentProjectInstance, new object[] { itemName });
            }

            public override string GetEvaluatedProperty(string propertyName)
            {
                MethodInfo getProperty = this.types.projectInstanceType.GetMethod("GetPropertyValue", new Type[] { typeof(string) });
                return (string)getProperty.Invoke(this.currentProjectInstance, new object[] { propertyName });
            }

            public override void Load(string projectFileName)
            {
                try
                {
                    //this.project = this.projectCollection.LoadProject(projectFileName);
                    this.project = this.types.projectCollectionType.GetMethod("LoadProject", new Type[] { typeof(string) }).Invoke(this.projectCollection, new object[] { projectFileName });

                    // this.currentProjectInstance = this.project.CreateProjectInstance();
                    MethodInfo createProjectInstanceMethod = projectType.GetMethod("CreateProjectInstance", new Type[] { });
                    this.currentProjectInstance = createProjectInstanceMethod.Invoke(this.project, null);
                }
                catch (TargetInvocationException tie)
                {
                    throw new WixException(VSErrors.CannotLoadProject(projectFileName, tie.InnerException.Message));
                }
                catch (Exception e)
                {
                    throw new WixException(VSErrors.CannotLoadProject(projectFileName, e.Message));
                }
            }
        }

        private class MSBuild40ProjectItemType : MSBuildProjectItemType
        {
            public MSBuild40ProjectItemType(object buildItem)
                : base(buildItem)
            {
            }

            public override string ToString()
            {
                PropertyInfo includeProperty = this.buildItem.GetType().GetProperty("EvaluatedInclude");
                return (string)includeProperty.GetValue(this.buildItem, null);
            }

            public override string GetMetadata(string name)
            {
                MethodInfo getMetadataMethod = this.buildItem.GetType().GetMethod("GetMetadataValue");
                if (null != getMetadataMethod)
                {
                    return (string)getMetadataMethod.Invoke(this.buildItem, new object[] { name });
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Used internally in the VSProjectHarvester class to encapsulate
        /// the settings for a particular MSBuild "project output group".
        /// </summary>
        private struct ProjectOutputGroup
        {
            public readonly string Name;
            public readonly string BuildOutputGroup;
            public readonly string FileSource;

            /// <summary>
            /// Creates a new project output group.
            /// </summary>
            /// <param name="name">Friendly name used by heat.</param>
            /// <param name="buildOutputGroup">MSBuild's name of the project output group.</param>
            /// <param name="fileSource">VS directory token containing the files of the POG.</param>
            public ProjectOutputGroup(string name, string buildOutputGroup, string fileSource)
            {
                this.Name = name;
                this.BuildOutputGroup = buildOutputGroup;
                this.FileSource = fileSource;
            }
        }

        /// <summary>
        /// Internal class for getting and setting common attrbiutes on
        /// directory elements.
        /// </summary>
        internal class DirectoryAttributeAccessor
        {
            public Wix.ISchemaElement directoryElement;

            public DirectoryAttributeAccessor(Wix.ISchemaElement directoryElement)
            {
                this.directoryElement = directoryElement;
            }

            /// <summary>
            /// Gets the element as a ISchemaElement.
            /// </summary>
            public Wix.ISchemaElement Element
            {
                get { return this.directoryElement; }
            }

            /// <summary>
            /// Gets the element as a IParentElement.
            /// </summary>
            public Wix.IParentElement ElementAsParent
            {
                get { return (Wix.IParentElement)this.directoryElement; }
            }

            /// <summary>
            /// Gets or sets the Id attrbiute.
            /// </summary>
            public string Id
            {
                get
                {
                    if (directoryElement is Wix.Directory)
                    {
                        return ((Wix.Directory)directoryElement).Id;
                    }
                    else if (directoryElement is Wix.DirectoryRef)
                    {
                        return ((Wix.DirectoryRef)directoryElement).Id;
                    }
                    else
                    {
                        throw new WixException(VSErrors.DirectoryAttributeAccessorBadType("Id"));
                    }
                }
                set
                {
                    if (directoryElement is Wix.Directory)
                    {
                        ((Wix.Directory)directoryElement).Id = value;
                    }
                    else if (directoryElement is Wix.DirectoryRef)
                    {
                        ((Wix.DirectoryRef)directoryElement).Id = value;
                    }
                    else
                    {
                        throw new WixException(VSErrors.DirectoryAttributeAccessorBadType("Id"));
                    }
                }
            }

            /// <summary>
            /// Gets or sets the Name attribute.
            /// </summary>
            public string Name
            {
                get
                {
                    if (directoryElement is Wix.Directory)
                    {
                        return ((Wix.Directory)directoryElement).Name;
                    }
                    else
                    {
                        throw new WixException(VSErrors.DirectoryAttributeAccessorBadType("Name"));
                    }
                }
                set
                {
                    if (directoryElement is Wix.Directory)
                    {
                        ((Wix.Directory)directoryElement).Name = value;
                    }
                    else
                    {
                        throw new WixException(VSErrors.DirectoryAttributeAccessorBadType("Name"));
                    }
                }
            }
        }

        // This logger will derive from the Microsoft.Build.Utilities.Logger class,
        // which provides it with getters and setters for Verbosity and Parameters,
        // and a default empty Shutdown() implementation.
        internal class HarvestLogger : Logger
        {
            private HarvesterCore harvesterCore;

            public HarvesterCore HarvesterCore
            {
                get { return this.harvesterCore; }
                set { this.harvesterCore = value; }
            }

            /// <summary>
            /// Initialize is guaranteed to be called by MSBuild at the start of the build
            /// before any events are raised.
            /// </summary>
            public override void Initialize(IEventSource eventSource)
            {
                eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
            }

            void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
            {
                if (this.harvesterCore != null)
                {
                    // BuildErrorEventArgs adds LineNumber, ColumnNumber, File, amongst other parameters
                    string line = String.Format(CultureInfo.InvariantCulture, "{0}({1},{2}): {3}", e.File, e.LineNumber, e.ColumnNumber, e.Message);
                    this.harvesterCore.OnMessage(VSErrors.BuildErrorDuringHarvesting(line));
                }
            }
        }
    }
}
