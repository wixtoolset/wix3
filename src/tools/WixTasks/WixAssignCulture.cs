//-------------------------------------------------------------------------------------------------
// <copyright file="WixAssignCulture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class WixAssignCulture : Task
    {
        private const string CultureAttributeName = "Culture";
        private const string OutputFolderMetadataName = "OutputFolder";
        private const string InvariantCultureIdentifier = "neutral";
        private const string NullCultureIdentifier = "null";

        /// <summary>
        /// The list of cultures to build.  Cultures are specified in the following form:
        ///     primary culture,first fallback culture, second fallback culture;...
        /// Culture groups are seperated by semi-colons 
        /// Culture precedence within a culture group is evaluated from left to right where fallback cultures are 
        /// separated with commas.
        /// The first (primary) culture in a culture group will be used as the output sub-folder.
        /// </summary>
        public string Cultures { get; set; }

        /// <summary>
        /// The list of files to apply culture information to.
        /// </summary>
        [Required]
        public ITaskItem[] Files
        {
            get;
            set;
        }

        /// <summary>
        /// The files that had culture information applied
        /// </summary>
        [Output]
        public ITaskItem[] CultureGroups
        {
            get;
            private set;
        }

        /// <summary>
        /// Applies culture information to the files specified by the Files property.
        /// This task intentionally does not validate that strings are valid Cultures so that we can support
        /// psuedo-loc.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            // First, process the culture group list the user specified in the cultures property
            List<CultureGroup> cultureGroups = new List<CultureGroup>();

            if (!String.IsNullOrEmpty(this.Cultures))
            {
                // Get rid of extra quotes
                this.Cultures = this.Cultures.Trim('\"');

                foreach (string cultureGroupString in this.Cultures.Split(';'))
                {
                    if (0 == cultureGroupString.Length)
                    {
                        // MSBuild v2.0.50727 cannnot handle "" items
                        // for the invariant culture we require the neutral keyword
                        continue;
                    }
                    CultureGroup cultureGroup = new CultureGroup(cultureGroupString);
                    cultureGroups.Add(cultureGroup);
                }
            }
            else
            {
                // Only process the EmbeddedResource items if cultures was unspecified
                foreach (ITaskItem file in this.Files)
                {
                    // Ignore non-wxls
                    if (!String.Equals(file.GetMetadata("Extension"), ".wxl", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError("Unable to retrieve the culture for EmbeddedResource {0}. The file type is not supported.", file.ItemSpec);
                        return false;
                    }
                    XmlDocument wxlFile = new XmlDocument();

                    try
                    {
                        wxlFile.Load(file.ItemSpec);
                    }
                    catch (FileNotFoundException)
                    {
                        Log.LogError("Unable to retrieve the culture for EmbeddedResource {0}. The file was not found.", file.ItemSpec);
                        return false;
                    }
                    catch (Exception e)
                    {
                        Log.LogError("Unable to retrieve the culture for EmbeddedResource {0}: {1}", file.ItemSpec, e.Message);
                        return false;
                    }

                    // Take the culture value and try using it to create a culture.
                    XmlAttribute cultureAttr = wxlFile.DocumentElement.Attributes[WixAssignCulture.CultureAttributeName];
                    string wxlCulture = null == cultureAttr ? String.Empty : cultureAttr.Value;
                    if (0 == wxlCulture.Length)
                    {
                        // We use a keyword for the invariant culture because MSBuild v2.0.50727 cannnot handle "" items
                        wxlCulture = InvariantCultureIdentifier;
                    }

                    // We found the culture for the WXL, we now need to determine if it maps to a culture group specified
                    // in the Cultures property or if we need to create a new one.
                    Log.LogMessage(MessageImportance.Low, "Culture \"{0}\" from EmbeddedResource {1}.", wxlCulture, file.ItemSpec);

                    bool cultureGroupExists = false;
                    foreach (CultureGroup cultureGroup in cultureGroups)
                    {
                        foreach (string culture in cultureGroup.Cultures)
                        {
                            if (String.Equals(wxlCulture, culture, StringComparison.OrdinalIgnoreCase))
                            {
                                cultureGroupExists = true;
                                break;
                            }
                        }
                    }

                    // The WXL didn't match a culture group we already have so create a new one.
                    if (!cultureGroupExists)
                    {
                        cultureGroups.Add(new CultureGroup(wxlCulture));
                    }
                }
            }

            // If we didn't create any culture groups the culture was unspecificed and no WXLs were included
            // Build an unlocalized target in the output folder
            if (cultureGroups.Count == 0)
            {
                cultureGroups.Add(new CultureGroup());
            }

            List<TaskItem> cultureGroupItems = new List<TaskItem>();

            if (1 == cultureGroups.Count && 0 == this.Files.Length)
            {
                // Maitain old behavior, if only one culturegroup is specified and no WXL, output to the default folder
                TaskItem cultureGroupItem = new TaskItem(cultureGroups[0].ToString());
                cultureGroupItem.SetMetadata(OutputFolderMetadataName, CultureGroup.DefaultFolder);
                cultureGroupItems.Add(cultureGroupItem);
            }
            else
            {
                foreach (CultureGroup cultureGroup in cultureGroups)
                {
                    TaskItem cultureGroupItem = new TaskItem(cultureGroup.ToString());
                    cultureGroupItem.SetMetadata(OutputFolderMetadataName, cultureGroup.OutputFolder);
                    cultureGroupItems.Add(cultureGroupItem);
                    Log.LogMessage("Culture: {0}", cultureGroup.ToString());
                }
            }

            this.CultureGroups = cultureGroupItems.ToArray();
            
            return true;
        }

        private class CultureGroup
        {
            private List<string> cultures = new List<string>();

            /// <summary>
            /// TargetPath already has a '\', do not double it!
            /// </summary>
            public const string DefaultFolder = "";

            /// <summary>
            /// Initialize a null culture group
            /// </summary>
            public CultureGroup()
            {
            }

            public CultureGroup(string cultureGroupString)
            {
                Debug.Assert(!String.IsNullOrEmpty(cultureGroupString));
                foreach (string cultureString in cultureGroupString.Split(','))
                {
                    this.cultures.Add(cultureString);
                }
            }

            public List<string> Cultures { get { return cultures; } }

            public string OutputFolder
            {
                get
                {
                    string result = DefaultFolder;
                    if (this.Cultures.Count > 0 && 
                        !this.Cultures[0].Equals(InvariantCultureIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        result = this.Cultures[0] + "\\";
                    }

                    return result;
                }
            }

            public override string ToString()
            {
                if (this.Cultures.Count > 0)
                {
                    return String.Join(",", this.Cultures.ToArray());
                }
                // We use a keyword for a null culture because MSBuild v2.0.50727 cannnot handle "" items
                // Null is different from neutral.  For neutral we still want to do WXL filtering in Light.
                return NullCultureIdentifier;
            }
        }
    }
}
