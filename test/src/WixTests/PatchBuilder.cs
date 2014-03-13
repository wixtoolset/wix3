//-----------------------------------------------------------------------
// <copyright file="PatchBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Provides methods for building a Patch (.MSP).
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using WixTest.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Provides methods for building a Patch.
    /// </summary>
    class PatchBuilder : BuilderBase<PatchBuilder>
    {
        public PatchBuilder(WixTests test, string name)
            : base(test, name)
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
            string source = String.IsNullOrEmpty(this.SourceFile) ? Path.Combine(this.test.TestDataDirectory2, String.Concat(this.Name, ".wxs")) : this.SourceFile;
            string rootDirectory = FileUtilities.GetUniqueFileName();
            string objDirectory = Path.Combine(rootDirectory, Settings.WixobjFolder);
            string msiDirectory = Path.Combine(rootDirectory, Settings.MSPFolder);
            string wixmsp = Path.Combine(objDirectory, String.Concat(this.Name, ".wixmsp"));
            string package = Path.Combine(msiDirectory, String.Concat(this.Name, ".msp"));

            // Add the root directory to be cleaned up.
            this.test.TestArtifacts.Add(new DirectoryInfo(rootDirectory));

            // Compile.
            Candle candle = new Candle();
            candle.Extensions.AddRange(this.Extensions);
            candle.OtherArguments = String.Concat("-dTestName=", this.test.TestContext.TestName);
            this.PreprocessorVariables.ToList().ForEach(kv => candle.OtherArguments = String.Concat(candle.OtherArguments, " -d", kv.Key, "=", kv.Value));
            candle.OutputFile = String.Concat(objDirectory, @"\");
            candle.SourceFiles.Add(source);
            candle.WorkingDirectory = this.test.TestDataDirectory2;
            candle.Run();

            // Make sure the output directory is cleaned up.
            this.test.TestArtifacts.Add(new DirectoryInfo(objDirectory));

            // Link.
            Light light = new Light();
            light.Extensions.AddRange(this.Extensions);
            light.OtherArguments = String.Concat("-b data=", Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\"));
            this.BindPaths.ToList().ForEach(kv => light.OtherArguments = String.Concat(light.OtherArguments, " -b ", kv.Key, "=", kv.Value));
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.OutputFile = wixmsp;
            light.SuppressMSIAndMSMValidation = true;
            light.WorkingDirectory = this.test.TestDataDirectory2;
            light.Run();

            // Make sure the output directory is cleaned up.
            this.test.TestArtifacts.Add(new DirectoryInfo(msiDirectory));

            // Pyro.
            Pyro pyro = new Pyro();

            Assert.IsNotNull(this.TargetPaths);
            Assert.IsNotNull(this.UpgradePaths);
            Assert.AreEqual<int>(this.TargetPaths.Length, this.UpgradePaths.Length);

            for (int i = 0; i < this.TargetPaths.Length; ++i)
            {
                // Torch.
                Torch torch = new Torch();
                torch.TargetInput = Path.ChangeExtension(this.TargetPaths[i], "wixpdb");
                torch.UpdatedInput = Path.ChangeExtension(this.UpgradePaths[i], "wixpdb");
                torch.PreserveUnmodified = true;
                torch.XmlInput = true;
                torch.OutputFile = Path.Combine(objDirectory, String.Concat(Path.GetRandomFileName(), ".wixmst"));
                torch.WorkingDirectory = this.test.TestDataDirectory2;
                torch.Run();

                pyro.Baselines.Add(torch.OutputFile, this.Name);
            }

            pyro.InputFile = light.OutputFile;
            pyro.OutputFile = package;
            pyro.WorkingDirectory = this.test.TestDataDirectory2;
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
