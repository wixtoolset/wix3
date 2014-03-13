//-------------------------------------------------------------------------------------------------
// <copyright file="InsigniaUnit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Insignia unit tester.
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
    /// The Windows Installer XML Insignia unit tester.
    /// </summary>
    internal sealed class InsigniaUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private InsigniaUnit()
        {
        }

        /// <summary>
        /// Run a Insignia unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string tempDirectory = element.GetAttribute("TempDirectory");
            string toolsDirectory = element.GetAttribute("ToolsDirectory");

            string toolFile = Path.Combine(toolsDirectory, "Insignia.exe");
            StringBuilder commandLine = new StringBuilder(arguments);

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
                        case "SourceFile":
                            string sourceFile = Environment.ExpandEnvironmentVariables(node.InnerText.Trim());
                            string outputFile = Path.Combine(tempDirectory, sourceFile);

                            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                            // If the file already exists, let's throw an error - otherwise one output file overwriting the other
                            // in the notidy case would be confusing
                            File.Copy(sourceFile, outputFile, false);

                            DirectoryInfo inputDirInfo = new DirectoryInfo(Path.GetDirectoryName(sourceFile));
                            FileInfo[] cabFiles = inputDirInfo.GetFiles("*.cab");
                            foreach (FileInfo cabFile in cabFiles)
                            {
                                File.Copy(cabFile.FullName, Path.Combine(Path.GetDirectoryName(outputFile), cabFile.Name), false);
                            }

                            // Remove any read-only attributes on the file
                            FileAttributes attributes = File.GetAttributes(outputFile);
                            attributes = (attributes & ~FileAttributes.ReadOnly);
                            File.SetAttributes(outputFile, attributes);

                            commandLine.AppendFormat(" -im \"{0}\"", outputFile);

                            previousUnitResults.OutputFiles.Add(outputFile);
                            break;
                    }
                }
            }

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);
        }
    }
}
