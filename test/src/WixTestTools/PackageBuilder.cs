//-----------------------------------------------------------------------
// <copyright file="PackageBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixTest.Utilities;

    /// <summary>
    /// Provides methods for building an MSI.
    /// </summary>
    public class PackageBuilder: BuilderBase<PackageBuilder>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PackageBuilder"/> class.
        /// </summary>
        /// <param name="testName">The name of the test.</param>
        /// <param name="name">The name of the test package to build. The default is the <paramref name="testName"/>.</param>
        /// <param name="dataDirectory">The root directory in which test source can be found.</param>
        /// <param name="testArtifacts">Optional list of files and directories created by the test case.</param>
        public PackageBuilder(string testName, string name, string dataDirectory, List<FileSystemInfo> testArtifacts = null)
            : base(testName, name, dataDirectory, testArtifacts)
        {
        }

        /// <summary>
        /// Builds the package.
        /// </summary>
        /// <returns>The path to the built MSI package.</returns>
        protected override PackageBuilder BuildItem()
        {
            // Create paths.
            string source = String.IsNullOrEmpty(this.SourceFile) ? Path.Combine(this.TestDataDirectory, String.Concat(this.Name, ".wxs")) : this.SourceFile;
            string rootDirectory = FileUtilities.GetUniqueFileName();
            string objDirectory = Path.Combine(rootDirectory, Settings.WixobjFolder);
            string msiDirectory = Path.Combine(rootDirectory, Settings.MsiFolder);
            string package = Path.Combine(msiDirectory, String.Concat(this.Name, ".msi"));

            // Add the root directory to be cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(rootDirectory));

            // Compile.
            Candle candle = new Candle();
            candle.Extensions.AddRange(this.Extensions);
            candle.OtherArguments = String.Concat("-dTestName=", this.TestName);
            this.PreprocessorVariables.ToList().ForEach(kv => candle.OtherArguments = String.Concat(candle.OtherArguments, " -d", kv.Key, "=", kv.Value));
            candle.OutputFile = String.Concat(objDirectory, @"\");
            candle.SourceFiles.Add(source);
            candle.SourceFiles.AddRange(this.AdditionalSourceFiles);
            candle.WorkingDirectory = this.TestDataDirectory;
            candle.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(objDirectory));

            // Link.
            Light light = new Light();
            light.Extensions.AddRange(this.Extensions);
            light.OtherArguments = String.Concat("-b data=", Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\"));
            this.BindPaths.ToList().ForEach(kv => light.OtherArguments = String.Concat(light.OtherArguments, " -b ", kv.Key, "=", kv.Value));
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.OutputFile = package;
            light.SuppressMSIAndMSMValidation = true;
            light.WorkingDirectory = this.TestDataDirectory;
            light.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(msiDirectory));

            this.Output = light.OutputFile;
            return this;
        }

        /// <summary>
        /// Ensures the packages built previously are uninstalled.
        /// </summary>
        protected override void UninstallItem(BuiltItem item)
        {
            MSIExec exec = new MSIExec();
            exec.ExecutionMode = MSIExec.MSIExecMode.Uninstall;
            exec.OtherArguments = "IGNOREDEPENDENCIES=ALL";
            exec.Product = item.Path;

            // Generate the log file name.
            string logFile = String.Format("{0}_{1:yyyyMMddhhmmss}_Cleanup_{2}.log", item.TestName, DateTime.UtcNow, Path.GetFileNameWithoutExtension(item.Path));
            exec.LogFile = Path.Combine(Path.GetTempPath(), logFile);

            exec.Run(false);
        }
    }
}
