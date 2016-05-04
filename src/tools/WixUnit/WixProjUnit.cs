// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Xml;

    /// <summary>
    /// The Windows Installer XML Wixproj unit tester (MSBuild targets).
    /// </summary>
    internal sealed class WixProjUnit
    {
        private static Mutex mutex = new Mutex(false, "WixProjUnit");

        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private WixProjUnit()
        {
        }

        /// <summary>
        /// Run a Wixproj unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="verbose">The level of verbosity for the MSBuild logging.</param>
        /// <param name="skipValidation">Tells light to skip validation.</param>
        /// <param name="update">Indicates whether to give the user the option to fix a failing test.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, bool verbose, bool skipValidation, bool update, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedResult = element.GetAttribute("ExpectedResult");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string extensions = element.GetAttribute("Extensions");
            bool noOutputName = XmlConvert.ToBoolean(element.GetAttribute("NoOutputName"));
            bool noOutputPath = XmlConvert.ToBoolean(element.GetAttribute("NoOutputPath"));
            bool defineSolutionProperties = XmlConvert.ToBoolean(element.GetAttribute("DefineSolutionProperties"));
            string suppressExtensions = element.GetAttribute("SuppressExtensions");
            string tempDirectory = element.GetAttribute("TempDirectory");
            string testName = element.ParentNode.Attributes["Name"].Value;
            string toolsDirectory = element.GetAttribute("ToolsDirectory");
            string msBuildDirectory = Environment.GetEnvironmentVariable("WixTestMSBuildDirectory");
            string msBuildToolsVersion = Environment.GetEnvironmentVariable("WixTestMSBuildToolsVersion");

            // check the combinations of attributes
            if (expectedErrors.Length > 0 && expectedResult.Length > 0)
            {
                throw new WixException(WixErrors.IllegalAttributeWithOtherAttribute(null, element.Name, "ExpectedErrors", "ExpectedResult"));
            }

            // we'll run MSBuild on the .wixproj to generate the output
            if (null == msBuildDirectory)
            {
                msBuildDirectory = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Microsoft.NET\Framework\v3.5");
            }

            string toolFile = Path.Combine(msBuildDirectory, "MSBuild.exe");
            StringBuilder commandLine = new StringBuilder(arguments);

            // rebuild by default
            commandLine.AppendFormat(" /target:Rebuild /verbosity:{0}", verbose ? "detailed" : "normal");

            if (skipValidation)
            {
                commandLine.Append(" /property:SuppressValidation=true");
            }

            // add DefineSolutionProperties
            commandLine.AppendFormat(" /property:DefineSolutionProperties={0}", defineSolutionProperties);

            // make sure the tools directory ends in a single backslash
            if (toolsDirectory[toolsDirectory.Length - 1] != Path.DirectorySeparatorChar)
            {
                toolsDirectory = String.Concat(toolsDirectory, Path.DirectorySeparatorChar);
            }

            // handle the wix-specific directories and files
            commandLine.AppendFormat(" /property:WixToolPath=\"{0}\\\"", toolsDirectory);
            commandLine.AppendFormat(" /property:WixExtDir=\"{0}\\\"", toolsDirectory);
            commandLine.AppendFormat(" /property:WixTargetsPath=\"{0}\"", Path.Combine(toolsDirectory, "wix.targets"));
            commandLine.AppendFormat(" /property:WixTasksPath=\"{0}\"", Path.Combine(toolsDirectory, "WixTasks.dll"));
            commandLine.AppendFormat(" /property:BaseIntermediateOutputPath=\"{0}\\\\\"", Path.Combine(tempDirectory, "obj"));

            // handle extensions
            string[] suppressedExtensionArray = suppressExtensions.Split(';');
            StringBuilder extensionsToUse = new StringBuilder();
            foreach (string extension in extensions.Split(';'))
            {
                if (0 > Array.BinarySearch(suppressedExtensionArray, extension))
                {
                    if (extensionsToUse.Length > 0)
                    {
                        extensionsToUse.Append(";");
                    }
                    extensionsToUse.Append(extension);
                }
            }
            commandLine.AppendFormat(" /property:WixExtension=\"{0}\"", extensionsToUse.ToString());

            previousUnitResults.OutputFiles.Clear();

            // handle the source file
            string sourceFile = element.GetAttribute("SourceFile").Trim();

            // handle the expected result output file
            string outputFile;
            if (expectedResult.Length > 0)
            {
                outputFile = Path.Combine(tempDirectory, Path.GetFileName(expectedResult));
            }
            else
            {
                outputFile = Path.Combine(tempDirectory, "ShouldNotBeCreated.msi");
            }

            if (!noOutputName)
            {
                commandLine.AppendFormat(" /property:OutputName=\"{0}\"", Path.GetFileNameWithoutExtension(outputFile));
            }

            if (!noOutputPath)
            {
                commandLine.AppendFormat(" /property:OutputPath=\"{0}\\\\\"", Path.GetDirectoryName(outputFile));
            }
            previousUnitResults.OutputFiles.Add(outputFile);

            if (!String.IsNullOrEmpty(msBuildToolsVersion))
            {
                commandLine.AppendFormat(" /tv:{0}", msBuildToolsVersion);
            }

            // add the source file as the last parameter
            commandLine.AppendFormat(" \"{0}\"", sourceFile);

            // run one msbuild process at a time due to multiproc issues
            ArrayList output = null;
            try
            {
                mutex.WaitOne();
                output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);

            // check the output file
            if (previousUnitResults.Errors.Count == 0)
            {
                if (expectedResult.Length > 0)
                {
                    ArrayList differences = CompareUnit.CompareResults(expectedResult, outputFile, testName, update);

                    previousUnitResults.Errors.AddRange(differences);
                    previousUnitResults.Output.AddRange(differences);
                }
                else if (expectedErrors.Length > 0 && File.Exists(outputFile)) // ensure the output doesn't exist
                {
                    string error = String.Format(CultureInfo.InvariantCulture, "Expected failure, but the unit test created output file \"{0}\".", outputFile);

                    previousUnitResults.Errors.Add(error);
                    previousUnitResults.Output.Add(error);
                }
            }
        }
    }
}
