//-------------------------------------------------------------------------------------------------
// <copyright file="GenerateReleaseInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
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
            ////{
            ////    "id" : "v3.6.2517.0",
            ////    "downloadable" : true,
            ////    "date" : "2012/1/17",
            ////    "roots" : [ "~" ],
            ////    "files" :
            ////    [
            ////      { "name" : "wix36.exe", "contentType" : "application/octet-stream", "size" : 100, "show" : true },
            ////      { "name" : "data/wix36.msi", "contentType" : "application/octet-stream", "size" : 110 },
            ////      { "name" : "data/wix36.cab", "contentType" : "application/octet-stream", "size" : 120 },
            ////      { "name" : "wix36-binaries.zip", "contentType" : "application/octet-stream", "size" : 130, "show" : true, "protected" : true }
            ////    ]
            ////}
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendFormat(" \"id\":  \"v{0}\",\r\n", this.Version);
            json.AppendLine(" \"downloadable\":  true,");
            json.AppendFormat(" \"date\":  \"{0}\",\r\n", DateTime.Now.ToString("yyyy-MM-dd"));
            json.AppendLine(" \"roots\": [ \"~\"],");
            json.AppendLine(" \"files\":");
            json.AppendLine(" [");
            json.AppendLine(this.UploadFilesToString());
            json.AppendLine(" ]");
            json.AppendLine("}");

            Directory.CreateDirectory(Path.GetDirectoryName(this.OutputFile.ItemSpec));
            using (StreamWriter stream = File.CreateText(this.OutputFile.ItemSpec))
            {
                stream.Write(json.ToString());
            }

            return true;
        }

        private string UploadFilesToString()
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

            return String.Join(",\r\n", files);
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
