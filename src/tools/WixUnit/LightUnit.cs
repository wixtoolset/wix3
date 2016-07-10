// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The Windows Installer XML Light unit tester.
    /// </summary>
    internal sealed class LightUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private LightUnit()
        {
        }

        /// <summary>
        /// Run a Light unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="update">Indicates whether to give the user the option to fix a failing test.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, bool update, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedResult = element.GetAttribute("ExpectedResult");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string extensions = element.GetAttribute("Extensions");
            string outputFile = element.GetAttribute("OutputFile");
            string intermediateOutputType = element.GetAttribute("IntermediateOutputType");
            string suppressExtensions = element.GetAttribute("SuppressExtensions");
            string tempDirectory = element.GetAttribute("TempDirectory");
            string testName = element.ParentNode.Attributes["Name"].Value;
            string toolsDirectory = element.GetAttribute("ToolsDirectory");
            bool usePreviousOutput = ("true" == element.GetAttribute("UsePreviousOutput"));

            if (expectedErrors.Length == 0 && expectedResult.Length == 0 && intermediateOutputType.Length == 0 && outputFile.Length == 0)
            {
                throw new WixException(WixErrors.ExpectedAttributes(null, element.Name, "ExpectedErrors", "ExpectedResult", "IntermediateOutputType", "OutputFile", null));
            }

            if (expectedErrors.Length > 0 && (expectedResult.Length > 0 || intermediateOutputType.Length > 0 || outputFile.Length > 0))
            {
                throw new WixException(WixErrors.IllegalAttributeWithOtherAttributes(null, element.Name, "ExpectedErrors", "ExpectedResult", "IntermediateOutputType", "OutputFile", null));
            }
            else if (expectedResult.Length > 0 && (intermediateOutputType.Length > 0 || outputFile.Length > 0))
            {
                throw new WixException(WixErrors.IllegalAttributeWithOtherAttributes(null, element.Name, "ExpectedResult", "IntermediateOutputType", "OutputFile"));
            }
            else if (intermediateOutputType.Length > 0 && outputFile.Length > 0)
            {
                throw new WixException(WixErrors.IllegalAttributeWithOtherAttributes(null, element.Name, "IntermediateOutputType", "OutputFile", null));
            }

            string toolFile = Path.Combine(toolsDirectory, "light.exe");
            StringBuilder commandLine = new StringBuilder(arguments);
            commandLine.Append(" -b \"%WIX%\\examples\\data\"");

            // handle wixunit arguments
            if (args.NoTidy)
            {
                commandLine.Append(" -notidy");
            }

            // handle extensions
            if (!String.IsNullOrEmpty(extensions))
            {
                string[] suppressedExtensionArray = suppressExtensions.Split(';');
                foreach (string extension in extensions.Split(';'))
                {
                    if (0 > Array.BinarySearch(suppressedExtensionArray, extension))
                    {
                        commandLine.AppendFormat(" -ext \"{0}\"", extension);
                    }
                }
            }

            // handle any previous outputs
            if (usePreviousOutput)
            {
                foreach (string inputFile in previousUnitResults.OutputFiles)
                {
                    commandLine.AppendFormat(" \"{0}\"", inputFile);
                }
            }
            previousUnitResults.OutputFiles.Clear();

            // handle child elements
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    switch (node.LocalName)
                    {
                        case "LibraryFile":
                            string libraryFile = node.InnerText.Trim();

                            commandLine.AppendFormat(" \"{0}\"", libraryFile);
                            break;
                        case "LocalizationFile":
                            string localizationFile = node.InnerText.Trim();

                            commandLine.AppendFormat(" -loc \"{0}\"", localizationFile);
                            break;
                    }
                }
            }

            if (outputFile.Length > 0)
            {
                // outputFile has been explicitly set
            }
            else if (expectedResult.Length > 0)
            {
                outputFile = Path.Combine(tempDirectory, Path.GetFileName(expectedResult));
            }
            else if (intermediateOutputType.Length > 0)
            {
                string intermediateFile = String.Concat("intermediate.", intermediateOutputType);

                outputFile = Path.Combine(tempDirectory, intermediateFile);
            }
            else
            {
                outputFile = Path.Combine(tempDirectory, "ShouldNotBeCreated.msi");
            }
            commandLine.AppendFormat("{0} -out \"{1}\"", (Path.GetExtension(outputFile) == ".wixout" ? " -xo" : String.Empty), outputFile);
            previousUnitResults.OutputFiles.Add(outputFile);

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
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
