// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml.Msi;

    /// <summary>
    /// The Windows Installer XML Torch unit tester.
    /// </summary>
    internal class TorchUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private TorchUnit()
        {
        }

        /// <summary>
        /// Run a Torch unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="update">Indicates whether to give the user the option to fix a failing test.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, bool update, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string extensions = element.GetAttribute("Extensions");
            string outputFile = element.GetAttribute("OutputFile");
            string targetDatabase = element.GetAttribute("TargetDatabase");
            string tempDirectory = element.GetAttribute("TempDirectory");
            string testName = element.ParentNode.Attributes["Name"].Value;
            string toolsDirectory = element.GetAttribute("ToolsDirectory");
            string updatedDatabase = element.GetAttribute("UpdatedDatabase");
            bool usePreviousOutput = ("true" == element.GetAttribute("UsePreviousOutput"));
            bool verifyTransform = ("true" == element.GetAttribute("VerifyTransform"));

            string toolFile = Path.Combine(toolsDirectory, "torch.exe");
            StringBuilder commandLine = new StringBuilder(arguments);

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

            // handle any previous outputs
            if (0 < previousUnitResults.OutputFiles.Count && usePreviousOutput)
            {
                commandLine.AppendFormat(" \"{0}\"", previousUnitResults.OutputFiles[0]);
                previousUnitResults.OutputFiles.Clear();
            }
            else // diff database files to create transform
            {
                commandLine.AppendFormat(" \"{0}\" \"{1}\"", targetDatabase, updatedDatabase);
            }


            if (null == outputFile || String.Empty == outputFile)
            {
                outputFile = Path.Combine(tempDirectory, "transform.mst");
            }
            commandLine.AppendFormat(" -out \"{0}\"", outputFile);
            previousUnitResults.OutputFiles.Add(outputFile);

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);

            // check the results
            if (verifyTransform && 0 == expectedErrors.Length && 0 == previousUnitResults.Errors.Count)
            {
                string actualDatabase = Path.Combine(tempDirectory, String.Concat(Guid.NewGuid(), ".msi"));
                File.Copy(targetDatabase, actualDatabase);
                File.SetAttributes(actualDatabase, File.GetAttributes(actualDatabase) & ~FileAttributes.ReadOnly);
                using (Database database = new Database(actualDatabase, OpenDatabase.Direct))
                {
                    // use transform validation bits set in the transform (if any; defaults to None).
                    database.ApplyTransform(outputFile);
                    database.Commit();
                }

                // check the output file
                ArrayList differences = CompareUnit.CompareResults(updatedDatabase, actualDatabase, testName, update);
                previousUnitResults.Errors.AddRange(differences);
                previousUnitResults.Output.AddRange(differences);
            }
        }
    }
}
