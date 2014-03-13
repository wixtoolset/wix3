//-------------------------------------------------------------------------------------------------
// <copyright file="PyroUnit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Pyro unit tester.
// </summary>
//-------------------------------------------------------------------------------------------------

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
    /// The Windows Installer XML Pyro unit tester.
    /// </summary>
    internal sealed class PyroUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private PyroUnit()
        {
        }

        /// <summary>
        /// Run a Pyro unit test.
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
            string inputFile = element.GetAttribute("InputFile");
            string outputFile = element.GetAttribute("OutputFile");
            string tempDirectory = element.GetAttribute("TempDirectory");
            string testName = element.ParentNode.Attributes["Name"].Value;
            string toolsDirectory = element.GetAttribute("ToolsDirectory");

            if (null == inputFile || String.Empty == inputFile)
            {
                throw new WixException(WixErrors.IllegalEmptyAttributeValue(null, element.Name, "InputFile"));
            }

            if (null == outputFile || String.Empty == outputFile)
            {
                throw new WixException(WixErrors.IllegalEmptyAttributeValue(null, element.Name, "OutputFile"));
            }

            // After Pyro is run, verify that this file was not created
            if (0 < expectedErrors.Length)
            {
                outputFile = Path.Combine(tempDirectory, "ShouldNotBeCreated.msp");
            }

            string toolFile = Path.Combine(toolsDirectory, "pyro.exe");
            StringBuilder commandLine = new StringBuilder(arguments);
            commandLine.AppendFormat(" \"{0}\" -out \"{1}\"", inputFile, outputFile);
            previousUnitResults.OutputFiles.Add(outputFile);

            // handle extensions
            if (!String.IsNullOrEmpty(extensions))
            {
                foreach (string extension in extensions.Split(';'))
                {
                    commandLine.AppendFormat(" -ext \"{0}\"", extension);
                }
            }

            // handle wixunit arguments
            if (args.NoTidy)
            {
                commandLine.Append(" -notidy");
            }

            // handle child elements
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    switch (node.LocalName)
                    {
                        case "Transform":
                            string transformFile = node.Attributes["File"].Value;
                            string baseline = node.Attributes["Baseline"].Value;
                            commandLine.AppendFormat(" -t {0} \"{1}\"", baseline, transformFile);
                            break;
                        default:
                            break;
                    }
                }
            }

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);

            // check the output file
            if (0 == previousUnitResults.Errors.Count)
            {
                if (0 < expectedResult.Length)
                {
                    ArrayList differences = CompareUnit.CompareResults(expectedResult, outputFile, testName, update);

                    previousUnitResults.Errors.AddRange(differences);
                    previousUnitResults.Output.AddRange(differences);
                }
                else if (0 < expectedErrors.Length && File.Exists(outputFile)) // ensure the output doesn't exist
                {
                    string error = String.Format(CultureInfo.InvariantCulture, "Expected failure, but the unit test created output file \"{0}\".", outputFile);

                    previousUnitResults.Errors.Add(error);
                    previousUnitResults.Output.Add(error);
                }
            }
        }

    }
}
