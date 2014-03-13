//-------------------------------------------------------------------------------------------------
// <copyright file="RefreshBundleGeneratedFile.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build task to refresh the generated file for bundle projects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task refreshes the generated file for bundle projects.
    /// </summary>
    public class RefreshBundleGeneratedFile : Task
    {
        private static readonly Regex AddPrefix = new Regex(@"^[^a-zA-Z_]", RegexOptions.Compiled);
        private static readonly Regex IllegalIdentifierCharacters = new Regex(@"[^A-Za-z0-9_\.]|\.{2,}", RegexOptions.Compiled); // non 'words' and assorted valid characters

        private ITaskItem[] generatedFiles;
        private ITaskItem[] projectReferencePaths;

        /// <summary>
        /// The list of files to generate.
        /// </summary>
        [Required]
        public ITaskItem[] GeneratedFiles
        {
            get { return this.generatedFiles; }
            set { this.generatedFiles = value; }
        }

        /// <summary>
        /// All the project references in the project.
        /// </summary>
        [Required]
        public ITaskItem[] ProjectReferencePaths
        {
            get { return this.projectReferencePaths; }
            set { this.projectReferencePaths = value; }
        }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            ArrayList payloadGroupRefs = new ArrayList();
            ArrayList packageGroupRefs = new ArrayList();
            for (int i = 0; i < this.ProjectReferencePaths.Length; i++)
            {
                ITaskItem item = this.ProjectReferencePaths[i];

                if (!String.IsNullOrEmpty(item.GetMetadata(Common.DoNotHarvest)))
                {
                    continue;
                }

                string projectPath = CreateProjectReferenceDefineConstants.GetProjectPath(this.ProjectReferencePaths, i);
                string projectName = Path.GetFileNameWithoutExtension(projectPath);
                string referenceName = Common.GetIdentifierFromName(CreateProjectReferenceDefineConstants.GetReferenceName(item, projectName));

                string[] pogs = item.GetMetadata("RefProjectOutputGroups").Split(';');
                foreach (string pog in pogs)
                {
                    if (!String.IsNullOrEmpty(pog))
                    {
                        // TODO: Add payload group references and package group references once heat is generating them
                        ////payloadGroupRefs.Add(String.Format(CultureInfo.InvariantCulture, "{0}.{1}", referenceName, pog));
                        packageGroupRefs.Add(String.Format(CultureInfo.InvariantCulture, "{0}.{1}", referenceName, pog));
                    }
                }
            }

            XmlDocument doc = new XmlDocument();

            XmlProcessingInstruction head = doc.CreateProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
            doc.AppendChild(head);

            XmlElement rootElement = doc.CreateElement("Wix");
            rootElement.SetAttribute("xmlns", "http://schemas.microsoft.com/wix/2006/wi");
            doc.AppendChild(rootElement);

            XmlElement fragment = doc.CreateElement("Fragment");
            rootElement.AppendChild(fragment);

            XmlElement payloadGroup = doc.CreateElement("PayloadGroup");
            payloadGroup.SetAttribute("Id", "Bundle.Generated.Payloads");
            fragment.AppendChild(payloadGroup);

            XmlElement packageGroup = doc.CreateElement("PackageGroup");
            packageGroup.SetAttribute("Id", "Bundle.Generated.Packages");
            fragment.AppendChild(packageGroup);

            foreach (string payloadGroupRef in payloadGroupRefs)
            {
                XmlElement payloadGroupRefElement = doc.CreateElement("PayloadGroupRef");
                payloadGroupRefElement.SetAttribute("Id", payloadGroupRef);
                payloadGroup.AppendChild(payloadGroupRefElement);
            }

            foreach (string packageGroupRef in packageGroupRefs)
            {
                XmlElement packageGroupRefElement = doc.CreateElement("PackageGroupRef");
                packageGroupRefElement.SetAttribute("Id", packageGroupRef);
                packageGroup.AppendChild(packageGroupRefElement);
            }

            foreach (ITaskItem item in this.GeneratedFiles)
            {
                string fullPath = item.GetMetadata("FullPath");

                payloadGroup.SetAttribute("Id", Path.GetFileNameWithoutExtension(fullPath) + ".Payloads");
                packageGroup.SetAttribute("Id", Path.GetFileNameWithoutExtension(fullPath) + ".Packages");
                try
                {
                    doc.Save(fullPath);
                }
                catch (Exception e)
                {
                    // e.Message will be something like: "Access to the path 'fullPath' is denied."
                    this.Log.LogMessage(MessageImportance.High, "Unable to save generated file to '{0}'. {1}", fullPath, e.Message);
                }
            }

            return true;
        }
    }
}
