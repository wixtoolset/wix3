// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Builder for setup.exe.
    /// </summary>
    public sealed class SetupExeBuilder
    {
        private FabricatorCore core;

        private string setupExePath;
        private string msiPath;

        /// <summary>
        /// Creates a new SetupExeBuilder object.
        /// </summary>
        /// <param name="core">Core build object for message handling.</param>
        public SetupExeBuilder(FabricatorCore core)
        {
            this.core = core;
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.setupExePath = Path.Combine(assemblyPath, "setup.exe");
        }

        /// <summary>
        /// Gets and sets the path to the MSI that is embedded in the setup.exe.
        /// </summary>
        /// <value>Path to MSI file.</value>
        public string MsiPath
        {
            get { return this.msiPath; }
            set { this.msiPath = value; }
        }

        /// <summary>
        /// Extracts the embedded package from this package.
        /// </summary>
        /// <param name="filePath">Path to this setup.exe to extract from.</param>
        /// <returns>Path to extracted embedded package.</returns>
        public static string GetEmbeddedPackage(string filePath)
        {
            string tempFileName = null;
            string extractPackagePath = null;

            try
            {
                // if the extension on the file path is ".exe" try to extract the MSI out of it
                tempFileName = Path.GetTempFileName();

                Process process = new Process();
                process.StartInfo.FileName = filePath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.Arguments = String.Concat("-o1 \"", tempFileName, "\"");

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new ApplicationException(String.Concat("Failed to extract MSI from ", process.StartInfo.FileName));
                }

                extractPackagePath = tempFileName; // the previous package path is now at the temp filename location
                tempFileName = null;
            }
            finally
            {
                if (tempFileName != null)
                {
                    File.Delete(tempFileName);
                }
            }

            return extractPackagePath;
        }

        /// <summary>
        /// Creates the setup.exe with embedded MSI.
        /// </summary>
        /// <param name="outputFile">Output path for setup.exe</param>
        /// <returns>True if build was successful, false if something went wrong.</returns>
        public bool Build(string outputFile)
        {
            if (null == this.msiPath)
            {
                throw new ArgumentNullException("MsiPath");
            }

            if (null == outputFile)
            {
                throw new ArgumentNullException("outputFile");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            int hr = NativeMethods.CreateSimpleSetup(this.setupExePath, this.msiPath, outputFile);
            if (hr != 0)
            {
                throw new ApplicationException(String.Format("Failed create setup.exe to: {0} from: {1}", outputFile, this.setupExePath));
            }

            return true;
        }
    }
}
