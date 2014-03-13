//-------------------------------------------------------------------------------------------------
// <copyright file="CandleUnit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Candle unit tester.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The Windows Installer XML Candle unit tester.
    /// </summary>
    internal sealed class CandleUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private CandleUnit()
        {
        }

        /// <summary>
        /// Run a Candle unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string extensions = element.GetAttribute("Extensions");
            string outputDirectory = element.GetAttribute("OutputDirectory");
            bool suppressWixCop = ("true" == element.GetAttribute("SuppressWixCop"));
            string tempDirectory = element.GetAttribute("TempDirectory");
            string toolsDirectory = element.GetAttribute("ToolsDirectory");
            bool usePreviousOutput = ("true" == element.GetAttribute("UsePreviousOutput"));

            string toolFile = Path.Combine(toolsDirectory, "candle.exe");
            StringBuilder commandLine = new StringBuilder(arguments);
            StringBuilder wixCopCommandLine = new StringBuilder("-set1");
            wixCopCommandLine.Append(Path.Combine(toolsDirectory, "WixCop.settings.xml"));

            // handle extensions
            if (!String.IsNullOrEmpty(extensions))
            {
                foreach (string extension in extensions.Split(';'))
                {
                    commandLine.AppendFormat(" -ext \"{0}\"", extension);
                }
            }

            // If the output directory is not set, set it to the temp directory.
            if (String.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = tempDirectory;
            }
            commandLine.AppendFormat(" -out \"{0}\\\\\"", outputDirectory);

            // handle any previous outputs
            if (usePreviousOutput)
            {
                ArrayList previousWixobjFiles = new ArrayList();
                foreach (string sourceFile in previousUnitResults.OutputFiles)
                {
                    string wixobjFile = String.Concat(Path.GetFileNameWithoutExtension(sourceFile), ".wixobj");

                    commandLine.AppendFormat(" \"{0}\"", sourceFile);
                    wixCopCommandLine.AppendFormat(" \"{0}\"", sourceFile);

                    previousWixobjFiles.Add(Path.Combine(tempDirectory, wixobjFile));
                }
                previousUnitResults.OutputFiles.Clear();
                previousUnitResults.OutputFiles.AddRange(previousWixobjFiles);
            }
            else
            {
                previousUnitResults.OutputFiles.Clear();
            }

            // handle child elements
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    switch (node.LocalName)
                    {
                        case "SourceFile":
                            string sourceFile = Environment.ExpandEnvironmentVariables(node.InnerText.Trim());
                            string wixobjFile = String.Concat(Path.GetFileNameWithoutExtension(sourceFile), ".wixobj");

                            commandLine.AppendFormat(" \"{0}\"", sourceFile);
                            wixCopCommandLine.AppendFormat(" \"{0}\"", sourceFile);

                            previousUnitResults.OutputFiles.Add(Path.Combine(outputDirectory, wixobjFile));
                            break;
                    }
                }
            }

            // run WixCop if it hasn't been suppressed
            if (!suppressWixCop)
            {
                string wixCopFile = Path.Combine(toolsDirectory, "wixcop.exe");

                ArrayList wixCopOutput = ToolUtility.RunTool(wixCopFile, wixCopCommandLine.ToString());
                previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(wixCopOutput, String.Empty, String.Empty));
                previousUnitResults.Output.AddRange(wixCopOutput);
            }

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);
        }
    }
}
