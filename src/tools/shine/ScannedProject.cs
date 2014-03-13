//-------------------------------------------------------------------------------------------------
// <copyright file="ScannedProject.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Tools.WindowsInstallerXml
{
    public enum ScannedProjectType
    {
        Unknown,
        Bundle,
        Library,
        Module,
        Package,
    }

    public class ScannedProject
    {
        public ScannedProject(string typeName, string path)
            : this(typeName, path, null, null)
        {
        }

        public ScannedProject(string typeName, string path, IDictionary<string, string> properties, string condition)
        {
            try
            {
                this.Type = (ScannedProjectType)Enum.Parse(typeof(ScannedProjectType), typeName);
            }
            catch (ArgumentException)
            {
                this.Type = ScannedProjectType.Unknown;
            }

            this.Path = path;
            this.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (null != properties)
            {
                foreach (KeyValuePair<string, string> kvp in properties)
                {
                    this.Properties.Add(kvp.Key, kvp.Value);
                }
            }

            if (!String.IsNullOrEmpty(condition))
            {
                this.Condition = condition;
            }

            this.Key = ScannedProject.CalculateKey(this.Path, this.Properties);

            this.SourceFiles = new List<ScannedSourceFile>();
            this.SourceProjects = new List<ScannedProject>();
            this.TargetProjects = new List<ScannedProject>();
        }

        public string Key { get; private set; }

        public string Condition { get; private set; }

        public string Path { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        public ScannedProjectType Type { get; private set; }

        public IList<ScannedSourceFile> SourceFiles { get; private set; }

        public IList<ScannedProject> SourceProjects { get; private set; }

        public IList<ScannedProject> TargetProjects { get; private set; }

        public static string CalculateKey(string path, IDictionary<string, string> properties)
        {
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.Append(path.ToLowerInvariant());

            if (null != properties)
            {
                foreach (KeyValuePair<string, string> kvp in properties)
                {
                    keyBuilder.AppendFormat(";{0}={1}", kvp.Key, kvp.Value);
                }
            }

            return keyBuilder.ToString();
        }
    }
}
