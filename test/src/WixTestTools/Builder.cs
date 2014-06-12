//-----------------------------------------------------------------------
// <copyright file="Builder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Provides methods for building MSI, MSM, MSP, MST, Wixout
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixTest.Utilities;

    /// <summary>
    /// Provides methods for building MSI, MSM, MSP, MST, Wixout
    /// </summary>
    public static class Builder
    {
        /// <summary>
        /// Name of the Bundle AttachedContainer output folder
        /// </summary>
        public static string AttachedContainerFolderName = "AttachedContainer";
        
        /// <summary>
        /// Name of the Bundle UX container output folder
        /// </summary>
        public static string UXContainerFolderName = "UX";

        /// <summary>
        /// Name of the Burn Manifest file 
        /// </summary>
        public static string BurnManifestFileName = Path.Combine(Builder.UXContainerFolderName, "manifest.xml");

        /// <summary>
        /// Name of the Burn UX Manifest file
        /// </summary>
        public static string UXManifestFileName = Path.Combine(Builder.UXContainerFolderName, "UxManifest.xml");
        

        /// <summary>
        /// Build bootstrapper packages for bundle input
        /// </summary>
        /// <param name="sourceFile">The .wxs files to compile</param>
        /// <returns>The path to the output .exe file</returns>
        public static string BuildBundlePackage(string sourceFile)
        {
            return BuildBundlePackage(Environment.CurrentDirectory, sourceFile);
        }

        /// <summary>
        /// Build bootstrapper packages for bundle input
        /// </summary>
        /// <param name="outputDirectory">The output directory for Candle and Light</param>
        /// <param name="sourceFile">The .wxs files to compile</param>
        /// <returns>The path to the output .exe file</returns>
        public static string BuildBundlePackage(string outputDirectory, string sourceFile)
        {
            return BuildBundlePackage(outputDirectory, sourceFile, null);
        }

        /// <summary>
        /// Build bootstrapper packages for bundle input
        /// </summary>
        /// <param name="outputDirectory">The output directory for Candle and Light</param>
        /// <param name="sourceFile">The .wxs files to compile</param>
        /// <returns>The path to the output .exe file</returns>
        public static string BuildBundlePackage(string outputDirectory, string sourceFile, string[] wixExtensions)
        {
            return BuildBundlePackage(outputDirectory, sourceFile, wixExtensions, string.Empty, string.Empty, true);
        }

        /// <summary>
        /// Build bootstrapper packages for bundle input
        /// </summary>
        /// <param name="outputDirectory">The output directory for Candle and Light</param>
        /// <param name="sourceFile">The .wxs files to compile</param>
        /// <param name="extractEmbeddedResourcesToOutputDirectory">If true, extract embeded resources and copy them to output directory</param>
        /// <returns>The path to the output .exe file</returns>
        public static string BuildBundlePackage(string outputDirectory, string sourceFile, string[] wixExtensions, string otherCandleArguments, string otherLightArguments, bool extractEmbeddedResourcesToOutputDirectory)
        {
            string outputFileName = String.Concat(Path.GetFileNameWithoutExtension(sourceFile), ".exe");
            otherLightArguments = string.IsNullOrEmpty(otherLightArguments) ? " -notidy " : otherLightArguments + " -notidy ";

            string extensionArguments = string.Empty;
            if (null != wixExtensions)
            {
                foreach (string wixExtension in wixExtensions)
                {
                    extensionArguments += string.Format(" -ext {0} ", wixExtension);
                }
            }
            otherCandleArguments += extensionArguments;
            otherLightArguments += extensionArguments;

            // build package
            string bootstrapper = Builder.BuildPackage(outputDirectory, sourceFile, outputFileName, otherCandleArguments, otherLightArguments);

            // copy all the resulting files to the output directory
            if (extractEmbeddedResourcesToOutputDirectory)
            {
                if (!File.Exists(bootstrapper))
                {
                    throw new ApplicationException(string.Format("Bootrapper file '{0}' was not created as expected.", bootstrapper));
                }

                // call dark.exe to extract the package contents to the output folder
                Dark dark = new Dark(outputDirectory);
                dark.InputFile = bootstrapper;
                dark.BinaryPath = outputDirectory;
                dark.Run();
            }

            return bootstrapper;
        }

        /// <summary>
        /// Build a setup package from the specified sources
        /// </summary>
        /// <param name="workingDirectory">The working directory from where Candle and Light are run</param>
        /// <param name="sourceFiles">The .wxs files to compile</param>
        /// <param name="outputFile">The name of the output file</param>
        /// <param name="outputToTemp">Save the output to temp</param>
        /// <param name="otherCandleArgs">Additional arguments to pass to Candle.exe</param>
        /// <param name="otherLightArgs">Additional arguments to pass to Light.exe</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string workingDirectory, string[] sourceFiles, string outputFile, bool outputToTemp, string otherCandleArgs, string otherLightArgs)
        {
            // Determine where to save the output
            string outputDirectory = Environment.CurrentDirectory;

            if (outputToTemp && Path.IsPathRooted(outputFile))
            {
                outputDirectory = FileUtilities.GetUniqueFileName();
            }

            // Create a directory for intermediate wixobj output
            string wixobjDirectory = Path.Combine(outputDirectory, Settings.WixobjFolder);
            FileUtilities.CreateOutputDirectory(wixobjDirectory);

            // Compile
            Candle candle = new Candle();
            candle.WorkingDirectory = workingDirectory;
            candle.SourceFiles = new List<string>(sourceFiles);
            candle.OutputFile = String.Concat(wixobjDirectory, @"\");
            candle.OtherArguments = otherCandleArgs;
            candle.Run();

            // Link
            Light light = new Light();
            light.WorkingDirectory = workingDirectory;
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.OutputFile = Path.Combine(outputDirectory, outputFile);
            FileUtilities.CreateOutputDirectory(Path.GetDirectoryName(light.OutputFile));
            light.OutputFile = Path.Combine(outputDirectory, outputFile);
            light.OtherArguments = otherLightArgs;
            light.Run();

            return light.OutputFile;
        }

        /// <summary>
        /// Build a setup package from the specified sources
        /// </summary>
        /// <param name="workingDirectory">The working directory from where Candle and Light are run</param>
        /// <param name="sourceFiles">The .wxs files to compile</param>
        /// <param name="outputFile">The name of the output file</param>
        /// <param name="otherCandleArgs">Additional arguments to pass to Candle.exe</param>
        /// <param name="otherLightArgs">Additional arguments to pass to Light.exe</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string workingDirectory, string[] sourceFiles, string outputFile, string otherCandleArgs, string otherLightArgs)
        {
            return Builder.BuildPackage(workingDirectory, sourceFiles, outputFile, true, otherCandleArgs, otherLightArgs);
        }

        /// <summary>
        /// Build a setup package from the specified sources
        /// </summary>
        /// <param name="workingDirectory">The working directory from where Candle and Light are run</param>
        /// <param name="sourceFile">The .wxs file to compile</param>
        /// <param name="outputFile">The name of the output file</param>
        /// <param name="otherCandleArgs">Additional arguments to pass to Candle.exe</param>
        /// <param name="otherLightArgs">Additional arguments to pass to Light.exe</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string workingDirectory, string sourceFile, string outputFile, string otherCandleArgs, string otherLightArgs)
        {
            return Builder.BuildPackage(workingDirectory, new string[] { sourceFile }, outputFile, otherCandleArgs, otherLightArgs);
        }

        /// <summary>
        /// Build a setup package (MSI) from the specified source file using the specified extension
        /// </summary>
        /// <param name="sourceFile">The .wxs file to compile</param>
        /// <param name="outputFile">The name of the output file</param>
        /// <param name="wixExtension">A WiX extension to use</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string sourceFile, string outputFile, string wixExtension)
        {
            return BuildPackage(sourceFile, outputFile, new string[] { wixExtension });
        }

        /// <summary>
        /// Build a setup package (MSI) from the specified source file using the specified extensions
        /// </summary>
        /// <param name="sourceFile">The .wxs file to compile</param>
        /// <param name="outputFile">The name of the output file</param>
        /// <param name="wixExtension">A list of WiX extensions to use</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string sourceFile, string outputFile, string[] wixExtensions)
        {
            string workingDirectory = Environment.CurrentDirectory;
            string otherArguments = string.Empty;
            foreach (string wixExtension in wixExtensions)
            {
                otherArguments += string.Format(" -ext {0} ", wixExtension);
            }

            return Builder.BuildPackage(workingDirectory, new string[] { sourceFile }, outputFile, true, otherArguments, otherArguments);
        }

        /// <summary>
        /// Build a setup package (MSI) from the specified source file
        /// </summary>
        /// <param name="sourceFile">The .wxs file to compile</param>
        /// <param name="outputFile">The name of the output file</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string sourceFile, string outputFile)
        {
            string workingDirectory = Environment.CurrentDirectory;

            return Builder.BuildPackage(workingDirectory, new string[] { sourceFile }, outputFile, true, null, null);
        }

        /// <summary>
        /// Build a setup package called test.msi from the specified source file
        /// </summary>
        /// <param name="sourceFile">The .wxs file to compile</param>
        /// <returns>The path to the output file</returns>
        public static string BuildPackage(string sourceFile)
        {
            string msi = String.Concat(Path.GetFileNameWithoutExtension(sourceFile), ".msi");
            return BuildPackage(sourceFile, msi);
        }

        /// <summary>
        /// Build a patch
        /// </summary>
        /// <param name="workingDirectory">WiX working directory</param>
        /// <param name="targetSources">Target source files</param>
        /// <param name="upgradeSources">Upgrade source files</param>
        /// <param name="patchSources">Patch source file</param>
        /// <param name="patchFileName">Name of output file</param>
        /// <param name="baseline">The name of the baseline</param>
        /// <param name="patchCompilerExtension">A Wix extension to pass when compiling the patch</param>
        public static void BuildPatch(string workingDirectory, string[] targetSources, string[] upgradeSources, string[] patchSources, string patchFileName, string baseline, string patchCompilerExtension)
        {
            string outputDirectory = Environment.CurrentDirectory;

            // Create directories for intermediate output
            string wixobjDirectory = Path.Combine(outputDirectory, Settings.WixobjFolder);
            FileUtilities.CreateOutputDirectory(wixobjDirectory);
            string wixoutDirectory = Path.Combine(outputDirectory, Settings.WixoutFolder);
            FileUtilities.CreateOutputDirectory(wixoutDirectory);
            string msiDirectory = Path.Combine(outputDirectory, Settings.MsiFolder);
            FileUtilities.CreateOutputDirectory(msiDirectory);
            string mstDirectory = Path.Combine(outputDirectory, Settings.MstFolder);
            FileUtilities.CreateOutputDirectory(mstDirectory);
            string mspDirectory = Path.Combine(outputDirectory, Settings.MspFolder);
            FileUtilities.CreateOutputDirectory(mspDirectory);

            // Build Target .wixout
            string targetWixout = Path.Combine(wixoutDirectory, "target.wixout");
            string targetMSI = Path.Combine(msiDirectory, @"target\target.msi");
            Builder.BuildPackage(workingDirectory, targetSources, targetWixout, null, "-xo");
            Builder.BuildPackage(workingDirectory, targetSources, targetMSI, null, null);

            // Build Upgrade .wixout
            string upgradeWixout = Path.Combine(wixoutDirectory, "upgrade.wixout");
            string upgradeMSI = Path.Combine(msiDirectory, @"upgrade\upgrade.msi");
            Builder.BuildPackage(workingDirectory, upgradeSources, upgradeWixout, null, "-xo");
            Builder.BuildPackage(workingDirectory, upgradeSources, upgradeMSI, null, null);

            // Build Transform
            string transformWixMST = Path.Combine(wixoutDirectory, "transform.wixmst");
            Builder.BuildTransform(workingDirectory, targetWixout, upgradeWixout, true, transformWixMST, true);

            // Build a transform database from diffing the msis
            string transformMST = Path.Combine(mstDirectory, "transform.mst");
            Builder.BuildTransform(workingDirectory, targetMSI, upgradeMSI, false, transformMST, false);

            string patchWixMSP = Path.Combine(wixoutDirectory, "patch.wixmsp");
            if (!String.IsNullOrEmpty(patchCompilerExtension))
            {
                string extensionArgument = String.Format(@"-ext ""{0}""", patchCompilerExtension);
                Builder.BuildPackage(workingDirectory, patchSources, patchWixMSP, extensionArgument, "-xo");
            }
            else
            {
                Builder.BuildPackage(workingDirectory, patchSources, patchWixMSP, null, "-xo");
            }

            Pyro pyro = new Pyro();
            pyro.WorkingDirectory = workingDirectory;
            pyro.InputFile = patchWixMSP;
            pyro.OutputFile = patchFileName;
            pyro.Baselines.Add(transformWixMST, baseline);
            pyro.ExpectedWixMessages.Add(new WixMessage(1079, WixMessage.MessageTypeEnum.Warning));
            pyro.Run();
        }

        /// <summary>
        /// Build a patch
        /// </summary>
        /// <param name="workingDirectory">WiX working directory</param>
        /// <param name="targetSources">Target source files</param>
        /// <param name="upgradeSources">Upgrade source files</param>
        /// <param name="patchSources">Patch source file</param>
        /// <param name="patchFileName">Name of output file</param>
        /// <param name="baseline">The name of the baseline</param>
        public static void BuildPatch(string workingDirectory, string[] targetSources, string[] upgradeSources, string[] patchSources, string patchFileName, string baseline)
        {
            Builder.BuildPatch(workingDirectory, targetSources, upgradeSources, patchSources, patchFileName, baseline, null);
        }

        /// <summary>
        /// Build a patch with one target source file and one update source file
        /// </summary>
        /// <param name="workingDirectory">WiX working directory</param>
        /// <param name="targetSource">Target source file</param>
        /// <param name="upgradeSource">Upgrade source file</param>
        /// <param name="patchSource">Patch source file</param>
        /// <param name="patchFileName">Name of output file</param>
        /// <param name="baseline">Id of the baseline layout</param>
        public static void BuildPatch(string workingDirectory, string targetSource, string upgradeSource, string patchSource, string patchFileName, string baseline)
        {
            Builder.BuildPatch(
                workingDirectory,
                new string[] { targetSource },
                new string[] { upgradeSource },
                new string[] { patchSource },
                patchFileName,
                baseline);
        }

        /// <summary>
        /// Build a patch from two admin images
        /// </summary>
        /// <param name="workingDirectory">WiX working directory</param>
        /// <param name="targetMsi">Target admin image</param>
        /// <param name="upgradeMsi">Upgrade admin image</param>
        /// <param name="patchSource">Patch source file</param>
        /// <param name="patchFileName">Name of output file</param>
        /// <param name="baseline">Id of the baseline layout</param>
        public static void BuildPatchFromAdminImages(string workingDirectory, string targetMsi, string upgradeMsi, string patchSource, string patchFileName, string baseline)
        {
            string outputDirectory = Environment.CurrentDirectory;

            // Create directories for intermediate output
            FileUtilities.CreateOutputDirectory(Path.Combine(outputDirectory, Settings.WixobjFolder));
            FileUtilities.CreateOutputDirectory(Path.Combine(outputDirectory, Settings.WixoutFolder));
            FileUtilities.CreateOutputDirectory(Path.Combine(outputDirectory, Settings.MstFolder));
            FileUtilities.CreateOutputDirectory(Path.Combine(outputDirectory, Settings.MspFolder));

            // Build Transforms
            // string transformWixMST = Path.Combine(Settings.WixoutFolder, "transform.wixmst");
            string transformWixMST = "transform.wixmst";
            Builder.BuildTransform(workingDirectory, targetMsi, upgradeMsi, false, transformWixMST, true, "-a");

            // string transformMST = Path.Combine(Settings.MSTFolder, "transform.mst");
            string transformMST = "transform.mst";
            Builder.BuildTransform(workingDirectory, targetMsi, upgradeMsi, false, transformMST, false, "-a");

            // Build Patch
            string patchWixMSP = Path.Combine(Settings.WixoutFolder, "patch.wixmsp");
            Builder.BuildPackage(workingDirectory, patchSource, patchWixMSP, null, "-xo");

            Pyro pyro = new Pyro();
            pyro.WorkingDirectory = workingDirectory;
            pyro.InputFile = patchWixMSP;
            pyro.OutputFile = patchFileName;
            pyro.Baselines.Add(transformWixMST, baseline);
            pyro.ExpectedWixMessages.Add(new WixMessage(1079, WixMessage.MessageTypeEnum.Warning));
            pyro.Run();
        }

        /// <summary>
        /// Builds a transform from two setup packages
        /// </summary>
        /// <param name="workingDirectory">WiX working directory</param>
        /// <param name="target">The target file</param>
        /// <param name="update">The updated file</param>
        /// <param name="inputXML">If true, the inputs to Torch are XML</param>
        /// <param name="transform">The name of the transform</param>
        /// <param name="outputXML">If true, create an MST in wixout format</param>
        public static void BuildTransform(string workingDirectory, string target, string update, bool inputXML, string transform, bool outputXML)
        {
            Builder.BuildTransform(workingDirectory, target, update, inputXML, transform, outputXML, null);
        }


        /// <summary>
        /// Builds a transform from two setup packages
        /// </summary>
        /// <param name="workingDirectory">WiX working directory</param>
        /// <param name="target">The target file</param>
        /// <param name="update">The updated file</param>
        /// <param name="inputXML">If true, the inputs to Torch are XML</param>
        /// <param name="transform">The name of the transform</param>
        /// <param name="outputXML">If true, create an MST in wixout format</param>
        /// <param name="otherTorchArguments">Additional arguments to pass to Torch</param>
        public static void BuildTransform(string workingDirectory, string target, string update, bool inputXML, string transform, bool outputXML, string otherTorchArguments)
        {
            // Build Transform
            Torch torch = new Torch();
            torch.WorkingDirectory = workingDirectory;
            torch.TargetInput = target;
            torch.UpdatedInput = update;
            torch.XmlInput = inputXML;
            torch.OutputFile = transform;
            torch.XmlOutput = outputXML;
            torch.PreserveUnmodified = true;
            torch.OtherArguments = otherTorchArguments;
            torch.Run();
        }
    }
}