// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WixTest.Utilities;

    /// <summary>
    /// Provides methods for building a Bundle.
    /// </summary>
    public class BundleBuilder : BuilderBase<BundleBuilder>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BundleBuilder"/> class.
        /// </summary>
        /// <param name="testName">The name of the test.</param>
        /// <param name="name">The name of the test bundle to build. The default is the <paramref name="testName"/>.</param>
        /// <param name="dataDirectory">The root directory in which test source can be found.</param>
        /// <param name="testArtifacts">Optional list of files and directories created by the test case.</param>
        public BundleBuilder(string testName, string name, string dataDirectory, List<FileSystemInfo> testArtifacts = null)
            : base(testName, name, dataDirectory, testArtifacts)
        {
        }

        /// <summary>
        /// Gets or sets whether or not to suppress patch sequence data.
        /// </summary>
        public bool SuppressPatchSequenceData { get; set; }

        /// <summary>
        /// Builds the package.
        /// </summary>
        /// <returns>The path to the built MSI package.</returns>
        protected override BundleBuilder BuildItem()
        {
            // Create paths.
            string rootDirectory = FileUtilities.GetUniqueFileName();
            string objDirectory = Path.Combine(rootDirectory, Settings.WixobjFolder);
            string exeDirectory = Path.Combine(rootDirectory, "Bundles");
            string bundle = Path.Combine(exeDirectory, String.Concat(this.Name, ".exe"));

            // Add the root directory to be cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(rootDirectory));

            // Compile.
            Candle candle = new Candle();
            candle.Extensions.AddRange(this.Extensions);
            candle.OtherArguments = String.Concat("-dTestName=", this.TestName);
            this.PreprocessorVariables.ToList().ForEach(kv => candle.OtherArguments = String.Concat(candle.OtherArguments, " -d", kv.Key, "=", kv.Value));
            candle.OutputFile = String.Concat(objDirectory, @"\");
            candle.SourceFiles.Add(this.SourceFile);
            candle.SourceFiles.AddRange(this.AdditionalSourceFiles);
            candle.WorkingDirectory = this.TestDataDirectory;
            candle.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(objDirectory));

            // Link.
            Light light = new Light();
            light.Extensions.AddRange(this.Extensions);
            light.OtherArguments = String.Format("-b build={0} -b data={1}", Settings.WixBuildDirectory, Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\"));
            this.BindPaths.ToList().ForEach(kv => light.OtherArguments = String.Concat(light.OtherArguments, " -b ", kv.Key, "=", kv.Value));
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.OutputFile = bundle;
            light.SuppressPatchSequenceData = this.SuppressPatchSequenceData;
            light.SuppressMSIAndMSMValidation = true;
            light.WorkingDirectory = this.TestDataDirectory;
            light.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(exeDirectory));

            this.Output = light.OutputFile;
            return this;
        }

        /// <summary>
        /// Ensures the packages built previously are uninstalled.
        /// </summary>
        protected override void UninstallItem(BuiltItem item)
        {
            TestTool bundle = new TestTool(item.Path, null);
            StringBuilder sb = new StringBuilder();

            // Run silent uninstall.
            sb.Append("-quiet -uninstall -burn.ignoredependencies=ALL");

            // Generate the log file name.
            string logFile = String.Format("{0}_{1:yyyyMMddhhmmss}_Cleanup_{2}.log", item.TestName, DateTime.UtcNow, Path.GetFileNameWithoutExtension(item.Path));
            sb.AppendFormat(" -log {0}", Path.Combine(Path.GetTempPath(), logFile));

            bundle.Arguments = sb.ToString();
            bundle.Run(false);
        }
    }
}
