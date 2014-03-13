//-------------------------------------------------------------------------------------------------
// <copyright file="GenerateCompileWithObjectPath.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build task to generate metadata on the for compile output objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task generates metadata on the for compile output objects.
    /// </summary>
    public class GenerateCompileWithObjectPath : Task
    {
        /// <summary>
        /// The list of files to generate outputs for.
        /// </summary>
        [Required]
        public ITaskItem[] Compile
        {
            get;
            set;
        }

        /// <summary>
        /// The list of files with ObjectPath metadata.
        /// </summary>
        [Output]
        public ITaskItem[] CompileWithObjectPath
        {
            get;
            private set;
        }

        /// <summary>
        /// The folder under which all ObjectPaths should reside.
        /// </summary>
        [Required]
        public string IntermediateOutputPath
        {
            get;
            set;
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public static string GenerateIdentifier(string prefix, params string[] args)
        {
            string stringData = String.Join("|", args);
            byte[] data = Encoding.Unicode.GetBytes(stringData);

            // hash the data
            byte[] hash;

            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                hash = md5.ComputeHash(data);
            }

            // build up the identifier
            StringBuilder identifier = new StringBuilder(35, 35);
            identifier.Append(prefix);

            // hard coded to 16 as that is the most bytes that can be used to meet the length requirements. SHA1 is 20 bytes.
            for (int i = 0; i < 16; i++)
            {
                identifier.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
            }

            return identifier.ToString();
        }

        /// <summary>
        /// Gets the full path of the directory in which the file is found.
        /// </summary>
        /// <param name='file'>The file from which to extract the directory.</param>
        /// <returns>The generated identifier.</returns>
        private static string GetDirectory(ITaskItem file)
        {
            return file.GetMetadata("RootDir") + file.GetMetadata("Directory");
        }

        /// <summary>
        /// Sets the object path to use for the file.
        /// </summary>
        /// <param name='file'>The file on which to set the ObjectPath metadata.</param>
        /// <remarks>
        /// For the same input path it will return the same ObjectPath. Case is not ignored, however that isn't a problem.
        /// </remarks>
        private void SetObjectPath(ITaskItem file)
        {
            // If the source file is in the project directory or in the intermediate directory, use the intermediate directory.
            if (string.IsNullOrEmpty(file.GetMetadata("RelativeDir")) || string.Compare(file.GetMetadata("RelativeDir"), this.IntermediateOutputPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                file.SetMetadata("ObjectPath", this.IntermediateOutputPath);
            }
            // Otherwise use a subdirectory of the intermediate directory. The subfolder's name is based on the full path of the folder containing the source file.
            else
            {
                file.SetMetadata("ObjectPath", Path.Combine(this.IntermediateOutputPath, GenerateIdentifier("pth", GetDirectory(file))) + Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// Gets a complete list of external cabs referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            if (string.IsNullOrEmpty(this.IntermediateOutputPath))
            {
                this.Log.LogError("IntermediateOutputPath parameter is required and cannot be empty");
                return false;
            }

            if (this.Compile == null || this.Compile.Length == 0)
            {
                return true;
            }

            this.CompileWithObjectPath = new ITaskItem[this.Compile.Length];
            for (int i = 0; i < this.Compile.Length; ++i)
            {
                this.CompileWithObjectPath[i] = new TaskItem(this.Compile[i].ItemSpec, this.Compile[i].CloneCustomMetadata());

                // Do not overwrite the ObjectPath metadata if it already was set.
                if (string.IsNullOrEmpty(this.CompileWithObjectPath[i].GetMetadata("ObjectPath")))
                {
                    SetObjectPath(this.CompileWithObjectPath[i]);
                }
            }

            return true;
        }
    }
}
