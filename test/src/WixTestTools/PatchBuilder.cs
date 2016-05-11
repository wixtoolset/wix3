// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixTest.Utilities;
    using Xunit;

    /// <summary>
    /// Provides methods for building an MSP.
    /// </summary>
    public class PatchBuilder : BuilderBase<PatchBuilder>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PatchBuilder"/> class.
        /// </summary>
        /// <param name="testName">The name of the test.</param>
        /// <param name="name">The name of the test patch to build. The default is the <paramref name="testName"/>.</param>
        /// <param name="dataDirectory">The root directory in which test source can be found.</param>
        /// <param name="testArtifacts">Optional list of files and directories created by the test case.</param>
        public PatchBuilder(string testName, string name, string dataDirectory, List<FileSystemInfo> testArtifacts = null)
            : base(testName, name, dataDirectory, testArtifacts)
        {
        }

        /// <summary>
        /// Gets and sets the path to the target MSI.
        /// </summary>
        public string TargetPath
        {
            get
            {
                if (this.TargetPaths != null)
                {
                    return this.TargetPaths[0];
                }
                else
                {
                    return null;
                }
            }

            set
            {
                this.TargetPaths = new string[] { value };
            }
        }

        /// <summary>
        /// Gets and sets the path to the upgrade MSI.
        /// </summary>
        public string UpgradePath
        {
            get
            {
                if (this.UpgradePaths != null)
                {
                    return this.UpgradePaths[0];
                }
                else
                {
                    return null;
                }
            }

            set
            {
                this.UpgradePaths = new string[] { value };
            }
        }

        /// <summary>
        /// Gets and sets the paths to the target MSIs.
        /// </summary>
        /// <remarks>
        /// The order of target paths correlates to the order of upgrade paths.
        /// </remarks>
        public string[] TargetPaths { get; set; }

        /// <summary>
        /// Gets and sets the paths to the upgrade MSIs.
        /// </summary>
        /// <remarks>
        /// The order of upgrade paths correlates to the order of target paths.
        /// </remarks>
        public string[] UpgradePaths { get; set; }

        /// <summary>
        /// Builds a patch using given paths for the target and upgrade packages.
        /// </summary>
        /// <returns>The path to the patch.</returns>
        protected override PatchBuilder BuildItem()
        {
            // Create paths.
            string source = String.IsNullOrEmpty(this.SourceFile) ? Path.Combine(this.TestDataDirectory, String.Concat(this.Name, ".wxs")) : this.SourceFile;
            string rootDirectory = FileUtilities.GetUniqueFileName();
            string objDirectory = Path.Combine(rootDirectory, Settings.WixobjFolder);
            string msiDirectory = Path.Combine(rootDirectory, Settings.MspFolder);
            string wixmsp = Path.Combine(objDirectory, String.Concat(this.Name, ".wixmsp"));
            string package = Path.Combine(msiDirectory, String.Concat(this.Name, ".msp"));

            // Add the root directory to be cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(rootDirectory));

            // Compile.
            Candle candle = new Candle();
            candle.Extensions.AddRange(this.Extensions);
            candle.OtherArguments = String.Concat("-dTestName=", this.TestName);
            this.PreprocessorVariables.ToList().ForEach(kv => candle.OtherArguments = String.Concat(candle.OtherArguments, " -d", kv.Key, "=", kv.Value));
            candle.OutputFile = String.Concat(objDirectory, @"\");
            candle.SourceFiles.Add(source);
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
            light.OutputFile = wixmsp;
            light.SuppressMSIAndMSMValidation = true;
            light.WorkingDirectory = this.TestDataDirectory;
            light.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(msiDirectory));

            // Pyro.
            Pyro pyro = new Pyro();

            Assert.NotNull(this.TargetPaths);
            Assert.NotNull(this.UpgradePaths);
            Assert.Equal<int>(this.TargetPaths.Length, this.UpgradePaths.Length);

            for (int i = 0; i < this.TargetPaths.Length; ++i)
            {
                // Torch.
                Torch torch = new Torch();
                torch.TargetInput = Path.ChangeExtension(this.TargetPaths[i], "wixpdb");
                torch.UpdatedInput = Path.ChangeExtension(this.UpgradePaths[i], "wixpdb");
                torch.PreserveUnmodified = true;
                torch.XmlInput = true;
                torch.OutputFile = Path.Combine(objDirectory, String.Concat(Path.GetRandomFileName(), ".wixmst"));
                torch.WorkingDirectory = this.TestDataDirectory;
                torch.Run();

                pyro.Baselines.Add(torch.OutputFile, this.Name);
            }

            pyro.InputFile = light.OutputFile;
            pyro.OutputFile = package;
            pyro.WorkingDirectory = this.TestDataDirectory;
            pyro.SuppressWarnings.Add("1079");
            pyro.Run();

            this.Output = pyro.OutputFile;
            return this;
        }

        /// <summary>
        /// Patches are uninstalled by the MSIs they target.
        /// </summary>
        protected override void UninstallItem(BuiltItem item)
        {
            // Nothing to do for patches.
        }
    }
}
